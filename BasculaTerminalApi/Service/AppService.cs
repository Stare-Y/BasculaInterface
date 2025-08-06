using BasculaTerminalApi.Models;
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
                string? connectionString = configuration.GetConnectionString("PostgresWeightConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidDataException("No se pudo cargar la cadena de conexión a la base de datos desde el archivo de configuración.");
                }
                sp.UseNpgsql(connectionString);
            });

            services.AddDbContext<ContpaqiSQLContext>(sp =>
            {
                string? connectionString = configuration.GetConnectionString("ContpaqSQLConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidDataException("No se pudo cargar la cadena de conexión a la base de datos desde el archivo de configuración.");
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
