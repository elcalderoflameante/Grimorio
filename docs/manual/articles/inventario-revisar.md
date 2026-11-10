## Permisos

- **Consultar conteos del personal** (`Inventory.Counts.View`) permite entrar y consultar los documentos.
- Para guardar una decisión necesitas también **Revisar diferencias de conteos del personal** (`Inventory.Counts.Review`).

Consultar no autoriza a configurar áreas ni registrar ajustes de stock.

## Abrir un documento

1. Entra en **Inventario → Conteos del personal → Pendientes de revisión**.
2. Ajusta el rango de fechas si el documento es de un día anterior.
3. Pulsa **Revisar detalle**.
4. Revisa artículo, bodega, unidad, cantidad contada, saldo del sistema, diferencia, hora y observación.

![Detalle de revisión de un conteo con cantidades ficticias](asset:inventario-revision)

*Captura con datos ficticios. Las cantidades solo ilustran el procedimiento.*

## Interpretar una diferencia

**Diferencia = cantidad contada − cantidad esperada guardada.** Un valor negativo indica menos unidades físicas que el saldo usado en la comparación; uno positivo indica más.

El saldo se guarda al confirmar o cambiar la cantidad de cada artículo, no cuando abres el detalle por la mañana. Ventas posteriores no recalculan esa diferencia histórica. Las líneas pueden tener horas de comparación distintas.

Antes de concluir que hubo una pérdida, comprueba unidades, bodega, ventas o movimientos simultáneos, observaciones y si corresponde repetir el conteo.

## Registrar la decisión

En un documento **Pendiente de revisión**, selecciona una opción:

| Decisión | Resultado |
| --- | --- |
| Marcar como revisado | Conserva el conteo y registra la revisión. No corrige stock. |
| Descartar con motivo | Conserva el documento marcado como descartado. No corrige stock. |
| Solicitar nuevo conteo | Deja una solicitud para que el personal vuelva a contar. No reabre el original. |

Escribe el motivo u observación, obligatorio, y pulsa **Guardar revisión**. La decisión registra responsable y hora. No sobrescribas una revisión anterior; el flujo no permite volver a revisar un documento que ya tiene decisión.

## Reconteo

El personal verá **Realizar reconteo**. El nuevo documento conserva el vínculo con el original, incluye sus artículos y los nuevos pendientes aplicables. Al enviarlo, el original pasa a **Con reconteo enviado**. Revisa el nuevo documento por separado.

## Historial y ajustes

En **Historial** puedes filtrar por fechas y estado. Cada consulta permite un rango de hasta un año y devuelve como máximo 500 documentos; reduce el rango si necesitas consultar un período con más registros.

> **No hay un botón de ajustar inventario desde la revisión.** Si después de investigar corresponde corregir existencias, registra el movimiento por separado con la autorización correspondiente.
