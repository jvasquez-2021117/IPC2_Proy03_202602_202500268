using SatDte.Core.Estructuras;
using SatDte.Core.Modelos;
using SatDte.Core.Persistencia;
using SatDte.Core.Validaciones;

namespace SatDte.Core.Servicios;

/// <summary>
/// Coordina las reglas del RTU, el almacenamiento persistente y el TDA.
/// Sus operaciones son atómicas frente a llamadas concurrentes en un mismo proceso.
/// </summary>
public sealed class ServicioRtu
{
    private readonly object _candado = new();
    private readonly TablaRtu _tabla = new();
    private readonly ValidadorNit _validador = new();
    private readonly ArchivoRtu _archivo;

    public ServicioRtu(ArchivoRtu archivo)
    {
        _archivo = archivo;
        foreach (Contribuyente contribuyente in _archivo.Leer())
        {
            // No se aceptan datos dañados en almacenamiento persistente.
            if (!_validador.EsValido(contribuyente.Nit) || string.IsNullOrWhiteSpace(contribuyente.Nombre))
                throw new InvalidDataException("El archivo RTU tiene contribuyentes inválidos.");
            _tabla.AgregarOActualizar(contribuyente);
        }
    }

    public bool ValidarNit(string? nit) => _validador.EsValido(nit);

    public RespuestaRtu Importar(IReadOnlyList<ContribuyenteEntrada?> entradas)
    {
        if (entradas is null)
            throw new ArgumentException("Debe enviar un arreglo JSON de contribuyentes.");

        // Primero se valida el lote COMPLETO para evitar un guardado parcial por errores de datos.
        foreach (ContribuyenteEntrada? entrada in entradas)
        {
            if (entrada is null)
                throw new ArgumentException("El arreglo no puede contener registros nulos.");

            if (_validador.EsValido(entrada.Nit) && string.IsNullOrWhiteSpace(entrada.Nombre))
                throw new ArgumentException("Los contribuyentes con NIT válido requieren un nombre.");
        }

        lock (_candado)
        {
            // La copia garantiza que, si falla la escritura del archivo, los datos en RAM
            // sigan siendo los últimos persistidos.
            TablaRtu trabajo = new();
            foreach (Contribuyente actual in _tabla.CopiarElementos())
                trabajo.AgregarOActualizar(actual);

            RespuestaRtu respuesta = new();
            foreach (ContribuyenteEntrada? entrada in entradas)
            {
                if (entrada is null) continue; // Ya se comprobó arriba.
                if (!_validador.EsValido(entrada.Nit))
                {
                    respuesta.NitsInvalidos++;
                    continue;
                }

                bool esNuevo = trabajo.AgregarOActualizar(
                    new Contribuyente(entrada.Nit!, entrada.Nombre!));
                if (esNuevo) respuesta.ContribuyentesNuevos++;
                else respuesta.ContribuyentesActualizados++;
            }

            _archivo.Guardar(trabajo.CopiarElementos());
            _tabla.Limpiar();
            foreach (Contribuyente actual in trabajo.CopiarElementos())
                _tabla.AgregarOActualizar(actual);
            return respuesta;
        }
    }

    public Contribuyente[] Listar()
    {
        lock (_candado) return _tabla.CopiarElementos();
    }

    public Contribuyente? Buscar(string nit)
    {
        lock (_candado) return _tabla.Buscar(nit);
    }

    public void Reiniciar()
    {
        lock (_candado)
        {
            _archivo.Guardar(Array.Empty<Contribuyente>());
            _tabla.Limpiar();
        }
    }
}
