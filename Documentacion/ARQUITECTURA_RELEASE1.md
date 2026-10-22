# Arquitectura preliminar — Release 1

## Separación de proyectos

- **SatDte.Web**: programa 1 del enunciado. Razor Pages, maneja archivos JSON y
  realiza llamadas HTTP reales a la API mediante `IHttpClientFactory`.
- **SatDte.Api**: servicio 2 del enunciado. API ASP.NET Core independiente,
  expone los endpoints requeridos para el módulo RTU.
- **SatDte.Core**: reglas de negocio, modelos, persistencia y TDA propia.
- **SatDte.Verificacion**: pruebas locales ejecutables sin paquetes adicionales.

## Diagrama de clases (preliminar)

```mermaid
classDiagram
    class Contribuyente {
        +string Nit
        +string Nombre
    }
    class ContribuyenteEntrada {
        +string? Nit
        +string? Nombre
    }
    class RespuestaRtu {
        +int ContribuyentesNuevos
        +int ContribuyentesActualizados
        +int NitsInvalidos
    }
    class TablaRtu {
        -Contribuyente[] registros
        +int Cantidad
        +AgregarOActualizar(Contribuyente) bool
        +Buscar(string) Contribuyente
        +CopiarElementos() Contribuyente[]
        +Limpiar() void
    }
    class ValidadorNit {
        +EsValido(string) bool
    }
    class ArchivoRtu {
        +Leer() Contribuyente[]
        +Guardar(Contribuyente[]) void
    }
    class ServicioRtu {
        +Importar(registros) RespuestaRtu
        +Listar() Contribuyente[]
        +Buscar(nit) Contribuyente
        +Reiniciar() void
    }
    class RtuController
    class SistemaController
    ServicioRtu --> TablaRtu
    ServicioRtu --> ValidadorNit
    ServicioRtu --> ArchivoRtu
    TablaRtu --> Contribuyente
    RtuController --> ServicioRtu
    SistemaController --> ServicioRtu
    ServicioRtu --> RespuestaRtu
    ServicioRtu --> ContribuyenteEntrada
```

## Diagrama de actividad: importación RTU

```mermaid
flowchart TD
  A[Recibir arreglo JSON] --> B{Estructura del arreglo valida?}
  B -- No --> X[Responder error 400]
  B -- Si --> C[Validar todos los nombres asociados a NIT validos]
  C --> D{Hay errores estructurales?}
  D -- Si --> X
  D -- No --> E[Copiar RTU a tabla de trabajo]
  E --> F{Quedan registros?}
  F -- Si --> G{NIT valido por modulo 11?}
  G -- No --> H[Contar NIT invalido]
  G -- Si --> I{Ya existe en RTU?}
  I -- Si --> J[Actualizar y contar]
  I -- No --> K[Insertar y contar]
  H --> F
  J --> F
  K --> F
  F -- No --> L[Guardar arreglo como archivo JSON]
  L --> M[Actualizar tabla en memoria]
  M --> N[Responder tres contadores obligatorios]
```

## Diagrama de despliegue (primera versión)

```mermaid
flowchart LR
  U[Usuario en navegador] -->|HTTP puerto 5176| W[SatDte.Web - Razor Pages]
  W -->|HTTP y JSON puerto 5175| A[SatDte.Api - ASP.NET Core]
  P[Postman / cliente externo] -->|HTTP y JSON| A
  A --> C[SatDte.Core - servicio RTU y TDA]
  C --> D[(App_Data/rtu.json)]
```

## Persistencia

El RTU se guarda en un archivo `rtu.json` del backend usando
`System.Text.Json`. El guardado escribe un archivo temporal y lo reemplaza
para reducir el riesgo de dejar un archivo incompleto. El servicio serializa
las escrituras mediante `lock`, y recarga el archivo cuando arranca.

## Compromisos de posteriores releases

Los diagramas se completarán con facturas, correlativos, reportes y PDF.
El documento final deberá ser un ensayo de 4-7 páginas de contenido más
anexos de acuerdo con las instrucciones del curso.
