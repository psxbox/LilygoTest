using Android.Views;
using Plugin.BLE.Abstractions.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LilyGoTestApp
{
    public class DeviceAdapter : BaseAdapter<IDevice>
    {
        private readonly Activity context;
        private readonly List<IDevice> devices;

        public DeviceAdapter(Activity context, List<IDevice> devices)
        {
            this.context = context;
            this.devices = devices;
        }
        public override IDevice this[int position] => devices[position];

        public override int Count => devices.Count;

        public override long GetItemId(int position)
        {
            return position;
        }

        public override View? GetView(int position, View? convertView, ViewGroup? parent)
        {
            var item = devices[position];
            View? view = convertView;
            view ??= context.LayoutInflater.Inflate(Android.Resource.Layout.TwoLineListItem, null);
            view.FindViewById<TextView>(Android.Resource.Id.Text1).Text = 
                string.IsNullOrEmpty(item.Name) ? "Nomalum" : item.Name;
            view.FindViewById<TextView>(Android.Resource.Id.Text2).Text = 
                $"{item.Rssi} dB | {item.NativeDevice}";
            return view;
        }
    }
}
