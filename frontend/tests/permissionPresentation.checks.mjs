import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import ts from 'typescript';

async function load(path) {
  const source = await readFile(new URL(path, import.meta.url), 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString('base64')}`);
}
const { groupPermissions, permissionPresentation, missingPermissionDependencies, permissionActionSection, permissionActionSections } = await load('../src/utils/permissionPresentation.ts');
const { resolveRolePermissionIds } = await load('../src/utils/rolePermissions.ts');
const p = (code, isActive = true) => ({ id: `id-${code}`, code, description: `Label ${code}`, isActive });
const catalog = [p('POS.Orders.View'), p('POS.Orders.CorrectItems'), p('Finance.Expenses.Create'),
  p('Finance.Expenses.View'), p('Finance.Config.View'), p('Billing.PaymentMethods.View'),
  p('Billing.Payments.CorrectMethod'), p('Billing.Cash.View'), p('Custom.Special.Access'), p('Purchases.Orders.View', false)];
assert.deepEqual(groupPermissions(catalog).map(g => g.name), ['POS', 'Compras', 'Facturación', 'Finanzas', 'Custom']);
assert.equal(permissionPresentation(catalog.at(-1)).screen, 'Compras');
assert.equal(permissionPresentation(catalog[8]).screen, 'Special');
assert.equal(permissionPresentation(catalog[1]).sensitive, true);
assert.equal(permissionPresentation(catalog[0]).sensitive, false);
assert.match(permissionPresentation(catalog[1]).detail, /inventario/);
assert.deepEqual(resolveRolePermissionIds([catalog[0].code, catalog[0].id, catalog[8].code, catalog[9].code], catalog),
  [catalog[0].id, catalog[8].id, catalog[9].id]);
assert.throws(() => resolveRolePermissionIds(['Missing.Permission'], catalog));
const before = structuredClone(catalog);
assert.deepEqual(missingPermissionDependencies([catalog[2]], catalog).map(p => p.code),
  ['Finance.Expenses.View', 'Finance.Config.View', 'Billing.PaymentMethods.View']);
assert.deepEqual(missingPermissionDependencies([catalog[6]], catalog).map(p => p.code),
  ['Billing.Cash.View', 'Billing.PaymentMethods.View']);
assert.deepEqual(missingPermissionDependencies(catalog, catalog), []);
assert.deepEqual(catalog, before);
assert.equal(permissionActionSection('Admin.Users.View'), 'Consultar');
assert.equal(permissionActionSection('Admin.Users.Create'), 'Crear');
assert.equal(permissionActionSection('Admin.Users.Update'), 'Modificar');
assert.equal(permissionActionSection('Admin.Users.Delete'), 'Eliminar o anular');
assert.equal(permissionActionSection('Purchases.Orders.Cancel'), 'Eliminar o anular');
assert.equal(permissionActionSection('Menu.Items.Manage'), 'Administrar');
assert.equal(permissionActionSection('Inventory.ProductionRecipes.Restricted.View'), 'Permisos especiales');
assert.equal(permissionActionSection('Billing.Cash.Open'), 'Permisos especiales');
assert.equal(permissionActionSection('Custom.Special.Access'), 'Permisos especiales');
assert.equal(permissionActionSections[0], 'Consultar');
console.log('Permission presentation checks passed: grouping, custom and inactive grants, sensitivity, dependencies, ID resolution and no implicit mutation.');
