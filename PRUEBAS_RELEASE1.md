# Pruebas manuales — Release 1

> Iniciar `SatDte.Api` (5175) **y** `SatDte.Web` (5176) como se indica en README.
>
> Para reproducibilidad, comienza con **Inicializar** desde el menú antes de la prueba 1.

## Caso 1. La API inicia

Entrar a <http://localhost:5175/api/sistema/estado>.

Esperado: JSON con `"estado": "activo"`, `"release": 1` y
`"contribuyentes": 0` si iniciaste el sistema sin datos.

## Caso 2. Importación RTU inicial

1. Ir a <http://localhost:5176/Rtu>.
2. Subir `Ejemplos/01_rtu_inicial.json`.
3. Presionar **Enviar solicitud a la API**.

Esperado:

```json
{
  "CONTRIBUYENTES_NUEVOS": 2,
  "CONTRIBUYENTES_ACTUALIZADOS": 0,
  "NITS_INVALIDOS": 1
}
```

**¿Por qué hay un NIT inválido?** El PDF usa `5123456` como ejemplo pero
no satisface el dígito verificador calculado con módulo 11.

## Caso 3. Consultar RTU

1. Abrir menú **Contribuyentes**.
2. Comprobar que hay exactamente **2 registros**:
   - 7378106 — Empresa ABC, S.A.
   - 8338817 — Comercial XYZ, S.A.

## Caso 4. Actualizar RTU

1. Regresar a **Enviar RTU**.
2. Subir `Ejemplos/02_rtu_actualizacion.json`.

Esperado:

```json
{
  "CONTRIBUYENTES_NUEVOS": 1,
  "CONTRIBUYENTES_ACTUALIZADOS": 1,
  "NITS_INVALIDOS": 0
}
```

En **Contribuyentes** deben aparecer **3** registros; el nombre
asociado al NIT 7378106 será `Empresa ABC Actualizada, S.A.` y se añade `6K`.

## Caso 5. Persistencia

1. Detener Visual Studio usando **Detener depuración**.
2. Volver a iniciar ambos proyectos.
3. Visitar **Contribuyentes**.

Esperado: **siguen existiendo los tres registros** y la actualización.
Además debe existir `SatDte.Api/App_Data/rtu.json`.

## Caso 6. Importación repetida

Volver a subir `Ejemplos/01_rtu_inicial.json` con los tres registros cargados.

Esperado: **0 nuevos, 2 actualizados, 1 inválido**, y solo 3 registros en la lista.
El NIT 7378106 recuperará el nombre anterior del primer archivo.

## Caso 7. JSON mal formado

Subir `Ejemplos/03_rtu_malformado.json`.

Esperado: error indicando formato JSON incorrecto. No deben añadirse registros.

## Caso 8. Inicializar

1. Abrir **Inicializar**.
2. Confirmar el mensaje de advertencia.
3. Abrir **Contribuyentes**.

Esperado: `0 registros`, incluso después de reiniciar la API.

## Caso 9. API independiente desde Postman

- Método: `POST`
- URL: `http://localhost:5175/api/rtu/importar`
- Body: **raw**, formato **JSON** y contenido completo de `01_rtu_inicial.json`.

La API debe responder con el mismo contrato JSON de la web (sin depender
ni de abrir ni de iniciar el frontend). Se recomienda reiniciar antes de esta prueba.

## Caso 10. Verificaciones del TDA, NIT y almacenamiento

En una terminal ubicada en la carpeta de la solución ejecutar:

```powershell
dotnet run --project SatDte.Verificacion
```

Esperado: **51 verificaciones exitosas** y código de salida 0.
La consola no modifica `SatDte.Api/App_Data`.

## Si aparece un problema

- **Página no encontrada**: comprueba que ambos proyectos estén configurados en Start.
- **No se puede conectar a API**: verifica que <http://localhost:5175/api/sistema/estado> responde.
- **Conflicto de puertos**: ajusta `applicationUrl` de API/Web y actualiza `Api:BaseUrl`.
- **Falta .NET 8**: instala la carga de trabajo ASP.NET y web y el SDK .NET 8.
- **Puertos HTTPS**: usa los perfiles `API HTTP` y `Razor HTTP` del proyecto.
