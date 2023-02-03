global using Altairis.Incitatus.Data;
global using System.ComponentModel.DataAnnotations;
global using Microsoft.AspNetCore.Mvc;
using Altairis.Incitatus.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Altairis.Services.DateProvider;

var builder = WebApplication.CreateBuilder(args);
// Load configuration
builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Register services
builder.Services.AddDbContext<IncitatusDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("IncitatusSqlServer"));
});
builder.Services.AddSingleton<IDateProvider>(new LocalDateProvider());
builder.Services.AddControllers();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("management", new OpenApiInfo {
        Title = "Incitatus Management API",
        Version = "v1",
        Description = "API for system management",
        TermsOfService = appSettings.Api.TermsOfServiceUrl,
        Contact = new OpenApiContact {
            Name = appSettings.Api.ContactName,
            Url = appSettings.Api.ContactUrl,
            Email = appSettings.Api.ContactEmail
        }
    });
    c.SwaggerDoc("search", new OpenApiInfo {
        Title = "Incitatus Search API",
        Version = "v1",
        Description = "API for public search",
        TermsOfService = appSettings.Api.TermsOfServiceUrl,
        Contact = new OpenApiContact {
            Name = appSettings.Api.ContactName,
            Url = appSettings.Api.ContactUrl,
            Email = appSettings.Api.ContactEmail
        }
    });
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Altairis.Incitatus.Web.xml"));
});

// Register update services
builder.Services.Configure<UpdateServiceOptions>(options => options.PollInterval = TimeSpan.FromSeconds(10));
builder.Services.AddHostedService<UpdateService>();

// Build and initialize application
var app = builder.Build();
using (var scope = app.Services.CreateScope()) {
    var dc = scope.ServiceProvider.GetRequiredService<IncitatusDbContext>();
    await dc.Database.MigrateAsync();
}

// Map requests
app.UseSwagger();
app.UseSwaggerUI(c => {
    c.RoutePrefix = "docs";
    c.SwaggerEndpoint("/swagger/search/swagger.json", "Search API");
    c.SwaggerEndpoint("/swagger/management/swagger.json", "Management API");
});
app.MapControllers();
app.MapGet("/", () => Results.Redirect(appSettings.HomepageRedirectUrl));

// Run application
await app.RunAsync();
