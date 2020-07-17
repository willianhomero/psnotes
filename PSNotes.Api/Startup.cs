using System;
using PSNotes.Api.Models.Settings;
using PSNotes.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog.Web;

namespace PSNotes.Api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup options with DI
            services.AddOptions();

            // Configure service settings using appsettings 
            services.Configure<CosmosDbSettings>(Configuration.GetSection("Storage"));
            services.AddSingleton<INoteStorageService, CosmosDbNoteStorageService>();

            // Adds a Redis in-memory implementation of IDistributedCache.
            services.AddDistributedRedisCache(options =>
            {
                options.Configuration = Configuration["Caching:RedisPrimaryEndpoint"];
            });

            services.AddSingleton(provider =>
                new DistributedCacheEntryOptions()
                    .SetSlidingExpiration(
                        TimeSpan.FromMinutes(
                            int.Parse(Configuration["Caching:CacheExpirationInMinutes"])
                        )
                    )
            );

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.AddNLogWeb();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
