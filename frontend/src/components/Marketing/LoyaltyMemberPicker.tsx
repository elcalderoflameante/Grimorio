import { useEffect, useState } from 'react';
import { Select, Typography } from 'antd';
import { loyaltyApi } from '../../services/loyaltyApi';
import type { LoyaltyMemberReference } from '../../services/loyaltyApi';

export default function LoyaltyMemberPicker({ value, onChange }: { value: LoyaltyMemberReference | null; onChange: (member: LoyaltyMemberReference | null) => void }) {
  const [search, setSearch] = useState('');
  const [members, setMembers] = useState<LoyaltyMemberReference[]>([]);
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    let live = true;
    const timer = window.setTimeout(() => {
      void loyaltyApi.lookup(search).then(r => { if (live) { setMembers(r.data); setFailed(false); } })
        .catch(() => { if (live) setFailed(true); });
    }, 250);
    return () => { live = false; window.clearTimeout(timer); };
  }, [search]);
  const options = [...(value && !members.some(m => m.id === value.id) ? [value] : []), ...members];
  return <div style={{ marginBottom: 12 }}>
    <Typography.Text type="secondary" style={{ display: 'block', marginBottom: 4 }}>Tarjeta de fidelizacion (opcional)</Typography.Text>
    <Select aria-label="Tarjeta de fidelizacion" style={{ width: '100%' }} allowClear showSearch filterOption={false}
      placeholder="Telefono, nombre o codigo de tarjeta" value={value?.id} onSearch={setSearch}
      options={options.map(m => ({ value: m.id, label: `${m.customerName} - ${m.phone ?? m.cardCode} - ${m.visits} visitas` }))}
      onChange={id => onChange(options.find(m => m.id === id) ?? null)} />
    {failed && <Typography.Text type="warning">No se pudo consultar fidelizacion. Puedes cobrar sin asociar una tarjeta.</Typography.Text>}
  </div>;
}
