# Casos de prueba documentados — Release 1

| ID | Prueba | Entrada | Resultado esperado |
|---|---|---|---|
| RTU-01 | Alta RTU | Archivo 01 sobre RTU vacío | 2 nuevos; 1 inválido |
| RTU-02 | Actualización RTU | Archivo 02 después de RTU-01 | 1 nuevo; 1 actualizado |
| RTU-03 | Duplicado lógico por NIT | Enviar dos veces 01 | No duplicar registros existentes |
| RTU-04 | Verificación con K | `6K` | Aceptado |
| RTU-05 | Verificación con minúscula | `6k` | Rechazado |
| RTU-06 | Longitud incorrecta | 21 dígitos base + verificador | Rechazado |
| RTU-07 | Persistencia | Reiniciar procesos | Conservar registros |
| RTU-08 | JSON inválido | Archivo 03 | Rechazo, sin cambios en RTU |
| RTU-09 | Inicialización | POST sistema/inicializar | El RTU queda vacío incluso tras reiniciar |
| RTU-10 | API autónoma | Importación desde Postman | Mismo JSON de salida sin frontend |
| RTU-11 | TDA dinámico | Cargar más de 8 registros en la prueba de consola | Crece capacidad y mantiene datos |
| RTU-12 | Guardado transaccional de lote | Registro sin nombre y NIT válido | Rechazo del lote sin guardar registros previos de ese lote |

La ejecución detallada de casos manuales está en `../PRUEBAS_RELEASE1.md`.
Los casos automáticos están implementados en `SatDte.Verificacion/Program.cs`.
