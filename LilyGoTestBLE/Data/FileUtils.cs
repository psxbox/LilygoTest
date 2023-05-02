using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LilyGoTestBLE.Data
{
    public static class FileUtils
    {
        public static async Task<string> GetFilePathAsync()
        {
            var result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Faylni tanlang"
            });

            if (result == null)
                return "";

            var FileFullPath = result.FullPath;
            return FileFullPath;
        }
    }
}
