using BasculaInterface.ViewModels;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BasculaInterface;

public static class MauiProgram
{
    public static string BasculaSocketUrl { get; set; } = "http://localhost:5284/basculaSocket";
    //service provider
    public static IServiceProvider ServiceProvider { get; set; } = null!;
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
        string settingsFile = Path.Combine(AppContext.BaseDirectory, "Config/settings.json");
        if (!File.Exists(settingsFile))
        {
            throw new InvalidDataException("El archivo settings.json no existe");
        }

        string jsonContent = File.ReadAllText(settingsFile);

        var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

        if (data is null)
            throw new InvalidDataException("El archivo settings.json parece ser nulo");

        var url = data["BasculaSocketUrl"].GetString();

        if (url is not null)
            BasculaSocketUrl = url;
    }
}
