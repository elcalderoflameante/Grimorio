import type { PermissionDto } from '../types';

export function resolveRolePermissionIds(values: string[], permissions: PermissionDto[]): string[] {
  const ids = new Map(permissions.map(permission => [permission.id, permission.id]));
  const codes = new Map(permissions.map(permission => [permission.code, permission.id]));

  return [...new Set(values.map(value => {
    const id = ids.get(value) ?? codes.get(value);
    if (!id) {
      throw new Error(`No se pudo identificar el permiso ${value}. Actualiza el listado antes de guardar.`);
    }
    return id;
  }))];
}
