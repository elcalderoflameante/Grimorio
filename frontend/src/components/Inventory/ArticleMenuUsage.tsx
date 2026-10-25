import { useState } from 'react';
import { Alert, Button, Empty, List, Popover, Space, Spin, Tag, Tooltip, Typography } from 'antd';
import { ReloadOutlined, UnorderedListOutlined } from '@ant-design/icons';
import { inventoryApi } from '../../services/api';
import type { ArticleMenuUsageDto } from '../../types';
import { formatError } from '../../utils/errorHandler';

export default function ArticleMenuUsage({ articleId, articleName }: { articleId: string; articleName: string }) {
  const [usages, setUsages] = useState<ArticleMenuUsageDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [open, setOpen] = useState(false);

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const result = await inventoryApi.getArticleMenuUsage(articleId);
      setUsages(result.data);
    } catch (e) {
      setError(formatError(e));
    } finally {
      setLoading(false);
    }
  };

  return (
    <Popover
      trigger="click"
      placement="bottom"
      autoAdjustOverflow={{ adjustX: 1, adjustY: 1 }}
      open={open}
      onOpenChange={value => { setOpen(value); if (value) void load(); }}
      title={<span style={{ overflowWrap: 'anywhere' }}>{'Uso en men\u00fa: '}{articleName}</span>}
      styles={{ root: { width: 360, maxWidth: 'calc(100vw - 24px)' }, container: { width: '100%', boxSizing: 'border-box' } }}
      content={
        <div style={{ maxHeight: 320, overflowY: 'auto', overflowWrap: 'anywhere' }}>
          {loading ? <div style={{ padding: 24, textAlign: 'center' }}><Spin aria-label="Cargando usos" /></div>
            : error ? <Alert type="error" title="No se pudieron consultar los platos" description={error}
                action={<Button icon={<ReloadOutlined />} onClick={load} aria-label="Reintentar" />} />
            : usages.length === 0 ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Sin usos en el menu" />
            : <List size="small" dataSource={usages} renderItem={usage => (
              <List.Item key={usage.menuItemId}>
                <Space orientation="vertical" size={2} style={{ width: '100%' }}>
                  <Typography.Text strong>{usage.menuItemName} {!usage.isActive && <Tag>Inactivo</Tag>}</Typography.Text>
                  {usage.usages.map(detail => <Typography.Text key={detail} type="secondary">{detail}</Typography.Text>)}
                </Space>
              </List.Item>
            )} />}
        </div>
      }
    >
      <Tooltip title={open ? null : 'Ver platos que usan este articulo'}>
        <Button size="small" icon={<UnorderedListOutlined />} aria-label={`Ver platos que usan ${articleName}`} />
      </Tooltip>
    </Popover>
  );
}
