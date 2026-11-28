## Configurar y comprobar una promoción

Abre **POS → Promociones** con permiso de actualizar pedidos
(`POS.Orders.Update`).

1. Crea una promoción, indica nombre, tipo, prioridad y estado **Activa**.
2. Configura descuento o precio según el tipo seleccionado.
3. Revisa **Política de pago**. Si existen condiciones distintas con tarjeta,
   completa su precio o porcentaje específico.
4. Selecciona **Fechas**, **Horario**, **Días** y categorías o ítems aplicables.
5. Guarda y prueba la oferta desde Promociones en un pedido no cobrado.

## Segundo plato a mitad de precio

Usa el tipo de descuento porcentual de segunda unidad y establece 50% en la
segunda unidad para efectivo/transferencia. Si tarjeta tiene otra condición,
configura ese porcentaje expresamente. Dos platos de $11.50 dan $17.25 antes
de otros conceptos cuando la segunda unidad tiene 50% de descuento.

Agrega los platos desde **Promociones**. Agregarlos desde el menú normal no
los transforma por sí solo en líneas de esa promoción. Revisa que las líneas
pertenezcan a la misma oferta y cumplan su cantidad y vigencia.

Los productos distintos incluidos en la misma oferta pueden formar pareja.
Se ordenan por precio de mayor a menor y el descuento se calcula sobre la unidad
mas economica de cada pareja completa, sin importar cual se agrego primero.
Por ejemplo, una hamburguesa de $4.25 y otra de $5.25 con 50% en la segunda
unidad suman $7.38, con el redondeo del ERP. Si hay tres unidades, solo se
descuenta una; con cuatro, se forman dos parejas. Las promociones diferentes
y los productos agregados sin promocion no se mezclan.

El ahorro se reparte entre las lineas de la pareja para conservar los centavos
en cobros divididos. Una pareja con cobros registrados conserva su calculo:
los nuevos productos forman otro grupo, sin modificar los pagos anteriores.

## Verificación antes de cobrar

Revisa el detalle guardado y vuelve a abrirlo. Al seleccionar tarjeta compara
el total de cobro, porque puede cambiar el descuento. No confíes solo en el
precio individual mostrado en una línea para concluir cuál es el total final.

Si una cuenta anterior quedó con otro cálculo, revisa las acciones de recálculo
disponibles y comprueba el resultado antes de cobrar. Cambiar una promoción no
es una corrección retroactiva de ventas ya cobradas. No cambies fechas o días en
producción solo para probar: usa un pedido y entorno de pruebas.

Consulta [Pedidos](/ayuda/pedidos-operar) y [Cobro](/ayuda/ventas-cobrar).

## Adicionales en combos

Las opciones fijadas al definir un combo forman parte de su precio base. Las
opciones que el cliente puede elegir conservan el recargo configurado en el
plato componente. Por ejemplo, un combo de $6.99 con carne adicional de $1.50
debe sumar $8.49. Si el combo contiene dos hamburguesas y la opcion se aplica
a ambas, el recargo por combo sera $3.00.

Una promocion de precio fijo o precio alternativo con tarjeta establece el
precio base; los adicionales seleccionados se suman sobre ese precio. Los otros
tipos de descuento conservan sus reglas. Comprueba el importe al guardar,
reabrir y cobrar. Las ventas y selecciones historicas no se reescriben
automaticamente al cambiar precios del catalogo.
