## Registrar compras y convertir cantidades de inventario

Abre **Compras → Compras**. Consulta con `Purchases.Orders.View`, crea e importa
XML con `Purchases.Orders.Create` y edita con `Purchases.Orders.Update`.
Los proveedores tienen permisos separados de consulta y gestión.

## Del documento a inventario

1. Busca el comprobante antes de crear una compra para evitar registrarlo dos veces.
2. Usa **Nueva compra** e importa el XML o completa el documento manualmente.
3. En **Documento** revisa proveedor, número, **Fecha del comprobante** y bodega
   destino. El XML puede precargar datos, pero debes comprobarlos antes de guardar.
4. En **Items** vincula cada línea con su artículo interno y revisa su destino.
   No todos los conceptos de la factura son existencias: utiliza el tratamiento
   correspondiente a inventario, gasto o distribución de costo disponible.
5. En **Resumen** comprueba cantidades, descuentos, impuestos y total.
6. Guarda y verifica la compra y los movimientos generados.

La fecha mostrada en la lista es la del comprobante registrada en la compra.
No la confundas con la fecha de autorización ni con el día en que importaste el XML.

## Unidad facturada frente a unidad de inventario

**Unidad fact.** y **Cant. fact.** describen lo comprado. **Cant. inv.** y
**Unidad inv.** describen la cantidad física que ingresará y deben ser
compatibles con el artículo. La unidad de compra no tiene que ser la base de stock.

Ejemplo sin impuestos, descuentos ni costos distribuidos: compras una lechuga
por **$0.50**, pero recibes **200 g** y el artículo se controla en kg. Registra
cantidad facturada 1, precio unitario $0.50 y cantidad de inventario 200 g
(o 0.200 kg). Ingresan 0.200 kg con costo total $0.50, equivalente a $2.50/kg.
No multipliques el precio facturado por 200 ni escribas $0.50 como costo por kg.

## Correcciones y anulaciones

Editar una compra registrada puede revertir y reconstruir sus movimientos.
Anular o eliminar exige permisos específicos y puede revertir existencias;
no son acciones cosméticas para ocultar una fila. Revisa consumos posteriores y
la respuesta del servidor antes de continuar. No hagas además un ajuste manual
por el mismo error sin comprobar el resultado.

Consulta [Artículos y unidades](/ayuda/inventario-catalogos) y
[Trazabilidad de movimientos](/ayuda/inventario-movimientos).
