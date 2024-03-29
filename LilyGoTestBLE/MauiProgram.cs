﻿using LilyGoTestBLE.Data;
using Microsoft.Extensions.Logging;
using BlazorBootstrap;

namespace LilyGoTestBLE
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<BLEService>();
            builder.Services.AddBlazorBootstrap();

            return builder.Build();
        }
    }
}