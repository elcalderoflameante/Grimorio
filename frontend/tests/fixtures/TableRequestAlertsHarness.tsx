import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { App, Button } from 'antd';
import dayjs from 'dayjs';
import utc from 'dayjs/plugin/utc';
import timezone from 'dayjs/plugin/timezone';
import TableRequestAlerts from '../../src/components/POS/TableRequestAlerts';
import { AuthContext } from '../../src/context/auth-context';
dayjs.extend(utc); dayjs.extend(timezone);

function Harness() {
  const [branch, setBranch] = useState('11111111-1111-4111-8111-111111111111');
  const [permission, setPermission] = useState('all');
  return <AuthContext.Provider value={{ user: null, token: branch.startsWith('1') ? 'branch-a' : 'branch-b', branchId: branch,
    loading: false, mustChangePassword: false, userPermissions: [], userRoles: [], login: () => {}, logout: () => {},
    hasPermission: code => permission === 'all' || (permission === 'view' && code.endsWith('.View')),
  }}><App>
    <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8 }}>
      <Button onClick={() => setBranch('22222222-2222-4222-8222-222222222222')}>Cambiar sucursal</Button>
      <Button onClick={() => setPermission('view')}>Solo consulta</Button>
      <Button onClick={() => setPermission('none')}>Sin permiso</Button>
      <TableRequestAlerts />
    </div>
  </App></AuthContext.Provider>;
}
createRoot(document.getElementById('root')!).render(<Harness />);
