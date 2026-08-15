import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { ApiError } from '../utils/api';
import LanguageSelector from '../i18n/LanguageSelector';
import { DEFAULT_SERVER_URL, DEFAULT_API_KEY_HINT } from '../utils/constants';

/**
 * Branded login. Fields: Server URL + API Key (static single-user key).
 */
function Login() {
  const { t } = useTranslation();
  const { login } = useAuth();
  const [serverUrl, setServerUrl] = useState(DEFAULT_SERVER_URL);
  const [apiKey, setApiKey] = useState('');
  const [showKey, setShowKey] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      await login(serverUrl.trim(), apiKey.trim());
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError(t('login.invalidKey'));
      } else {
        setError(t('login.failed'));
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-lang">
        <LanguageSelector />
      </div>

      <div className="login-card">
        <div className="login-header">
          <img src="/logo.png" alt={t('common.appName')} className="login-logo" />
          <h1 className="login-brand">{t('common.appName')}</h1>
          <p className="login-tagline">{t('common.tagline')}</p>
        </div>

        <form onSubmit={handleSubmit} className="login-form">
          <h2 className="login-title">{t('login.title')}</h2>
          <p className="login-subtitle">{t('login.subtitle')}</p>

          <div className="form-group">
            <label htmlFor="serverUrl">{t('login.serverUrl')}</label>
            <input
              id="serverUrl"
              type="text"
              value={serverUrl}
              onChange={(e) => setServerUrl(e.target.value)}
              placeholder={DEFAULT_SERVER_URL}
              autoComplete="url"
              required
              disabled={loading}
            />
          </div>

          <div className="form-group">
            <label htmlFor="apiKey">{t('login.apiKey')}</label>
            <div className="input-with-affix">
              <input
                id="apiKey"
                type={showKey ? 'text' : 'password'}
                value={apiKey}
                onChange={(e) => setApiKey(e.target.value)}
                placeholder={DEFAULT_API_KEY_HINT}
                autoComplete="off"
                required
                disabled={loading}
              />
              <button
                type="button"
                className="input-affix-button"
                onClick={() => setShowKey((s) => !s)}
                aria-label={showKey ? t('login.hideKey') : t('login.showKey')}
                title={showKey ? t('login.hideKey') : t('login.showKey')}
              >
                {showKey ? '🙈' : '👁'}
              </button>
            </div>
            <p className="form-hint">{t('login.apiKeyHint')}</p>
          </div>

          {error && <div className="error-message" role="alert">{error}</div>}

          <button type="submit" className="button-primary login-submit" disabled={loading}>
            {loading ? t('login.connecting') : t('login.connect')}
          </button>
        </form>
      </div>
    </div>
  );
}

export default Login;
