## Investigar diferencias y registrar movimientos

En **Inventario → Movimientos** consulta con `Inventory.Movements.View`.
Registrar requiere `Inventory.Movements.Create`. **Conciliación** usa el permiso
de consulta de movimientos.

## Investigar primero

Filtra por artículo, bodega y período. Revisa movimientos y su trazabilidad de
origen para distinguir compras, consumos, producción y ajustes. En Conciliación
abre el artículo y bodega para inspeccionar su detalle. Una pantalla con error
no demuestra que el saldo sea cero.

## Registrar una corrección autorizada

1. Confirma la diferencia física y revisa que no exista ya un movimiento que
   la explique. Un conteo enviado no corrige el saldo automáticamente.
2. Abre **Registrar movimiento de stock** y selecciona artículo, bodega, tipo,
   cantidad y unidad. Comprueba el sentido de entrada o salida del tipo elegido.
3. Si se solicita costo, ingresa el costo por unidad base según el campo del
   formulario, no el precio de venta del plato.
4. Completa referencia y observación que permitan entender el ajuste.
5. Guarda una vez y verifica el movimiento y el saldo resultante.

No registres una segunda entrada manual para una compra que ya ingresó stock,
ni una salida para compensar una receta sin investigar la causa. Corrige el
origen cuando proceda y conserva una explicación trazable.

Los ajustes sí cambian existencias y pueden afectar valoración. El tratamiento
del IVA recuperable en el costo debe revisarse con la persona responsable de
contabilidad; esta guía no determina ese criterio.

Consulta [Conteos y revisión](/ayuda/inventario-revisar) y
[Registro de compras](/ayuda/compras-registrar).
