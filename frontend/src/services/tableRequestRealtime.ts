import { HubConnectionBuilder } from '@microsoft/signalr';
import type { TableServiceRequestDto } from '../types';
import { TABLE_SERVICE_HUB_PATH } from '../constants/tableService';

export function subscribeTableRequests(options: {
  apiUrl?: string; token: string; branchId: string;
  onChange: (request: TableServiceRequestDto) => void;
  onSync: () => void;
  onConnection?: (connected: boolean) => void;
}) {
  const url = `${(options.apiUrl ?? '').replace(/\/api\/?$/, '').replace(/\/$/, '')}${TABLE_SERVICE_HUB_PATH}`;
  const connection = new HubConnectionBuilder().withUrl(url, { accessTokenFactory: () => options.token })
    .withAutomaticReconnect().build();
  let disposed = false;
  let retry: ReturnType<typeof setTimeout> | undefined;
  const change = (request: TableServiceRequestDto) => {
    if (!disposed && request?.branchId?.toLowerCase() === options.branchId.toLowerCase()) options.onChange(request);
  };
  const ready = () => {
    if (disposed) return;
    options.onConnection?.(true); options.onSync();
  };
  const retryStart = () => {
    if (disposed || retry !== undefined) return;
    options.onConnection?.(false);
    retry = setTimeout(() => { retry = undefined; void start(); }, 5000);
  };
  const start = async () => {
    if (disposed) return;
    try { await connection.start(); ready(); } catch { retryStart(); }
  };
  connection.on('tableService:new-request', change);
  connection.on('tableService:request-updated', change);
  connection.onreconnected(ready);
  connection.onreconnecting(() => { if (!disposed) options.onConnection?.(false); });
  connection.onclose(retryStart);
  void start();
  return () => {
    disposed = true; clearTimeout(retry);
    connection.off('tableService:new-request', change);
    connection.off('tableService:request-updated', change);
    void connection.stop().catch(() => {});
  };
}
