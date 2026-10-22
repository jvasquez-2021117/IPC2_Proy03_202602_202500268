using SatDte.Core.Modelos;

namespace SatDte.Core.Estructuras;

/// <summary>TDA propio de arreglo ampliable. No usa Node&lt;T&gt; ni nodos enlazados.</summary>
public sealed class RegistroSolicitudes
{
    private RegistroFactura[] _datos = new RegistroFactura[8];
    public int Cantidad { get; private set; }

    public void Agregar(RegistroFactura registro)
    {
        ArgumentNullException.ThrowIfNull(registro);
        if (Cantidad == _datos.Length)
            Array.Resize(ref _datos, _datos.Length * 2);
        _datos[Cantidad++] = registro;
    }

    public RegistroFactura[] Copiar()
    {
        RegistroFactura[] copia = new RegistroFactura[Cantidad];
        Array.Copy(_datos, copia, Cantidad);
        return copia;
    }

    public bool ContieneReferencia(string referencia)
    {
        for (int i = 0; i < Cantidad; i++)
            if (string.Equals(_datos[i].Referencia, referencia, StringComparison.Ordinal))
                return true;
        return false;
    }

    public long UltimoCorrelativo(DateOnly fecha)
    {
        long ultimo = 0;
        string prefijo = fecha.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        for (int i = 0; i < Cantidad; i++)
        {
            long? codigo = _datos[i].CodigoAprobacion;
            if (codigo is not null && codigo.Value.ToString(System.Globalization.CultureInfo.InvariantCulture).StartsWith(prefijo, StringComparison.Ordinal))
            {
                long numero = codigo.Value % 100000000L;
                if (numero > ultimo) ultimo = numero;
            }
        }
        return ultimo;
    }

    public void Limpiar()
    {
        Array.Clear(_datos, 0, Cantidad);
        Cantidad = 0;
    }
}
