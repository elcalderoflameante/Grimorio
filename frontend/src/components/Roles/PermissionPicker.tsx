import { useState, type Key } from 'react';
import { Alert, Checkbox, Empty, Input, Space, Switch, Tag, Tooltip, Tree, Typography } from 'antd';
import { InfoCircleOutlined, SearchOutlined, SafetyOutlined } from '@ant-design/icons';
import type { PermissionDto } from '../../types';
import { groupPermissions, missingPermissionDependencies, permissionActionSection, permissionActionSections, permissionPresentation } from '../../utils/permissionPresentation';
import './PermissionPicker.css';

interface Props {
  permissions: PermissionDto[];
  value?: string[];
  onChange?: (ids: string[]) => void;
  disabled?: boolean;
}

export default function PermissionPicker({ permissions, value = [], onChange, disabled }: Props) {
  const [search, setSearch] = useState('');
  const [assignedOnly, setAssignedOnly] = useState(false);
  const [screenKey, setScreenKey] = useState<string | null>(null);
  const [expandedKeys, setExpandedKeys] = useState<Key[] | undefined>();
  const selected = new Set(value);
  const normalize = (text: string) => text.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  const query = normalize(search.trim());
  const groups = groupPermissions(permissions);
  const visibleGroups = groups.flatMap(group => {
    const screens = [...group.screens].flatMap(([name, all]) => {
      const entries = all.filter(p => (!assignedOnly || selected.has(p.id)) &&
        normalize(`${group.name} ${name} ${p.code} ${p.description}`).includes(query));
      return entries.length ? [{ key: JSON.stringify([group.name, name]), name, all, entries, module: group.name }] : [];
    });
    return screens.length ? [{ name: group.name, screens, all: [...group.screens.values()].flat() }] : [];
  });
  const visibleScreens = visibleGroups.flatMap(group => group.screens);
  const current = visibleScreens.find(screen => screen.key === screenKey) ?? visibleScreens[0];
  const missing = missingPermissionDependencies(current?.all.filter(p => selected.has(p.id)) ?? [], permissions);
  const count = (entries: PermissionDto[]) => `${entries.filter(p => selected.has(p.id)).length} de ${entries.length}`;
  const treeData = visibleGroups.map(group => ({
    key: group.name,
    selectable: false,
    title: <span className="permission-nav-label"><strong>{group.name}</strong><span>{count(group.all)}</span></span>,
    children: group.screens.map(screen => ({
      key: screen.key,
      title: <span className="permission-nav-label"><span>{screen.name}</span><span>{count(screen.all)}</span></span>,
    })),
  }));

  return <div>
    <div className="permission-toolbar">
      <Input prefix={<SearchOutlined />} aria-label="Buscar permisos" placeholder="Buscar pantalla o acción" allowClear value={search}
        disabled={disabled} onChange={event => setSearch(event.target.value)} />
      <Space><Switch size="small" checked={assignedOnly} disabled={disabled} onChange={setAssignedOnly} aria-label="Solo asignados" />Solo asignados</Space>
      <Typography.Text type="secondary">{value.length} asignados</Typography.Text>
    </div>
    {current ? <div className="permission-layout">
      <nav className="permission-navigation" aria-label="Módulos y pantallas">
        <Tree blockNode treeData={treeData} selectedKeys={[current.key]}
          expandedKeys={query ? visibleGroups.map(group => group.name) : expandedKeys ?? groups.map(group => group.name)}
          onExpand={keys => setExpandedKeys(keys)}
          onSelect={keys => { if (keys.length) setScreenKey(String(keys[0])); }} />
      </nav>
      <section className="permission-detail" aria-label={`Permisos de ${current.name}`}>
        <div className="permission-detail-heading">
          <div><Typography.Text type="secondary">{current.module}</Typography.Text>
            <Typography.Title level={5} style={{ margin: '2px 0 0' }}>{current.name}</Typography.Title></div>
          <Tag>{count(current.all)}</Tag>
        </div>
        {missing.length > 0 && <Alert type="warning" showIcon style={{ marginBottom: 12 }} title="Accesos de consulta pendientes"
          description={missing.map(p => p.description || p.code).join(' · ')} />}
        {permissionActionSections.map(action => {
          const entries = current.entries.filter(p => permissionActionSection(p.code) === action);
          if (!entries.length) return null;
          return <section key={action} className="permission-action-section" aria-label={action}>
            <Typography.Text strong className="permission-action-title">{action}</Typography.Text>
            {entries.map(permission => {
              const presentation = permissionPresentation(permission);
              return <div key={permission.id} className="permission-option">
                <Checkbox checked={selected.has(permission.id)} disabled={disabled || (!permission.isActive && !selected.has(permission.id))}
                  onChange={event => onChange?.(event.target.checked ? [...value, permission.id] : value.filter(id => id !== permission.id))}>
                  {presentation.label}
                </Checkbox>
                <Space size={6}>
                  {presentation.sensitive && <Tooltip title="Permiso sensible"><SafetyOutlined aria-label="Permiso sensible" style={{ color: '#ad6800' }} /></Tooltip>}
                  {!permission.isActive && <Tag>Inactivo</Tag>}
                  <Tooltip trigger={['hover', 'focus']} title={<><div>{presentation.detail}</div><div>{permission.code}</div></>}>
                    <button type="button" className="permission-info" aria-label={`Información: ${presentation.label}`}><InfoCircleOutlined /></button>
                  </Tooltip>
                </Space>
              </div>;
            })}
          </section>;
        })}
      </section>
    </div> : <Empty description="No hay permisos que coincidan" />}
  </div>;
}
