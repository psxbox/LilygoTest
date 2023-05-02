using Android.OS;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LilyGoTestBLE.Data
{
    public enum OtaStatus
    {
        IDLE,
        READY_FOR_FLASH,
        FLASHING
    }

    public class BLEService
    {
        public List<IDevice> Devices { get; private set; }
        public ushort BatLevel { get; private set; }
        public string SensorLevel { get; private set; }
        public byte SigQua { get; private set; }
        public string FwVer { get; private set; }
        public string DeviceOtaStatus { get; private set; }
        public OtaStatus OtaStatus { get; private set; }
        public string OtaProgressStr { get; private set; }
        public int OtaProgress { get; private set; } = 0;

        public ConcurrentQueue<string> Logs { get; private set; } = new ConcurrentQueue<string>();

        private IBluetoothLE ble;
        private IAdapter adapter;
        private IService batService;
        private ICharacteristic batLevelChar;
        private IService customService;
        private ICharacteristic sigQuaChar;
        private ICharacteristic levelChar;
        private ICharacteristic logChar;
        private IService otaService;
        private ICharacteristic otaCmdChar = null;
        private ICharacteristic otaDataChar = null;

        public event Action OnValueChanged;

        public BLEService()
        {
            Devices = new List<IDevice>();

            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
            adapter.ScanMode = ScanMode.Balanced;
            adapter.ScanTimeout = 5000;
            adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;
            adapter.DeviceDisconnected += Adapter_DeviceDisconnected;
            adapter.DeviceConnected += Adapter_DeviceConnected;

            ble.StateChanged += (s, e) =>
            {
                OnValueChanged?.Invoke();
#if DEBUG
                Console.WriteLine($"The bluetooth state changed to {e.NewState}");
#endif
            };
        }

        public static async Task<PermissionStatus> CheckAndRequestLocationPermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            return status;
        }

        public static async Task<PermissionStatus> CheckAndRequestStoragePermission()
        {
            PermissionStatus status = await Permissions.CheckStatusAsync<Permissions.StorageRead>();

            if (status == PermissionStatus.Granted)
                return status;

            if (status == PermissionStatus.Denied && DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Prompt the user to turn on in settings
                // On iOS once a permission has been denied it may not be requested again from the application
                return status;
            }

            if (Permissions.ShouldShowRationale<Permissions.StorageRead>())
            {
                // Prompt the user with additional information as to why the permission is needed
            }

            status = await Permissions.RequestAsync<Permissions.StorageRead>();

            return status;
        }

        private async void Adapter_DeviceConnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            //            int mtu = await e.Device.RequestMtuAsync(255);
            //#if DEBUG
            //            Console.WriteLine($"MTU: {mtu}");
            //#endif
            OnValueChanged?.Invoke();
        }

        private void Adapter_DeviceDisconnected(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            OnValueChanged?.Invoke();
        }

        public async Task ScanDevices()
        {
            Devices.Clear();
            await adapter.StartScanningForDevicesAsync();
        }

        public async Task StopScan()
        {
            await adapter.StopScanningForDevicesAsync();
        }

        public IDevice this[string index]
        {
            get => Devices.FirstOrDefault(d => d.NativeDevice.ToString() == index);
        }

        private async void Adapter_ScanTimeoutElapsed(object sender, EventArgs e)
        {
            await adapter.StopScanningForDevicesAsync();
#if DEBUG
            Console.WriteLine("Scan timeout");
#endif
        }

        private void Adapter_DeviceDiscovered(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
#if DEBUG
            Console.WriteLine($"{e.Device.Rssi} | {e.Device.NativeDevice} | {e.Device.Name}");
#endif
            Devices.Add(e.Device);
        }

        public async Task<bool> ConnectToDevice(IDevice device)
        {
            //await DisconnectDevices();
            await adapter.ConnectToDeviceAsync(device);
            var connected = device.State == Plugin.BLE.Abstractions.DeviceState.Connected;
#if DEBUG
            Console.WriteLine($"Device {device.State}");
#endif
            return connected;
        }

        public async Task DisconnectDevices()
        {
            foreach (var device in adapter.ConnectedDevices)
            {
                await DisconnectDevice(device);
            }
        }

        public void ClearLog()
        {
            Logs.Clear();
        }

        public async Task Subscribe(IDevice device)
        {
            int mtu = await device.RequestMtuAsync(500);
#if DEBUG
            Console.WriteLine($"MTU: {mtu}");
#endif
            OnValueChanged?.Invoke();

#if DEBUG
            Console.WriteLine("Subscribe");
#endif

            OtaStatus = OtaStatus.IDLE;
            OtaProgress = 0;
            OtaProgressStr = string.Empty;
            DeviceOtaStatus = string.Empty;

            /*
                BATTERY SERVICE
             */
            batService = await device.GetServiceAsync(
                Guid.Parse("0000180f-0000-1000-8000-00805f9b34fb"));
            batLevelChar = await batService.GetCharacteristicAsync(
                Guid.Parse("00002a19-0000-1000-8000-00805f9b34fb"));
            if (batLevelChar != null)
            {
                batLevelChar.ValueUpdated += BatLevelChar_ValueUpdated;
                await batLevelChar.StartUpdatesAsync();
            }

            /*
                CUSTOM SERVICE   
             */
            customService = await device.GetServiceAsync(
                Guid.Parse("0000aa11-0000-1000-8000-00805f9b34fb"));

            sigQuaChar = await customService.GetCharacteristicAsync(
                Guid.Parse("0000bb01-0000-1000-8000-00805f9b34fb"));
            if (sigQuaChar != null)
            {
                sigQuaChar.ValueUpdated += SigQuaChar_ValueUpdated;
                await sigQuaChar.StartUpdatesAsync();
            }

            levelChar = await customService.GetCharacteristicAsync(
                Guid.Parse("0000bb02-0000-1000-8000-00805f9b34fb"));
            if (levelChar != null)
            {
                levelChar.ValueUpdated += LevelChar_ValueUpdated;
                await levelChar.StartUpdatesAsync();
            }

            logChar = await customService.GetCharacteristicAsync(
                Guid.Parse("0000bb03-0000-1000-8000-00805f9b34fb"));
            if (logChar != null)
            {
                logChar.ValueUpdated += LogChar_ValueUpdated;
                await logChar.StartUpdatesAsync();
            }

            /*
                OTA SERVICE
             */
            otaService = await device.GetServiceAsync(
                Guid.Parse("0000aa00-0000-1000-8000-00805f9b34fb"));
            otaCmdChar = await otaService?.GetCharacteristicAsync(
                Guid.Parse("0000aa01-0000-1000-8000-00805f9b34fb"));
            otaDataChar = await otaService?.GetCharacteristicAsync(
                Guid.Parse("0000aa02-0000-1000-8000-00805f9b34fb"));

            if (otaCmdChar != null)
            {
                otaCmdChar.ValueUpdated += OtaCmdChar_ValueUpdated;
                await otaCmdChar.StartUpdatesAsync();
            }
        }

        private void OtaCmdChar_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            var value = e.Characteristic.StringValue;
            var arg = value.Split('#');
            if (arg.Length >= 2)
            {
                if (arg[0] == "*VER") FwVer = arg[1];
                else if (arg[0] == "*STAT") DeviceOtaStatus = arg[1];
                else if (arg[0] == "*OTA")
                {
                    if (arg[1] == "OK")
                    {
                        OtaProgressStr = "FW UPDATE READY";
                        OtaStatus = OtaStatus.READY_FOR_FLASH;
                        OtaProgress = 0;
                    }
                    else if (arg[1] == "ERR")
                    {
                        OtaProgressStr = "FW UPDATE BEGIN ERROR";
                        OtaStatus = OtaStatus.IDLE;
                    }
                }
                else if (arg[0] == "*DONE")
                {
                    if (arg[1] == "OK")
                    {
                        OtaProgressStr = "FW UPDATE SUCCESS";
                        OtaStatus = OtaStatus.IDLE;
                    }
                    else if (arg[1] == "ERR")
                    {
                        OtaProgressStr = "FW UPDATE ERROR";
                        OtaStatus = OtaStatus.IDLE;
                    }
                }
            }
            OnValueChanged?.Invoke();
        }

        public async Task RequestOtaStatus()
        {
#if DEBUG
            Console.WriteLine("Request OTA STATUS");
#endif
            await otaCmdChar.WriteAsync(Encoding.ASCII.GetBytes("*STAT#"));
        }

        public async Task RequestOtaVer()
        {
#if DEBUG
            Console.WriteLine("Request OTA version");
#endif 
            await otaCmdChar.WriteAsync(Encoding.ASCII.GetBytes("*VER#"));
        }

        public async Task RequestOtaFlash(long fwSize)
        {
#if DEBUG
            Console.WriteLine("Request OTA flash");
#endif
            await otaCmdChar.WriteAsync(Encoding.ASCII.GetBytes($"*OTA#{fwSize}#"));
        }

        public async Task RequestOtaAbort()
        {
#if DEBUG
            Console.WriteLine("Request OTA abort");
#endif
            await otaCmdChar.WriteAsync(Encoding.ASCII.GetBytes("*ABORT#"));
        }

        public async Task RequestOtaDone()
        {
#if DEBUG
            Console.WriteLine("Request OTA done");
#endif
            await otaCmdChar.WriteAsync(Encoding.ASCII.GetBytes("*DONE#"));
        }

        // TODO: SEND FLASH TO DEVICE
        public async Task SendFlash(string fileName)
        {
#if DEBUG
            Console.WriteLine("sending flash");
#endif
            OtaStatus = OtaStatus.FLASHING;

            try
            {
                var fileSize = new FileInfo(fileName).Length;
                long writed = 0;

                byte[] buffer = new byte[490];

                using FileStream fs = new(fileName, FileMode.Open, FileAccess.Read);
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (bytesRead == 490)
                    {
                        await otaDataChar?.WriteAsync(buffer);
                    }
                    else
                    {
                        await otaDataChar?.WriteAsync(buffer.Take(bytesRead).ToArray());
                    }
                    writed += bytesRead;
                    OtaProgress = (int)(writed / (float)fileSize * 100);
                    OnValueChanged?.Invoke();
                }
                await RequestOtaDone();
            }
            catch (Exception ex)
            {
                OtaProgressStr = "FLASH ERROR";
                OtaStatus = OtaStatus.IDLE;
                LogWrite("Flashing error: " + ex.Message);
            }
        }

        public async Task DisconnectDevice(IDevice device)
        {
            await adapter.DisconnectDeviceAsync(device);

#if DEBUG
            Console.WriteLine("Unsubscribe");
#endif
            if (logChar != null)
            {
                logChar.ValueUpdated -= LogChar_ValueUpdated;
            }
            if (levelChar != null)
            {
                levelChar.ValueUpdated -= LevelChar_ValueUpdated;
            }
            if (sigQuaChar != null)
            {
                sigQuaChar.ValueUpdated -= SigQuaChar_ValueUpdated;
            }
            customService?.Dispose();



            if (batLevelChar != null)
            {
                batLevelChar.ValueUpdated -= BatLevelChar_ValueUpdated;
            }
            batService?.Dispose();

            if (otaCmdChar != null)
            {
                otaCmdChar.ValueUpdated -= OtaCmdChar_ValueUpdated;
            }
            otaService?.Dispose();

        }

        private void LogChar_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            var log = e.Characteristic.StringValue;
            LogWrite(log);
        }

        private void LogWrite(string log)
        {
            Logs.Enqueue($"[{DateTime.Now:T}] {log}");
            if (Logs.Count > 100)
                Logs.TryDequeue(out var _);

            OnValueChanged?.Invoke();
        }

        private void LevelChar_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            SensorLevel = e.Characteristic.StringValue;
            OnValueChanged?.Invoke();
        }

        private void SigQuaChar_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            SigQua = e.Characteristic.Value[0];
            OnValueChanged?.Invoke();
        }

        private void BatLevelChar_ValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            BatLevel = BitConverter.ToUInt16(e.Characteristic.Value);
#if DEBUG
            Console.WriteLine("BAT UPDATE");
#endif
            OnValueChanged?.Invoke();
        }


    }
}
