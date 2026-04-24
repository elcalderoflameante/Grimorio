export const REQUEST_TYPE_LABELS: Record<number, string> = {
  1: 'Servilletas',
  2: 'Sal',
  3: 'Salsa de tomate',
  4: 'Mayonesa',
  5: 'Aji',
  6: 'Contenedor',
  7: 'Cuenta',
  8: 'Llamar mesero',
  99: 'Personalizado',
};

export const REQUEST_STATUS_LABELS: Record<number, string> = {
  1: 'Pendiente',
  2: 'Tomada',
  3: 'En proceso',
  4: 'Completada',
  5: 'Cancelada',
};

export const REQUEST_STATUS_COLORS: Record<number, string> = {
  1: 'gold',
  2: 'blue',
  3: 'purple',
  4: 'green',
  5: 'red',
};

export const QR_SERVER_BASE_URL = 'https://api.qrserver.com/v1/create-qr-code/';
export const REQUESTS_POLLING_INTERVAL_MS = 10_000;
export const TABLE_SERVICE_HUB_PATH = '/hubs/table-service';

export const REQUEST_STATUS = {
  PENDING: 1,
  TAKEN: 2,
  IN_PROGRESS: 3,
  COMPLETED: 4,
  CANCELLED: 5,
} as const;
