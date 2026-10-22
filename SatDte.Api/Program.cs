using SatDte.Core.Persistencia;
using SatDte.Core.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
string datos = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new ArchivoRtu(Path.Combine(datos, "rtu.json")));
builder.Services.AddSingleton(new ArchivoFacturas(Path.Combine(datos, "solicitudes.json")));
builder.Services.AddSingleton<ServicioRtu>();
builder.Services.AddSingleton<ServicioFacturas>();
builder.Services.AddSingleton<ServicioReportes>();

var app = builder.Build();
app.MapControllers();
// Asignar un puerto distinto al de Razor Pages incluso si VS no carga launchSettings.
app.Run("http://localhost:5175");
