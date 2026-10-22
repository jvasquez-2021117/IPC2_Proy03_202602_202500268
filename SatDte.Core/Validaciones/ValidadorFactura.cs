using System.Globalization;
using System.Text.RegularExpressions;
using SatDte.Core.Modelos;

namespace SatDte.Core.Validaciones;

public sealed class ValidadorFactura
{
    // Buscar solo fecha y hora; el enunciado indica ignorar lugar y texto extra.
    private static readonly Regex FechaHora = new(
        @"(?<!\d)(?<fecha>\d{2}/\d{2}/\d{4})\s+(?<hora>(?:[01]\d|2[0-3]):[0-5]\d)(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ValidadorNit _nit = new();

    public bool IntentarExtraerFecha(string? tiempo, out DateTime fecha)
    {
        fecha = default;
        if (tiempo is null) return false;
        Match m = FechaHora.Match(tiempo);
        return m.Success && DateTime.TryParseExact(
            m.Groups["fecha"].Value + " " + m.Groups["hora"].Value,
            "dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
    }

    public string[] Revisar(FacturaEntrada factura, bool emisorExiste, bool receptorExiste, bool duplicada)
    {
        List<string> errores = new();
        if (!IntentarExtraerFecha(factura.Tiempo, out _)) errores.Add("FECHA_INVALIDA");
        if (string.IsNullOrWhiteSpace(factura.Referencia) || factura.Referencia.Length > 40)
            errores.Add("REFERENCIA_INVALIDA");
        if (duplicada) errores.Add("REFERENCIA_DUPLICADA");

        bool emisorValido = _nit.EsValido(factura.NitEmisor);
        bool receptorValido = _nit.EsValido(factura.NitReceptor);
        if (!emisorValido) errores.Add("NIT_EMISOR_INVALIDO");
        else if (!emisorExiste) errores.Add("NIT_EMISOR_INEXISTENTE");
        if (!receptorValido) errores.Add("NIT_RECEPTOR_INVALIDO");
        else if (!receptorExiste) errores.Add("NIT_RECEPTOR_INEXISTENTE");

        if (factura.Valor is null || factura.Valor <= 0 || !TieneDosDecimalesComoMaximo(factura.Valor.Value))
        {
            errores.Add("VALOR_INVALIDO");
        }
        else
        {
            decimal ivaEsperado = decimal.Round(factura.Valor.Value * 0.12m, 2, MidpointRounding.AwayFromZero);
            if (factura.Iva is null || factura.Iva != ivaEsperado || !TieneDosDecimalesComoMaximo(factura.Iva.Value))
                errores.Add("IVA_MAL_CALCULADO");
            if (factura.Total is null || factura.Iva is null || factura.Total != factura.Valor.Value + factura.Iva.Value ||
                !TieneDosDecimalesComoMaximo(factura.Total.Value))
                errores.Add("TOTAL_MAL_CALCULADO");
        }
        return errores.ToArray();
    }

    private static bool TieneDosDecimalesComoMaximo(decimal numero)
    {
        int[] bits = decimal.GetBits(numero);
        return ((bits[3] >> 16) & 0xff) <= 2;
    }
}
