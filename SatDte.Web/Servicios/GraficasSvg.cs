using System.Globalization;
using System.Net;
using System.Text;
using SatDte.Core.Modelos;

namespace SatDte.Web.Servicios;

/// <summary>
/// Construye gráficas SVG desde resultados de la API, sin bibliotecas ni CDN.
/// Todos los textos de los datos se codifican para evitar inyección de HTML.
/// </summary>
public static class GraficasSvg
{
    private static string N(float valor) => valor.ToString("0.##", CultureInfo.InvariantCulture);
    private static string E(string valor) => WebUtility.HtmlEncode(valor);
    private const int LimiteColumnas = 30;

    public static string Iva(ResumenIvaFecha reporte)
    {
        return Construir(reporte.Movimientos.Take(LimiteColumnas)
                .Select(m => new Dato(m.Nit, m.IvaEmitido, m.IvaRecibido)).ToArray(),
            "IVA emitido", "IVA recibido", "Gráfica de IVA por contribuyente");
    }

    public static string Fechas(ResumenRangoFechas reporte)
    {
        return Construir(reporte.Dias.Take(LimiteColumnas)
                .Select(d => new Dato(d.Fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture), d.MontoAutorizado, 0m)).ToArray(),
            reporte.Modo == "siniva" ? "Monto sin IVA" : "Monto con IVA", null,
            "Gráfica de montos autorizados por fecha");
    }

    private sealed record Dato(string Etiqueta, decimal Primero, decimal Segundo);

    private static string Construir(Dato[] datos, string nombrePrimero, string? nombreSegundo, string titulo)
    {
        if (datos.Length == 0)
            return "<p class=\"chart-empty\">No existen movimientos para representar en la gráfica.</p>";

        const float izquierda = 70, arriba = 35, alto = 255, baseGrafica = arriba + alto;
        float unidad = nombreSegundo is null ? 72 : 94;
        float ancho = Math.Max(720, izquierda + 40 + unidad * datos.Length);
        float largo = ancho - izquierda - 30;
        decimal maximo = Math.Max(1, datos.Max(d => Math.Max(d.Primero, d.Segundo)));
        StringBuilder svg = new();
        svg.Append($"<svg class=\"chart-svg\" xmlns=\"http://www.w3.org/2000/svg\" role=\"img\" aria-label=\"{E(titulo)}\" viewBox=\"0 0 {N(ancho)} 366\" width=\"{N(ancho)}\" height=\"366\">");
        svg.Append("<title>").Append(E(titulo)).Append("</title>");
        svg.Append("<desc>Los valores exactos aparecen en la tabla que acompaña a esta gráfica.</desc>");
        for (int i = 0; i <= 5; i++)
        {
            float y = arriba + i * alto / 5f;
            decimal valor = maximo * (5 - i) / 5m;
            svg.Append($"<line x1=\"{N(izquierda)}\" y1=\"{N(y)}\" x2=\"{N(izquierda + largo)}\" y2=\"{N(y)}\" stroke=\"#e5eaf3\"/>");
            svg.Append($"<text x=\"{N(izquierda - 10)}\" y=\"{N(y + 4)}\" text-anchor=\"end\" fill=\"#7b88a0\" font-size=\"11\">{E(valor.ToString("N2", CultureInfo.InvariantCulture))}</text>");
        }
        for (int i = 0; i < datos.Length; i++)
        {
            Dato dato = datos[i];
            float centro = izquierda + (i + .5f) * (largo / datos.Length);
            float anchoBarra = nombreSegundo is null ? 30 : 25;
            float h1 = (float)(dato.Primero / maximo) * alto;
            float x1 = nombreSegundo is null ? centro - anchoBarra / 2 : centro - anchoBarra - 2;
            svg.Append($"<rect x=\"{N(x1)}\" y=\"{N(baseGrafica - h1)}\" width=\"{N(anchoBarra)}\" height=\"{N(h1)}\" rx=\"3\" fill=\"#4269dd\"><title>{E(dato.Etiqueta + " - " + nombrePrimero + ": " + dato.Primero.ToString("N2", CultureInfo.InvariantCulture))}</title></rect>");
            if (nombreSegundo is not null)
            {
                float h2 = (float)(dato.Segundo / maximo) * alto;
                svg.Append($"<rect x=\"{N(centro + 2)}\" y=\"{N(baseGrafica - h2)}\" width=\"{N(anchoBarra)}\" height=\"{N(h2)}\" rx=\"3\" fill=\"#13a184\"><title>{E(dato.Etiqueta + " - " + nombreSegundo + ": " + dato.Segundo.ToString("N2", CultureInfo.InvariantCulture))}</title></rect>");
            }
            svg.Append($"<text x=\"{N(centro)}\" y=\"{N(baseGrafica + 21)}\" text-anchor=\"middle\" fill=\"#506079\" font-size=\"11\">{E(dato.Etiqueta)}</text>");
        }
        svg.Append($"<rect x=\"{N(izquierda)}\" y=\"337\" width=\"13\" height=\"10\" rx=\"2\" fill=\"#4269dd\"/>");
        svg.Append($"<text x=\"{N(izquierda + 20)}\" y=\"346\" fill=\"#56647a\" font-size=\"11\">{E(nombrePrimero)}</text>");
        if (nombreSegundo is not null)
        {
            svg.Append($"<rect x=\"{N(izquierda + 210)}\" y=\"337\" width=\"13\" height=\"10\" rx=\"2\" fill=\"#13a184\"/>");
            svg.Append($"<text x=\"{N(izquierda + 230)}\" y=\"346\" fill=\"#56647a\" font-size=\"11\">{E(nombreSegundo)}</text>");
        }
        svg.Append("</svg>");
        return svg.ToString();
    }
}
