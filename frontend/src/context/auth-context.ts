import { createContext } from 'react';

export interface User {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
}

export interface AuthContextValue {
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

export interface DecodedToken {
  permissions?: string | string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  BranchId?: string;
  branchId?: string;
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined);