# Stock Negativo Opcional por Articulo

`InventoryArticle.AllowNegativeStock`, por sucursal y artículo, desactivado por
defecto. La migración `20261111173602_AllowNegativeInventoryStock` añade únicamente
la columna booleana con valor predeterminado falso; no activa artículos ni modifica saldos.
La API aplica las migraciones pendientes al arrancar. Reconstruir y reiniciar API
y frontend para usar el cambio. No desplegar solo el frontend.

## Administración

Inventario > Artículos > Editar > Permitir stock negativo. Conserva el permiso
existente `Inventory.Articles.Manage`. El DTO de actualización es nullable para
que un cliente antiguo que no envíe el campo no desactive una opción ya configurada.
Crear sin el campo mantiene el control estricto.

## Ventas

- La disponibilidad compartida usa stock real menos reservas activas, políticas
  vigentes y aislamiento de sucursal. No inventa existencias ni cantidades infinitas.
- Solo los artículos autorizados dejan de limitar la capacidad. Un plato con carne
  estricta y ensalada aproximada queda limitado por la carne. Las opciones siguen
  comprobando conversiones, actividad y límites de selección.
- Cuando todos los ingredientes relevantes admiten negativos, la disponibilidad
  no tiene límite numérico (`AvailableQuantity = null`, `IsTracked = false` en
  el contrato de disponibilidad). Esto significa "sin límite para seleccionar",
  no "sin consumo de inventario". Mantiene compatibilidad con clientes que usan
  `IsTracked` para decidir si deben imponer un máximo en pantalla.
- La reserva mantiene el consumo completo, incluida la parte sin existencia.
  Usa primero saldos positivos y asigna el déficit a una bodega activa con registro
  del artículo; sin registro usa la primera bodega activa. Sin bodega activa rechaza
  la operación. No crea una bodega ficticia ni aumenta saldos.
- Los artículos se bloquean antes de sus filas de stock, en orden estable, incluso
  si aún no hay stock en bodega. Esto conserva la exclusión mutua de reservas estrictas.
- Los cobros totales/parciales siguen consumiendo las reservas mediante movimientos
  ordinarios. Se conservan idempotencia, cancelación y liberación de reservas.
  La corrección con merma preparada respeta la misma opción.
- Desactivar la opción vuelve a exigir saldo para nuevos pedidos. No reescribe
  movimientos ni elimina compromisos ya registrados.

No cambia compras, conteos, ajustes ni reglas de producción de inventario. Un
saldo negativo continúa apareciendo en movimientos, stock y alertas configuradas.
Conciliación conserva `NegativeStock`; no trata como `OverReserved` las reservas
excedidas autorizadas expresamente por esta política. Otros hallazgos no se ocultan.
El stock físico se corrige mediante el flujo autorizado, no al activar el interruptor.

## Comprobaciones

`backend/tests/NegativeStock.Checks` usa el mediador, handlers y PostgreSQL reales,
solo con usuario `grimorio_checks` y base local `grimorio_checks`. Aplica la migración
solo allí y crea datos sintéticos bajo una sucursal identificada en la salida.
Los fixtures permanecen en la base aislada; nunca usa `.env`, producción, SRI o correo.

```powershell
dotnet run --project backend/tests/NegativeStock.Checks -- path/to/ignored-checks-connection.json
```

El JSON local contiene `host`, `port`, `database`, `username`, `password` y no debe
subirse al repositorio. Se verifica valor predeterminado, edición compatible,
menú POS/QR, opciones, conversiones, reservas completas, cobros parciales,
reintentos, cancelación, combos, concurrencia estricta y marcas manuales de agotado.
La prueba de navegador intercepta todas las llamadas API con datos simulados:

```powershell
node frontend/tests/negativeStockArticles.browser.mjs
```
