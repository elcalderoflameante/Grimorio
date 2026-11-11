## Artículos, unidades y consulta de existencias

En **Inventario → Configuración** se mantienen unidades, conversiones,
categorías y bodegas. Consulta con `Inventory.Config.View` y modifica con
`Inventory.Config.Manage`.

## Preparar el artículo

1. En **Artículos** busca primero si ya existe. Consulta con
   `Inventory.Articles.View`; crea o edita con `Inventory.Articles.Manage`.
2. Define nombre, código, tipo, categoría y **Unidad base**.
3. Revisa mínimo, alerta de stock y estado activo. No dupliques un artículo
   solo para recibirlo en otra presentación de compra.
4. Comprueba conversiones compatibles antes de usarlo en recetas o movimientos.

La unidad base determina cómo se expresan los saldos y costos. No la cambies
como solución rápida a un error histórico: revisa antes recetas, compras y
movimientos relacionados.

## Ingredientes de consumo aproximado

En la edición del artículo, **Permitir stock negativo** está apagado por defecto.
Actívalo solo para ingredientes cuyo consumo por receta sea aproximado, como
lechuga, tomate o cebolla. No lo actives para gaseosas u otros productos que
deban agotarse al llegar a cero.

El pedido podrá reservar esos ingredientes y se seguirá descontando el consumo
al cobrar, aunque quede un saldo negativo. Las carnes y demás ingredientes de
control estricto del mismo plato siguen limitando la venta. También se aplica
al menú QR, opciones y componentes de combos. Un artículo inactivo o un plato
marcado como no disponible no se habilita por esta opción.

Conserva las alertas de stock y revisa los negativos. El
[conteo del personal](/ayuda/inventario-contar) registra lo encontrado; después
un responsable investiga la diferencia y registra un
[ajuste autorizado](/ayuda/inventario-movimientos). El conteo no ajusta existencias
por sí solo. Para un agotado físico real, marca el plato como no disponible.

## Consultar

En **Stock actual**, con `Inventory.Stock.View`, usa **Buscar por nombre** y
**Filtrar por tipo**, además de los filtros disponibles. Confirma la bodega y
la unidad: dos saldos de bodegas distintas no son necesariamente duplicados.

En Artículos abre la consulta de uso en menú para identificar dónde participa
el producto. Esto permite investigar una receta; no representa el consumo real
de un período ni sustituye el historial de movimientos.

Crear el artículo no ingresa una cantidad física. Para cargar existencias usa
una [compra](/ayuda/compras-registrar), una [producción](/ayuda/inventario-produccion)
o un [movimiento autorizado](/ayuda/inventario-movimientos), según el origen.
Un [conteo del personal](/ayuda/inventario-contar) solo registra la observación.
