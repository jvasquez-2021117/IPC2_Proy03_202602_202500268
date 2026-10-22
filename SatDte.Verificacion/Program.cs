using System.Text;
using SatDte.Core.Exportacion;
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


    Console.WriteLine("\nPRUEBAS RELEASE 3 — REPORTES Y CONSULTAS");
    ArchivoRtu archivoRtuReportes = new(Path.Combine(carpeta, "rtu_reportes.json"));
    ServicioRtu rtuReportes = new(archivoRtuReportes);
    rtuReportes.Importar(new List<ContribuyenteEntrada?>
    {
        new() { Nit = "7378106", Nombre = "Empresa ABC" },
        new() { Nit = "8338817", Nombre = "Empresa XYZ" }
    });
    ArchivoFacturas archivoReportes = new(Path.Combine(carpeta, "facturas_reportes.json"));
    ServicioFacturas servicioReportesDte = new(archivoReportes, rtuReportes, () => new DateTime(2026, 10, 21, 9, 0, 0));
    FacturaEntrada FacturaReporte(string tiempo, string referencia, string emisor, string receptor,
        decimal valor, decimal iva, decimal total) => new()
        { Tiempo = "Guatemala, " + tiempo + " hrs.", Referencia = referencia,
          NitEmisor = emisor, NitReceptor = receptor, Valor = valor, Iva = iva, Total = total };
    var loteReportes = new List<FacturaEntrada?>
    {
        FacturaReporte("15/01/2025 09:00", "R3-001", "7378106", "8338817", 100m, 12m, 112m),
        FacturaReporte("15/01/2025 10:30", "R3-002", "7378106", "8338817", 200m, 24m, 224m),
        FacturaReporte("15/01/2025 11:15", "R3-003", "8338817", "7378106", 50m, 6m, 56m),
        FacturaReporte("16/01/2025 12:45", "R3-004", "7378106", "8338817", 75m, 9m, 84m),
        FacturaReporte("16/01/2025 13:45", "R3-ERROR-IVA", "7378106", "8338817", 100m, 11m, 111m),
        FacturaReporte("16/01/2025 14:10", "R3-ERROR-TOTAL", "7378106", "8338817", 100m, 12m, 113m)
    };
    RespuestaAutorizaciones autorizacionesR3 = servicioReportesDte.Procesar(loteReportes);
    Afirmar(autorizacionesR3.ResultadoAutorizaciones.CantidadAutorizacionesAprobadas == 4,
        "Cuatro facturas aprobadas para los nuevos reportes");
    Afirmar(servicioReportesDte.Cantidad == 6, "Se almacenan cuatro aprobaciones y dos rechazos");

    DateOnly quince = new(2025, 1, 15);
    DateOnly dieciseis = new(2025, 1, 16);
    ServicioReportes reportes = new(servicioReportesDte, rtuReportes);
    ReporteContribuyente emitidas = reportes.ConsultarContribuyente("7378106", true, quince, dieciseis);
    Afirmar(emitidas.CantidadTotalFacturas == 3 && emitidas.MontoTotalFacturado == 420m,
        "Emitidas del contribuyente: 3 documentos y 420.00");
    Afirmar(emitidas.Dias.Count == 2 && emitidas.Dias[0].CantidadFacturas == 2 &&
        emitidas.Dias[0].MontoFacturado == 336m && emitidas.Dias[1].MontoFacturado == 84m,
        "Emitidas agrupadas correctamente por fecha del DTE");
    Afirmar(emitidas.Facturas.Count == 3 && emitidas.Facturas[0].CodigoAprobacion != 0,
        "Consulta presenta listado detallado con código de autorización");
    ReporteContribuyente recibidas = reportes.ConsultarContribuyente("7378106", false, quince, dieciseis);
    Afirmar(recibidas.CantidadTotalFacturas == 1 && recibidas.MontoTotalFacturado == 56m,
        "Recibidas del mismo contribuyente: 1 documento y 56.00");
    Afirmar(recibidas.Facturas[0].NitContraparte == "8338817", "NIT contraparte en recibidas");
    ReporteContribuyente soloDia = reportes.ConsultarContribuyente("7378106", true, quince, quince);
    Afirmar(soloDia.CantidadTotalFacturas == 2 && soloDia.MontoTotalFacturado == 336m,
        "Extremos de rango inclusivos en consulta por NIT");

    ResumenIvaFecha ivaQuince = reportes.ConsultarIva(quince);
    MovimientoIvaNit ivaA = ivaQuince.Movimientos.Single(x => x.Nit == "7378106");
    MovimientoIvaNit ivaB = ivaQuince.Movimientos.Single(x => x.Nit == "8338817");
    Afirmar(ivaQuince.Movimientos.Count == 2 && ivaA.IvaEmitido == 36m && ivaA.IvaRecibido == 6m,
        "IVA emitido y recibido para el NIT A");
    Afirmar(ivaA.Diferencia == 30m && ivaB.IvaEmitido == 6m && ivaB.IvaRecibido == 36m &&
        ivaB.Diferencia == -30m, "IVA y saldo del NIT B");
    Afirmar(ivaQuince.TotalIvaEmitido == 42m && ivaQuince.TotalIvaRecibido == 42m,
        "Conservación del IVA global emitido y recibido");
    ResumenIvaFecha ivaDieciseis = reportes.ConsultarIva(dieciseis);
    Afirmar(ivaDieciseis.TotalIvaEmitido == 9m && ivaDieciseis.TotalIvaRecibido == 9m,
        "DTE rechazados no contaminan IVA del día siguiente");

    ResumenRangoFechas montosTotales = reportes.ConsultarRango(quince, dieciseis, false);
    ResumenRangoFechas montosSinIva = reportes.ConsultarRango(quince, dieciseis, true);
    Afirmar(montosTotales.CantidadTotalFacturas == 4 && montosTotales.MontoTotalAutorizado == 476m,
        "Total autorizado con IVA: 476.00 y 4 facturas");
    Afirmar(montosTotales.Dias.Count == 2 && montosTotales.Dias[0].MontoAutorizado == 392m &&
        montosTotales.Dias[1].MontoAutorizado == 84m, "Total por día con IVA");
    Afirmar(montosSinIva.MontoTotalAutorizado == 425m && montosSinIva.Dias[0].MontoAutorizado == 350m &&
        montosSinIva.Dias[1].MontoAutorizado == 75m, "Valor autorizado sin IVA por día y global");
    Afirmar(montosTotales.MontoTotalAutorizado - montosSinIva.MontoTotalAutorizado == 51m,
        "Diferencia del período equivalente al IVA aprobado");

    ReporteRecepciones auditoria = reportes.ConsultarRecepciones(quince, dieciseis);
    Afirmar(auditoria.TotalRecibidas == 6 && auditoria.TotalSinErrores == 4,
        "Auditoría cuenta intentos y aprobaciones");
    Afirmar(auditoria.Dias.Count == 2 && auditoria.Dias[0].CantidadTotalFacturasRecibidas == 3 &&
        auditoria.Dias[0].CantidadFacturasSinErrores == 3,
        "Fecha 15: tres recibidas sin errores");
    Afirmar(auditoria.Dias[0].CantidadEmisoresDistintos == 2 &&
        auditoria.Dias[0].CantidadReceptoresDistintos == 2,
        "NIT emisores/receptores distintos por fecha");
    Afirmar(auditoria.Dias[1].CantidadTotalFacturasRecibidas == 3 &&
        auditoria.Dias[1].CantidadFacturasSinErrores == 1 &&
        auditoria.Dias[1].Errores.IvaMalCalculado == 1 &&
        auditoria.Dias[1].Errores.TotalMalCalculado == 1,
        "Fecha 16: un aprobado, un error IVA y un error TOTAL");

    ServicioFacturas recuperadoReportes = new(archivoReportes, rtuReportes, () => new DateTime(2026, 10, 22));
    ServicioReportes reportePersistido = new(recuperadoReportes, rtuReportes);
    Afirmar(reportePersistido.ConsultarRango(quince, dieciseis, false).MontoTotalAutorizado == 476m,
        "Reporte idéntico tras recuperar el almacenamiento persistente");
    Afirmar(reportePersistido.ConsultarRango(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31), false)
        .Dias.Count == 0, "Rango sin movimientos regresa lista vacía");
    bool nitDesconocido = false;
    try { reportePersistido.ConsultarContribuyente("6K", true, quince, dieciseis); }
    catch (KeyNotFoundException) { nitDesconocido = true; }
    Afirmar(nitDesconocido, "NIT no registrado genera respuesta controlable para la API");
    bool rangoInvertido = false;
    try { reportePersistido.ConsultarRango(dieciseis, quince, false); }
    catch (ArgumentException) { rangoInvertido = true; }
    Afirmar(rangoInvertido, "Rangos invertidos son rechazados");

    // Un documento con fecha inválida cuenta para la auditoría en su fecha de recepción local.
    ArchivoFacturas archivoFechaInvalida = new(Path.Combine(carpeta, "invalida_fecha.json"));
    archivoFechaInvalida.Guardar(new[]
    {
        new RegistroFactura
        {
            NumeroSolicitud = 1, FechaDocumento = null,
            FechaRecepcionUtc = new DateTime(2025, 1, 16, 2, 0, 0, DateTimeKind.Utc),
            Referencia = "INVALIDA-FECHA", Aprobada = false,
            Errores = new[] { "FECHA_INVALIDA" }
        }
    });
    ServicioReportes fechasInvalidas = new(new ServicioFacturas(archivoFechaInvalida, rtuReportes), rtuReportes);
    ReporteRecepciones auditoriaFechaInvalida = fechasInvalidas.ConsultarRecepciones(quince, quince);
    Afirmar(auditoriaFechaInvalida.TotalRecibidas == 1 &&
        auditoriaFechaInvalida.Dias[0].Errores.FechaInvalida == 1,
        "Fechas DTE inválidas se auditan según fecha de recepción Guatemala");
    Console.WriteLine("\nPRUEBAS RELEASE 4 — EXPORTACION PDF");
    GeneradorReportesPdf exportador = new();
    byte[] pdfContribuyente = exportador.Contribuyente(emitidas);
    byte[] pdfIva = exportador.Iva(ivaQuince);
    byte[] pdfRango = exportador.Rango(montosTotales);
    byte[] pdfSinIva = exportador.Rango(montosSinIva);
    byte[] pdfRecepciones = exportador.Recepciones(auditoria);

    bool EsPdf(byte[] documento) => documento.Length > 800 &&
        Encoding.ASCII.GetString(documento, 0, 5) == "%PDF-" &&
        Encoding.ASCII.GetString(documento).Contains("startxref") &&
        Encoding.ASCII.GetString(documento).Contains("%%EOF");
    Afirmar(EsPdf(pdfContribuyente), "PDF valido para facturas por contribuyente");
    Afirmar(EsPdf(pdfIva), "PDF valido para resumen de IVA");
    Afirmar(EsPdf(pdfRango), "PDF valido para resumen por rango de fechas");
    Afirmar(EsPdf(pdfSinIva), "PDF valido para resumen sin IVA");
    Afirmar(EsPdf(pdfRecepciones), "PDF valido para auditoria de recepciones");
    Afirmar(Encoding.Latin1.GetString(pdfContribuyente).Contains("R3-001") &&
        Encoding.Latin1.GetString(pdfContribuyente).Contains("420.00"),
        "PDF del contribuyente incluye referencias y total facturado");
    Afirmar(Encoding.Latin1.GetString(pdfIva).Contains("IVA emitido") &&
        Encoding.Latin1.GetString(pdfIva).Contains("36.00"),
        "PDF de IVA presenta indicadores reales");
    Afirmar(Encoding.Latin1.GetString(pdfRango).Contains("476.00") &&
        Encoding.Latin1.GetString(pdfSinIva).Contains("425.00"),
        "PDF refleja el filtro seleccionado con y sin IVA");
    Afirmar(Encoding.Latin1.GetString(pdfRecepciones).Contains("IVA mal calculado") &&
        Encoding.Latin1.GetString(pdfRecepciones).Contains("TOTAL mal calculado"),
        "PDF de recepciones incluye ambos errores de calculo");
    ReporteContribuyente reporteLargo = new()
    {
        Nit = "7378106", Nombre = "Empresa ABC", Tipo = "emitidas",
        FechaInicio = quince, FechaFin = dieciseis,
        Facturas = Enumerable.Range(1, 135).Select(i => new FacturaConsultada
        {
            Fecha = quince, Referencia = "MASIVA-" + i.ToString("D3"),
            NitContraparte = "8338817", CodigoAprobacion = 2026102100000001 + i,
            Valor = 100m, Iva = 12m, Total = 112m
        }).ToList()
    };
    byte[] pdfLargo = exportador.Contribuyente(reporteLargo);
    Afirmar(EsPdf(pdfLargo) && Encoding.Latin1.GetString(pdfLargo).Contains("MASIVA-135") &&
        Encoding.Latin1.GetString(pdfLargo).Contains("continuacion"),
        "Tablas PDF extensas se distribuyen en varias paginas");
    Afirmar(EsPdf(exportador.Iva(new ResumenIvaFecha { Fecha = quince })) &&
        EsPdf(exportador.Rango(new ResumenRangoFechas { FechaInicio = quince, FechaFin = dieciseis })),
        "Los PDF tambien funcionan sin movimientos");

    recuperadoReportes.Reiniciar();
    Afirmar(reportePersistido.ConsultarRango(quince, dieciseis, false).MontoTotalAutorizado == 0m,
        "Reportes cambian al inicializar las solicitudes");

    Console.WriteLine($"\nRESULTADO: {pasadas} verificaciones correctas. Releases 1, 2, 3 y 4 OK.");
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
