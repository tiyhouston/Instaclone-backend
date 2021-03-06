using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Swashbuckle.Swagger.Model;
using Swashbuckle.SwaggerGen.Generator;
using Microsoft.AspNetCore.Mvc.Formatters.Xml;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Session;
using Npgsql;
// using Npgsql.EntityFrameworkCore.PostgreSQL;

public partial class Handler {

    public IConfigurationRoot Configuration { get; }
    private IHostingEnvironment Env;

    public Handler(IHostingEnvironment env)
    {
        Env = env;

        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables(prefix: "ASPNETCORE_");

        if (env.IsDevelopment())
        {
            // For more details on using the user secret store see https://go.microsoft.com/fwlink/?LinkID=532709
            // builder.AddUserSecrets();
        }

        Configuration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // sqlite
        // services.AddDbContext<DB>(options => options.UseSqlite(Configuration.GetConnectionString("Sqlite")));

        // in-memory
        services.AddDbContext<DB>(options => options.UseInMemoryDatabase());

        // postgresql
        // Use a PostgreSQL database
        // string endpoint = Env.IsDevelopment() ? "dev" : "prod";
        // services.AddDbContext<DB>(options =>
        //     options.UseNpgsql(
        //         Configuration.GetConnectionString($"Postgres-{endpoint}")));

        // using (var conn = new NpgsqlConnection(Configuration.GetConnectionString("Postgres-dev")))
        // {
        //     conn.Open();
        //     using (var cmd = new NpgsqlCommand())
        //     {
        //         cmd.Connection = conn;
        //         cmd.CommandText = "SELECT * FROM products";
        //         cmd.ExecuteNonQuery();
        //         using (var reader = cmd.ExecuteReader())
        //         {
        //             while (reader.Read())
        //             {
        //                 Console.WriteLine(reader.GetString(0));
        //             }
        //         }
        //     }
        // }

        // services.AddIdentity<User, IdentityRole>()
        //     .AddEntityFrameworkStores<DB>()
        //     .AddDefaultTokenProviders();

        services.AddDistributedMemoryCache();
        services.AddSession(o => {
            o.IdleTimeout = TimeSpan.FromSeconds(120);
        });
        services.AddCors(options =>
            options.AddPolicy("CorsPolicy",
                builder => builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()));
        services.AddMvc();

        // instead of
        //      services.AddScoped<IRepository<Card>, Repo<Card>>();
        // do
        RegisterRepos(services); // RegisterRepos() is defined in Models.cs

        // Inject an implementation of ISwaggerProvider with defaulted settings applied
        services.AddSwaggerGen();

        services.ConfigureSwaggerGen(options =>
        {
            options.SingleApiVersion(new Info
            {
                Version = "v1",
                Title = "API Docs",
                Description = "Test and demo the API"
            });
            options.IgnoreObsoleteActions();
            options.IgnoreObsoleteProperties();
            options.DescribeAllEnumsAsStrings();
        });
    }

    public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory logger, DB db) {
        // logger.AddConsole(Configuration.GetSection("Logging"));
        logger.AddDebug();

        app.UseSession();
        app.UseCors("CorsPolicy");

        // Example custom middleware
        // app.Use(async (context, next) =>
        // {
        //     await context.Response.WriteAsync("Pre Processing");
        //     await next();
        //     await context.Response.WriteAsync("Post Processing");
        // });

        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseDatabaseErrorPage();
            app.UseStatusCodePages();
        }

        Seed.Initialize(db, env.IsDevelopment());

        // app.UseApplicationInsightsRequestTelemetry();
        // app.UseApplicationInsightsExceptionTelemetry();

        app.UseStaticFiles();
        // app.UseIdentity();
        // app.EnsureRolesCreated();

        // See comments in config.json for info on enabling Facebook auth
        // var facebookId = Configuration["Auth:Facebook:AppId"];
        // var facebookSecret = Configuration["Auth:Facebook:AppSecret"];
        // if (!string.IsNullOrWhiteSpace(facebookId) && !string.IsNullOrWhiteSpace(facebookSecret))
        // {
        //     app.UseFacebookAuthentication(new FacebookOptions
        //     {
        //         AppId = facebookId,
        //         AppSecret = facebookSecret
        //     });
        // }

        // // See comments in config.json for info on enabling Google auth
        // var googleId = Configuration["Auth:Google:ClientId"];
        // var googleSecret = Configuration["Auth:Google:ClientSecret"];
        // if (!string.IsNullOrWhiteSpace(googleId) && !string.IsNullOrWhiteSpace(googleSecret))
        // {
        //     app.UseGoogleAuthentication(new GoogleOptions
        //     {
        //         ClientId = googleId,
        //         ClientSecret = googleSecret
        //     });
        // }

        app.UseMvc(); //.AddXmlSerializerFormatters();
        // app.UseStatusCodePagesWithReExecute("/Home/Errors/{0}");

        // Enable middleware to serve generated Swagger as a JSON endpoint
        app.UseSwagger();

        // Enable middleware to serve swagger-ui assets (HTML, JS, CSS etc.)
        app.UseSwaggerUi();
    }

}

