using BasculaInterface.ViewModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BasculaInterface;

public static class MauiProgram
{
    public static string BasculaSocketUrl { get; set; } = "http://localhost:5284/";
    public static IServiceProvider ServiceProvider { get; set; } = null!;
    public static string PrintTemplate { get; set; } = "\n\tCOOPERATIVA\n\tPEDRO\n\tEZQUEDA\n\n{fechaHora}\n\nTara: {tara}kg\nNeto: {neto}kg\nBruto: {bruto}kg\n";

    public static MauiApp CreateMauiApp()
	{
        LoadSettings();

        var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

        builder.Services.AddTransient<BasculaViewModel>();

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
#if DEBUG
        BasculaSocketUrl = "http://10.0.2.2:5284/";
#endif
#endif
    }
}
