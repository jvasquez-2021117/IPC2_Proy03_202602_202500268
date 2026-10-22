using System.Globalization;
using System.Text;
using SatDte.Core.Modelos;

namespace SatDte.Core.Exportacion;

/// <summary>
/// Exportador PDF 1.4 escrito en C# puro. No depende de herramientas externas ni de
/// un navegador: la API puede enviar documentos reales también a Postman.
/// </summary>
public sealed class GeneradorReportesPdf
{
    private static string Dinero(decimal monto) => monto.ToString("N2", CultureInfo.InvariantCulture);
    private static string Fecha(DateOnly dia) => dia.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

    public byte[] Contribuyente(ReporteContribuyente r)
    {
        DocumentoPdf pdf = new("FACTURAS POR CONTRIBUYENTE", "Documentos tributarios electronicos autorizados");
        pdf.Detalle("NIT: " + r.Nit + "  |  Nombre: " + r.Nombre);
        pdf.Detalle("Tipo: " + r.Tipo + "  |  Periodo: " + Fecha(r.FechaInicio) + " al " + Fecha(r.FechaFin));
        pdf.Resumen("Facturas autorizadas: " + r.CantidadTotalFacturas + "   |   Total facturado: " + Dinero(r.MontoTotalFacturado));
        pdf.Tabla("Resumen diario", new[] { "Fecha", "Facturas", "Monto facturado" },
            new[] { 125f, 100f, 284f }, r.Dias.Select(d => new[]
            { Fecha(d.Fecha), d.CantidadFacturas.ToString(CultureInfo.InvariantCulture), Dinero(d.MontoFacturado) }));
        pdf.Tabla("Detalle de facturas", new[] { "Fecha", "Referencia", "NIT contraparte", "Codigo", "Total" },
            new[] { 75f, 100f, 105f, 135f, 94f }, r.Facturas.Select(f => new[]
            { Fecha(f.Fecha), f.Referencia, f.NitContraparte, f.CodigoAprobacion.ToString(CultureInfo.InvariantCulture), Dinero(f.Total) }));
        return pdf.Crear();
    }

    public byte[] Iva(ResumenIvaFecha r)
    {
        DocumentoPdf pdf = new("RESUMEN DE IVA", "IVA emitido y recibido por NIT");
        pdf.Detalle("Fecha del documento: " + Fecha(r.Fecha));
        pdf.Resumen("IVA emitido: " + Dinero(r.TotalIvaEmitido) + "   |   IVA recibido: " + Dinero(r.TotalIvaRecibido));
        pdf.Grafica("Comparacion de IVA por NIT (primeros 12)",
            r.Movimientos.Take(12).Select(m => new BarraPdf(m.Nit, m.IvaEmitido, m.IvaRecibido)).ToArray(),
            "IVA emitido", "IVA recibido");
        pdf.Tabla("Movimientos por contribuyente", new[] { "NIT", "Nombre", "Emitido", "Recibido", "Diferencia" },
            new[] { 92f, 162f, 85f, 85f, 85f }, r.Movimientos.Select(m => new[]
            { m.Nit, m.Nombre, Dinero(m.IvaEmitido), Dinero(m.IvaRecibido), Dinero(m.Diferencia) }));
        return pdf.Crear();
    }

    public byte[] Rango(ResumenRangoFechas r)
    {
        DocumentoPdf pdf = new("RESUMEN POR FECHAS", "Montos correspondientes a documentos autorizados");
        pdf.Detalle("Periodo: " + Fecha(r.FechaInicio) + " al " + Fecha(r.FechaFin));
        pdf.Detalle("Importes: " + (r.Modo == "siniva" ? "sin IVA (VALOR)" : "con IVA (TOTAL)"));
        pdf.Resumen("Facturas: " + r.CantidadTotalFacturas + "   |   Monto autorizado: " + Dinero(r.MontoTotalAutorizado));
        pdf.Grafica("Montos diarios (primeros 12 dias con movimiento)",
            r.Dias.Take(12).Select(m => new BarraPdf(Fecha(m.Fecha), m.MontoAutorizado, 0m)).ToArray(),
            "Monto autorizado", null);
        pdf.Tabla("Detalle por fecha", new[] { "Fecha", "Facturas", "Monto autorizado" },
            new[] { 135f, 105f, 269f }, r.Dias.Select(d => new[]
            { Fecha(d.Fecha), d.CantidadFacturas.ToString(CultureInfo.InvariantCulture), Dinero(d.MontoAutorizado) }));
        return pdf.Crear();
    }

