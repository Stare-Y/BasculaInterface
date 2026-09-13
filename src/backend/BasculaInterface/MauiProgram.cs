using BasculaInterface.Services;
using BasculaInterface.ViewModels;
using CommunityToolkit.Maui;
using Core.Application.Services;
using Infrastructure.Service;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;

namespace BasculaInterface;

public static class MauiProgram
{
    public static IServiceProvider ServiceProvider { get; set; } = null!;
    public static string PrintTemplate { get; set; } = "\n\tCOOPERATIVA\n\tPEDRO\n\tEZQUEDA\n\n{fechaHora}\n\nTara: {tara}kg\nNeto: {neto}kg\nBruto: {bruto}kg\n";
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            fonts.AddFont("Montserrat-Regular.ttf", "Montserrat");

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
                        bool hideTaskbar = Preferences.Get("HideTaskbar", false);
                        if (hideTaskbar)
                        {
                            overlappedPresenter.IsResizable = false;
                            overlappedPresenter.IsMaximizable = false;
                            overlappedPresenter.IsMinimizable = true;
                            overlappedPresenter.SetBorderAndTitleBar(true, true);

                            // Cover the full display area (including taskbar) while keeping title bar with minimize
                            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(id, Microsoft.UI.Windowing.DisplayAreaFallback.Primary);
                            appWindow.MoveAndResize(displayArea.OuterBounds);
                            overlappedPresenter.Maximize();
                        }
                        else
                        {
                            overlappedPresenter.IsResizable = true;
                            overlappedPresenter.IsMaximizable = true;
                            overlappedPresenter.IsMinimizable = true;
                            overlappedPresenter.SetBorderAndTitleBar(true, true);
                            overlappedPresenter.Maximize();
                        }
                    }

                    // fix-session-inactivity-timeout design.md Decision 1 (revised): Page (and
                    // therefore Shell) has no GestureRecognizers property in MAUI — that's a
                    // View-only member, so a <Shell.GestureRecognizers> hook in XAML doesn't
                    // compile. Observing pointer presses at the native WinUI window root instead
                    // sees every page — modal stack included — through one registration, unlike any
                    // single MAUI view could; handledEventsToo means a control marking its own
                    // press "handled" (e.g. a Button) still counts as activity here.
                    if (window.Content is not null)
                    {
                        window.Content.AddHandler(
                            Microsoft.UI.Xaml.UIElement.PointerPressedEvent,
                            new Microsoft.UI.Xaml.Input.PointerEventHandler((_, _) =>
                            {
                                (ServiceProvider.GetService(typeof(InactivityWatcherService)) as InactivityWatcherService)
                                    ?.RegisterActivity();
                            }),
                            handledEventsToo: true);
                    }

                    // fix-session-inactivity-timeout follow-up: navigating (e.g. the inactivity
                    // timeout's forced logout) while the window lacks OS focus has been observed to
                    // leave the app stuck — visible and hoverable, but unresponsive to clicks/keys.
                    // We can't fix Windows' own input-focus handling, so callers instead defer such
                    // navigation until this fires with the window active again.
                    window.Activated += (_, e) =>
                    {
                        (ServiceProvider.GetService(typeof(InactivityWatcherService)) as InactivityWatcherService)
                            ?.NotifyWindowActivationChanged(e.WindowActivationState != Microsoft.UI.Xaml.WindowActivationState.Deactivated);
                    };
                });
            });
        });
#endif

        Preferences.Set("DeviceName", DeviceInfo.Name);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddTransient<BasculaViewModel>();
        builder.Services.AddTransient<PendingWeightsViewModel>();
        builder.Services.AddTransient<ProductSelectorViewModel>();
        builder.Services.AddTransient<PartnerSelectorViewModel>();
        builder.Services.AddTransient<FinishedWeightsViewModel>();
        builder.Services.AddTransient<ReadOnlyDetailedViewModel>();
        builder.Services.AddTransient<PedidoListViewModel>();

        // Auth (issue #134): one singleton session backs the whole app; AuthHeaderHandler attaches
        // its token to every request through IApiService without touching existing call sites.
        builder.Services.AddSingleton<ISessionService, SessionService>();
        builder.Services.AddSingleton<InactivityWatcherService>();
        builder.Services.AddTransient<AuthHeaderHandler>();

        builder.Services.AddTransient<IApiService, ApiService>();
        builder.Services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.BaseAddress = new Uri(Preferences.Get("HostUrl", "http://bascula.cpe/"));
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        }).AddHttpMessageHandler<AuthHeaderHandler>();
        //build service provider
        MauiApp app = builder.Build();

        ServiceProvider = app.Services;

        return app;
    }
}