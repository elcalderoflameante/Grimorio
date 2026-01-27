import { createContext, useState, useContext, type ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';

interface User {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
}

interface AuthContextValue {
  user: User | null;
  token: string | null;
  loading: boolean;
  branchId: string | null;
  userPermissions: string[];
  userRoles: string[];
  login: (userData: User, accessToken: string, branchIdFromToken: string) => void;
  logout: () => void;
  hasPermission: (permissionCode: string) => boolean;
}

interface DecodedToken {
  permissions?: string | string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  BranchId?: string;
  branchId?: string;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(() => {
    const savedUser = localStorage.getItem('user');
    if (savedUser) {
      try {
        return JSON.parse(savedUser);
      } catch {
        return null;
      }
    }
    return null;
  });
  
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('accessToken'));
  const [loading] = useState(false);
  const [branchId, setBranchId] = useState<string | null>(() => localStorage.getItem('branchId'));
  
  const [userPermissions, setUserPermissions] = useState<string[]>(() => {
    const savedToken = localStorage.getItem('accessToken');
    if (savedToken) {
      try {
        const decoded = jwtDecode<DecodedToken>(savedToken);
        const permissions = decoded.permissions || [];
        return Array.isArray(permissions) ? permissions : [permissions];
      } catch {
        return [];
      }
    }
    return [];
  });
  
  const [userRoles, setUserRoles] = useState<string[]>(() => {
    const savedToken = localStorage.getItem('accessToken');
    if (savedToken) {
      try {
        const decoded = jwtDecode<DecodedToken>(savedToken);
        const roles = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
        return Array.isArray(roles) ? roles : (roles ? [roles] : []);
      } catch {
        return [];
      }
    }
    return [];
  });

  // Login: guarda token y usuario
  const login = (userData: User, accessToken: string, branchIdFromToken: string) => {
    setUser(userData);
    setToken(accessToken);
    setBranchId(branchIdFromToken);

    // Extraer permisos del JWT
    try {
      const decoded = jwtDecode<DecodedToken>(accessToken);
      const permissions = decoded.permissions || [];
      setUserPermissions(Array.isArray(permissions) ? permissions : [permissions]);
      
      // Extraer roles del JWT
      const roles = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      setUserRoles(Array.isArray(roles) ? roles : (roles ? [roles] : []));
    } catch {
      setUserPermissions([]);
      setUserRoles([]);
    }

    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('user', JSON.stringify(userData));
    localStorage.setItem('branchId', branchIdFromToken);
  };

  // Logout: borra todo
  const logout = () => {
    setUser(null);
    setToken(null);
    setBranchId(null);
    setUserPermissions([]);
    setUserRoles([]);

    localStorage.removeItem('accessToken');
    localStorage.removeItem('user');
    localStorage.removeItem('branchId');
  };

  // Función helper para verificar si el usuario tiene un permiso
  const hasPermission = (permissionCode: string): boolean => {
    // Si es Administrador, tiene todos los permisos
    if (userRoles.includes('Administrador')) {
      return true;
    }
    return userPermissions.includes(permissionCode);
  };

  return (
    <AuthContext.Provider value={{ user, token, loading, branchId, userPermissions, userRoles, login, logout, hasPermission }}>
      {children}
    </AuthContext.Provider>
  );
};

// Hook para usar el contexto
export const useAuth = (): AuthContextValue => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth debe usarse dentro de AuthProvider');
  }
  return context;
};
