## Permiso y preparación

Necesitas **Configurar áreas y frecuencias de conteo** (`Inventory.Counts.Manage`) para las áreas. Para modificar usuarios y roles necesitas además los permisos correspondientes de Administración; configurar un área no concede esos permisos.

Antes de comenzar, comprueba que las personas, artículos y bodegas estén activos y pertenezcan a la sucursal correcta.

## Crear un área

1. Entra en **Inventario → Conteos del personal → Áreas y frecuencias**.
2. Pulsa **Nueva área**.
3. Escribe un nombre, por ejemplo **Cocina**, **Bar** o **Mesas**, y deja **Área activa** habilitada.
4. Selecciona las **Personas autorizadas**.
5. Pulsa **Agregar artículo** por cada combinación de artículo y bodega.
6. Selecciona **Artículo y unidad**, **Bodega** y **Frecuencia**.
7. Si la frecuencia es semanal, elige **Día semanal**.
8. Guarda el área y comprueba sus responsables y artículos en la lista.

![Formulario de áreas y frecuencias con usuarios y artículos ficticios](asset:inventario-configuracion)

*Captura del ERP con datos de prueba. No representa existencias ni empleados reales.*

## Asignar personas

- Una persona puede contar varias áreas.
- Varias personas pueden compartir una misma área y continuar su borrador.
- Cada empleado debe usar su propio PIN operativo y tener el permiso **Realizar conteos del personal con PIN**.
- Seleccionar una persona en el área no concede automáticamente ese permiso ni crea su PIN.

El catálogo muestra usuarios activos de la sucursal; comprueba también sus permisos y PIN en Administración.

## Artículos, bodegas y frecuencias

Una combinación de **artículo y bodega** tiene una sola área responsable, incluso si el área anterior está inactiva. Para trasladarla, quítala del área anterior antes de agregarla a otra.

Las cantidades se registran en la unidad base del artículo. No se configura una conversión de presentación de compra dentro del conteo.

| Frecuencia | Comportamiento |
| --- | --- |
| Diaria | Un conteo enviado por fecha para el artículo correspondiente. |
| Semanal | Pendiente desde el día elegido hasta completarse en esa semana, de lunes a domingo. No acumula semanas anteriores. |

Evita cambiar unidades o asignaciones durante un conteo abierto. Si cambió la unidad base, el servidor puede impedir guardar y exigir revisión administrativa.

## Primera prueba

Configura un área con dos o tres artículos, asigna un empleado, comprueba su ingreso en Android y realiza un conteo completo. Luego abre [Revisar diferencias y pedir reconteo](/ayuda/inventario-revisar) con una cuenta autorizada.

El resultado esperado es que el empleado vea solo sus áreas y pendientes. Crear el área no modifica las existencias ni carga cantidades iniciales.
