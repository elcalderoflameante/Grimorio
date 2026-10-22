import { useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { publicLoyaltyApi } from '../../services/publicLoyaltyApi';

export default function PublicLoyaltyLink({ className, children }: { className?: string; children: ReactNode }) {
  const [available, setAvailable] = useState(false);
  useEffect(() => {
    let current = true;
    void publicLoyaltyApi.info().then(r => { if (current) setAvailable(r.data.enabled); }).catch(() => {});
    return () => { current = false; };
  }, []);
  return available ? <a className={className} href="/fidelizacion" target="_blank" rel="noopener noreferrer">{children}</a> : null;
}
