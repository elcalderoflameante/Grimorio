## Tomar y dar seguimiento a un pedido

Abre **POS → Pedidos**. Consultar requiere `POS.Orders.View`; crear,
actualizar, cancelar y cobrar son permisos diferentes. El POS web exige abrir
caja antes de iniciar pedidos. No prestes tu usuario para sortear un permiso.

## Registrar el consumo

1. En **Mesas**, selecciona la mesa correcta. Para llevar o domicilio usa su
   pestaña y completa los datos del tipo de pedido.
2. Agrega los platos y selecciona términos, sabores, tamaños y acompañamientos
   obligatorios. Revisa observaciones y cantidades antes de confirmar.
3. Para una oferta, selecciona el producto desde **Promociones**; agregar el
   mismo plato desde el menú normal no selecciona automáticamente esa oferta.
4. Confirma el pedido y verifica su número y mesa. Un borrador no equivale a una
   comanda confirmada.
5. Abre de nuevo el pedido para registrar adicionales y comprobar sus estados.

Antes de cambiar opciones o quitar platos ya confirmados, revisa las acciones
permitidas y coordina con cocina. Una corrección de platos requiere
`POS.Orders.CorrectItems`; cancelar no es equivalente a borrar un borrador.
Las restricciones del estado y del servidor siguen aplicando.

## Seguimiento del mapa

Con la conexión activa, el mapa recibe avisos de nuevos pedidos, confirmaciones,
cambios de mesa, cancelaciones y cobros, y consulta el estado actualizado de la
sucursal. Los minutos transcurridos avanzan en el equipo sin consultar al servidor.

Al recuperar la conexión o volver a la pestaña se vuelve a consultar el mapa.
Como respaldo, también se actualiza cada dos minutos mientras la pestaña esté
visible. **Actualizar** permite solicitar una consulta manual. Si aparece un aviso
de error, las mesas anteriores siguen visibles: no presupongas que esos saldos
siguen vigentes hasta recuperar la conexión.

## Entrega y cobro

Confirma que el pedido esté asociado a la mesa correcta antes de imprimir la
cuenta o cambiarlo de mesa. Los estados de preparación no demuestran pago.
Para registrar el dinero sigue [Cobrar y dividir una cuenta](/ayuda/ventas-cobrar).

Si falla la conexión, consulta el pedido existente antes de crear otro. Un aviso
de error o una pantalla sin actualizar no demuestra que la operación no se haya
guardado. Ante diferencias revisa el detalle y los cobros registrados.

Consulta [Estaciones](/ayuda/apps-estaciones) y
[Promociones](/ayuda/menu-promociones).
