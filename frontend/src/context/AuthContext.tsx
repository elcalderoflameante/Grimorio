import { useState, type ReactNode } from 'react';
import { jwtDecode } from 'jwt-decode';
import { AuthContext, type DecodedToken, type User } from './auth-context';

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
