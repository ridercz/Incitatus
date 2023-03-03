global using System.ComponentModel.DataAnnotations;
global using Altairis.Incitatus.Data;
global using Microsoft.AspNetCore.Mvc;
using Altairis.Incitatus.Services;
using Altairis.Incitatus.Web;
using Altairis.Incitatus.Web.Security;
using Altairis.Services.DateProvider;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Services.Configure<AppSettings>(builder.Configuration);
var appSettings = new AppSettings();
builder.Configuration.Bind(appSettings);

// Register general services
builder.Services.AddDbContext<IncitatusDbContext>(options => {
    options.UseSqlServer(builder.Configuration.GetConnectionString("IncitatusSqlServer"));
});
builder.Services.AddSingleton<IDateProvider>(new LocalDateProvider());
builder.Services.AddControllers();

// Register authentication and authorization
builder.Services.AddAuthentication(defaultScheme: ApiKeyBearerDefaults.Scheme)
    .AddScheme<ApiKeyBearerAuthenticationSchemeOptions, ApiKeyBearerAuthenticationSchemeHandler>(ApiKeyBearerDefaults.Scheme, options => { });
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IBearerTokenValidator, ConfigurationBearerTokenValidator>();

// Register Swagger
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
    c.AddSecurityDefinition(name: ApiKeyBearerDefaults.Scheme, securityScheme: new OpenApiSecurityScheme {
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });
    c.OperationFilter<ApiKeyBearerOperationFilter>();
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Altairis.Incitatus.Web.xml"));
});

// Register update service
if (appSettings.UpdateService.Enabled) {
    builder.Services.Configure<UpdateServiceOptions>(options => {
        options.ConnectionTimeout = appSettings.UpdateService.ConnectionTimeout;
        options.DelayBetweenPageRequests = appSettings.UpdateService.DelayBetweenPageRequests;
        options.DescriptionMetaFields = appSettings.UpdateService.DescriptionMetaFields;
        options.PollInterval = appSettings.UpdateService.PollInterval;
        options.PooledCollectionLifetime = appSettings.UpdateService.PooledCollectionLifetime;
        options.TitleMetaFields = appSettings.UpdateService.TitleMetaFields;
    });
    builder.Services.AddHostedService<UpdateService>();
}

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
    c.SwaggerEndpoint("/swagger/search/swagger.json", "Search");
    c.SwaggerEndpoint("/swagger/management/swagger.json", "Management");
});
app.MapControllers();
app.MapGet("/", () => Results.Redirect(appSettings.HomepageRedirectUrl));

// Run application
await app.RunAsync();
