import type { TableServiceRequestDto } from '../types';
import { REQUEST_TYPE_LABELS } from '../constants/tableService';
import { requestTableLabel } from './tableRequestAudio';

export const supportsTableRequestDesktop = () => window.isSecureContext && typeof window.Notification === 'function';

export class TableRequestDesktop {
  private enabled = false;
  private disposed = false;
  private current: Notification | null = null;
  private ids: string[] = [];
  private onOpen: () => void;
  private onBlocked: (message: string) => void;

  constructor(onOpen: () => void, onBlocked: (message: string) => void) {
    this.onOpen = onOpen; this.onBlocked = onBlocked;
  }

  async enable() {
    if (this.disposed) return false;
    if (!supportsTableRequestDesktop()) throw new Error('Las notificaciones de escritorio requieren un navegador compatible y HTTPS o localhost.');
    const permission = window.Notification.permission === 'default'
      ? await window.Notification.requestPermission() : window.Notification.permission;
    if (this.disposed) return false;
    if (permission !== 'granted') throw new Error(permission === 'denied'
      ? 'Las notificaciones estan bloqueadas. Permitelas en la configuracion del sitio en el navegador.'
      : 'No se concedio permiso para las notificaciones de escritorio.');
    this.enabled = true;
    this.test();
    return this.enabled;
  }

  test() {
    if (this.enabled) this.show('Solicitudes de mesas', 'Notificaciones de escritorio activadas.', []);
  }

  announce(rows: TableServiceRequestDto[]) {
    if (!this.enabled || !rows.length || (document.visibilityState === 'visible' && document.hasFocus())) return;
    const title = rows.length === 1 ? requestTableLabel(rows[0]) : `${rows.length} nuevas solicitudes de mesas`;
    const body = rows.length === 1 ? REQUEST_TYPE_LABELS[rows[0].type] || 'Nueva solicitud' : 'Hay mesas que necesitan atencion.';
    this.show(title, body, rows.map(row => row.id));
  }

  retain(rows: TableServiceRequestDto[]) {
    if (this.ids.some(id => !rows.some(row => row.id === id && row.status === 1))) this.close();
  }

  disable() { this.enabled = false; this.close(); }
  dispose() { this.disposed = true; this.disable(); }

  private close() {
    if (this.current) { this.current.onclick = null; this.current.close(); }
    this.current = null; this.ids = [];
  }

  private show(title: string, body: string, ids: string[]) {
    if (this.disposed) return;
    try {
      if (window.Notification.permission !== 'granted') throw new Error('Permiso de notificaciones revocado. Revisa la configuracion del sitio.');
      this.close();
      const notice = new window.Notification(title, { body, tag: 'grimorio-table-requests', silent: true, icon: '/restaurant/logo.png' });
      this.current = notice; this.ids = ids;
      notice.onclick = () => {
        if (this.disposed || !this.enabled) return;
        this.close(); window.focus(); this.onOpen();
      };
    } catch (error) {
      this.disable();
      this.onBlocked(error instanceof Error ? error.message : 'No se pudo mostrar la notificacion de escritorio.');
    }
  }
}
