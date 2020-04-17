using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using System;
using System.IO;

namespace OneCSharp.Web.Server
{
    public class Program
    {
        private const string METADATA_CATALOG_NAME = "metadata";
        public static void Main(string[] args)
        {
            OneCSharpSettings settings = OneCSharpSettings();

            var host = CreateHostBuilder(args).Build();
            
            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                try
                {
                    var env = services.GetRequiredService<IWebHostEnvironment>();
                    var metadata = services.GetRequiredService<IMetadataService>();
                    ConfigureMetadataService(metadata, settings.MetadataSettings, env);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred.");
                }
            }

            host.Run();
        }
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
        }
        private static OneCSharpSettings OneCSharpSettings()
        {
            OneCSharpSettings settings = new OneCSharpSettings();
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            config.GetSection("OneCSharpSettings").Bind(settings);
            return settings;
        }
        private static string MetadataCatalogPath(IWebHostEnvironment environment)
        {
            string metadataCatalogPath = Path.Combine(environment.ContentRootPath, METADATA_CATALOG_NAME);
            if (!Directory.Exists(metadataCatalogPath))
            {
                _ = Directory.CreateDirectory(metadataCatalogPath);
            }
            return metadataCatalogPath;
        }
        private static void ConfigureMetadataService(IMetadataService metadata, MetadataServiceSettings settings, IWebHostEnvironment environment)
        {
            if (string.IsNullOrWhiteSpace(settings.Catalog))
            {
                settings.Catalog = MetadataCatalogPath(environment);
            }
            metadata.Configure(settings);
        }
    }
}