    public byte[] Recepciones(ReporteRecepciones r)
    {
        DocumentoPdf pdf = new("RECEPCION Y ERRORES DTE", "Auditoria de solicitudes atendidas por el servicio");
        pdf.Detalle("Periodo: " + Fecha(r.FechaInicio) + " al " + Fecha(r.FechaFin));
        pdf.Resumen("Solicitudes recibidas: " + r.TotalRecibidas + "   |   Sin errores: " + r.TotalSinErrores);
        foreach (RecepcionDiaria d in r.Dias)
        {
            pdf.Tabla("Fecha: " + Fecha(d.Fecha), new[] { "Metrica", "Cantidad" },
                new[] { 377f, 132f }, new[]
                {
                    new[] { "Solicitudes recibidas", d.CantidadTotalFacturasRecibidas.ToString() },
                    new[] { "Facturas sin errores", d.CantidadFacturasSinErrores.ToString() },
                    new[] { "Emisores distintos", d.CantidadEmisoresDistintos.ToString() },
                    new[] { "Receptores distintos", d.CantidadReceptoresDistintos.ToString() },
                    new[] { "NIT emisor invalido", d.Errores.NitEmisorInvalido.ToString() },
                    new[] { "NIT receptor invalido", d.Errores.NitReceptorInvalido.ToString() },
                    new[] { "NIT emisor inexistente", d.Errores.NitEmisorInexistente.ToString() },
                    new[] { "NIT receptor inexistente", d.Errores.NitReceptorInexistente.ToString() },
                    new[] { "IVA mal calculado", d.Errores.IvaMalCalculado.ToString() },
                    new[] { "TOTAL mal calculado", d.Errores.TotalMalCalculado.ToString() },
                    new[] { "REFERENCIA duplicada", d.Errores.ReferenciaDuplicada.ToString() },
                    new[] { "Fecha invalida", d.Errores.FechaInvalida.ToString() },
                    new[] { "Referencia invalida", d.Errores.ReferenciaInvalida.ToString() },
                    new[] { "Valor invalido", d.Errores.ValorInvalido.ToString() }
                });
        }
        if (r.Dias.Count == 0) pdf.Detalle("Sin solicitudes registradas en el periodo seleccionado.");
        return pdf.Crear();
    }

    private sealed record BarraPdf(string Etiqueta, decimal Primaria, decimal Secundaria);

    /// <summary>Constructor de páginas A4 con paginación de tablas y contenido vectorial.</summary>
    private sealed class DocumentoPdf
    {
        private const float Ancho = 595f, Alto = 842f, Margen = 43f, AnchoContenido = Ancho - 2 * Margen;
        private readonly List<StringBuilder> _paginas = new();
        private readonly string _titulo, _subtitulo;
        private float _y;
        private StringBuilder Actual => _paginas[^1];

        public DocumentoPdf(string titulo, string subtitulo)
        {
            _titulo = titulo;
            _subtitulo = subtitulo;
            NuevaPagina();
        }

