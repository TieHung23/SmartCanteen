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

    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseRequestLogEnrichment();
app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.Run();