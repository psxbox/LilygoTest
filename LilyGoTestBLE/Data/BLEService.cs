using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LilyGoTestBLE.Data
{
    public class BLEService
    {
        public List<IDevice> Devices { get; private set; }

        private IBluetoothLE ble;
        private IAdapter adapter;

        public BLEService()
        {
            Devices = new List<IDevice>();

            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
            adapter.ScanMode = ScanMode.Balanced;
            adapter.ScanTimeout = 5000;
            adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;

            ble.StateChanged += (s, e) =>
            {
#if DEBUG
                Console.WriteLine($"The bluetooth state changed to {e.NewState}");
#endif
            };
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

        private async void Adapter_ScanTimeoutElapsed(object? sender, EventArgs e)
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
            await DisconnectDevices();
            await adapter.ConnectToDeviceAsync(device);
            var connected = device.State == Plugin.BLE.Abstractions.DeviceState.Connected;
#if DEBUG
            Console.WriteLine($"Device {device.State}");
#endif
            return connected;
        }

        private async Task DisconnectDevices()
        {
            foreach (var device in adapter.ConnectedDevices)
            {
                await adapter.DisconnectDeviceAsync(device);
            }
        }
    }
}
