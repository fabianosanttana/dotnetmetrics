using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using App.Metrics.Configuration;
using App.Metrics.Extensions.Reporting.InfluxDB;
using App.Metrics.Extensions.Reporting.InfluxDB.Client;
using App.Metrics.Reporting.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotnetMetrics
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMetrics(options =>
                {
                    options.WithGlobalTags((globalTags, info) =>
                    {
                        globalTags.Add("app", info.EntryAssemblyName);
                        globalTags.Add("env", "local");
                    });
                })
                .AddHealthChecks()
                .AddReporting(
                    factory =>
                    {
                        factory.AddInfluxDb(
                            new InfluxDBReporterSettings
                            {
                                InfluxDbSettings = new InfluxDBSettings("dotnetmetrics", new Uri("http://localhost:8086")),
                                ReportInterval = TimeSpan.FromSeconds(5)
                            });
                    })
                .AddMetricsMiddleware(options =>
                {
                    options.IgnoredHttpStatusCodes = new[] { 404 };
                });

            services.AddMvc(options => options.AddMetricsResourceFilter());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseMetrics();
            app.UseMetricsReporting(lifetime);
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
