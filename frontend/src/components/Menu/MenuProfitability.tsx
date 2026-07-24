import { useEffect, useMemo, useState } from 'react';
import { App as AntApp, Alert,
  Button,
  Card,
  Col,
  Progress,
  Row,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
  Tooltip,
  Typography } from 'antd';
import { BarChartOutlined, ReloadOutlined, WarningOutlined } from '@ant-design/icons';
import { menuApi } from '../../services/api';
import type {
  MenuCategoryDto,
  MenuItemProfitabilityDto,
  MenuItemProfitabilityIngredientDto,
} from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Text, Title } = Typography;

const money = (value?: number) => `$${(value ?? 0).toFixed(2)}`;
const pct = (value?: number) => `${(value ?? 0).toFixed(2)}%`;

const statusColor: Record<string, string> = {
  Healthy: 'green',
  Low: 'blue',
  High: 'orange',
  Critical: 'red',
  Warning: 'gold',
  NoRecipe: 'default',
};

const gaugeColor = (value: number) => {
  if (value <= 0) return '#8c8c8c';
  if (value < 25) return '#1677ff';
  if (value <= 35) return '#52c41a';
  if (value <= 45) return '#faad14';
  return '#ff4d4f';
};

export default function MenuProfitability() {
  const { message } = AntApp.useApp();

  const [items, setItems] = useState<MenuItemProfitabilityDto[]>([]);
  const [categories, setCategories] = useState<MenuCategoryDto[]>([]);
  const [categoryId, setCategoryId] = useState<string | undefined>();
  const [loading, setLoading] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const [profitabilityRes, categoriesRes] = await Promise.all([
        menuApi.getProfitability({ categoryId, activeOnly: true }),
        menuApi.getCategories(),
      ]);
      setItems(profitabilityRes.data);
      setCategories(categoriesRes.data);
    } catch (error) {
      message.error(formatError(error));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [categoryId]);

  const summary = useMemo(() => {
    const priced = items.filter(i => i.netSalePrice > 0 && i.hasRecipe && !i.hasMissingCosts && !i.hasConversionWarnings);
    const avgFoodCost = priced.length
      ? priced.reduce((sum, item) => sum + item.foodCostPercentage, 0) / priced.length
      : 0;
    const healthy = items.filter(i => i.status === 'Healthy').length;
    const warnings = items.filter(i => i.hasMissingCosts || i.hasConversionWarnings || !i.hasRecipe).length;
    const avgProfit = priced.length
      ? priced.reduce((sum, item) => sum + item.grossProfit, 0) / priced.length
      : 0;
    return { avgFoodCost, healthy, warnings, avgProfit };
  }, [items]);

  const categoryOptions = categories.map(c => ({ label: c.name, value: c.id }));

  const ingredientColumns = [
    {
      title: 'Ingrediente',
      key: 'article',
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Space orientation="vertical" size={0}>
          <Text>{row.articleName || 'Sin nombre'}</Text>
          {row.internalCode && <Text type="secondary" style={{ fontSize: 12 }}>{row.internalCode}</Text>}
        </Space>
      ),
    },
    {
      title: 'Cantidad receta',
      width: 140,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text>{row.quantity} {row.unitSymbol}</Text>
      ),
    },
    {
      title: 'Cantidad base',
      width: 140,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text>{row.baseQuantity.toFixed(4)} {row.baseUnitSymbol}</Text>
      ),
    },
    {
      title: 'Costo prom.',
      width: 120,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text>{money(row.averageUnitCost)}</Text>
      ),
    },
    {
      title: 'Ultimo costo',
      width: 120,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text type="secondary">{row.lastUnitCost === undefined ? '-' : money(row.lastUnitCost)}</Text>
      ),
    },
    {
      title: 'Total',
      width: 120,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text strong>{money(row.totalCost)}</Text>
      ),
    },
    {
      title: 'Peso',
      width: 90,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => pct(row.costSharePercentage),
    },
    {
      title: 'Estado',
      width: 180,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        row.warning
          ? <Tooltip title={row.warning}><Tag color="gold" icon={<WarningOutlined />}>Revisar</Tag></Tooltip>
          : <Tag color="green">OK</Tag>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', gap: 12, marginBottom: 16, flexWrap: 'wrap' }}>
        <Space orientation="vertical" size={0}>
          <Title level={5} style={{ margin: 0 }}>Rentabilidad de platos</Title>
          <Text type="secondary">Precio neto sin IVA vs costo promedio neto de ingredientes.</Text>
        </Space>
        <Space wrap>
          <Select
            allowClear
            placeholder="Todas las categorias"
            value={categoryId}
            options={categoryOptions}
            onChange={setCategoryId}
            style={{ width: 220 }}
          />
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading}>Actualizar</Button>
        </Space>
      </div>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message="El IVA de ventas se excluye de la utilidad. El IVA de compras tampoco se suma al costo cuando es credito tributario; aqui se usa la base neta de compra."
      />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small">
            <Statistic title="Food cost promedio" value={summary.avgFoodCost} precision={2} suffix="%" />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small">
            <Statistic title="Platos saludables" value={summary.healthy} suffix={`/ ${items.length}`} />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small">
            <Statistic title="Utilidad bruta prom." value={summary.avgProfit} precision={2} prefix="$" />
          </Card>
        </Col>
        <Col xs={24} sm={12} lg={6}>
          <Card size="small">
            <Statistic title="Por revisar" value={summary.warnings} />
          </Card>
        </Col>
      </Row>

      <Table
        dataSource={items}
        rowKey="menuItemId"
        loading={loading}
        size="small"
        tableLayout="fixed"
        scroll={{ x: 1280 }}
        pagination={{ defaultPageSize: 15, showSizeChanger: true, pageSizeOptions: ['15', '30', '50', '100'] }}
        expandable={{
          expandedRowRender: item => (
            <Table
              dataSource={item.ingredients}
              rowKey="recipeIngredientId"
              columns={ingredientColumns}
              size="small"
              pagination={false}
            />
          ),
          rowExpandable: item => item.ingredients.length > 0,
        }}
        columns={[
          {
            title: 'Plato',
            key: 'item',
            width: 280,
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <div style={{ maxWidth: 260, minWidth: 0 }}>
                <Space size={6} style={{ maxWidth: '100%', minWidth: 0, display: 'flex' }}>
                  <Text
                    strong
                    ellipsis={{ tooltip: item.menuItemName }}
                    style={{ minWidth: 0, maxWidth: item.hasRecipe ? 180 : 150 }}
                  >
                    {item.menuItemName}
                  </Text>
                  {!item.hasRecipe && <Tag>Sin receta</Tag>}
                </Space>
                <Space size={6} style={{ maxWidth: '100%', minWidth: 0, display: 'flex', marginTop: 2 }}>
                  {item.categoryColor && <span style={{ width: 10, height: 10, borderRadius: 2, background: item.categoryColor, display: 'inline-block' }} />}
                  <Text type="secondary" ellipsis={{ tooltip: item.categoryName }} style={{ fontSize: 12, minWidth: 0, maxWidth: 150 }}>{item.categoryName}</Text>
                  {item.internalCode && <Text type="secondary" style={{ fontSize: 12, flexShrink: 0 }}>#{item.internalCode}</Text>}
                </Space>
              </div>
            ),
          },
          {
            title: 'Tacometro',
            width: 130,
            align: 'center',
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Progress
                type="dashboard"
                percent={Math.min(item.foodCostPercentage, 100)}
                size={72}
                strokeColor={gaugeColor(item.foodCostPercentage)}
                format={() => pct(item.foodCostPercentage)}
              />
            ),
          },
          {
            title: 'Estado',
            width: 130,
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Tag color={statusColor[item.status] ?? 'default'}>{item.statusLabel}</Tag>
            ),
          },
          {
            title: 'Precio cliente',
            dataIndex: 'grossSalePrice',
            width: 120,
            align: 'right',
            render: money,
          },
          {
            title: 'Precio sin IVA',
            dataIndex: 'netSalePrice',
            width: 130,
            align: 'right',
            render: money,
          },
          {
            title: 'IVA',
            width: 100,
            align: 'right',
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Text type="secondary">{money(item.taxAmount)}</Text>
            ),
          },
          {
            title: 'Costo receta',
            dataIndex: 'recipeCost',
            width: 120,
            align: 'right',
            render: (value: number) => <Text strong>{money(value)}</Text>,
          },
          {
            title: 'Utilidad bruta',
            dataIndex: 'grossProfit',
            width: 130,
            align: 'right',
            render: (value: number) => <Text type={value < 0 ? 'danger' : undefined} strong>{money(value)}</Text>,
          },
          {
            title: 'Margen',
            dataIndex: 'grossMarginPercentage',
            width: 100,
            align: 'right',
            render: pct,
          },
          {
            title: 'Alertas',
            width: 130,
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Space size={4} wrap>
                {item.hasMissingCosts && <Tooltip title="Hay ingredientes sin compras registradas"><Tag color="gold" icon={<WarningOutlined />}>Costos</Tag></Tooltip>}
                {item.hasConversionWarnings && <Tooltip title="Hay unidades sin conversion"><Tag color="orange" icon={<WarningOutlined />}>Unidades</Tag></Tooltip>}
                {!item.hasMissingCosts && !item.hasConversionWarnings && item.hasRecipe && <Tag icon={<BarChartOutlined />} color="green">Listo</Tag>}
              </Space>
            ),
          },
        ]}
      />
    </div>
  );
}
