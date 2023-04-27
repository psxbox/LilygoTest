using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LilygoTest.Services
{
    public class BLEService
    {
        private IBluetoothLE ble;
        private IAdapter adapter;
        private List<IDevice> devices;

        public event Action<List<IDevice>> OnDeviceDiscovered;

        public BLEService()
        {
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.ScanTimeout = 5000;
        }

        public async Task ScanDevicesAsync()
        {
            adapter.DeviceDiscovered += (s, a) =>
            {
                devices.Add(a.Device);
                OnDeviceDiscovered.Invoke(devices);
            };

            await adapter.StartScanningForDevicesAsync();
        }

        public async Task StopScan()
        {
            await adapter.StopScanningForDevicesAsync();
        }
    }
}
