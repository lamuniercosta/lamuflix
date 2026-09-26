using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LamuFlix.Data;
using Microsoft.EntityFrameworkCore;
using LamuFlix.Web.Services;

namespace LamuFlix.Web;

public class Startup(IConfiguration configuration)
{
    private IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        var connection = Configuration.GetConnectionString("DefaultConnection")
                         ?? throw new InvalidOperationException(
                             "Connection string 'DefaultConnection' not configured. Set 'ConnectionStrings:DefaultConnection' via user secrets " +
                             "or the ConnectionStrings__DefaultConnection environment variable.");
        services.AddDbContext<LamuFlixContext>(opts => opts.UseMySql(connection, new MySqlServerVersion(new Version(8, 0, 31)), s => s.MigrationsAssembly("LamuFlix.Data")));

        services.Configure<CookiePolicyOptions>(options =>
        {
            // This lambda determines whether user consent for non-essential cookies is needed for a given request.
            options.CheckConsentNeeded = _ => true;
            options.MinimumSameSitePolicy = SameSiteMode.None;
        });

        services.AddTransient<IMovieService, MovieService>();

        services.AddDistributedMemoryCache(); // Adds a default in-memory implementation of IDistributedCache
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(30);
        });

        services.AddControllersWithViews();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCookiePolicy();
        app.UseSession();

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });
    }
}