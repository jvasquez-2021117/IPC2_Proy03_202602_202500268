using SatDte.Core.Modelos;

namespace SatDte.Core.Servicios;

/// <summary>
/// Agrega las solicitudes persistidas. Los importes de las consultas financieras usan
/// únicamente facturas aprobadas; el reporte de recepción incluye también rechazos.
/// Para agrupar se utiliza la fecha del DTE, no la del código de autorización.
/// </summary>
public sealed class ServicioReportes
{
    private readonly ServicioFacturas _facturas;
    private readonly ServicioRtu _rtu;

    public ServicioReportes(ServicioFacturas facturas, ServicioRtu rtu)
    {
        _facturas = facturas;
        _rtu = rtu;
    }

    public ReporteContribuyente ConsultarContribuyente(string nit, bool emitidas, DateOnly inicio, DateOnly fin)
    {
        ValidarRango(inicio, fin);
        if (string.IsNullOrWhiteSpace(nit)) throw new ArgumentException("Debes indicar un NIT.");
        Contribuyente? contribuyente = _rtu.Buscar(nit);
        if (contribuyente is null) throw new KeyNotFoundException("El NIT no existe en el RTU.");
        ReporteContribuyente respuesta = new()
        {
            Nit = nit, Nombre = contribuyente.Nombre, Tipo = emitidas ? "emitidas" : "recibidas",
            FechaInicio = inicio, FechaFin = fin
        };
        Dictionary<DateOnly, MovimientoDiarioContribuyente> acumulados = new();
        foreach (RegistroFactura registro in _facturas.ObtenerTodos())
        {
            if (!EstaAutorizada(registro) || registro.FechaDocumento is null) continue;
            DateOnly fecha = DateOnly.FromDateTime(registro.FechaDocumento.Value);
            if (fecha < inicio || fecha > fin) continue;
            if (emitidas ? registro.NitEmisor != nit : registro.NitReceptor != nit) continue;

            decimal importe = registro.Total!.Value;
            if (!acumulados.TryGetValue(fecha, out MovimientoDiarioContribuyente? dia))
            {
                dia = new MovimientoDiarioContribuyente { Fecha = fecha };
                acumulados[fecha] = dia;
            }
            dia.CantidadFacturas++;
            dia.MontoFacturado += importe;
            respuesta.Facturas.Add(new FacturaConsultada
            {
                Fecha = fecha, Referencia = registro.Referencia!,
                NitContraparte = emitidas ? registro.NitReceptor! : registro.NitEmisor!,
                CodigoAprobacion = registro.CodigoAprobacion!.Value,
                Valor = registro.Valor!.Value, Iva = registro.Iva!.Value, Total = importe
            });
        }
        respuesta.Dias = acumulados.Values.OrderBy(x => x.Fecha).ToList();
        respuesta.Facturas = respuesta.Facturas.OrderBy(x => x.Fecha).ThenBy(x => x.Referencia).ToList();
        respuesta.CantidadTotalFacturas = respuesta.Facturas.Count;
        respuesta.MontoTotalFacturado = respuesta.Facturas.Sum(x => x.Total);
        return respuesta;
    }

    public ResumenIvaFecha ConsultarIva(DateOnly fecha)
    {
        Dictionary<string, MovimientoIvaNit> acumulados = new(StringComparer.Ordinal);
        foreach (RegistroFactura registro in _facturas.ObtenerTodos())
        {
            if (!EstaAutorizada(registro) || registro.FechaDocumento is null ||
                DateOnly.FromDateTime(registro.FechaDocumento.Value) != fecha) continue;
            decimal iva = registro.Iva!.Value;
            string emisor = registro.NitEmisor!;
            string receptor = registro.NitReceptor!;
            ObtenerMovimiento(acumulados, emisor).IvaEmitido += iva;
            ObtenerMovimiento(acumulados, receptor).IvaRecibido += iva;
        }
        ResumenIvaFecha resultado = new() { Fecha = fecha };
        foreach (MovimientoIvaNit movimiento in acumulados.Values.OrderBy(x => x.Nit, StringComparer.Ordinal))
        {
            movimiento.Diferencia = movimiento.IvaEmitido - movimiento.IvaRecibido;
            resultado.Movimientos.Add(movimiento);
        }
        resultado.TotalIvaEmitido = resultado.Movimientos.Sum(x => x.IvaEmitido);
        resultado.TotalIvaRecibido = resultado.Movimientos.Sum(x => x.IvaRecibido);
        return resultado;
    }

    public ResumenRangoFechas ConsultarRango(DateOnly inicio, DateOnly fin, bool sinIva)
    {
        ValidarRango(inicio, fin);
        Dictionary<DateOnly, MovimientoDiarioAutorizado> acumulados = new();
        foreach (RegistroFactura registro in _facturas.ObtenerTodos())
        {
            if (!EstaAutorizada(registro) || registro.FechaDocumento is null) continue;
            DateOnly fecha = DateOnly.FromDateTime(registro.FechaDocumento.Value);
            if (fecha < inicio || fecha > fin) continue;
            if (!acumulados.TryGetValue(fecha, out MovimientoDiarioAutorizado? dia))
            {
                dia = new MovimientoDiarioAutorizado { Fecha = fecha };
                acumulados[fecha] = dia;
            }
            dia.CantidadFacturas++;
            dia.MontoAutorizado += sinIva ? registro.Valor!.Value : registro.Total!.Value;
        }
        ResumenRangoFechas resultado = new()
        {
            FechaInicio = inicio, FechaFin = fin,
            Modo = sinIva ? "siniva" : "total",
            Dias = acumulados.Values.OrderBy(x => x.Fecha).ToList()
        };
        resultado.CantidadTotalFacturas = resultado.Dias.Sum(x => x.CantidadFacturas);
        resultado.MontoTotalAutorizado = resultado.Dias.Sum(x => x.MontoAutorizado);
        return resultado;
    }

