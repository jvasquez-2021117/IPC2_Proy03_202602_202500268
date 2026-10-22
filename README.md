# IPC2 Proyecto 3 — SAT DTE | Release 1 de 4

Solución académica en C# para Visual Studio 2022: API HTTP en ASP.NET Core 8,
frontend en Razor Pages, biblioteca POO con TDA propio y verificador de pruebas sin
paquetes NuGet externos.

## Requisitos

- **Visual Studio 2022 versión 17.8 o posterior** (idealmente actualizada).
- Carga de trabajo **Desarrollo de ASP.NET y web** instalada.
- **SDK .NET 8.0** (Visual Studio normalmente lo ofrece al instalar la carga de trabajo).
- Windows, macOS o Linux para .NET; estas instrucciones de interfaz están dirigidas a Visual Studio 2022 en Windows.

> Visual Studio Code **no** es lo mismo que Visual Studio. Este ZIP incluye una solución `.sln`
> para abrir directamente con Visual Studio.

## Iniciar la solución paso a paso

1. Extraer TODO el ZIP en una carpeta de trabajo, no abrir archivos directamente desde el ZIP.
2. Abrir **IPC2_Proyecto3.sln** en Visual Studio.
3. Aceptar la restauración automática de los proyectos y esperar a que termine.
4. En el Explorador de soluciones, hacer clic derecho sobre **la solución** `IPC2_Proyecto3`
   → **Configurar proyectos de inicio** (o **Configure Startup Projects**).
5. Elegir **Varios proyectos de inicio (Multiple startup projects)**.
6. Establecer **Iniciar / Start** para `SatDte.Api` y `SatDte.Web`.
   Establecer **Ninguno / None** para `SatDte.Core` y `SatDte.Verificacion`.
7. Pulsar **F5** o **Ctrl+F5**. Si aparece un aviso de Firewall para localhost,
   permitir el acceso local. Esperar que abran las dos aplicaciones.
8. Abrir manualmente si no se abre:
   - API: <http://localhost:5175/api/sistema/estado>
   - Web: <http://localhost:5176/>

Puertos definidos en `SatDte.Api/Properties/launchSettings.json` y
`SatDte.Web/Properties/launchSettings.json`. Si los cambias, también debes actualizar
`SatDte.Web/appsettings.json`, en la clave `Api:BaseUrl`.

## Estructura

```text
IPC2_Proyecto3.sln
SatDte.Core/
  Modelos/              Contribuyente, formato entrada y respuesta RTU
  Estructuras/          TablaRtu: TDA propio con arreglo dinámico
  Validaciones/         ValidadorNit: Regex + módulo 11
  Persistencia/         ArchivoRtu: JSON persistente, guardado atómico
  Servicios/            ServicioRtu: reglas y coordinación
SatDte.Api/             Servicio HTTP consumible por Postman
  Controllers/          RtuController, SistemaController
  App_Data/             Aquí se genera rtu.json al importar
SatDte.Web/             Cliente web Razor Pages (HTTP hacia API)
  Pages/                Inicio, RTU, contribuyentes, inicializar y ayuda
SatDte.Verificacion/    Consola de pruebas sin dependencias externas
Ejemplos/               JSON de prueba
Documentacion/          Información del Release 1 y diagramas preliminares
```

## Endpoints HTTP del Release 1

| Método | Ruta | Función |
|---|---|---|
| GET | `/api/sistema/estado` | Estado de la API y cantidad de contribuyentes |
| POST | `/api/rtu/importar` | Envía el **arreglo JSON EXACTO** del enunciado |
| GET | `/api/rtu` | Ver contribuyentes existentes |
| GET | `/api/rtu/{nit}` | Consultar contribuyente individual |
| POST | `/api/sistema/inicializar` | Reiniciar el RTU |

La respuesta de `POST /api/rtu/importar` conserva las claves obligatorias:

```json
{
  "CONTRIBUYENTES_NUEVOS": 2,
  "CONTRIBUYENTES_ACTUALIZADOS": 0,
  "NITS_INVALIDOS": 1
}
```

## Guía completa de pruebas

Ver `PRUEBAS_RELEASE1.md` (en la raíz) y `Documentacion/CASOS_DE_PRUEBA.md`.

Las pruebas automáticas se inician mediante **Ver → Terminal** en Visual Studio:

```powershell
dotnet run --project SatDte.Verificacion
```

Deberías obtener una lista de `OK` y la línea final `RESULTADO: 51 verificaciones correctas. Release 1 OK.`

> Si ejecutas esta consola por separado no borra el RTU principal: trabaja en una carpeta temporal.

## Notas importantes

- El RTU se almacena en `SatDte.Api/App_Data/rtu.json`. Para verificar persistencia,
  detén y vuelve a iniciar la **API** y revisa la página Contribuyentes.
- `5123456` (en el ejemplo del enunciado) **no pasa** la fórmula del dígito
  verificador; se cuenta como inválido. Los válidos `7378106` y `8338817` sí se admiten.
- No se utiliza `Node<T>` ni ninguna clase de nodos; el TDA está implementado con un arreglo
  redimensionable propio. C# permite estructuras estándar, pero se ha optado por TDA propio.
- **El Release 1 no implementa autorizaciones de facturas ni estadísticas**: se
  incorporan de forma acumulativa en los Releases 2, 3 y 4.
- No publiques en el repositorio los archivos de `App_Data` generados durante las pruebas.
- **Antes de la entrega final**, sustituye los campos de estudiante en
  `SatDte.Web/Pages/Ayuda.cshtml` y completa el ensayo solicitado.
- Nombre exigido del repositorio: `IPC2_Proy03_202602_#Carnet` (reemplaza `#Carnet`).
- Agrega como colaborador al auxiliar correspondiente a tu sección.
- El enunciado establece el **22 de octubre de 2026 a las 23:59** como límite,
  y especifica que se calificará el **cuarto release de GitHub**.

## Esquema previsto para las entregas posteriores

1. **Release 1:** arquitectura, RTU, validador NIT, TDA, archivos, frontend.
2. **Release 2:** autorizaciones DTE, fecha/regex, referencia, IVA/TOTAL,
   correlativo diario, persistencia de todas las solicitudes y errores.
3. **Release 3:** consultas de contribuyentes, resúmenes de IVA, estadísticas por fechas.
4. **Release 4:** gráficas, PDF, ensayo de 4-7 páginas con diagramas, casos integrales.

Cada ZIP posterior incluirá los archivos actualizados de esta solución completa.
