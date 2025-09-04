using BasculaTerminalApi.Models;
using Core.Application.Services;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repos;
using Microsoft.EntityFrameworkCore;

namespace BasculaTerminalApi.Service
{
    public static class AppService
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddPersistency(configuration);

            services.AddBasculaService();

            services.AddPrintService(configuration);

            services.AddRepoServices();

            return services;
        }

        private static IServiceCollection AddPersistency(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<WeightDBContext>(sp =>
            {
                string? connectionString = Environment.GetEnvironmentVariable("PostgresWeightConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidDataException("Couldn't load PostgresWeightConnection conn string.");
                }
                sp.UseNpgsql(connectionString);
            });

            services.AddDbContext<ContpaqiSQLContext>(sp =>
            {
                string? connectionString = Environment.GetEnvironmentVariable("ContpaqSQLConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidDataException("Couldn't load ContpaqSQLConnection conn string.");
                }

                sp.UseSqlServer(connectionString);
            });

            return services;
        }

        private static IServiceCollection AddRepoServices(this IServiceCollection services)
        {
            services.AddTransient<IWeightRepo, WeightRepo>();
            services.AddTransient<IClienteProveedorRepo, ClienteProveedorRepo>();
            services.AddTransient<IProductoRepo, ProductoRepo>();
            return services;
        }

        private static IServiceCollection AddBasculaService(this IServiceCollection services)
        {
            services.AddSingleton<BasculaService>();
            services.AddSingleton<IWeightLogisticService, WeightLogisticService>();
            return services;
        }

        public static IServiceCollection AddPrintService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<PrintSettings>(sp =>
            {
                PrintSettings? settings = configuration.GetSection("SerialSettings").Get<PrintSettings>();
                if (settings is null)
                {
                    throw new InvalidDataException("No se pudo cargar la configuración de impresion/bascula desde el archivo de configuración.");
                }
                return settings;
            });

            services.AddSingleton<PrintService>();

            return services;
        }
    }
}
