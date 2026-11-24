import { createRoot } from 'react-dom/client';
import { App } from 'antd';
import { AuthContext } from '../../src/context/auth-context';
import { posApi } from '../../src/services/api';
import TakeOrder from '../../src/components/POS/TakeOrder';

const existingOrder = new URLSearchParams(window.location.search).has('reopen')
  ? (await posApi.getOrden('fixture')).data : undefined;
createRoot(document.getElementById('root')!).render(
  <AuthContext.Provider value={{ user: null, token: null, branchId: 'fixture', loading: false, mustChangePassword: false,
    userPermissions: [], userRoles: [], login: () => {}, logout: () => {}, hasPermission: () => true }}>
    <App><TakeOrder orderType="Takeout" existingOrder={existingOrder}
      onClose={() => { document.body.dataset.saved = 'yes'; }} onConfirm={() => {}} /></App>
  </AuthContext.Provider>,
);
