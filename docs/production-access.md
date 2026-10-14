# Acceso a produccion

Las recetas, fichas operativas y registros de produccion se consultan desde
Inventario > Produccion. El antiguo submenu Recetario se retiro.

- Cajero: consultar recetas generales activas, imprimir sus fichas y registrar
  su produccion. No puede editar recetas ni acceder a preparaciones restringidas.
- Administrador: consultar y producir todas las recetas, editar ingredientes y
  preparacion, y establecer el acceso General o Restringida.
- Mesero y Cocina: no reciben acceso a Produccion con sus permisos por defecto;
  trabajan con las fichas impresas que les entregue el administrador.

El permiso `Inventory.Production.Create` habilita la operacion de produccion sin
otorgar acceso a registrar movimientos manuales de inventario. Se agrega al rol
Cajero al iniciar el backend. Los usuarios deben volver a iniciar sesion para
actualizar sus permisos. Los permisos de produccion existentes de Bodega siguen
permitiendo registrar recetas generales.

El backend restringe la consulta de recetas y fichas, el registro de produccion
y el historial con sus ingredientes. Las recetas restringidas y la edicion
requieren el rol Administrador, incluso si otro rol tiene asignados los permisos
anteriores de consulta restringida o administracion de recetas.

Clasifica aji y mayonesa como General; los alinos privados deben permanecer como
Restringida. No se reclasifican recetas automaticamente con este cambio.

Registrar produccion conserva el comportamiento existente: registra el producto
elaborado y consume sus insumos; no crea una tarea pendiente para cocina.
