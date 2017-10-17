﻿﻿using PaderbornUniversity.SILab.Hip.CmsApi.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using PaderbornUniversity.SILab.Hip.CmsApi.Services;
using Swashbuckle.AspNetCore.Swagger;
using PaderbornUniversity.SILab.Hip.Webservice;

namespace PaderbornUniversity.SILab.Hip.CmsApi
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Startup
    {
    internal static IServiceProvider ServiceProvider { get; private set; }

        private IConfigurationRoot Configuration { get; }

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Configures the built-in container's services, i.e. the services added to the IServiceCollection
        /// parameter are available via dependency injection afterwards.
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Read configurations from json
            var appConfig = new Utility.AppConfig(Configuration);

            // Register AppConfig in Services 
            services.AddSingleton(appConfig);
            services.AddTransient<IEmailSender, EmailSender>();

            string domain = appConfig.AuthConfig.Authority;
            services.AddAuthorization(options =>
            {
                options.AddPolicy("read:webapi",
                    policy => policy.Requirements.Add(new HasScopeRequirement("read:webapi", domain)));
                options.AddPolicy("write:webapi",
                    policy => policy.Requirements.Add(new HasScopeRequirement("write:webapi", domain)));
            });

            // Adding Cross Orign Requests 
            services.AddCors();

            // Add database service for Postgres
            services.AddDbContext<CmsDbContext>(options => options.UseNpgsql(appConfig.DatabaseConfig.ConnectionString));

            // Add framework services.
            services.AddMvc();

            // Add Swagger service
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info() { Title = "HiPCMS API", Version = "v1", Description = "A REST api to serve History in Paderborn CMS System" });

                c.IncludeXmlComments(Path.Combine(PlatformServices.Default.Application.ApplicationBasePath, "Api.xml"));
                c.OperationFilter<SwaggerOperationFilter>();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, Utility.AppConfig appConfig)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            if (env.IsDevelopment())
                loggerFactory.AddDebug();

            app.UseCors(builder =>
                // This will allow any request from any server. Tweak to fit your needs!
                builder.AllowAnyHeader()
                       .AllowAnyMethod()
                       .AllowAnyOrigin()
            );

            var options = new JwtBearerOptions
            {
                Audience = appConfig.AuthConfig.Audience,
                Authority = appConfig.AuthConfig.Authority
            };
            app.UseJwtBearerAuthentication(options);

            app.UseMvc();

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger(c =>
            {
                c.PreSerializeFilters.Add((swaggerDoc, httpReq) => swaggerDoc.Host = httpReq.Host.Value);
            });
            app.UseSwaggerUI(c =>
            {
                // Only a hack, if HiP-Swagger is running, SwaggerUI can be disabled for Production
                c.SwaggerEndpoint((env.IsDevelopment() ? "/swagger" : "..") + "/v1/swagger.json", "HiPCMS API V1");
            });

            // Run all pending Migrations and Seed DB with initial data
            app.RunMigrationsAndSeedDb();
            app.UseStaticFiles();

            ServiceProvider = app.ApplicationServices;
        }

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
