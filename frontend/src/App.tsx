import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { App as AntApp, ConfigProvider, Spin } from 'antd';
import esES from 'antd/locale/es_ES';
import { useAuth } from './context/useAuth';
import Login from './pages/Login';
import RestaurantHome from './pages/RestaurantHome';
import PublicTableRequest from './pages/PublicTableRequest';
import PublicLoyaltyCard from './pages/PublicLoyaltyCard';
import PublicLoyaltyEnrollment from './pages/PublicLoyaltyEnrollment';
import PublicLoyaltyPrivacy from './pages/PublicLoyaltyPrivacy';
import { grimorioAppTheme } from './theme/grimorioTheme';
import type { ReactNode } from 'react';
import RequiredPasswordChange from './components/Profile/RequiredPasswordChange';

const Dashboard = lazy(() => import('./pages/Dashboard'));

const configuredErpHost = (() => {
  const value = import.meta.env.VITE_ERP_APP_URL as string | undefined;
  if (!value) return null;
  try {
    return new URL(value).host.toLowerCase();
  } catch {
    return null;
  }
})();

function isErpHost() {
  const currentHost = window.location.host.toLowerCase();
  return configuredErpHost
    ? currentHost === configuredErpHost
    : window.location.hostname.toLowerCase().startsWith('erp.');
}

function FullPageSpinner() {
  return (
    <div style={{
      display: 'flex',
      justifyContent: 'center',
      alignItems: 'center',
      minHeight: '100vh',
    }}>
      <Spin size="large" />
    </div>
  );
}

function RootRoute() {
  const { token, loading } = useAuth();

  if (!isErpHost()) return <RestaurantHome />;
  if (loading) return <FullPageSpinner />;
  return <Navigate to={token ? '/dashboard' : '/login'} replace />;
}

function LoginRoute() {
  const { token, loading } = useAuth();

  if (loading) return <FullPageSpinner />;
  return token ? <Navigate to="/dashboard" replace /> : <Login />;
}

interface ProtectedRouteProps {
  children: ReactNode;
}

// Componente protegido: solo entra si hay token
function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { token, loading, mustChangePassword } = useAuth();

  if (loading) {
    return <FullPageSpinner />;
  }

  if (!token) {
    return <Navigate to="/login" />;
  }

  return mustChangePassword ? <RequiredPasswordChange /> : <>{children}</>;
}

export default function App() {
  return (
    <ConfigProvider locale={esES} theme={grimorioAppTheme}>
      <AntApp>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginRoute />} />
          <Route path="/mesa/:token" element={<PublicTableRequest />} />
          <Route path="/fidelizacion/:token" element={<PublicLoyaltyCard />} />
          <Route path="/fidelizacion" element={<PublicLoyaltyEnrollment />} />
          <Route path="/fidelizacion/privacidad" element={<PublicLoyaltyPrivacy />} />
          <Route path="/fidelizacion/eliminar-datos" element={<PublicLoyaltyPrivacy deletion />} />
          
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Suspense fallback={<Spin size="large" />}><Dashboard /></Suspense>
              </ProtectedRoute>
            }
          />

          <Route path="/" element={<RootRoute />} />
        </Routes>
      </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  );
}
