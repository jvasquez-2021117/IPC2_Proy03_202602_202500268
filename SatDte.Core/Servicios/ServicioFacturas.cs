using System.Globalization;
using SatDte.Core.Estructuras;
using SatDte.Core.Modelos;
using SatDte.Core.Persistencia;
using SatDte.Core.Validaciones;

namespace SatDte.Core.Servicios;

/// <summary>Procesa DTE, guarda todos los intentos y garantiza correlativos diarios únicos.</summary>
public sealed class ServicioFacturas
{
    private readonly object _candado = new();
    private readonly RegistroSolicitudes _registros = new();
    private readonly ArchivoFacturas _archivo;
    private readonly ServicioRtu _rtu;
    private readonly ValidadorFactura _validador = new();
    private readonly Func<DateTime> _ahoraLocal;

    // Constructor normal para inyección de dependencias de ASP.NET Core.
    public ServicioFacturas(ArchivoFacturas archivo, ServicioRtu rtu) : this(archivo, rtu, null) { }

    // Reloj inyectable para pruebas reproducibles sin depender del calendario del equipo.
    public ServicioFacturas(ArchivoFacturas archivo, ServicioRtu rtu, Func<DateTime>? ahoraLocal)
    {
        _archivo = archivo;
        _rtu = rtu;
        _ahoraLocal = ahoraLocal ?? HoraGuatemala;
        foreach (RegistroFactura solicitud in archivo.Leer())
        {
            if (solicitud is null || solicitud.NumeroSolicitud < 1 || solicitud.Errores is null)
                throw new InvalidDataException("Archivo de solicitudes corrupto.");
            _registros.Agregar(solicitud);
        }
    }

    private static DateTime HoraGuatemala()
    {
        // El correlativo se crea con la fecha de autorización, no la fecha de la factura.
        string zona = OperatingSystem.IsWindows() ? "Central America Standard Time" : "America/Guatemala";
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById(zona));
    }

    public RespuestaAutorizaciones Procesar(IReadOnlyList<FacturaEntrada?> entradas)
    {
        if (entradas is null) throw new ArgumentException("Se espera un arreglo JSON de facturas.");
        if (entradas.Count == 0) throw new ArgumentException("El arreglo de facturas no puede estar vacío.");
        if (entradas.Any(e => e is null)) throw new ArgumentException("No se admiten elementos nulos en el arreglo.");

        lock (_candado)
        {
            RegistroSolicitudes trabajo = new();
            foreach (RegistroFactura existente in _registros.Copiar()) trabajo.Agregar(existente);
            ResultadoAutorizaciones resultado = new();
            DateTime instante = _ahoraLocal();
            DateOnly fechaAutorizacion = DateOnly.FromDateTime(instante);
            long correlativo = trabajo.UltimoCorrelativo(fechaAutorizacion);

            foreach (FacturaEntrada? entrada in entradas)
            {
                FacturaEntrada f = entrada!;
                string? referencia = f.Referencia?.Trim();
                bool duplicada = !string.IsNullOrEmpty(referencia) && trabajo.ContieneReferencia(referencia);
                FacturaEntrada normalizada = new()
                {
                    Tiempo = f.Tiempo, Referencia = referencia, NitEmisor = f.NitEmisor,
                    NitReceptor = f.NitReceptor, Valor = f.Valor, Iva = f.Iva, Total = f.Total
                };
                string[] errores = _validador.Revisar(normalizada,
                    f.NitEmisor is not null && _rtu.Buscar(f.NitEmisor) is not null,
                    f.NitReceptor is not null && _rtu.Buscar(f.NitReceptor) is not null,
                    duplicada);
                _validador.IntentarExtraerFecha(f.Tiempo, out DateTime fechaDocumento);
                RegistroFactura registro = new()
                {
                    NumeroSolicitud = trabajo.Cantidad + 1L,
                    FechaRecepcionUtc = DateTime.UtcNow,
                    TiempoOriginal = f.Tiempo,
                    FechaDocumento = fechaDocumento == default ? null : fechaDocumento,
                    Referencia = referencia,
                    NitEmisor = f.NitEmisor,
                    NitReceptor = f.NitReceptor,
                    Valor = f.Valor,
                    Iva = f.Iva,
                    Total = f.Total,
                    Errores = errores,
                    Aprobada = errores.Length == 0
                };
                if (registro.Aprobada)
                {
                    if (correlativo >= 99999999L)
                        throw new InvalidOperationException("Se agotaron los correlativos de ocho cifras del día.");
                    correlativo++;
                    registro.CodigoAprobacion = long.Parse(
                        fechaAutorizacion.ToString("yyyyMMdd", CultureInfo.InvariantCulture) +
                        correlativo.ToString("D8", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);
                    resultado.CantidadAutorizacionesAprobadas++;
                    resultado.DetalleAprobaciones.Add(new DetalleAprobacion
                    {
                        Referencia = referencia!, CodigoAprobacion = registro.CodigoAprobacion.Value
                    });
                }
                if (errores.Contains("NIT_EMISOR_INEXISTENTE"))
                    resultado.CantidadAutorizacionesRechazadasPorNitEmisorInexistente++;
                if (errores.Contains("NIT_RECEPTOR_INEXISTENTE"))
                    resultado.CantidadAutorizacionesRechazadasPorNitReceptorInexistente++;
                trabajo.Agregar(registro);
            }

            // El archivo debe escribirse antes de actualizar el estado del servicio en memoria.
            _archivo.Guardar(trabajo.Copiar());
            _registros.Limpiar();
            foreach (RegistroFactura registro in trabajo.Copiar()) _registros.Agregar(registro);
            return new RespuestaAutorizaciones { ResultadoAutorizaciones = resultado };
        }
    }

    public RegistroFactura[] Listar(int limite = 100)
    {
        lock (_candado)
        {
            RegistroFactura[] todos = _registros.Copiar();
            return todos.TakeLast(Math.Clamp(limite, 1, 1000)).Reverse().ToArray();
        }
    }

    public int Cantidad
    {
        get { lock (_candado) return _registros.Cantidad; }
    }

    public void Reiniciar()
    {
        lock (_candado)
        {
            _archivo.Guardar(Array.Empty<RegistroFactura>());
            _registros.Limpiar();
        }
    }
}
