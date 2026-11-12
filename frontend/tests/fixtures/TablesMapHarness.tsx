import { useState } from 'react';
import { createRoot } from 'react-dom/client';
import { App, Button } from 'antd';
import TablesMap from '../../src/components/POS/TablesMap';
import { AuthContext } from '../../src/context/auth-context';

function Harness() {
  const [branch, setBranch] = useState('11111111-1111-4111-8111-111111111111');
  const [visible, setVisible] = useState(true);
  return <AuthContext.Provider value={{
    user: null, token: branch.startsWith('1') ? 'branch-a' : 'branch-b', branchId: branch, loading: false, mustChangePassword: false,
    userPermissions: [], userRoles: [], login: () => {}, logout: () => {}, hasPermission: () => true,
  }}><App>
    <Button onClick={() => setBranch('22222222-2222-4222-8222-222222222222')}>Cambiar sucursal</Button>
    <Button onClick={() => setVisible(false)}>Salir del mapa</Button>
    {visible && <TablesMap branchId={branch} onSelectTable={table => {
      document.body.dataset.selectedTable = table.currentOrderId ?? 'free';
    }} />}
  </App></AuthContext.Provider>;
}
createRoot(document.getElementById('root')!).render(<Harness />);
