using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneCSharp.Metadata.Services;
using OneCSharp.Scripting.Services;

namespace OneCSharp.Web.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc(options =>
            //{
            //    options.Filters.Add(new ProducesAttribute("application/json"));
            //});
            //services.Configure<MvcOptions>(options =>
            //{
            //    options.Filters.Add(new ProducesAttribute("application/json"));
            //});
            services.AddControllers();
            services.AddSingleton<IQueryExecutor, QueryExecutor>();
            services.AddSingleton<IMetadataService, MetadataService>();
            services.AddSingleton<IScriptingService, ScriptingService>();
        }
    }
}