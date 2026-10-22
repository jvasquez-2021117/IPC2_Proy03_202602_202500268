using SatDte.Core.Modelos;

namespace SatDte.Core.Estructuras;

/// <summary>
/// TDA propia: tabla dinámica basada en un arreglo y búsqueda por NIT.
/// No depende de List para guardar datos, ni de nodos enlazados.
/// La capacidad se duplica al llenarse. Las búsquedas son O(n).
/// </summary>
public sealed class TablaRtu
{
    private Contribuyente[] _registros = new Contribuyente[8];

    public int Cantidad { get; private set; }

    public bool Contiene(string nit) => BuscarIndice(nit) >= 0;

    public Contribuyente? Buscar(string nit)
    {
        int indice = BuscarIndice(nit);
        if (indice < 0) return null;

        Contribuyente item = _registros[indice];
        return new Contribuyente(item.Nit, item.Nombre);
    }

    /// <returns>true cuando se inserta uno nuevo; false cuando se actualiza.</returns>
    public bool AgregarOActualizar(Contribuyente contribuyente)
    {
        int indice = BuscarIndice(contribuyente.Nit);
        if (indice >= 0)
        {
            _registros[indice] = new Contribuyente(contribuyente.Nit, contribuyente.Nombre);
            return false;
        }

        if (Cantidad == _registros.Length)
            AmpliarCapacidad();

        _registros[Cantidad] = new Contribuyente(contribuyente.Nit, contribuyente.Nombre);
        Cantidad++;
        return true;
    }

    public Contribuyente[] CopiarElementos()
    {
        Contribuyente[] resultado = new Contribuyente[Cantidad];
        for (int i = 0; i < Cantidad; i++)
            resultado[i] = new Contribuyente(_registros[i].Nit, _registros[i].Nombre);
        return resultado;
    }

    public void Limpiar()
    {
        _registros = new Contribuyente[8];
        Cantidad = 0;
    }

    private int BuscarIndice(string nit)
    {
        for (int i = 0; i < Cantidad; i++)
            if (string.Equals(_registros[i].Nit, nit, StringComparison.Ordinal))
                return i;
        return -1;
    }

    private void AmpliarCapacidad()
    {
        Contribuyente[] nuevoArreglo = new Contribuyente[_registros.Length * 2];
        for (int i = 0; i < Cantidad; i++)
            nuevoArreglo[i] = _registros[i];
        _registros = nuevoArreglo;
    }
}
