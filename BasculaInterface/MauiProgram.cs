using BasculaInterface.Services;
using BasculaInterface.ViewModels;
using CommunityToolkit.Maui;
using Core.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using System.Text.Json;

namespace BasculaInterface;
public static class MauiProgram
{
    public static string BasculaSocketUrl { get; set; } = "http://192.168.1.240:6969/";
    public static IServiceProvider ServiceProvider { get; set; } = null !;
    public static string PrintTemplate { get; set; } = "\n\tCOOPERATIVA\n\tPEDRO\n\tEZQUEDA\n\n{fechaHora}\n\nTara: {tara}kg\nNeto: {neto}kg\nBruto: {bruto}kg\n";

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
        }).UseMauiCommunityToolkit();
#if WINDOWS
        builder.ConfigureLifecycleEvents(events =>
        {
            events.AddWindows(windowsLifeCycleBuilder =>
            {
                windowsLifeCycleBuilder.OnWindowCreated(window =>
                {
                    window.ExtendsContentIntoTitleBar = false;

                    var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

                    var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);

                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(id);

                    if (appWindow.Presenter is Microsoft.UI.Windowing.OverlappedPresenter overlappedPresenter)
                    {
                        overlappedPresenter.IsResizable = false;
                        overlappedPresenter.IsMaximizable = false;
                        overlappedPresenter.IsMinimizable = true;
                        overlappedPresenter.Maximize();
                    }
                });
            });
        });

#endif
#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.AddTransient<BasculaViewModel>();
        builder.Services.AddTransient<PendingWeightsViewModel>();
        builder.Services.AddTransient<ProductSelectorViewModel>();
        builder.Services.AddTransient<PartnerSelectorViewModel>();
        builder.Services.AddTransient<IApiService, ApiService>();
        builder.Services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.BaseAddress = new Uri(BasculaSocketUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });
        //build service provider
        ServiceProvider = builder.Services.BuildServiceProvider();
        return builder.Build();
    }
}