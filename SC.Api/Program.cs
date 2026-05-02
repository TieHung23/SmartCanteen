using SC.Api.DependencyInjection.Configurations;

var builder = WebApplication.CreateBuilder(args);

builder.AddApiConfigurations();

var app = builder.Build();
app.UseApiConfigurations();

app.Run();