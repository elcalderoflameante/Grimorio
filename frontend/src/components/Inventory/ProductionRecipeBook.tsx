import { useCallback, useEffect, useState } from 'react';
import { App as AntApp, Button, Space, Table, Tabs, Tag, Tooltip, Typography } from 'antd';
import { FilePdfOutlined, ReloadOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { ProductionRecipeDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

const { Title, Text } = Typography;

export default function ProductionRecipeBook() {
  const { message } = AntApp.useApp();
  const [recipes, setRecipes] = useState<ProductionRecipeDto[]>([]);
  const [loading, setLoading] = useState(false);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const response = await inventoryApi.getProductionRecipes(true);
      setRecipes(response.data);
    } catch (e) {
      message.error(formatError(e));
    } finally {
      setLoading(false);
    }
  }, [message]);

  useEffect(() => { load(); }, [load]);

  const openOperationalSheet = async (recipe: ProductionRecipeDto) => {
    try {
      const response = await inventoryApi.getProductionRecipeOperationalSheetPdf(recipe.id);
      const url = URL.createObjectURL(response.data);
      window.open(url, '_blank', 'noopener,noreferrer');
      setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (e) {
      message.error(formatError(e));
    }
  };

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Title level={5} style={{ margin: 0 }}>Recetario de producción</Title>
        <Tooltip title="Actualizar">
          <Button icon={<ReloadOutlined />} onClick={load} loading={loading} />
        </Tooltip>
      </div>

      <Table
        dataSource={recipes}
        rowKey="id"
        loading={loading}
        size="small"
        pagination={{ defaultPageSize: 20 }}
        columns={[
          { title: 'Preparación', dataIndex: 'outputArticleName', key: 'outputArticleName' },
          {
            title: 'Rendimiento',
            key: 'yield',
            render: (_: unknown, recipe: ProductionRecipeDto) => `${recipe.outputQuantity} ${recipe.outputUnitSymbol}`,
            width: 140,
          },
          {
            title: 'Acceso',
            key: 'access',
            render: (_: unknown, recipe: ProductionRecipeDto) => (
              <Tag color={recipe.accessLevel === 'Restricted' ? 'red' : 'green'}>
                {recipe.accessLevel === 'Restricted' ? 'Restringida' : 'General'}
              </Tag>
            ),
            width: 115,
          },
          {
            title: 'Ingredientes',
            key: 'ingredients',
            render: (_: unknown, recipe: ProductionRecipeDto) => recipe.ingredients.length,
            width: 105,
          },
          {
            title: 'Pasos',
            key: 'steps',
            render: (_: unknown, recipe: ProductionRecipeDto) => recipe.preparationSteps.length,
            width: 80,
          },
          {
            title: 'Ficha',
            key: 'sheet',
            render: (_: unknown, recipe: ProductionRecipeDto) => (
              <Tooltip title="Abrir ficha para imprimir">
                <Button icon={<FilePdfOutlined />} onClick={() => openOperationalSheet(recipe)} />
              </Tooltip>
            ),
            width: 72,
          },
        ]}
        expandable={{
          expandedRowRender: recipe => (
            <Tabs
              size="small"
              items={[
                {
                  key: 'ingredients',
                  label: `Ingredientes (${recipe.ingredients.length})`,
                  children: (
                    <Table
                      dataSource={recipe.ingredients}
                      rowKey="id"
                      size="small"
                      pagination={false}
                      columns={[
                        { title: 'Ingrediente', dataIndex: 'articleName', key: 'articleName' },
                        {
                          title: 'Cantidad',
                          key: 'quantity',
                          render: (_: unknown, ingredient) => `${ingredient.quantity} ${ingredient.unitSymbol}`,
                          width: 150,
                        },
                        { title: 'Observación', dataIndex: 'notes', key: 'notes' },
                      ]}
                    />
                  ),
                },
                {
                  key: 'preparation',
                  label: `Preparación (${recipe.preparationSteps.length})`,
                  children: recipe.preparationSteps.length > 0 ? (
                    <div style={{ display: 'grid', gap: 10 }}>
                      {recipe.preparationSteps.map(step => (
                        <div key={step.id} style={{ display: 'grid', gridTemplateColumns: '72px minmax(0, 1fr) auto', gap: 12, alignItems: 'start' }}>
                          <Tag color="blue" style={{ margin: 0, textAlign: 'center' }}>Paso {step.stepNumber}</Tag>
                          <Text>{step.instructions}</Text>
                          <Space size={4} wrap>
                            {step.estimatedMinutes != null && <Tag>{step.estimatedMinutes} min</Tag>}
                            {step.temperature && <Tag>{step.temperature}</Tag>}
                          </Space>
                        </div>
                      ))}
                    </div>
                  ) : <Text type="secondary">Sin pasos de preparación registrados.</Text>,
                },
              ]}
            />
          ),
        }}
      />
    </div>
  );
}
