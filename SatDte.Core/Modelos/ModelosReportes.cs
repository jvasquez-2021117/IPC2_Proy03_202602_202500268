namespace SatDte.Core.Modelos;

/// <summary>Factura autorizada con la información necesaria para una consulta tributaria.</summary>
public sealed class FacturaConsultada
{
    public DateOnly Fecha { get; set; }
    public string Referencia { get; set; } = string.Empty;
    public string NitContraparte { get; set; } = string.Empty;
    public long CodigoAprobacion { get; set; }
    public decimal Valor { get; set; }
    public decimal Iva { get; set; }
    public decimal Total { get; set; }
}

public sealed class MovimientoDiarioContribuyente
{
    public DateOnly Fecha { get; set; }
    public int CantidadFacturas { get; set; }
    public decimal MontoFacturado { get; set; }
}

public sealed class ReporteContribuyente
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public List<MovimientoDiarioContribuyente> Dias { get; set; } = new();
    public List<FacturaConsultada> Facturas { get; set; } = new();
    public int CantidadTotalFacturas { get; set; }
    public decimal MontoTotalFacturado { get; set; }
}

public sealed class MovimientoIvaNit
{
    public string Nit { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal IvaEmitido { get; set; }
    public decimal IvaRecibido { get; set; }
    public decimal Diferencia { get; set; }
}

public sealed class ResumenIvaFecha
{
    public DateOnly Fecha { get; set; }
    public List<MovimientoIvaNit> Movimientos { get; set; } = new();
    public decimal TotalIvaEmitido { get; set; }
    public decimal TotalIvaRecibido { get; set; }
}

public sealed class MovimientoDiarioAutorizado
{
    public DateOnly Fecha { get; set; }
    public int CantidadFacturas { get; set; }
    public decimal MontoAutorizado { get; set; }
}

public sealed class ResumenRangoFechas
{
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string Modo { get; set; } = "total";
    public List<MovimientoDiarioAutorizado> Dias { get; set; } = new();
    public int CantidadTotalFacturas { get; set; }
    public decimal MontoTotalAutorizado { get; set; }
}

/// <summary>Cuentas de errores requeridas por el reporte diario del enunciado.</summary>
public sealed class ConteoErroresFacturas
{
    public int NitEmisorInvalido { get; set; }
    public int NitReceptorInvalido { get; set; }
    public int NitEmisorInexistente { get; set; }
    public int NitReceptorInexistente { get; set; }
    public int IvaMalCalculado { get; set; }
    public int TotalMalCalculado { get; set; }
    public int ReferenciaDuplicada { get; set; }
    public int FechaInvalida { get; set; }
    public int ReferenciaInvalida { get; set; }
    public int ValorInvalido { get; set; }
}

public sealed class RecepcionDiaria
{
    public DateOnly Fecha { get; set; }
    public int CantidadTotalFacturasRecibidas { get; set; }
    public int CantidadFacturasSinErrores { get; set; }
    public int CantidadEmisoresDistintos { get; set; }
    public int CantidadReceptoresDistintos { get; set; }
    public ConteoErroresFacturas Errores { get; set; } = new();
}

public sealed class ReporteRecepciones
{
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public List<RecepcionDiaria> Dias { get; set; } = new();
    public int TotalRecibidas { get; set; }
    public int TotalSinErrores { get; set; }
}
