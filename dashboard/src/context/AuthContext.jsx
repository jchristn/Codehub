import { createContext, useContext, useState, useEffect, useCallback } from 'react';
import ApiClient from '../utils/api';
import { STORAGE } from '../utils/constants';

const AuthContext = createContext(null);

/**
 * Authentication + theme provider. Single-user static-key model: the API key
 * and server URL are persisted in localStorage and validated against
 * GET /v1.0/api/token. Any 401 anywhere dispatches logout.
 */
export function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [apiClient, setApiClient] = useState(null);
  const [serverUrl, setServerUrl] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [theme, setTheme] = useState('light');

  // Restore theme + session on mount.
  useEffect(() => {
    const savedTheme = localStorage.getItem(STORAGE.theme) || 'light';
    setTheme(savedTheme);
    document.documentElement.setAttribute('data-theme', savedTheme);

    const savedUrl = localStorage.getItem(STORAGE.serverUrl);
    const savedKey = localStorage.getItem(STORAGE.apiKey);

    if (savedUrl && savedKey) {
      const client = new ApiClient(savedUrl, savedKey);
      client
        .validateToken()
        .then(() => {
          setServerUrl(savedUrl);
          setApiKey(savedKey);
          setApiClient(client);
          setIsAuthenticated(true);
        })
        .catch(() => {
          localStorage.removeItem(STORAGE.serverUrl);
          localStorage.removeItem(STORAGE.apiKey);
        })
        .finally(() => setIsLoading(false));
    } else {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(STORAGE.serverUrl);
    localStorage.removeItem(STORAGE.apiKey);
    setServerUrl('');
    setApiKey('');
    setApiClient(null);
    setIsAuthenticated(false);
  }, []);

  // Global 401 handler → force logout.
  useEffect(() => {
    const handler = () => logout();
    window.addEventListener('auth:unauthorized', handler);
    return () => window.removeEventListener('auth:unauthorized', handler);
  }, [logout]);

  /**
   * Login with a server URL + static API key. Throws on validation failure.
   */
  const login = useCallback(async (url, key) => {
    const client = new ApiClient(url, key);
    await client.validateToken();
    localStorage.setItem(STORAGE.serverUrl, url);
    localStorage.setItem(STORAGE.apiKey, key);
    setServerUrl(url);
    setApiKey(key);
    setApiClient(client);
    setIsAuthenticated(true);
  }, []);

  const toggleTheme = useCallback(() => {
    setTheme((prev) => {
      const next = prev === 'light' ? 'dark' : 'light';
      localStorage.setItem(STORAGE.theme, next);
      document.documentElement.setAttribute('data-theme', next);
      return next;
    });
  }, []);

  const value = {
    isAuthenticated,
    isLoading,
    apiClient,
    serverUrl,
    apiKey,
    theme,
    login,
    logout,
    toggleTheme
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
}

export default AuthContext;
