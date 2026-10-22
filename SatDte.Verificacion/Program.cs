using SatDte.Core.Estructuras;
using SatDte.Core.Modelos;
using SatDte.Core.Persistencia;
using SatDte.Core.Servicios;
using SatDte.Core.Validaciones;

int pasadas = 0;
string carpeta = Path.Combine(Path.GetTempPath(), "satdte-pruebas-" + Guid.NewGuid().ToString("N"));

void Afirmar(bool condicion, string caso)
{
    if (!condicion) throw new Exception("FALLÓ: " + caso);
    pasadas++;
    Console.WriteLine("  OK: " + caso);
}

try
{
    ValidadorNit validador = new();
    Afirmar(validador.EsValido("7378106"), "NIT válido del enunciado: 7378106");
    Afirmar(validador.EsValido("8338817"), "NIT válido del enunciado: 8338817");
    Afirmar(!validador.EsValido("5123456"), "Ejemplo de NIT inválido 5123456");
    Afirmar(validador.EsValido("1234560"), "Cálculo de verificador numérico");
    Afirmar(!validador.EsValido("7378107"), "Verificador numérico incorrecto");
    Afirmar(validador.EsValido("6K"), "Verificador con letra K");
    Afirmar(!validador.EsValido("6k"), "Letra k minúscula no admitida");
    Afirmar(!validador.EsValido(""), "NIT vacío");
    Afirmar(!validador.EsValido("A7378106"), "Caracteres no numéricos");
    Afirmar(!validador.EsValido(new string('1', 21) + "0"), "Más de 20 dígitos base");

    TablaRtu tda = new();
    for (int i = 0; i < 30; i++)
        Afirmar(tda.AgregarOActualizar(new Contribuyente(i.ToString(), "Contribuyente " + i)), "TDA inserción " + i);
    Afirmar(tda.Cantidad == 30, "Ampliación dinámica del TDA");
    Afirmar(!tda.AgregarOActualizar(new Contribuyente("12", "Nombre modificado")), "TDA actualización sin duplicar");
    Afirmar(tda.Cantidad == 30 && tda.Buscar("12")?.Nombre == "Nombre modificado", "TDA consulta y tamaño");

    ArchivoRtu archivo = new(Path.Combine(carpeta, "rtu.json"));
    ServicioRtu servicio = new(archivo);
    RespuestaRtu inicial = servicio.Importar(new List<ContribuyenteEntrada?>
    {
        new() { Nit = "7378106", Nombre = "Empresa ABC, S.A." },
        new() { Nit = "8338817", Nombre = "Comercial XYZ, S.A." },
        new() { Nit = "5123456", Nombre = "NIT inválido del ejemplo" }
    });
    Afirmar(inicial.ContribuyentesNuevos == 2 && inicial.ContribuyentesActualizados == 0 && inicial.NitsInvalidos == 1,
        "Respuesta RTU: 2 nuevos, 0 actualizados, 1 inválido");
    Afirmar(servicio.Listar().Length == 2, "No se guardan NIT inválidos");
    Afirmar(File.Exists(Path.Combine(carpeta, "rtu.json")), "Persistencia en archivo");

    ServicioRtu otroProceso = new(archivo);
    Afirmar(otroProceso.Listar().Length == 2, "Se recuperan contribuyentes después de recrear el servicio");
    RespuestaRtu repetido = otroProceso.Importar(new List<ContribuyenteEntrada?>
    {
        new() { Nit = "7378106", Nombre = "Empresa ABC ACTUALIZADA" },
        new() { Nit = "6K", Nombre = "Empresa con K" }
    });
    Afirmar(repetido.ContribuyentesActualizados == 1 && repetido.ContribuyentesNuevos == 1,
        "Conteo de actualizaciones y altas");
    Afirmar(otroProceso.Buscar("7378106")?.Nombre == "Empresa ABC ACTUALIZADA", "Reemplazo de nombre");

    bool loteRechazado = false;
    try
    {
        otroProceso.Importar(new List<ContribuyenteEntrada?>
        {
            new() { Nit = "1234560", Nombre = "Nombre correcto" },
            new() { Nit = "7358", Nombre = null }
        });
    }
    catch (ArgumentException) { loteRechazado = true; }
    Afirmar(loteRechazado && otroProceso.Buscar("1234560") is null, "Lote inválido no se guarda parcialmente");

    otroProceso.Reiniciar();
    Afirmar(new ServicioRtu(archivo).Listar().Length == 0, "Inicialización también limpia datos persistidos");

    Console.WriteLine("\nPRUEBAS RELEASE 2 — FACTURAS DTE");
    ServicioRtu rtuDte = new(new ArchivoRtu(Path.Combine(carpeta, "rtu_dte.json")));
    rtuDte.Importar(new List<ContribuyenteEntrada?>
    {
        new() { Nit = "7378106", Nombre = "Empresa ABC" },
        new() { Nit = "8338817", Nombre = "Empresa XYZ" }
    });
    ValidadorFactura validadorFactura = new();
    Afirmar(validadorFactura.IntentarExtraerFecha("Guatemala, 15/01/2025 15:25 hrs.", out DateTime fechaValida)
        && fechaValida.Year == 2025 && fechaValida.Hour == 15, "Extraer fecha/hora del ejemplo");
    Afirmar(validadorFactura.IntentarExtraerFecha("Quetzaltenango / extra 31/12/2024 23:59 hrs.", out _),
        "Ignorar texto adicional al extraer fecha");
    Afirmar(!validadorFactura.IntentarExtraerFecha("Guatemala, 15/01/2025 24:00", out _), "Rechazar hora 24");
    Afirmar(!validadorFactura.IntentarExtraerFecha("Guatemala, 31/02/2025 13:00", out _), "Rechazar día inexistente");
    Afirmar(!validadorFactura.IntentarExtraerFecha("Guatemala, 15/01/2025 13:60", out _), "Rechazar minutos 60");

    DateTime reloj = new(2026, 10, 21, 18, 0, 0);
    ArchivoFacturas archivoDte = new(Path.Combine(carpeta, "solicitudes.json"));
    ServicioFacturas dte = new(archivoDte, rtuDte, () => reloj);
    FacturaEntrada Nueva(string referencia, decimal valor = 100m, decimal iva = 12m, decimal total = 112m)
        => new()
        {
            Tiempo = "Guatemala, 15/01/2025 15:25 hrs.", Referencia = referencia,
            NitEmisor = "7378106", NitReceptor = "8338817",
            Valor = valor, Iva = iva, Total = total
        };

    RespuestaAutorizaciones primeras = dte.Procesar(new List<FacturaEntrada?>
    {
        Nueva("A1990"), Nueva("A1991", 200m, 24m, 224m)
    });
    ResultadoAutorizaciones primera = primeras.ResultadoAutorizaciones;
    Afirmar(primera.CantidadAutorizacionesAprobadas == 2, "Dos facturas del ejemplo aprobadas");
    Afirmar(primera.CantidadAutorizacionesRechazadasPorNitEmisorInexistente == 0 &&
        primera.CantidadAutorizacionesRechazadasPorNitReceptorInexistente == 0,
        "Sin rechazos por NIT inexistente");
    Afirmar(primera.DetalleAprobaciones.Count == 2 && primera.DetalleAprobaciones[0].Referencia == "A1990",
        "Respuesta incluye referencias aprobadas");
    Afirmar(primera.DetalleAprobaciones[0].CodigoAprobacion == 2026102100000001L &&
        primera.DetalleAprobaciones[1].CodigoAprobacion == 2026102100000002L,
        "Fecha de aprobación actual y correlativo 1, 2");
    Afirmar(dte.Cantidad == 2 && File.Exists(Path.Combine(carpeta, "solicitudes.json")),
        "Persistencia de ambas solicitudes");

    RespuestaAutorizaciones rechazadas = dte.Procesar(new List<FacturaEntrada?>
    {
        Nueva("A1990"),
        Nueva("ERR-IVA", 100m, 11m, 111m),
        Nueva("ERR-TOTAL", 100m, 12m, 113m),
        new() { Tiempo = "Guatemala, 15/01/2025 15:25", Referencia = "ERR-EMISOR",
            NitEmisor = "6K", NitReceptor = "8338817", Valor = 100m, Iva = 12m, Total = 112m },
        new() { Tiempo = "Guatemala, 15/01/2025 15:25", Referencia = "ERR-RECEPTOR",
            NitEmisor = "7378106", NitReceptor = "6K", Valor = 100m, Iva = 12m, Total = 112m },
        new() { Tiempo = "Guatemala, 15/01/2025 15:25", Referencia = "ERR-NIT",
            NitEmisor = "7378107", NitReceptor = "8338817", Valor = 100m, Iva = 12m, Total = 112m },
        new() { Tiempo = "Guatemala, 15/01/2025 25:25", Referencia = "ERR-FECHA",
            NitEmisor = "7378106", NitReceptor = "8338817", Valor = 100m, Iva = 12m, Total = 112m },
        Nueva("ERR-DECIMALES", 100.001m, 12m, 112m),
        Nueva("ERR-REFERENCIA", 100m, 12m, 112m)
    });
    Afirmar(rechazadas.ResultadoAutorizaciones.CantidadAutorizacionesAprobadas == 1,
        "Facturas inválidas rechazadas individualmente");
    Afirmar(rechazadas.ResultadoAutorizaciones.CantidadAutorizacionesRechazadasPorNitEmisorInexistente == 1,
        "Conteo NIT emisor inexistente");
    Afirmar(rechazadas.ResultadoAutorizaciones.CantidadAutorizacionesRechazadasPorNitReceptorInexistente == 1,
        "Conteo NIT receptor inexistente");
    Afirmar(dte.Listar().Any(f => f.Referencia == "A1990" && f.Errores.Contains("REFERENCIA_DUPLICADA")),
        "Referencia duplicada rechazada");
    Afirmar(dte.Listar().Any(f => f.Referencia == "ERR-IVA" && f.Errores.Contains("IVA_MAL_CALCULADO")),
        "IVA mal calculado marcado");
    Afirmar(dte.Listar().Any(f => f.Referencia == "ERR-TOTAL" && f.Errores.Contains("TOTAL_MAL_CALCULADO")),
        "Total mal calculado marcado");
    Afirmar(dte.Listar().Any(f => f.Referencia == "ERR-NIT" && f.Errores.Contains("NIT_EMISOR_INVALIDO")),
        "NIT emisor mal formado detectado");
    Afirmar(dte.Listar().Any(f => f.Referencia == "ERR-FECHA" && f.Errores.Contains("FECHA_INVALIDA")),
        "Fecha fuera de horario detectada");
    Afirmar(dte.Listar().Any(f => f.Referencia == "ERR-DECIMALES" && f.Errores.Contains("VALOR_INVALIDO")),
        "Valor con más de dos decimales rechazado");
    Afirmar(dte.Cantidad == 11, "Se guardan también las solicitudes rechazadas");
    Afirmar(dte.Listar().First().Referencia == "ERR-REFERENCIA", "Historial descendente");

    ServicioFacturas recuperado = new(archivoDte, rtuDte, () => reloj);
    Afirmar(recuperado.Cantidad == 11, "Recuperación de solicitudes persistidas");
    RespuestaAutorizaciones continuacion = recuperado.Procesar(new List<FacturaEntrada?> { Nueva("CONTINUACION") });
    Afirmar(continuacion.ResultadoAutorizaciones.DetalleAprobaciones[0].CodigoAprobacion == 2026102100000004L,
        "Correlativo continúa tras reinicio de servicio sin repetir códigos");
    reloj = new DateTime(2026, 10, 22, 8, 0, 0);
    RespuestaAutorizaciones diaSiguiente = recuperado.Procesar(new List<FacturaEntrada?> { Nueva("DIA-SIGUIENTE") });
    Afirmar(diaSiguiente.ResultadoAutorizaciones.DetalleAprobaciones[0].CodigoAprobacion == 2026102200000001L,
        "Correlativo reinicia en uno al cambiar el día");
    bool nulosRechazados = false;
    try { recuperado.Procesar(new List<FacturaEntrada?> { Nueva("NO-GUARDAR"), null }); }
    catch (ArgumentException) { nulosRechazados = true; }
    Afirmar(nulosRechazados && recuperado.Cantidad == 13, "Lote con elemento nulo no se guarda parcialmente");
    recuperado.Reiniciar();
    Afirmar(new ServicioFacturas(archivoDte, rtuDte, () => reloj).Cantidad == 0,
        "Inicialización borra las solicitudes persistidas");
    Afirmar(rtuDte.Listar().Length == 2, "Reset de DTE por separado no altera el RTU");

    ServicioFacturas loteMixto = new(archivoDte, rtuDte, () => reloj);
    RespuestaAutorizaciones mismoLote = loteMixto.Procesar(new List<FacturaEntrada?>
    {
        Nueva("REPETIDA"), Nueva("REPETIDA"), Nueva(new string('X', 41)),
        Nueva("CERO", 0m, 0m, 0m), Nueva("REDONDEO", 0.04m, 0m, 0.04m)
    });
    Afirmar(mismoLote.ResultadoAutorizaciones.CantidadAutorizacionesAprobadas == 2,
        "Duplicados internos del lote y números inválidos no se aprueban");
    Afirmar(loteMixto.Listar().Any(f => f.Errores.Contains("REFERENCIA_DUPLICADA")),
        "Se detecta duplicado dentro de un mismo lote");
    Afirmar(loteMixto.Listar().Any(f => f.Errores.Contains("REFERENCIA_INVALIDA")),
        "Referencias de más de 40 posiciones rechazadas");
    Afirmar(loteMixto.Listar().Any(f => f.Errores.Contains("VALOR_INVALIDO")),
        "Valor cero rechazado");
    Afirmar(mismoLote.ResultadoAutorizaciones.DetalleAprobaciones[1].Referencia == "REDONDEO",
        "Cálculo IVA 0.04 * 12% redondea a 0.00");
    loteMixto.Reiniciar();


    Console.WriteLine($"\nRESULTADO: {pasadas} verificaciones correctas. Releases 1 y 2 OK.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("\nERROR DE VERIFICACIÓN: " + ex);
    return 1;
}
finally
{
    if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
}
