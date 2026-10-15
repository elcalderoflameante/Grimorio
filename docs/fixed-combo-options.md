# Opciones fijadas en combos

En Items y recetas, configurar primero las opciones del plato individual.
En cada opcion se conserva el consumo principal existente y se pueden agregar
hasta 20 consumos adicionales con articulo, cantidad y unidad compatible.
Estos consumos se SUMAN a la receta base; no la reemplazan. Por ejemplo, si
la base descuenta 300 g de costilla y 30 g de salsa, la opcion 600 g agrega
300 g de costilla y 20 g de salsa para un total de 600 g y 50 g.

Al editar un combo, seleccionar el plato y fijar las opciones de sus grupos.
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
tambien en sus combos antes de volver a venderlos.

Aplicar AddFixedComboOptionsAndModifierIngredients antes de desplegar. Agrega
JSON estructurado para selecciones y consumos adicionales, con listas vacias
como valor inicial, y el resumen de opciones de la tarea de preparacion.
No modifica recetas ni movimientos historicos.

Pruebas: backend/tests/ComboWorkflow.Checks usa exclusivamente la base aislada
127.0.0.1:15432, database checks, usuario checks. Cubre reserva, cobro parcial,
reintentos, snapshot despues de editar, costos, disponibilidad, cocina y QR.
No envia facturas al SRI ni correos.
