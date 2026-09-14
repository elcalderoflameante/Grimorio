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
const centeredTitle = (label: string) => <div style={{ textAlign: 'center' }}>{label}</div>;

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
      width: 115,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text>{row.quantity} {row.unitSymbol}</Text>
      ),
    },
    {
      title: 'Cantidad base',
      width: 115,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text>{row.baseQuantity.toFixed(4)} {row.baseUnitSymbol}</Text>
      ),
    },
    {
      title: 'Costo prom.',
      width: 95,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text>{money(row.averageUnitCost)}</Text>
      ),
    },
    {
      title: 'Ultimo costo',
      width: 95,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text type="secondary">{row.lastUnitCost === undefined ? '-' : money(row.lastUnitCost)}</Text>
      ),
    },
    {
      title: 'Total',
      width: 85,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        <Text strong>{money(row.totalCost)}</Text>
      ),
    },
    {
      title: 'Peso',
      width: 75,
      align: 'right' as const,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => pct(row.costSharePercentage),
    },
    {
      title: 'Estado',
      width: 105,
      render: (_: unknown, row: MenuItemProfitabilityIngredientDto) => (
        row.warning
          ? <Tooltip title={row.warning}><Tag color="gold" icon={<WarningOutlined />} style={{ margin: 0 }}>Revisar</Tag></Tooltip>
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
        sticky
        scroll={{ x: 995, y: 'calc(100vh - 380px)' }}
        pagination={{ defaultPageSize: 15, showSizeChanger: true, pageSizeOptions: ['15', '30', '50', '100'] }}
        expandable={{
          fixed: 'left',
          expandedRowRender: item => (
            <Table
              dataSource={item.ingredients}
              rowKey="recipeIngredientId"
              columns={ingredientColumns}
              size="small"
              pagination={false}
              tableLayout="fixed"
            />
          ),
          rowExpandable: item => item.ingredients.length > 0,
        }}
        columns={[
          {
            title: 'Plato',
            key: 'item',
            width: 245,
            fixed: 'left',
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <div style={{ maxWidth: 225, minWidth: 0 }}>
                <div style={{ display: 'flex', alignItems: 'flex-start', gap: 6, minWidth: 0 }}>
                  <Tooltip title={item.menuItemName}>
                    <Text
                      strong
                      style={{
                        flex: 1,
                        minWidth: 0,
                        whiteSpace: 'normal',
                        overflowWrap: 'anywhere',
                        lineHeight: 1.25,
                      }}
                    >
                      {item.menuItemName}
                    </Text>
                  </Tooltip>
                  {!item.hasRecipe && <Tag>Sin receta</Tag>}
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginTop: 3, minWidth: 0 }}>
                  {item.categoryColor && <span style={{ width: 10, height: 10, borderRadius: 2, background: item.categoryColor, display: 'inline-block', flexShrink: 0 }} />}
                  <Text type="secondary" ellipsis={{ tooltip: item.categoryName }} style={{ fontSize: 12, minWidth: 0 }}>{item.categoryName}</Text>
                </div>
                {item.internalCode && (
                  <Text type="secondary" style={{ display: 'block', fontSize: 12, marginTop: 1 }}>
                    #{item.internalCode}
                  </Text>
                )}
              </div>
            ),
          },
          {
            title: centeredTitle('Tacometro'),
            width: 105,
            align: 'center',
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Progress
                type="dashboard"
                percent={Math.min(item.foodCostPercentage, 100)}
                size={60}
                strokeColor={gaugeColor(item.foodCostPercentage)}
                format={() => pct(item.foodCostPercentage)}
              />
            ),
          },
          {
            title: centeredTitle('Estado'),
            width: 120,
            align: 'center',
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Tag color={statusColor[item.status] ?? 'default'} style={{ margin: 0 }}>{item.statusLabel}</Tag>
            ),
          },
          {
            title: centeredTitle('Precio cliente'),
            dataIndex: 'grossSalePrice',
            width: 90,
            align: 'right',
            render: money,
          },
          {
            title: centeredTitle('Precio sin IVA'),
            dataIndex: 'netSalePrice',
            width: 95,
            align: 'right',
            render: money,
          },
          {
            title: centeredTitle('IVA'),
            width: 65,
            align: 'right',
            render: (_: unknown, item: MenuItemProfitabilityDto) => (
              <Text type="secondary">{money(item.taxAmount)}</Text>
            ),
          },
          {
            title: centeredTitle('Costo receta'),
            dataIndex: 'recipeCost',
            width: 90,
            align: 'right',
            render: (value: number) => <Text strong>{money(value)}</Text>,
          },
          {
            title: centeredTitle('Utilidad bruta'),
            dataIndex: 'grossProfit',
            width: 95,
            align: 'right',
            render: (value: number) => <Text type={value < 0 ? 'danger' : undefined} strong>{money(value)}</Text>,
          },
          {
            title: centeredTitle('Margen'),
            dataIndex: 'grossMarginPercentage',
            width: 85,
            align: 'right',
            render: pct,
          },
          {
            title: centeredTitle('Alertas'),
            width: 115,
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
