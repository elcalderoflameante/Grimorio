import { useEffect, useState } from 'react';
import { Alert, Button, Empty, QRCode, Spin, Tag } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { useParams } from 'react-router-dom';
import { loyaltyApi } from '../services/loyaltyApi';
import type { PublicLoyaltyCard as Card } from '../services/loyaltyApi';
import { formatBranchDateTime } from '../utils/branchTimeZone';
import { loyaltyStampCycle } from '../utils/loyaltyPresentation';
import CauldronStamp from '../components/Marketing/CauldronStamp';
import './PublicLoyaltyCard.css';

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
  const cycle = loyaltyStampCycle(card?.visits ?? 0);
  const remaining = card?.nextVisit && card.nextVisit > cycle.total ? card.nextVisit - cycle.total : null;
  const currentRewards = card?.rewards.filter(r => r.status !== 'Expired') ?? [];
  const expiredRewards = card?.rewards.filter(r => r.status === 'Expired') ?? [];
  function reward(r: Card['rewards'][number]) {
    return <article key={r.id} className="loyalty-passport-reward">
      <strong>{r.name}</strong>
      <Tag color={r.status === 'Expired' ? 'default' : r.status === 'Upcoming' ? 'gold' : 'green'}>
        {r.status === 'Expired' ? 'Vencido' : r.status === 'Upcoming' ? 'Desde la proxima visita' : 'Disponible'}
      </Tag>
      <time dateTime={r.expiresAt}>Vence {formatBranchDateTime(r.expiresAt)}</time>
    </article>;
  }
  return <main className="loyalty-passport-page"><div className="loyalty-passport">
    <header className="loyalty-passport-brand">
      <img src="/restaurant/logo.png" alt="El Caldero Flameante" width={86} height={64} />
      <div><h1>EL CALDERO FLAMEANTE</h1><p>Tu tarjeta de recompensas</p></div>
    </header>
    {loading ? <Spin /> : error ? <Alert type="warning" showIcon title="No pudimos consultar esta tarjeta" /> : card && <>
      <section className="loyalty-passport-sheet" aria-label="Progreso de visitas">
        <p className="loyalty-passport-program">{card.programName}</p>
        <h2>Hola, {card.customerName}</h2>
        <div className="loyalty-passport-count"><strong>{cycle.total} {cycle.total === 1 ? 'visita registrada' : 'visitas registradas'}</strong>
          <span>Visitas {cycle.firstVisit} a {cycle.lastVisit}</span></div>
        <ol className="loyalty-stamps" aria-label={`${cycle.completed} de 5 sellos en este ciclo`}>
          {Array.from({ length: 5 }, (_, i) => <li key={i} aria-label={`Visita ${cycle.firstVisit + i}: ${i < cycle.completed ? 'registrada' : 'pendiente'}`}>
            <div className={`loyalty-stamp-seal${i < cycle.completed ? ' is-filled' : ''}`}><CauldronStamp filled={i < cycle.completed} /></div>
            <span className="loyalty-stamp-number" aria-hidden="true">{i + 1}</span>
          </li>)}
        </ol>
        {remaining && card.nextReward ? <div className="loyalty-next-reward">
          <p>Te {remaining === 1 ? 'falta 1 visita' : `faltan ${remaining} visitas`} para tu proximo premio</p>
          <strong>{card.nextReward}</strong>
        </div> : <div className="loyalty-next-reward"><p>Tu historial de visitas sigue acumulandose</p></div>}
      </section>
      <section className="loyalty-passport-identity" aria-label="Identificacion de tarjeta">
        <QRCode value={card.cardCode} size={132} color="#142e25" bgColor="#ffffff" />
        <div><h2>Mi tarjeta</h2><p>{card.cardCode}</p><Tag color="green">{card.rewards.filter(r => r.status === 'Available').length} premios disponibles</Tag></div>
      </section>
      <section className="loyalty-passport-rewards" aria-label="Premios">
        <h2>Mis premios</h2>
        {currentRewards.length ? currentRewards.map(reward) : <Empty image={Empty.PRESENTED_IMAGE_SIMPLE} description="Aun no tienes premios disponibles" />}
        {expiredRewards.length > 0 && <details><summary>Premios vencidos ({expiredRewards.length})</summary>{expiredRewards.map(reward)}</details>}
      </section>
    </>}
    <Button className="loyalty-passport-refresh" aria-label="Actualizar" icon={<ReloadOutlined />} loading={loading} onClick={() => setReload(n => n + 1)}>Actualizar</Button>
  </div></main>;
}
