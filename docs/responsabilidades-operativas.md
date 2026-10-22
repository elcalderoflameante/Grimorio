# Responsabilidades operativas

## Ubicacion y alcance

- Web: **Operaciones > Responsabilidades**, separado de RRHH y POS para poder extenderlo a cocina, caja y otras areas.
- App de meseros: **Mi jornada**, junto a Mesas y Solicitudes; visible con `Operations.Responsibilities.Execute`.
- Cada actividad generada tiene un solo empleado responsable. No existen tareas compartidas ni un boton para que cualquier empleado tome tareas ajenas.

## Configuracion inicial

1. Vincular cada usuario de la app con su empleado en RRHH.
2. Guardar los horarios de los empleados en RRHH > Horarios.
3. Crear areas operativas, por ejemplo Banos, Salon y Terraza.
4. Crear puestos operativos reutilizables: Mesera 1, Mesera 2, Mesera 3. No son cargos laborales nuevos.
5. Crear las plantillas de tareas con su puesto, area, instrucciones, frecuencia, momento del turno, controles y requisito fotografico.
6. Asignar cada puesto a un empleado por rango de fechas y hora de inicio del turno. La hora debe coincidir exactamente con el turno guardado. Cambiar semanalmente esta relacion, no las tareas.

Ejemplo: Jessica ocupa Mesera 1 del lunes al domingo en el turno de las 10:00; la semana siguiente ocupa Mesera 2. Solo se generan actividades en los dias en que tenga un turno guardado coincidente. La vista Jornada informa asignaciones sin horario coincidente y puestos sin responsable.

## Frecuencias y ventanas

- Diaria; semanal en dias seleccionados; cada 14 dias desde una fecha de referencia; mensual en el dia de referencia; cada N dias.
- La frecuencia cada 14 dias no significa automaticamente los dias 1 y 15 de cada mes.
- Para fechas mensuales como el 31, febrero usa su ultimo dia y marzo vuelve al 31.
- Apertura: desde el inicio del turno. Cierre: desde la ventana anterior al final del turno. Durante la jornada: una actividad o revisiones cada cierto numero de minutos (minimo 15).
- Se utiliza la zona horaria de la sucursal y se contemplan turnos nocturnos. Cada revision tiene su propia actividad y plazo. Vencer el plazo no completa una actividad: queda atrasada y puede registrarse despues con su hora real.
- Un proceso del backend materializa las actividades cada cinco minutos, incluso si nadie abre la app. La consulta de jornada tambien solicita la generacion. En esta primera version se recuperan hoy y el turno nocturno anterior; no se reconstruyen automaticamente periodos largos con el servidor apagado.

## Evidencia y novedades

La app abre exclusivamente la camara, sin selector de galeria. Puede requerirse foto final o antes/despues. Cada captura requiere obtener una sesion del servidor, valida durante 30 minutos y vinculada a la actividad y al empleado. Se normaliza la imagen y se elimina metadata. Esto restringe el flujo de la app oficial; no constituye una certificacion criptografica de la escena fotografiada.

Si se pierde internet despues de iniciar la captura, el telefono conserva la fotografia y el registro pendiente en almacenamiento privado de esa sesion, separados por sucursal y usuario. Se reintenta al volver a la pantalla, al reanudar o en su refresco. Hay hasta 48 horas adicionales para transmitir una captura tomada dentro de su ventana. Para iniciar una fotografia nueva se necesita conexion. No se marca completada una actividad hasta recibir confirmacion del servidor. Los reintentos usan el mismo identificador para evitar duplicados.

Cada control admite opciones parametrizadas y opciones que exigen novedad. Por ejemplo: Jabon = Suficiente / Bajo / Agotado; Bajo y Agotado pueden exigir observaciones. Un faltante queda como **registrada con novedad**, no como problema resuelto. Esta version no incluye un flujo separado de resolucion o aprobacion de novedades.

Las fotos no se guardan en los uploads publicos. Su descarga exige autenticacion, sucursal y permiso de supervision o propiedad de la actividad. El volumen Docker `responsibility_evidence` persiste `/app/private/responsibilities`; incluirlo en respaldos junto con PostgreSQL. Fuera de Docker se usa `private/responsibilities` bajo el directorio de la API, configurable mediante `RESPONSIBILITY_EVIDENCE_PATH`.

## Historial, cambios y permisos

Las actividades guardan una copia de la plantilla, puesto y responsable. Editar una tarea o una rotacion no reescribe ni duplica lo ya generado. Para reemplazar a alguien en una actividad pendiente, usar la reasignacion explicita con motivo; queda auditoria de origen, destino, usuario y fecha. La nueva responsable debe aportar sus propias fotos; las anteriores se conservan para supervision. No se reasignan registros completados.

El administrador y supervisor reciben los permisos del modulo segun el seed existente. El rol Mesero recibe ejecucion; los roles personalizados requieren asignacion explicita. Renovar la sesion para actualizar los permisos del token. Los empleados sin usuario vinculado aparecen advertidos en la configuracion y no podran ejecutar tareas desde la app.

## Despliegue y comprobacion

Se agrega la migracion EF `AddOperationalResponsibilities`, con siete tablas en el schema `operations`, siguiendo auditoria, sucursal y borrado logico existentes. Debe aplicarse usando el flujo normal de migraciones del proyecto antes de utilizar el modulo. No aplicar manualmente versiones anteriores ni recrear la base de produccion.

Pruebas automatizadas:

```powershell
dotnet run --project backend/tests/Responsibilities.Checks
cd mobile/waitstaff_app
flutter test --no-pub
flutter analyze --no-pub
```

El argumento opcional `--database` del proyecto de checks usa exclusivamente PostgreSQL local en el puerto 15439, base `responsibilities_checks`, usuario `checks`; la fabrica impide apuntarlo a otra base o servidor. Verifica migraciones, aislamiento por sucursal/empleado, generacion sin duplicados, controles, evidencia, reintentos y reasignacion. Nunca usar credenciales de produccion en estas pruebas.

Antes de desplegar en produccion: probar en telefono apertura de camara y permisos, interrupcion de red, reapertura de la app, registro de novedad, rotacion entre dos empleados y consulta de fotos desde la web. La version de Meseros que incorpora este modulo es `1.0.27+28`; su APK y manifiesto se publican juntos en `frontend/public/downloads`. Subir el repositorio no despliega automaticamente al VPS: actualizar backend y frontend antes de ofrecer la nueva APK, conservando el volumen privado de evidencias.
