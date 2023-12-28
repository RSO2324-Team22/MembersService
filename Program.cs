using MembersService.Database;
using MembersService.HealthCheck;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

string postgres_server = builder.Configuration["POSTGRES_SERVER"] ?? "";
string postgres_database = builder.Configuration["POSTGRES_DATABASE"] ?? "";
string postgres_username = builder.Configuration["POSTGRES_USERNAME"] ?? "";
string postgres_password = builder.Configuration["POSTGRES_PASSWORD"] ?? "";

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<MembersDbContext>(options => {
    options.UseNpgsql($"Host={postgres_server};Username={postgres_username};Password={postgres_password};Database={postgres_database}");
});
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseCreationHealthCheck>("database_creation", tags: new [] { "startup" });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapHealthChecks("/health/startup", new HealthCheckOptions {
    Predicate = healthcheck => healthcheck.Tags.Contains("startup") 
});

app.UseAuthorization();

app.MapControllers();

app.Run();
