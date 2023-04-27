using Android;
using Android.Content;
using Android.Content.Res;
using Android.Views;
using Android.Webkit;
using Java.Security;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;

namespace LilyGoTestApp
{
    [Activity(Label = "@string/app_name", MainLauncher = true)]
    public class MainActivity : Activity
    {
        private IBluetoothLE ble;
        private Plugin.BLE.Abstractions.Contracts.IAdapter adapter;
        private Button? scanButton;
        private ListView? listViewDevices;
        private List<IDevice> devices = new();

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            //Platform.Init(this.Application);


            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
            adapter.ScanMode = ScanMode.Balanced;
            adapter.ScanTimeout = 7000;
            adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;

            ble.StateChanged += (s, e) =>
            {
                Console.WriteLine($"The bluetooth state changed to {e.NewState}");
            };

            scanButton = FindViewById<Button>(Resource.Id.buttonScan);
            scanButton.Click += async (s, e) =>
            {
                devices.Clear();
                scanButton.Enabled = false;
                await adapter.StartScanningForDevicesAsync();
            };

            listViewDevices = FindViewById<ListView>(Resource.Id.listViewDevices);
            listViewDevices.ItemClick += ListViewDevices_ItemClick; ;
        }

        private async void ListViewDevices_ItemClick(object? sender, AdapterView.ItemClickEventArgs e)
        {
            await adapter.StopScanningForDevicesAsync();
            var intent = new Intent(this, typeof(ActivitySelected));
            StartActivity(intent);
        }

        private async void Adapter_ScanTimeoutElapsed(object? sender, EventArgs e)
        {
            await adapter.StopScanningForDevicesAsync();
            scanButton.Enabled = true;
            Console.WriteLine("Scan timeout");
        }

        private void Adapter_DeviceDiscovered(object? sender, Plugin.BLE.Abstractions.EventArgs.DeviceEventArgs e)
        {
            Console.WriteLine($"{e.Device.State} | {e.Device.Rssi} | {e.Device.NativeDevice} | {e.Device.Name}");
            devices.Add(e.Device);
            listViewDevices.Adapter = new DeviceAdapter(this, devices);
        }

    }
}