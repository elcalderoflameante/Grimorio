import { useCallback, useEffect, useRef, useState } from 'react';
import { Alert, Button, Drawer, Input, Select, Space, Table, Tag, Typography } from 'antd';
import { ReloadOutlined, UnorderedListOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { StockMovementDto } from '../../types';
import type { InventoryReconciliationDto, InventoryReconciliationFindingDto } from '../../types/inventoryReconciliation';
import { formatBranchDateTime } from '../../utils/branchTimeZone';
import { formatError } from '../../utils/errorHandler';
import StockMovementTrace from './StockMovementTrace';

const severityLabels = { Confirmed: 'Inconsistencia confirmada', Review: 'Revisar', Incomplete: 'Trazabilidad incompleta' };
const severityColors = { Confirmed: 'error', Review: 'warning', Incomplete: 'default' };
const quantity = (value: number | undefined, unit?: string) => value == null ? '-' : `${Number(value.toFixed(4))} ${unit || ''}`;

export default function InventoryReconciliation() {
  const [result, setResult] = useState<InventoryReconciliationDto>();
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [search, setSearch] = useState('');
  const [severity, setSeverity] = useState<string>();
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);
  const [selected, setSelected] = useState<InventoryReconciliationFindingDto>();
  const [movements, setMovements] = useState<StockMovementDto[]>([]);
  const [movementError, setMovementError] = useState<string>();
  const [movementsLoading, setMovementsLoading] = useState(false);
  const request = useRef(0);
  const load = useCallback(async () => {
    const id = ++request.current;
    setLoading(true);
    setError(undefined);
    try {
      const response = await inventoryApi.getReconciliation({ search, severity, page, pageSize });
      if (request.current === id) setResult(response.data);
    } catch (e) {
      if (request.current === id) setError(formatError(e));
    } finally {
      if (request.current === id) setLoading(false);
    }
  }, [search, severity, page, pageSize]);
  useEffect(() => { void load(); return () => { request.current++; }; }, [load]);
  useEffect(() => {
    if (!selected) return;
    let active = true;
    setMovements([]);
    setMovementError(undefined);
    setMovementsLoading(true);
    inventoryApi.getMovements({ articleId: selected.articleId, warehouseId: selected.warehouseId, pageSize: 200 })
      .then(response => { if (active) setMovements(response.data); })
      .catch(e => { if (active) setMovementError(formatError(e)); })
      .finally(() => { if (active) setMovementsLoading(false); });
    return () => { active = false; };
  }, [selected]);
  const findings = result?.findings ?? [];
  return <div>
    <Space wrap style={{ width: '100%', justifyContent: 'space-between', marginBottom: 16 }}>
      <Typography.Title level={5} style={{ margin: 0 }}>Conciliación de inventario</Typography.Title>
      <Button icon={<ReloadOutlined />} loading={loading} onClick={() => void load()}>Actualizar</Button>
    </Space>
    {error && <Alert type="error" showIcon title={error} style={{ marginBottom: 12 }} />}
    {result && <Space wrap style={{ marginBottom: 16 }}>
      <Typography.Text type="secondary">{formatBranchDateTime(result.checkedAt)} · {result.checkedBalances} saldos revisados</Typography.Text>
      <Tag color="error">{result.confirmedCount} inconsistencias</Tag>
      <Tag color="warning">{result.reviewCount} por revisar</Tag>
      <Tag>{result.incompleteCount} sin trazabilidad completa</Tag>
    </Space>}
    <Space wrap style={{ marginBottom: 16 }}>
      <Input.Search allowClear placeholder="Artículo, bodega o referencia"
        onSearch={value => { setSearch(value); setPage(1); }} style={{ width: 280, maxWidth: '100%' }} />
      <Select allowClear placeholder="Estado" value={severity} onChange={value => { setSeverity(value); setPage(1); }} style={{ width: 240 }}
        options={Object.entries(severityLabels).map(([value, label]) => ({ value, label }))} />
    </Space>
    <Table dataSource={findings} loading={loading} size="small"
      rowKey={x => `${x.code}-${x.articleId}-${x.warehouseId}-${x.sourceId || ''}-${x.unitSymbol || ''}`}
      scroll={{ x: 900 }} pagination={{ current: page, pageSize, total: result?.totalFindings ?? 0, showSizeChanger: true,
        onChange: (nextPage, nextSize) => { setPage(nextPage); setPageSize(nextSize); } }}
      locale={{ emptyText: result ? 'Sin hallazgos en esta consulta' : 'Sin diagnóstico disponible' }}
      expandable={{ expandedRowRender: x => <Space orientation="vertical">
        <span>{x.message}</span>
        {x.sourceId && <Typography.Text copyable>{x.sourceId}</Typography.Text>}
        <Button icon={<UnorderedListOutlined />} onClick={() => setSelected(x)}>Ver movimientos</Button>
      </Space> }}
      columns={[
        { title: 'Estado', dataIndex: 'severity', width: 205, render: (value: InventoryReconciliationFindingDto['severity']) => <Tag color={severityColors[value]} style={{ whiteSpace: 'normal' }}>{severityLabels[value]}</Tag> },
        { title: 'Artículo', dataIndex: 'articleName', width: 180 },
        { title: 'Bodega', dataIndex: 'warehouseName', width: 130 },
        { title: 'Hallazgo', dataIndex: 'message', width: 290 },
        { title: 'Esperado', width: 110, render: (_, x) => quantity(x.expectedQuantity, x.unitSymbol) },
        { title: 'Registrado', width: 110, render: (_, x) => quantity(x.actualQuantity, x.unitSymbol) },
        { title: 'Referencia', dataIndex: 'reference', width: 140 },
      ]} />
    <Drawer title={selected ? `${selected.articleName} · ${selected.warehouseName}` : 'Movimientos'}
      open={!!selected} onClose={() => setSelected(undefined)} size="large" styles={{ body: { padding: 16 } }}>
      {movementError && <Alert type="error" title={movementError} />}
      <Typography.Text type="secondary">Últimos 200 movimientos</Typography.Text>
      <Table dataSource={movements} loading={movementsLoading} rowKey="id" size="small" scroll={{ x: 600 }}
        pagination={{ defaultPageSize: 15 }} expandable={{ expandedRowRender: x => <StockMovementTrace movementId={x.id} /> }}
        columns={[
          { title: 'Fecha', dataIndex: 'movedAt', render: (v: string) => formatBranchDateTime(v) },
          { title: 'Cantidad base', render: (_, x) => quantity(x.baseQuantity, x.baseUnitSymbol) },
          { title: 'Referencia', dataIndex: 'reference' },
        ]} />
    </Drawer>
  </div>;
}
