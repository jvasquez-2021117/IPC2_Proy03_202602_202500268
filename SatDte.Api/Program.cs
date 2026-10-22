using SatDte.Core.Persistencia;
using SatDte.Core.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton(new ArchivoRtu(
    Path.Combine(builder.Environment.ContentRootPath, "App_Data", "rtu.json")));
builder.Services.AddSingleton<ServicioRtu>();

var app = builder.Build();

// La API no depende del sitio Razor; se puede usar con Postman y otros clientes HTTP.
app.MapControllers();
app.Run("http://localhost:5175");
