using SatDte.Core.Estructuras;
using SatDte.Core.Modelos;
using SatDte.Core.Persistencia;
using SatDte.Core.Servicios;
using SatDte.Core.Validaciones;

int pasadas = 0;
string carpeta = Path.Combine(Path.GetTempPath(), "satdte-pruebas-" + Guid.NewGuid().ToString("N"));

void Afirmar(bool condicion, string caso)
{
    if (!condicion) throw new Exception("FALLÓ: " + caso);
    pasadas++;
    Console.WriteLine("  OK: " + caso);
}

try
{
    ValidadorNit validador = new();
    Afirmar(validador.EsValido("7378106"), "NIT válido del enunciado: 7378106");
    Afirmar(validador.EsValido("8338817"), "NIT válido del enunciado: 8338817");
    Afirmar(!validador.EsValido("5123456"), "Ejemplo de NIT inválido 5123456");
    Afirmar(validador.EsValido("1234560"), "Cálculo de verificador numérico");
    Afirmar(!validador.EsValido("7378107"), "Verificador numérico incorrecto");
    Afirmar(validador.EsValido("6K"), "Verificador con letra K");
    Afirmar(!validador.EsValido("6k"), "Letra k minúscula no admitida");
    Afirmar(!validador.EsValido(""), "NIT vacío");
    Afirmar(!validador.EsValido("A7378106"), "Caracteres no numéricos");
    Afirmar(!validador.EsValido(new string('1', 21) + "0"), "Más de 20 dígitos base");

    TablaRtu tda = new();
    for (int i = 0; i < 30; i++)
        Afirmar(tda.AgregarOActualizar(new Contribuyente(i.ToString(), "Contribuyente " + i)), "TDA inserción " + i);
    Afirmar(tda.Cantidad == 30, "Ampliación dinámica del TDA");
    Afirmar(!tda.AgregarOActualizar(new Contribuyente("12", "Nombre modificado")), "TDA actualización sin duplicar");
    Afirmar(tda.Cantidad == 30 && tda.Buscar("12")?.Nombre == "Nombre modificado", "TDA consulta y tamaño");

    ArchivoRtu archivo = new(Path.Combine(carpeta, "rtu.json"));
    ServicioRtu servicio = new(archivo);
    RespuestaRtu inicial = servicio.Importar(new List<ContribuyenteEntrada?>
    {
        new() { Nit = "7378106", Nombre = "Empresa ABC, S.A." },
        new() { Nit = "8338817", Nombre = "Comercial XYZ, S.A." },
        new() { Nit = "5123456", Nombre = "NIT inválido del ejemplo" }
    });
    Afirmar(inicial.ContribuyentesNuevos == 2 && inicial.ContribuyentesActualizados == 0 && inicial.NitsInvalidos == 1,
        "Respuesta RTU: 2 nuevos, 0 actualizados, 1 inválido");
    Afirmar(servicio.Listar().Length == 2, "No se guardan NIT inválidos");
    Afirmar(File.Exists(Path.Combine(carpeta, "rtu.json")), "Persistencia en archivo");

    ServicioRtu otroProceso = new(archivo);
    Afirmar(otroProceso.Listar().Length == 2, "Se recuperan contribuyentes después de recrear el servicio");
    RespuestaRtu repetido = otroProceso.Importar(new List<ContribuyenteEntrada?>
    {
        new() { Nit = "7378106", Nombre = "Empresa ABC ACTUALIZADA" },
        new() { Nit = "6K", Nombre = "Empresa con K" }
    });
    Afirmar(repetido.ContribuyentesActualizados == 1 && repetido.ContribuyentesNuevos == 1,
        "Conteo de actualizaciones y altas");
    Afirmar(otroProceso.Buscar("7378106")?.Nombre == "Empresa ABC ACTUALIZADA", "Reemplazo de nombre");

    bool loteRechazado = false;
    try
    {
        otroProceso.Importar(new List<ContribuyenteEntrada?>
        {
            new() { Nit = "1234560", Nombre = "Nombre correcto" },
            new() { Nit = "7358", Nombre = null }
        });
    }
    catch (ArgumentException) { loteRechazado = true; }
    Afirmar(loteRechazado && otroProceso.Buscar("1234560") is null, "Lote inválido no se guarda parcialmente");

    otroProceso.Reiniciar();
    Afirmar(new ServicioRtu(archivo).Listar().Length == 0, "Inicialización también limpia datos persistidos");
    Console.WriteLine($"\nRESULTADO: {pasadas} verificaciones correctas. Release 1 OK.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine("\nERROR DE VERIFICACIÓN: " + ex);
    return 1;
}
finally
{
    if (Directory.Exists(carpeta)) Directory.Delete(carpeta, recursive: true);
}
