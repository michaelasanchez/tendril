import { createContext, useContext, type ReactNode } from 'react';
import type { UserProfile } from '../types/auth';

const UserContext = createContext<UserProfile | null>(null);

export function UserProvider({
  user,
  children,
}: {
  user: UserProfile | null;
  children: ReactNode;
}) {
  return <UserContext.Provider value={user}>{children}</UserContext.Provider>;
}

export function useUser() {
  return useContext(UserContext);
}

export function useIsAdmin() {
  return useUser()?.isAdmin ?? false;
}
