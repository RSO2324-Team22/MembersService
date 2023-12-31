using Confluent.Kafka;
using GraphQL.AspNet.Configuration;
using MembersService.Database;
using MembersService.HealthCheck;
using MembersService.Metrics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using Serilog;
using Serilog.Events;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

string postgres_server = builder.Configuration["POSTGRES_SERVER"] ?? "";
string postgres_database = builder.Configuration["POSTGRES_DATABASE"] ?? "";
string postgres_username = builder.Configuration["POSTGRES_USERNAME"] ?? "";
string postgres_password = builder.Configuration["POSTGRES_PASSWORD"] ?? "";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddGraphQL();
builder.Services.AddKafkaClient()
    .Configure(options => {
        options.Configure(new ProducerConfig {
            BootstrapServers = configuration["KAFKA_URL"]
        }); 
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MembersDbContext>(options => {
    options.UseNpgsql($"Host={postgres_server};Username={postgres_username};Password={postgres_password};Database={postgres_database}");
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseCreationHealthCheck>("database_creation", tags: new [] { "startup" });

builder.Services.AddSingleton<MembersMetrics>();

builder.Services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        builder.AddPrometheusExporter();

        builder.AddMeter("Microsoft.AspNetCore.Hosting",
            "Microsoft.AspNetCore.Server.Kestrel",
            "Members.Web");
    });

builder.Host.UseSerilog();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
    options.RoutePrefix = "openapi";
    options.DocumentTitle = "OpenAPI documentation";
});

app.UseHttpsRedirection();

app.MapHealthChecks("/health/startup", new HealthCheckOptions {
    Predicate = healthcheck => healthcheck.Tags.Contains("startup") 
});

app.MapPrometheusScrapingEndpoint();

app.UseAuthorization();

app.MapControllers();
app.UseGraphQL();

app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "Handled {RequestPath}";
    
    // Emit debug-level events instead of the defaults
    options.GetLevel = (httpContext, elapsed, ex) => LogEventLevel.Debug;
    
    // Attach additional properties to the request completion event
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
    };
});

app.Run();
