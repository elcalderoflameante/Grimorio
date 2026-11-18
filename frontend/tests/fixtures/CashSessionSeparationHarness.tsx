import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from 'antd';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import { AuthContext } from '../../src/context/auth-context';
import { CashSessionSeparation } from '../../src/components/Billing/CashSessionSeparation';
import CashRegister from '../../src/components/Billing/CashRegister';
import type { CashSessionDto } from '../../src/types';

dayjs.extend(utc); dayjs.extend(timezone);
const session: CashSessionDto = {
  id: 'session-old', cashRegisterId: 'register', cashRegisterName: 'Caja principal', cashRegisterCode: 'MAIN',
  openedByName: 'Cajero original', openedAt: '2026-11-16T17:00:00Z', openingBalance: 50,
  status: 'Open', totals: [], totalSales: 255, totalOrders: 5, totalCash: 135,
  totalExpenses: 13, totalCashExpenses: 13, expectedCash: 172,
};
function Harness() {
  const [visible, setVisible] = useState(true);
  const query = new URLSearchParams(location.search);
  return <AuthContext.Provider value={{ user: null, token: 'fixture', branchId: 'branch', loading: false,
    mustChangePassword: false, userPermissions: [], userRoles: [], login: () => {}, logout: () => {},
    hasPermission: code => query.get('permission') !== 'no' || code !== 'Billing.Cash.Separate',
  }}><App>{query.has('page') ? <CashRegister /> : visible && <CashSessionSeparation session={session}
    onCancel={() => { document.body.dataset.result = 'cancelled'; setVisible(false); }}
    onDone={result => { document.body.dataset.result = result.newSessionId; setVisible(false); }} />}</App></AuthContext.Provider>;
}
createRoot(document.getElementById('root')!).render(<Harness />);
