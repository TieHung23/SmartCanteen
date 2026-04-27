using Microsoft.EntityFrameworkCore;
using SC.Api.DependencyInjection.Configurations;
using SC.Persistence.Database;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureLogging();
builder.Services.ConfigureSwagger(builder.Configuration);

builder.Services.AddDbContext<SmartCanteenDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.MigrationsAssembly(typeof(SmartCanteenDbContext).Assembly.FullName));
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SmartCanteenDbContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    var swaggerVersion = builder.Configuration["Swagger:Version"] ?? "v1";
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint($"/swagger/{swaggerVersion}/swagger.json", $"SmartCanteen API {swaggerVersion}");
    });
}

app.UseRequestLogEnrichment();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
    {
        var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}