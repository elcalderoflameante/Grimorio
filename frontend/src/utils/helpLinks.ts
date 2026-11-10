export function helpLink(url: string, image = false): string {
  if (image) return /^asset:[a-z0-9]+(?:-[a-z0-9]+)*$/.test(url) ? url : '';
  if (/^\/ayuda(?:\/[a-z0-9]+(?:-[a-z0-9]+)*)?(?:#[a-z0-9-]+)?$/.test(url)) return url;
  if (url === '/downloads/grimorio-inventario.apk') return url;
  if (/^#[a-z0-9-]+$/.test(url)) return url;
  try {
    const parsed = new URL(url);
    if (parsed.protocol === 'https:' && !parsed.username && !parsed.password) return parsed.href;
  } catch { /* Reject unrecognised relative paths and protocols. */ }
  return '';
}
