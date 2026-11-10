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

## Verificación antes de cobrar

Revisa el detalle guardado y vuelve a abrirlo. Al seleccionar tarjeta compara
el total de cobro, porque puede cambiar el descuento. No confíes solo en el
precio individual mostrado en una línea para concluir cuál es el total final.

Si una cuenta anterior quedó con otro cálculo, revisa las acciones de recálculo
disponibles y comprueba el resultado antes de cobrar. Cambiar una promoción no
es una corrección retroactiva de ventas ya cobradas. No cambies fechas o días en
producción solo para probar: usa un pedido y entorno de pruebas.

Consulta [Pedidos](/ayuda/pedidos-operar) y [Cobro](/ayuda/ventas-cobrar).