        private static string Num(float n) => n.ToString("0.###", CultureInfo.InvariantCulture);
        private static string TextoPdf(string texto)
        {
            // La fuente PDF WinAnsi cubre vocales acentuadas y ñ. Sustituir símbolos ajenos a Latin1.
            string limpio = texto.Replace('\r', ' ').Replace('\n', ' ')
                .Replace('–', '-').Replace('—', '-').Replace('’', '\'');
            StringBuilder escapado = new();
            foreach (char c in limpio)
            {
                char caracter = c <= 255 && (c >= ' ' || c == '\t') ? c : '?';
                if (caracter is '(' or ')' or '\\') escapado.Append('\\');
                escapado.Append(caracter);
            }
            return escapado.ToString();
        }
        private void Relleno(float r, float g, float b) => Actual.AppendLine($"{Num(r)} {Num(g)} {Num(b)} rg");
        private void Caja(float x, float desdeArriba, float ancho, float alto)
            => Actual.AppendLine($"{Num(x)} {Num(Alto - desdeArriba - alto)} {Num(ancho)} {Num(alto)} re f");
        private void Linea(float x1, float y1, float x2, float y2)
            => Actual.AppendLine($"0.8 0.84 0.9 RG 0.5 w {Num(x1)} {Num(Alto - y1)} m {Num(x2)} {Num(Alto - y2)} l S");
        private void Escribir(string texto, float x, float arriba, float tamanio = 9, bool negrita = false,
            float r = .13f, float g = .20f, float b = .33f)
        {
            Relleno(r, g, b);
            Actual.AppendLine($"BT /{(negrita ? "F2" : "F1")} {Num(tamanio)} Tf 1 0 0 1 {Num(x)} {Num(Alto - arriba)} Tm ({TextoPdf(texto)}) Tj ET");
        }
        private void NuevaPagina()
        {
            _paginas.Add(new StringBuilder());
            Relleno(.07f, .13f, .27f); Caja(0, 0, Ancho, 90);
            Escribir("SAT  /  SIMULADOR DTE", Margen, 31, 9, true, .68f, .78f, .98f);
            Escribir(_titulo, Margen, 54, 17, true, 1, 1, 1);
            Escribir(_subtitulo, Margen, 74, 9, false, .78f, .85f, .95f);
            _y = 115;
        }
        private void Asegurar(float altura)
        {
            if (_y + altura > Alto - 65) NuevaPagina();
        }
        public void Detalle(string mensaje)
        {
            foreach (string linea in Envolver(mensaje, 95))
            {
                Asegurar(17);
                Escribir(linea, Margen, _y, 9);
                _y += 16;
            }
        }
        public void Resumen(string texto)
        {
            Asegurar(58);
            Relleno(.91f, .95f, 1f); Caja(Margen, _y - 9, AnchoContenido, 44);
            Escribir(texto, Margen + 13, _y + 9, 10, true);
            _y += 55;
        }
        public void Tabla(string titulo, string[] encabezados, float[] anchos, IEnumerable<string[]> filas)
        {
            if (encabezados.Length != anchos.Length || Math.Abs(anchos.Sum() - AnchoContenido) > 1f)
                throw new ArgumentException("La tabla necesita columnas cuyas anchuras sumen el ancho disponible.");
            string[][] registros = filas.ToArray();
            Asegurar(65);
            _y += 10;
            Escribir(titulo, Margen, _y, 12, true);
            _y += 15;
            EncabezadoTabla(encabezados, anchos);
            int indice = 0;
            foreach (string[] registro in registros)
            {
                string[][] lineas = new string[encabezados.Length][];
                for (int c = 0; c < encabezados.Length; c++)
                {
                    string valor = c < registro.Length ? registro[c] ?? "" : "";
                    int letras = Math.Max(7, (int)((anchos[c] - 14) / 5.2f));
                    lineas[c] = Envolver(valor, letras).ToArray();
                }
                float altoFila = Math.Max(25, lineas.Max(l => l.Length) * 12 + 12);
                if (_y + altoFila > Alto - 65)
                {
                    NuevaPagina();
                    Escribir(titulo + " (continuacion)", Margen, _y, 11, true);
                    _y += 15;
                    EncabezadoTabla(encabezados, anchos);
                }
                if (indice % 2 == 0) { Relleno(.96f, .975f, .995f); Caja(Margen, _y, AnchoContenido, altoFila); }
                float x = Margen;
                for (int c = 0; c < anchos.Length; c++)
                {
                    for (int l = 0; l < lineas[c].Length; l++)
                        Escribir(lineas[c][l], x + 7, _y + 15 + 12 * l, 8);
                    x += anchos[c];
                }
                _y += altoFila;
                indice++;
            }
            if (registros.Length == 0)
            {
                Asegurar(33);
                Escribir("Sin datos para los filtros seleccionados.", Margen + 8, _y + 14, 9);
                _y += 31;
            }
            _y += 12;
        }
        private void EncabezadoTabla(string[] encabezados, float[] anchos)
        {
            Asegurar(28);
            Relleno(.83f, .88f, .97f); Caja(Margen, _y, AnchoContenido, 26);
            float x = Margen;
            for (int c = 0; c < anchos.Length; c++)
            {
                Escribir(encabezados[c], x + 7, _y + 17, 8, true);
                x += anchos[c];
            }
            _y += 26;
        }
        public void Grafica(string titulo, IReadOnlyList<BarraPdf> barras, string nombreUno, string? nombreDos)
        {
            if (barras.Count == 0) return;
            Asegurar(265);
            _y += 10;
            Escribir(titulo, Margen, _y, 12, true);
            _y += 22;
            Escribir(nombreUno, Margen, _y, 8, true, .18f, .38f, .78f);
            if (nombreDos is not null)
                Escribir(nombreDos, Margen + 170, _y, 8, true, .04f, .57f, .47f);
            _y += 11;
            float inicio = _y + 5, graficaAlto = 170, anchoBarras = AnchoContenido / barras.Count;
            decimal maximo = Math.Max(1m, barras.Max(b => Math.Max(b.Primaria, b.Secundaria)));
            for (int i = 0; i <= 4; i++)
                Linea(Margen, inicio + graficaAlto * i / 4f, Margen + AnchoContenido, inicio + graficaAlto * i / 4f);
            for (int i = 0; i < barras.Count; i++)
            {
                BarraPdf b = barras[i];
                float baseX = Margen + i * anchoBarras;
                float anchoUnidad = Math.Min(21, anchoBarras / (nombreDos is null ? 2.3f : 3.0f));
                float alturaUno = (float)(b.Primaria / maximo) * graficaAlto;
                Relleno(.20f, .42f, .83f);
                Caja(baseX + 5, inicio + graficaAlto - alturaUno, anchoUnidad, alturaUno);
                if (nombreDos is not null)
                {
                    float alturaDos = (float)(b.Secundaria / maximo) * graficaAlto;
                    Relleno(.08f, .62f, .49f);
                    Caja(baseX + 8 + anchoUnidad, inicio + graficaAlto - alturaDos, anchoUnidad, alturaDos);
                }
                string etiqueta = b.Etiqueta.Length > 12 ? b.Etiqueta[..11] + "~" : b.Etiqueta;
                Escribir(etiqueta, baseX + 2, inicio + graficaAlto + 13, 6.5f);
            }
            _y = inicio + graficaAlto + 28;
        }
        private static IEnumerable<string> Envolver(string texto, int max)
        {
            if (string.IsNullOrEmpty(texto)) { yield return ""; yield break; }
            string[] palabras = texto.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder linea = new();
            foreach (string palabra in palabras)
            {
                if (linea.Length > 0 && linea.Length + 1 + palabra.Length > max)
                {
                    yield return linea.ToString();
                    linea.Clear();
                }
                if (palabra.Length > max)
                {
                    if (linea.Length > 0) { yield return linea.ToString(); linea.Clear(); }
                    for (int i = 0; i < palabra.Length; i += max)
                        yield return palabra.Substring(i, Math.Min(max, palabra.Length - i));
                }
                else
                {
                    if (linea.Length > 0) linea.Append(' ');
                    linea.Append(palabra);
                }
            }
            if (linea.Length > 0) yield return linea.ToString();
        }

