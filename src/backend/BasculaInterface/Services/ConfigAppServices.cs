using Microsoft.Extensions.Configuration;

namespace BasculaInterface.Services
{
    public static class ConfigAppServices
    {
        public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration configuration)
        {
            bool filterClasif6 = configuration.GetValue<bool>("FilterClasif6");

            Preferences.Set("FilterClasif6", filterClasif6);

            return services;
        }
    }
}
