import type { PermissionDto } from '../types';

export const permissionActionSections = ['Consultar', 'Crear', 'Modificar', 'Eliminar o anular', 'Administrar', 'Permisos especiales'] as const;

export function permissionActionSection(code: string): typeof permissionActionSections[number] {
  const parts = code.split('.');
  if (parts.length !== 3) return 'Permisos especiales';
  switch (parts[2]) {
    case 'View': return 'Consultar';
    case 'Create': return 'Crear';
    case 'Update': return 'Modificar';
    case 'Delete':
    case 'Cancel': return 'Eliminar o anular';
    case 'Manage': return 'Administrar';
    default: return 'Permisos especiales';
  }
}

const modules: Record<string, string> = {
  Admin: 'Administración', RRHH: 'Personal', POS: 'POS', Menu: 'Menú',
  Inventory: 'Inventario', Purchases: 'Compras', Billing: 'Facturación', Finance: 'Finanzas',
};
const screens: Record<string, string> = {
  Users: 'Usuarios', Roles: 'Roles', Permissions: 'Permisos', Branch: 'Sucursal',
  Employees: 'Empleados', Positions: 'Cargos', Scheduling: 'Horarios y turnos',
  Payroll: 'Rol de pagos', Attendance: 'Asistencia', Orders: 'Pedidos',
  DirectSale: 'Venta directa', Kitchen: 'Cocina', Stations: 'Estaciones',
  Tables: 'Mesas QR', TableRequests: 'Atención QR', Categories: 'Categorías',
  Items: 'Ítems y recetas', StockConsume: 'Consumo de inventario', Config: 'Configuración',
  Articles: 'Artículos', Stock: 'Stock actual', Movements: 'Movimientos',
  Production: 'Producción', ProductionRecipes: 'Recetas de producción', Suppliers: 'Proveedores',
  Customers: 'Clientes', Cash: 'Caja y ventas', Payments: 'Correcciones de pagos',
  CashRegisters: 'Cajas', PaymentMethods: 'Medios de pago', Tax: 'Tarifas fiscales',
  Sri: 'Documentos electrónicos y SRI', Expenses: 'Gastos',
};
const details: Record<string, string> = {
  'Billing.Cash.View': 'Acceso compartido a Caja, Ventas, Propinas y Rentabilidad. Las acciones de cobro y corrección requieren sus propios permisos.',
  'Menu.Items.View': 'Acceso compartido a Ítems y recetas, Subrecetas y Rentabilidad del menú.',
  'POS.Orders.Update': 'Permite editar, confirmar y entregar pedidos; también habilita la pantalla de Promociones.',
  'POS.Orders.CorrectItems': 'Permite corregir platos sin cobrar, incluso preparados o entregados. Exige motivo y tratamiento del inventario; no modifica platos ya cobrados.',
  'Billing.Payments.CorrectMethod': 'Reclasifica el medio de pago de una nota de venta con caja abierta. Exige motivo y conserva auditoría; no vuelve a cobrar ni modifica inventario.',
  'Inventory.ProductionRecipes.Restricted.View': 'Acceso a recetas restringidas. Se mantiene además la restricción al rol Administrador.',
  'Inventory.ProductionRecipes.Manage': 'Administración de recetas de producción. Se mantiene además la restricción al rol Administrador.',
  'Admin.Roles.Update': 'Permite editar roles y asignar permisos, lo que puede ampliar el acceso de otros usuarios.',
  'Admin.Permissions.Manage': 'Modifica el catálogo de permisos del sistema.',
  'Billing.Sri.Manage': 'Modifica la configuración SRI, certificado, correo y plantilla de facturación.',
};

export function permissionPresentation(permission: PermissionDto) {
  const [moduleKey, screenKey] = permission.code.split('.');
  return {
    module: modules[moduleKey] ?? moduleKey ?? 'Otros',
    screen: moduleKey === 'Purchases' && screenKey === 'Orders' ? 'Compras' : screens[screenKey] ?? screenKey ?? 'Otros',
    label: permission.description || permission.code,
    sensitive: /\.(Delete|Cancel|CorrectItems|CorrectMethod|Manage|Enroll)$|\.Restricted\./.test(permission.code) || permission.code === 'Admin.Roles.Update',
    detail: details[permission.code] ?? permission.description ?? permission.code,
  };
}

export function groupPermissions(permissions: PermissionDto[]) {
  const groups = new Map<string, { name: string; screens: Map<string, PermissionDto[]> }>();
  for (const permission of permissions) {
    const { module, screen } = permissionPresentation(permission);
    if (!groups.has(module)) groups.set(module, { name: module, screens: new Map() });
    const group = groups.get(module)!;
    if (!group.screens.has(screen)) group.screens.set(screen, []);
    group.screens.get(screen)!.push(permission);
  }
  const order = Object.values(modules);
  return [...groups.values()].sort((a, b) => {
    const rank = (name: string) => order.includes(name) ? order.indexOf(name) : order.length;
    return rank(a.name) - rank(b.name) || a.name.localeCompare(b.name, 'es');
  });
}

// Advisory only: never add grants implicitly or prevent intentional custom roles.
export function missingPermissionDependencies(selected: PermissionDto[], catalog: PermissionDto[]) {
  const codes = new Set(selected.map(p => p.code));
  const available = new Map(catalog.map(p => [p.code, p]));
  const missing = new Map<string, PermissionDto>();
  for (const permission of selected) {
    const segments = permission.code.split('.');
    const candidates: string[] = [];
    if (segments.length === 3 && segments[2] !== 'View') candidates.push(`${segments[0]}.${segments[1]}.View`);
    if (permission.code === 'Finance.Expenses.Create') candidates.push('Finance.Config.View', 'Billing.PaymentMethods.View');
    if (permission.code === 'Billing.Payments.CorrectMethod') candidates.push('Billing.Cash.View', 'Billing.PaymentMethods.View');
    for (const code of candidates) {
      const dependency = available.get(code);
      if (dependency && !codes.has(code)) missing.set(code, dependency);
    }
  }
  return [...missing.values()];
}
