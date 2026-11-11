import { createRoot } from 'react-dom/client';
import { App } from 'antd';
import { AuthContext } from '../../src/context/auth-context';
import ArticlesList from '../../src/components/Inventory/ArticlesList';

createRoot(document.getElementById('root')!).render(
  <AuthContext.Provider value={{
    user: null, token: null, branchId: null, loading: false, mustChangePassword: false,
    userPermissions: [], userRoles: [], login: () => {}, logout: () => {},
    hasPermission: () => location.search.includes('manage=1'),
  }}>
    <App><ArticlesList /></App>
  </AuthContext.Provider>,
);
