import { createRoot } from 'react-dom/client';
import { App } from 'antd';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import { AuthContext } from '../../src/context/auth-context';
import TableOrderView from '../../src/components/POS/TableOrderView';

dayjs.extend(utc);
dayjs.extend(timezone);
createRoot(document.getElementById('root')!).render(
  <AuthContext.Provider value={{
    user: null, token: null, branchId: 'fixture', loading: false, mustChangePassword: false,
    userPermissions: [], userRoles: [], login: () => {}, logout: () => {}, hasPermission: () => true,
  }}>
    <App><TableOrderView orderId="fixture" branchId="fixture" onClose={() => {}} onTableUpdated={() => {}} /></App>
  </AuthContext.Provider>,
);