    public ReporteRecepciones ConsultarRecepciones(DateOnly inicio, DateOnly fin)
    {
        ValidarRango(inicio, fin);
        Dictionary<DateOnly, RecepcionDiaria> dias = new();
        Dictionary<DateOnly, HashSet<string>> emisores = new();
        Dictionary<DateOnly, HashSet<string>> receptores = new();
        foreach (RegistroFactura registro in _facturas.ObtenerTodos())
        {
            // Una fecha del DTE inválida se contabiliza el día de recepción en Guatemala.
            DateOnly fecha = registro.FechaDocumento is not null
                ? DateOnly.FromDateTime(registro.FechaDocumento.Value)
                : DiaDeRecepcionGuatemala(registro.FechaRecepcionUtc);
            if (fecha < inicio || fecha > fin) continue;
            if (!dias.TryGetValue(fecha, out RecepcionDiaria? dia))
            {
                dia = new RecepcionDiaria { Fecha = fecha };
                dias[fecha] = dia;
                emisores[fecha] = new HashSet<string>(StringComparer.Ordinal);
                receptores[fecha] = new HashSet<string>(StringComparer.Ordinal);
            }
            dia.CantidadTotalFacturasRecibidas++;
            if (EstaAutorizada(registro))
            {
                dia.CantidadFacturasSinErrores++;
                emisores[fecha].Add(registro.NitEmisor!);
                receptores[fecha].Add(registro.NitReceptor!);
            }
            ConteoErroresFacturas errores = dia.Errores;
            foreach (string codigo in registro.Errores ?? Array.Empty<string>())
            {
                switch (codigo)
                {
                    case "NIT_EMISOR_INVALIDO": errores.NitEmisorInvalido++; break;
                    case "NIT_RECEPTOR_INVALIDO": errores.NitReceptorInvalido++; break;
                    case "NIT_EMISOR_INEXISTENTE": errores.NitEmisorInexistente++; break;
                    case "NIT_RECEPTOR_INEXISTENTE": errores.NitReceptorInexistente++; break;
                    case "IVA_MAL_CALCULADO": errores.IvaMalCalculado++; break;
                    case "TOTAL_MAL_CALCULADO": errores.TotalMalCalculado++; break;
                    case "REFERENCIA_DUPLICADA": errores.ReferenciaDuplicada++; break;
                    case "FECHA_INVALIDA": errores.FechaInvalida++; break;
                    case "REFERENCIA_INVALIDA": errores.ReferenciaInvalida++; break;
                    case "VALOR_INVALIDO": errores.ValorInvalido++; break;
                }
            }
        }
        foreach (KeyValuePair<DateOnly, RecepcionDiaria> elemento in dias)
        {
            elemento.Value.CantidadEmisoresDistintos = emisores[elemento.Key].Count;
            elemento.Value.CantidadReceptoresDistintos = receptores[elemento.Key].Count;
        }
        ReporteRecepciones reporte = new()
        {
            FechaInicio = inicio, FechaFin = fin,
            Dias = dias.Values.OrderBy(x => x.Fecha).ToList()
        };
        reporte.TotalRecibidas = reporte.Dias.Sum(x => x.CantidadTotalFacturasRecibidas);
        reporte.TotalSinErrores = reporte.Dias.Sum(x => x.CantidadFacturasSinErrores);
        return reporte;
    }

    private MovimientoIvaNit ObtenerMovimiento(Dictionary<string, MovimientoIvaNit> tabla, string nit)
    {
        if (!tabla.TryGetValue(nit, out MovimientoIvaNit? movimiento))
        {
            movimiento = new MovimientoIvaNit { Nit = nit, Nombre = _rtu.Buscar(nit)?.Nombre ?? nit };
            tabla.Add(nit, movimiento);
        }
        return movimiento;
    }

    private static bool EstaAutorizada(RegistroFactura registro) =>
        registro.Aprobada && registro.CodigoAprobacion.HasValue &&
        registro.Valor.HasValue && registro.Iva.HasValue && registro.Total.HasValue;

    private static void ValidarRango(DateOnly inicio, DateOnly fin)
    {
        if (inicio > fin) throw new ArgumentException("La fecha inicial no puede ser posterior a la fecha final.");
    }

    private static DateOnly DiaDeRecepcionGuatemala(DateTime utc)
    {
        string nombre = OperatingSystem.IsWindows() ? "Central America Standard Time" : "America/Guatemala";
        DateTime fechaUtc = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        DateTime local = TimeZoneInfo.ConvertTimeFromUtc(fechaUtc, TimeZoneInfo.FindSystemTimeZoneById(nombre));
        return DateOnly.FromDateTime(local);
    }
}
