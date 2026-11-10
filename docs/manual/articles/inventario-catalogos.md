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
