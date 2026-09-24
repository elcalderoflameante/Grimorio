import { lazy, Suspense } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { App as AntApp, ConfigProvider, Spin } from 'antd';
import esES from 'antd/locale/es_ES';
import { useAuth } from './context/useAuth';
import Login from './pages/Login';
import PublicTableRequest from './pages/PublicTableRequest';
import { grimorioAppTheme } from './theme/grimorioTheme';
import type { ReactNode } from 'react';

const Dashboard = lazy(() => import('./pages/Dashboard'));

interface ProtectedRouteProps {
  children: ReactNode;
}

// Componente protegido: solo entra si hay token
function ProtectedRoute({ children }: ProtectedRouteProps) {
  const { token, loading } = useAuth();

  if (loading) {
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

  if (!token) {
    return <Navigate to="/login" />;
  }

  return <>{children}</>;
}

export default function App() {
  return (
    <ConfigProvider locale={esES} theme={grimorioAppTheme}>
      <AntApp>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/mesa/:token" element={<PublicTableRequest />} />
          
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <Suspense fallback={<Spin size="large" />}><Dashboard /></Suspense>
              </ProtectedRoute>
            }
          />

          <Route path="/" element={<Navigate to="/dashboard" />} />
        </Routes>
      </BrowserRouter>
      </AntApp>
    </ConfigProvider>
  );
}
