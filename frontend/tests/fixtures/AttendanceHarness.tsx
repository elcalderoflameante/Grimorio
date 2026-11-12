import { createRoot } from 'react-dom/client';
import { App } from 'antd';
import { AuthContext } from '../../src/context/auth-context';
import { AttendanceDashboard } from '../../src/components/Attendance/AttendanceDashboard';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';

dayjs.extend(utc);
dayjs.extend(timezone);

createRoot(document.getElementById('root')!).render(
  <AuthContext.Provider value={{
    user: null, token: null, branchId: null, loading: false, mustChangePassword: false,
    userPermissions: [], userRoles: [], login: () => {}, logout: () => {},
    hasPermission: () => location.search.includes('manage=1'),
  }}>
    <App><AttendanceDashboard /></App>
  </AuthContext.Provider>,
);
