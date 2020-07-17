using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using PSNotes.Services;
using NLog.Web;
using PSNotes.Models.Settings;

namespace PSNotes
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            //env.ConfigureNLog("nlog.config");

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets<Startup>();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Setup options with DI
            services.AddOptions();

            services.AddAuthorization();

            services.AddAuthentication(o =>
            {
                o.DefaultChallengeScheme = Constants.GitHubAuthenticationScheme;
                o.DefaultSignInScheme = Constants.CookieAuthenticationScheme;
                o.DefaultAuthenticateScheme = Constants.CookieAuthenticationScheme;
            })
                .AddCookie(Constants.CookieAuthenticationScheme, o =>
                {
                    o.LoginPath = new PathString("/Account/Login/");
                    o.AccessDeniedPath = new PathString("/Account/AccessDenied/");
                })
                .AddGitHub(
                    Configuration["Authentication:GitHub:ClientId"],
                    Configuration["Authentication:GitHub:ClientSecret"],
                    Constants.CookieAuthenticationScheme
                );

            // Configure service settings using appsettings 
            services.Configure<StorageApiSettings>(Configuration.GetSection("Storage"));
            services.Configure<EventHubsSettings>(Configuration.GetSection("Events"));

            services.AddSingleton<INoteStorageService, ApiNoteStorageService>();
            services.AddSingleton<IEventPublisher, EventHubsEventPublisher>();

            services.AddMvc();

            // call this in case you need aspnet-user-authtype/aspnet-user-identity
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            //add NLog to ASP.NET Core
            //loggerFactory.AddNLog();

            //add NLog.Web
            app.AddNLogWeb();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAuthentication();

            //app.UseHttpsRedirection();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
