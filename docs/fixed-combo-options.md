# Opciones, Tamanos Y Combos

En Items y recetas, configurar primero las opciones del plato individual.
En cada opcion se conserva el consumo principal existente y se pueden agregar
hasta 20 consumos adicionales con articulo, cantidad y unidad compatible.
Estos consumos fijos se SUMAN a la receta base; no la reemplazan. Para una
salsa que se elige por separado, usar las reglas de cantidad descritas abajo,
no agregar una salsa fija adicional al tamano.

Al editar un combo, seleccionar el plato y fijar las opciones de sus grupos,
o marcar Cliente elige para los grupos que se seleccionan al tomar el pedido.
Por ejemplo, fijar Tamano = 300 g y dejar Salsa a eleccion del cliente.
Se validan obligatoriedad, minimos, maximos, repeticiones y pertenencia de cada
opcion. Los componentes siguen siendo distintos por plato; no hay combos
anidados. El precio adicional de la opcion NO se suma al precio del combo.
La venta individual continua aplicando el precio adicional normalmente.

Al tomar el pedido, se guarda la receta expandida por componente y un resumen
de opciones para la estacion. Cambiar despues la receta, las opciones o el
combo no modifica el consumo ni la preparacion de pedidos ya creados. El
cobro y la factura conservan una sola linea correspondiente al combo.

Guardar modificadores conserva los IDs existentes. Quitar o desactivar una
opcion fijada deja el combo no disponible y obliga a corregirlo; no se elige
otra porcion automaticamente. Los combos anteriores sin opciones conservan
su configuracion. Si se agrega un grupo obligatorio al plato, se debe fijar
tambien en sus combos (o dejarlo a eleccion del cliente) antes de venderlos.

## Cantidades Segun Tamano

Configurar Tamano como grupo obligatorio de una sola seleccion. Cada opcion
conserva su precio adicional y su consumo de costilla. En el grupo Salsa
seleccionar Cantidad de inventario segun = Tamano y definir la cantidad para
cada tamano (por ejemplo 30, 50 y 80 g). Cada sabor indica su propio articulo
y la misma unidad de consumo. La regla reemplaza la cantidad principal de
la opcion de salsa; no suma otra salsa por separado. Los consumos adicionales
de la opcion siguen siendo fijos. No se permiten ciclos ni reglas incompletas.

Si la receta base ya descuenta una unidad de costilla, la opcion 300 g no
debe repetirla: dejar esa opcion sin consumo adicional y configurar los
incrementos en las otras porciones. Si la base no incluye costilla, las
opciones pueden descontar 1, 2 y 3 unidades. Tampoco duplicar la salsa elegida
mediante una salsa fija en la receta base.

La seleccion se resuelve en el servidor y se guarda con su cantidad exacta.
En combos se combina con las opciones fijadas y se congela en la receta de
la tarea, sin duplicar el consumo en la linea padre. El catalogo usa cantidades
minimas cuando falta escoger tamano; el pedido QR y la confirmacion validan
la cantidad exacta. La disponibilidad de cada salsa se calcula por item,
sin mezclar el plato individual con combos de distintas porciones.

Aplicar AddFixedComboOptionsAndModifierIngredients antes de desplegar. Agrega
JSON estructurado para selecciones y consumos adicionales, con listas vacias
como valor inicial, y el resumen de opciones de la tarea de preparacion.
No modifica recetas ni movimientos historicos.

Aplicar tambien AddSizeDependentModifiersAndComboChoices antes de desplegar.
No recalcula consumos de pedidos anteriores.

Pruebas: backend/tests/ComboWorkflow.Checks usa exclusivamente la base aislada
127.0.0.1:15432, database checks, usuario checks. Cubre reserva, cobro parcial,
reintentos, snapshot despues de editar, costos, disponibilidad, cocina y QR.
No envia facturas al SRI ni correos.
