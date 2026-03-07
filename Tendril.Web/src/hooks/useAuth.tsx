import { useCallback, useEffect, useRef, useState } from 'react';
import { UserApi } from '../api/users';
import type { GoogleAuthCodeResponse, UserProfile } from '../types/auth';

export const useAuth = (clientId: string) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState<boolean>(true); // Start loading to avoid UI flicker
  const [client, setClient] = useState<any>(null);

  const hasLoaded = useRef(false);

  // 1. Initial Session Check (Page Load)
  useEffect(() => {
    if (hasLoaded.current) return;

    const checkSession = async () => {
      try {
        const profile = await UserApi.getMe();
        setUser(profile);
      } catch {
        setUser(null);
      } finally {
        setLoading(false);
      }
    };

    hasLoaded.current = true;
    checkSession();
  }, []);

  // 2. Initialize Google Code Client
  useEffect(() => {
    if (window.google?.accounts?.oauth2) {
      const codeClient = window.google.accounts.oauth2.initCodeClient({
        client_id: clientId,
        scope: 'openid email profile',
        ux_mode: 'popup',
        callback: async (response: GoogleAuthCodeResponse) => {
          if (response.code) {
            handleLogin(response.code);
          }
        },
      });
      setClient(codeClient);
    }
  }, [clientId]);

  // Internal handler for the Google Callback
  const handleLogin = async (code: string) => {
    setLoading(true);
    try {
      const profile = await UserApi.login(code);
      setUser(profile);
    } catch (err) {
      console.error('Authentication failed', err);
    } finally {
      setLoading(false);
    }
  };

  // 3. Public Methods for External Buttons
  const login = useCallback(() => {
    if (client) {
      client.requestCode();
    }
  }, [client]);

  const logout = useCallback(async () => {
    setLoading(true);
    try {
      await UserApi.logout();
      setUser(null);
    } catch (err) {
      console.error('Logout failed', err);
    } finally {
      setLoading(false);
    }
  }, []);

  // Useful if you want to manually trigger a token refresh
  const refresh = useCallback(async () => {
    try {
      await UserApi.refresh();
      // Optionally re-fetch profile if name/pic might have changed
      const profile = await UserApi.getMe();
      setUser(profile);
    } catch (err) {
      setUser(null);
    }
  }, []);

  return { user, loading, login, logout, refresh };
};
