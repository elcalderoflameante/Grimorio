import { useCallback, useEffect, useState } from 'react';
import { Table, Select, Space, Typography, message, Tag, Badge, Button } from 'antd';
import dayjs from 'dayjs';
import { ReloadOutlined, WarningOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { WarehouseStockDto, WarehouseDto, InventoryCategoryDto, ArticleType } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title } = Typography;

const TIPO_COLOR: Record<string, string> = {
  Ingredient: 'blue',
  FinishedProduct: 'green',
  Supply: 'orange',
};

const formatQuantity = (value: number, unit: string) => `${Number(value).toFixed(2)} ${unit}`;

export default function CurrentStock() {
  const [stock, setStock] = useState<WarehouseStockDto[]>([]);
  const [bodegas, setBodegas] = useState<WarehouseDto[]>([]);
  const [categorias, setCategorias] = useState<InventoryCategoryDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [filterBodega, setFilterBodega] = useState<string | undefined>();
  const [filterCategoria, setFilterCategoria] = useState<string | undefined>();
  const [lowStockOnly, setLowStockOnly] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const [s, b, c] = await Promise.all([
        inventoryApi.getStock({ warehouseId: filterBodega, categoryId: filterCategoria, lowStockOnly }),
        inventoryApi.getWarehouses(),
        inventoryApi.getCategories(),
      ]);
      setStock(s.data);
      setBodegas(b.data);
      setCategorias(c.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  }, [filterBodega, filterCategoria, lowStockOnly]);

  useEffect(() => { load(); }, [load]);

  const bajoStockCount = stock.filter(s => s.lowStock).length;
  const reservedCount = stock.filter(s => s.reservedQuantity > 0).length;

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Space>
          <Title level={5} style={{ margin: 0 }}>Stock Actual</Title>
          {bajoStockCount > 0 && (
            <Tag color="warning" icon={<WarningOutlined />}>
              {bajoStockCount} artículo{bajoStockCount > 1 ? 's' : ''} bajo stock
            </Tag>
          )}
          {reservedCount > 0 && (
            <Tag color="processing">
              {reservedCount} con reserva
            </Tag>
          )}
        </Space>
        <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
      </div>

      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear
          placeholder="Filtrar por bodega"
          style={{ width: 200 }}
          options={bodegas.map(b => ({ label: b.name, value: b.id }))}
          onChange={setFilterBodega}
        />
        <Select
          allowClear
          placeholder="Filtrar por categoría"
          style={{ width: 200 }}
          options={categorias.map(c => ({ label: c.name, value: c.id }))}
          onChange={setFilterCategoria}
        />
        <Button
          type={lowStockOnly ? 'primary' : 'default'}
          danger={lowStockOnly}
          icon={<WarningOutlined />}
          onClick={() => setLowStockOnly(v => !v)}
        >
          Solo bajo stock
        </Button>
      </Space>

      <Table
        dataSource={stock}
        rowKey={r => `${r.articleId}-${r.warehouseId}`}
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 30, showSizeChanger: true, pageSizeOptions: ['10', '30', '50', '100'] }}
        rowClassName={r => r.lowStock ? 'ant-table-row-danger' : ''}
        columns={[
          {
            title: 'Artículo', key: 'articulo',
            render: (_: unknown, s: WarehouseStockDto) => (
              <Space>
                {s.lowStock && <WarningOutlined style={{ color: '#ff4d4f' }} />}
                <span>{s.articleName}</span>
                {s.internalCode && <Tag>{s.internalCode}</Tag>}
              </Space>
            ),
          },
          {
            title: 'Tipo', dataIndex: 'type', key: 'tipo',
            render: (v: ArticleType) => <Tag color={TIPO_COLOR[v]}>{v}</Tag>,
          },
          { title: 'Categoría', dataIndex: 'categoryName', key: 'categoryName' },
          { title: 'Bodega', dataIndex: 'warehouseName', key: 'warehouseName' },
          {
            title: 'Stock fisico', key: 'cantidad',
            render: (_: unknown, s: WarehouseStockDto) => (
              <Badge
                status={s.lowStock ? 'error' : 'success'}
                text={formatQuantity(s.quantity, s.unitSymbol)}
              />
            ),
          },
          {
            title: 'Reservado', key: 'reservedQuantity',
            render: (_: unknown, s: WarehouseStockDto) =>
              s.reservedQuantity > 0
                ? <Tag color="processing">{formatQuantity(s.reservedQuantity, s.unitSymbol)}</Tag>
                : <Typography.Text type="secondary">0 {s.unitSymbol}</Typography.Text>,
          },
          {
            title: 'Disponible', key: 'availableQuantity',
            render: (_: unknown, s: WarehouseStockDto) => (
              <Badge
                status={s.lowStock ? 'error' : 'success'}
                text={formatQuantity(s.availableQuantity, s.unitSymbol)}
              />
            ),
          },
          {
            title: 'Stock mín.', key: 'stockMin',
            render: (_: unknown, s: WarehouseStockDto) => formatQuantity(s.minStock, s.unitSymbol),
          },
          {
            title: 'Última actualización', dataIndex: 'lastUpdatedAt', key: 'lastUpdatedAt',
            render: (v: string) => dayjs(v).format('DD/MM/YYYY HH:mm'),
          },
        ]}
      />
    </div>
  );
}
