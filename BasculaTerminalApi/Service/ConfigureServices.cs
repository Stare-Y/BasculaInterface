using Core.Application.Services;
using Core.Application.Settings;
using Core.Domain.Interfaces;
using Infrastructure.Data;
using Infrastructure.Repos;
using Infrastructure.Service;
using Microsoft.EntityFrameworkCore;

namespace BasculaTerminalApi.Service
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<WeightSettings>(configuration.GetSection("WeightSettings"));

            services.AddPersistency(configuration);

            services.AddBasculaService();

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
            services.AddScoped<IWeightRepo, WeightRepo>();

            services.AddScoped<IClienteProveedorRepo, ClienteProveedorRepo>();

            services.AddScoped<IProductRepo, ProductRepo>();

            services.AddScoped<ITurnRepo, TurnRepo>();

            services.AddScoped<IProductService, ProductService>();

            services.AddScoped<IClienteProveedorService, ClienteProveedorService>();

            services.AddScoped<ITurnService, TurnService>();

            return services;
        }

        private static IServiceCollection AddBasculaService(this IServiceCollection services)
        {
            services.AddSingleton<IBasculaService, BasculaService>();

            services.AddSingleton<IWeightLogisticService, WeightLogisticService>();

            services.AddScoped<IWeightService, WeightService>();

            services.AddScoped<IPrintService, PrintService>();

            return services;
        }
    }
}
