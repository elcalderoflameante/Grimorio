import { HubConnectionBuilder } from '@microsoft/signalr';

export const TABLES_CHANGED_EVENT = 'pos:tables-changed';

interface Subscription {
  apiUrl?: string;
  token: string;
  branchId: string;
  onChange: () => void;
}

export function subscribeTableMap({ apiUrl, token, branchId, onChange }: Subscription) {
  const url = `${(apiUrl ?? '').replace(/\/api\/?$/, '').replace(/\/$/, '')}/hubs/kitchen`;
  const connection = new HubConnectionBuilder()
    .withUrl(url, { accessTokenFactory: () => token })
    .withAutomaticReconnect()
    .build();
  let disposed = false;
  let retry: ReturnType<typeof setTimeout> | undefined;
  const changed = (payload: { branchId?: string } | undefined) => {
    if (!disposed && payload?.branchId?.toLowerCase() === branchId.toLowerCase()) onChange();
  };
  const retryStart = () => {
    if (disposed || retry !== undefined) return;
    // Automatic reconnect does not retry the initial start or a fully exhausted retry cycle.
    retry = setTimeout(() => { retry = undefined; void start(); }, 5_000);
  };
  const start = async () => {
    if (disposed) return;
    try {
      await connection.start();
      if (!disposed) onChange();
    } catch {
      retryStart();
    }
  };
  connection.on(TABLES_CHANGED_EVENT, changed);
  connection.onreconnected(() => { if (!disposed) onChange(); });
  connection.onclose(retryStart);
  void start();
  return () => {
    disposed = true;
    clearTimeout(retry);
    connection.off(TABLES_CHANGED_EVENT, changed);
    void connection.stop().catch(() => {});
  };
}
