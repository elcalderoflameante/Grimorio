# Combos con varias estaciones

En Menu > Items y recetas, crea un item con Tipo de item = Combo y agrega los
platos y cantidades que lo componen. El precio, codigo fiscal, IVA y visibilidad
pertenecen al combo. Cada componente conserva su estacion y receta.

Un componente puede tener visibilidad Solo combos para no aparecer en la venta
individual ni en el menu QR. No necesita tener precio de venta individual para
calcular el costo del combo. Puede seguir vendiendose por separado si tiene la
visibilidad Menu y promociones.

Esta version admite componentes fijos: no admite combos anidados ni platos con
opciones obligatorias. Para una variante fija (por ejemplo alitas con una salsa
determinada), registra su receta como un componente sin opciones obligatorias.
Los combos no tienen receta ni modificadores propios. Su ficha operativa imprime
los componentes, cantidades y estaciones; cada plato conserva su ficha completa.

## Pedidos y cocina

La mesera agrega una sola linea. Al enviar a preparar, cada estacion recibe sus
componentes, indicando el combo y el pedido. Los monitores web y la app de
estaciones utilizan el mismo contrato e identificadores de tarea. Las cantidades
se multiplican por el numero de combos. Alexa tambien opera sobre esas tareas.

Cada componente tiene su estado. El combo queda En preparacion cuando comienza
alguna parte o hay alguna lista, y Listo solo cuando todas estan listas. No puede
entregarse el pedido mientras tenga componentes de combo pendientes. No puede
marcarse el combo completo desde una estacion. Las cancelaciones siguen las
reglas existentes: no cancelar componentes por separado ni cancelar un combo que
ya empezo a prepararse o fue cobrado. Cancelar un combo pendiente libera sus
reservas y notifica a las estaciones.

La composicion, nombres de preparacion, estaciones e ingredientes en unidad base
se guardan al crear la linea del pedido, incluso en borrador. Editar luego el
combo o la receta no modifica esas tareas ni ingredientes. Un item con pedidos
no puede cambiar entre Plato y Combo: debe crearse otro item para cambiar su tipo.
Un combo con pedidos debe desactivarse en lugar de eliminarse para conservar
sus referencias e historial.
Los pedidos y combos registrados antes de esta migracion mantienen su conducta.

## Inventario y cobros

Se reservan los ingredientes de todos los componentes, agrupando los articulos
compartidos. Los pagos parciales y sus reintentos siguen ligados a la unica linea
del combo. Las tareas no son ventas adicionales y no generan cobros, impuestos,
facturas ni movimientos independientes. Inventario y rentabilidad de ventas usan
los ingredientes guardados en el pedido; la rentabilidad del catalogo utiliza la
composicion actual. Se mantiene el metodo existente de costo promedio de compras.

La confirmacion y el cobro conservan sus transacciones, bloqueo de orden e
idempotencia. Un fallo al registrar movimientos revierte el cobro y sus cambios
de inventario. El envio al SRI y el correo siguen usando el procesamiento existente.

## Despliegue y pruebas

Aplicar `20261014215841_AddMultiStationCombos` antes de utilizar los nuevos
endpoints. Esta migracion agrega IsCombo (false para los existentes), componentes
y tareas de preparacion. No modifica precios, recetas ni pedidos anteriores.

`backend/tests/ComboWorkflow.Checks` prueba contra PostgreSQL real aislado.
Requiere COMBO_CHECKS_CONNECTION con host 127.0.0.1, puerto 15432, base checks y
usuario checks; rechaza otros destinos. COMBO_CHECKS_ALLOW_RESET=true elimina y
recrea SOLO esa base aislada para repetir las pruebas. No usar una base con datos
que se quieran conservar. Las pruebas no envian correos ni solicitudes al SRI.
