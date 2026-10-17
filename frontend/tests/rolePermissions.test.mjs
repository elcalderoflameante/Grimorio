import assert from 'node:assert/strict';
import { test } from 'node:test';
import { resolveRolePermissionIds } from '../src/utils/rolePermissions.ts';

const catalog = [
  { id: '3dd768f4-7d8c-434d-b39d-7dfeb9f38d1e', code: 'POS.Orders.View', description: 'Ver pedidos', isActive: true },
  { id: 'b10d5258-d526-410a-9959-1f7ec6d788f2', code: 'POS.Orders.CorrectItems', description: 'Corregir platos sin cobrar', isActive: true },
];

test('converts existing role codes to the identifiers expected by the API', () => {
  assert.deepEqual(resolveRolePermissionIds(['POS.Orders.View'], catalog), [catalog[0].id]);
});

test('preserves existing grants while adding a newly selected permission identifier', () => {
  assert.deepEqual(resolveRolePermissionIds(['POS.Orders.View', catalog[1].id], catalog), catalog.map(p => p.id));
});

test('supports saving unchanged selections and removing a permission', () => {
  const selected = resolveRolePermissionIds(catalog.map(p => p.code), catalog);
  assert.deepEqual(resolveRolePermissionIds(selected, catalog), selected);
  assert.deepEqual(resolveRolePermissionIds(selected.slice(1), catalog), [catalog[1].id]);
});

test('does not silently discard unknown codes or identifiers', () => {
  for (const unknown of ['POS.Unknown', '00000000-0000-0000-0000-000000000000']) {
    assert.throws(() => resolveRolePermissionIds([catalog[0].id, unknown], catalog), /No se pudo identificar/);
  }
});

test('deduplicates the same permission supplied by code and identifier', () => {
  assert.deepEqual(resolveRolePermissionIds([catalog[0].code, catalog[0].id], catalog), [catalog[0].id]);
});

test('keeps assigned inactive permissions and supports an empty role', () => {
  assert.deepEqual(resolveRolePermissionIds([catalog[0].code], [{ ...catalog[0], isActive: false }]), [catalog[0].id]);
  assert.deepEqual(resolveRolePermissionIds([], catalog), []);
});
