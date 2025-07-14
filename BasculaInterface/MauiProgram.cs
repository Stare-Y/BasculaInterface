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
    public static string BasculaSocketUrl { get; set; } = "http://localhost:6969/";
    public static IServiceProvider ServiceProvider { get; set; } = null !;
    public static string PrintTemplate { get; set; } = "\n\tCOOPERATIVA\n\tPEDRO\n\tEZQUEDA\n\n{fechaHora}\n\nTara: {tara}kg\nNeto: {neto}kg\nBruto: {bruto}kg\n";

    public static MauiApp CreateMauiApp()
    {
        LoadSettings();
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

    private static void LoadSettings()
    {
#if WINDOWS
        string settingsFile = Path.Combine(AppContext.BaseDirectory, "Config/settings.json");
        if (!File.Exists(settingsFile))
        {
            return;
        }

        string jsonContent = File.ReadAllText(settingsFile);

        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

        if (data is null)
            throw new InvalidDataException("El archivo settings.json parece ser nulo");

        var url = data["BasculaSocketUrl"].GetString();

        var template = data["PrintTemplate"].GetString();

        if (url is not null)
            BasculaSocketUrl = url;

        if (template is not null)
            PrintTemplate = template;
#else
#if ANDROID
        BasculaSocketUrl = "http://www.basculacpe.com/";
#endif
#endif
    }
}