        public byte[] Crear()
        {
            // Objetos PDF: catalogo, paginas, Helvetica, Helvetica-Bold,
            // y por pagina un stream y su descriptor /Page.
            using MemoryStream salida = new();
            void Raw(string texto) { byte[] b = Encoding.ASCII.GetBytes(texto); salida.Write(b); }
            List<long> posiciones = new() { 0 };
            void Objeto(int indice, Action contenido)
            {
                while (posiciones.Count <= indice) posiciones.Add(0);
                posiciones[indice] = salida.Position;
                Raw(indice + " 0 obj\n"); contenido(); Raw("\nendobj\n");
            }
            Raw("%PDF-1.4\n%SAT DTE\n");
            Objeto(1, () => Raw("<< /Type /Catalog /Pages 2 0 R >>"));
            Objeto(2, () => Raw("<< /Type /Pages /Count " + _paginas.Count + " /Kids [" +
                string.Join(" ", Enumerable.Range(0, _paginas.Count).Select(i => (6 + 2 * i) + " 0 R")) + "] >>"));
            Objeto(3, () => Raw("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
            Objeto(4, () => Raw("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
            for (int i = 0; i < _paginas.Count; i++)
            {
                StringBuilder pagina = _paginas[i];
                StringBuilder pie = new();
                pie.AppendLine("0.64 0.68 0.75 rg");
                pie.AppendLine($"BT /F1 8 Tf 1 0 0 1 43 29 Tm (SAT DTE - Reporte generado por el sistema) Tj ET");
                pie.AppendLine($"BT /F1 8 Tf 1 0 0 1 488 29 Tm (Pag. {i + 1} / {_paginas.Count}) Tj ET");
                byte[] flujo = Encoding.Latin1.GetBytes(pagina.ToString() + pie.ToString());
                int idFlujo = 5 + 2 * i, idPagina = idFlujo + 1;
                Objeto(idFlujo, () =>
                {
                    Raw("<< /Length " + flujo.Length + " >>\nstream\n");
                    salida.Write(flujo);
                    Raw("\nendstream");
                });
                Objeto(idPagina, () => Raw("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] " +
                    "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + idFlujo + " 0 R >>"));
            }
            long tabla = salida.Position;
            Raw("xref\n0 " + posiciones.Count + "\n0000000000 65535 f \n");
            for (int i = 1; i < posiciones.Count; i++)
                Raw(posiciones[i].ToString("D10", CultureInfo.InvariantCulture) + " 00000 n \n");
            Raw("trailer\n<< /Size " + posiciones.Count + " /Root 1 0 R >>\nstartxref\n" + tabla + "\n%%EOF\n");
            return salida.ToArray();
        }
    }
}
