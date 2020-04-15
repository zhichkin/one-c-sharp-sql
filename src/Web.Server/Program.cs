using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OneCSharp.TSQL.Scripting;
using System;

namespace OneCSharp.Web.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
            OneCSharpSettings settings = new OneCSharpSettings();
            config.GetSection("OneCSharpSettings").Bind(settings);

            var host = CreateHostBuilder(args).Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;
                try
                {
                    var service = services.GetRequiredService<IScriptingService>();
                    ConfigureScriptingService(service, settings);
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
        private static void ConfigureScriptingService(IScriptingService scripting, OneCSharpSettings settings)
        {
            if (string.IsNullOrEmpty(settings.UseServer))
            {
                return;
            }
            scripting.UseServer("zhichkin");

            if (settings.UseDatabases == null || settings.UseDatabases.Count == 0)
            {
                return;
            }
            //foreach (string database in settings.UseDatabases)
            //{
            //    scripting.UseDatabase(database);
            //}
        }
    }
}