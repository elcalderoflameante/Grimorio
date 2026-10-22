import { useEffect, useState } from 'react';
import { Alert, Button, Empty, QRCode, Spin, Tag, Typography } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { useParams } from 'react-router-dom';
import { loyaltyApi } from '../services/loyaltyApi';
import type { PublicLoyaltyCard as Card } from '../services/loyaltyApi';
import { formatBranchDateTime } from '../utils/branchTimeZone';

export default function PublicLoyaltyCard() {
  const { token = '' } = useParams();
  const [result, setResult] = useState<{ token: string; reload: number; card?: Card; error?: boolean }>();
  const [reload, setReload] = useState(0);
  const loading = result?.token !== token || result?.reload !== reload;
  const card = loading ? undefined : result?.card;
  const error = !loading && result?.error;
  useEffect(() => {
    let current = true;
    void loyaltyApi.card(token).then(r => { if (current) setResult({ token, reload, card: r.data }); })
      .catch(() => { if (current) setResult({ token, reload, error: true }); });
    return () => { current = false; };
  }, [token, reload]);
  return <main style={{ maxWidth: 560, margin: '0 auto', padding: '28px 20px', minHeight: '100vh' }}>
    <Typography.Title level={3}>EL CALDERO FLAMEANTE</Typography.Title>
    {loading ? <Spin /> : error ? <Alert type="warning" showIcon title="No pudimos consultar esta tarjeta" /> : card && <>
      <Typography.Title level={4}>{card.programName}</Typography.Title>
      <Typography.Paragraph>Hola, {card.customerName}</Typography.Paragraph>
      <section style={{ padding: '24px 0', borderTop: '1px solid #ddd', borderBottom: '1px solid #ddd', textAlign: 'center' }}>
        <QRCode value={card.cardCode} size={180} style={{ margin: '0 auto 16px' }} />
        <div style={{ overflowWrap: 'anywhere' }}>{card.cardCode}</div>
        <Typography.Title level={2}>{card.visits} visitas</Typography.Title>
        {card.nextVisit && <p>Visita {card.nextVisit}: {card.nextReward}</p>}
      </section>
      <Typography.Title level={4}>Mis premios</Typography.Title>
      {card.rewards.length === 0 ? <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Aun no tienes premios" /> : card.rewards.map(r =>
        <article key={r.id} style={{ padding: '16px 0', borderBottom: '1px solid #ddd' }}>
          <strong>{r.name}</strong> <Tag color={r.status === 'Expired' ? 'default' : 'green'}>{r.status === 'Expired' ? 'Vencido' : r.status === 'Upcoming' ? 'Desde la proxima visita' : 'Vigente'}</Tag>
          <div>Vence {formatBranchDateTime(r.expiresAt)}</div>
        </article>)}
    </>}
    <Button icon={<ReloadOutlined />} loading={loading} onClick={() => setReload(n => n + 1)} style={{ marginTop: 20 }}>Actualizar</Button>
  </main>;
}
