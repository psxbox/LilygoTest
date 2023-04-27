using LilygoTest.Services;
using Plugin.BLE.Abstractions.Contracts;

namespace LilygoTest;

public partial class MainPage : ContentPage
{
    public List<IDevice> Devices { get; set; }

    private readonly BLEService bleService;

    public MainPage()
    {
#if ANDROID
        bleService = MainApplication.Current.Services.GetRequiredService<BLEService>();
#endif
        bleService.OnDeviceDiscovered += BleService_OnDeviceDiscovered;

        Task.Run(async () => await bleService.ScanDevicesAsync());

        InitializeComponent();
    }

    private void BleService_OnDeviceDiscovered(List<IDevice> devices)
    {
        Devices = devices;
        DeviceListView.ItemsSource = Devices;
    }
}

