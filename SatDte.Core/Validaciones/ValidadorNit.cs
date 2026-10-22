using System.Text.RegularExpressions;

namespace SatDte.Core.Validaciones;

/// <summary>Algoritmo de dígito verificador del NIT descrito en el enunciado.</summary>
public sealed class ValidadorNit
{
    // Entre 1 y 20 dígitos de base, seguidos por un dígito o la letra K.
    private static readonly Regex Patron = new(
        @"^[0-9]{1,20}[0-9K]$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool EsValido(string? nit)
    {
        if (string.IsNullOrEmpty(nit) || !Patron.IsMatch(nit))
            return false;

        int suma = 0;
        int peso = 2;

        // Se recorre desde el último dígito base hacia la izquierda.
        // La posición 1 es el verificador y NO se suma.
        for (int i = nit.Length - 2; i >= 0; i--)
        {
            suma += (nit[i] - '0') * peso;
            peso++;
        }

        int verificador = (11 - (suma % 11)) % 11;
        char esperado = verificador == 10 ? 'K' : (char)('0' + verificador);
        return nit[^1] == esperado;
    }
}
