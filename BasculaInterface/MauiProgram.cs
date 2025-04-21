using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BasculaInterface;

public static class MauiProgram
{
    public static string PortName = null!;
    public static int PortBaud = 9600;
    public static string PrinterName = null!;
    public static bool NeedManualAsk = false;
    public static string AskChar = "P";
    public static int TimerElapse = 750;
    public static string PrintTemplate = null!;
    public static int PrintFontSize;
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

        PortName = data["BasculaPort"].GetString();
        PortBaud = data["BasculaBauds"].GetInt32();
        if (PortBaud == 0)
            PortBaud = 9600;
        PrinterName = data["PrinterName"].GetString();
        NeedManualAsk = data["NeedManualAsk"].GetBoolean();
        if (NeedManualAsk)
        {
            AskChar = data["AskChar"].GetString();
            TimerElapse = data["TimerElapse"].GetInt32();
        }

        PrintTemplate = data["PrintTemplate"].GetString();

        PrintFontSize = data["PrintFontSize"].GetInt32();
    }
}
