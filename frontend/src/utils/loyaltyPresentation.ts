export function loyaltyStampCycle(visits: number) {
  const total = Number.isFinite(visits) ? Math.max(0, Math.floor(visits)) : 0;
  const completed = total % 5;
  const firstVisit = total - completed + 1;
  return { total, completed, firstVisit, lastVisit: firstVisit + 4 };
}

export function loyaltyEnrollmentUrl(configuredBase: string | undefined, currentOrigin: string): string | null {
  try {
    const url = new URL(configuredBase?.trim() || currentOrigin);
    if (!['https:', 'http:'].includes(url.protocol) || url.username || url.password) return null;
    return new URL('/fidelizacion', url.origin).href;
  } catch { return null; }
}

export function isLocalLoyaltyUrl(value: string): boolean {
  const host = new URL(value).hostname.toLowerCase();
  return host === 'localhost' || host === '[::1]' || host.startsWith('127.') || host.startsWith('192.168.')
    || host.startsWith('10.') || /^172\.(1[6-9]|2\d|3[01])\./.test(host) || host.endsWith('.local');
}
