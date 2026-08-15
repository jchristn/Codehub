import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import LanguageSelector from '../i18n/LanguageSelector';
import CopyButton from './CopyButton';
import { formatRelativeTime } from '../i18n/formatters';
import { GITHUB_REPO_URL } from '../utils/constants';

/**
 * Application top bar. Left: brand context (server URL chip, root path,
 * last-scanned, health summary). Right: compact utility actions (Scan Now,
 * GitHub, theme, language, logout).
 */
function Topbar({ onToggleSidebar, rootPath, lastScannedUtc, healthSummary, isScanning, onScanNow, scanDisabled }) {
  const { t } = useTranslation();
  const { serverUrl, theme, toggleTheme, logout } = useAuth();

  return (
    <header className="topbar">
      <div className="topbar-left">
        <button type="button" className="icon-button" onClick={onToggleSidebar} aria-label="Toggle navigation" title="Toggle navigation">
          ☰
        </button>

        <span className="chip chip-server" title={serverUrl}>
          <span className="chip-value">{serverUrl}</span>
          <CopyButton value={serverUrl} label="server URL" size="sm" />
        </span>

        {rootPath && (
          <span className="chip chip-root mono" title={rootPath}>
            {rootPath}
          </span>
        )}

        <span className="chip chip-scan">
          {t('common.lastScanned')}: {isScanning ? t('common.scanning') : formatRelativeTime(lastScannedUtc)}
        </span>

        {healthSummary && (
          <span className="health-summary" aria-label="Health summary">
            <span className="hs tone-success">{healthSummary.green ?? 0} G</span>
            <span className="hs tone-warning">{healthSummary.yellow ?? 0} Y</span>
            <span className="hs tone-danger">{healthSummary.red ?? 0} R</span>
          </span>
        )}
      </div>

      <div className="topbar-right">
        <button type="button" className="button-primary scan-now" onClick={onScanNow} disabled={scanDisabled || isScanning}>
          {isScanning ? t('common.scanning') : t('common.scanNow')}
        </button>

        <a className="icon-button" href={GITHUB_REPO_URL} target="_blank" rel="noreferrer" aria-label={t('common.githubRepo')} title={t('common.githubRepo')}>
          <svg viewBox="0 0 16 16" width="18" height="18" fill="currentColor" aria-hidden="true">
            <path d="M8 0C3.58 0 0 3.58 0 8c0 3.54 2.29 6.53 5.47 7.59.4.07.55-.17.55-.38 0-.19-.01-.82-.01-1.49-2.01.37-2.53-.49-2.69-.94-.09-.23-.48-.94-.82-1.13-.28-.15-.68-.52-.01-.53.63-.01 1.08.58 1.23.82.72 1.21 1.87.87 2.33.66.07-.52.28-.87.51-1.07-1.78-.2-3.64-.89-3.64-3.95 0-.87.31-1.59.82-2.15-.08-.2-.36-1.02.08-2.12 0 0 .67-.21 2.2.82.64-.18 1.32-.27 2-.27.68 0 1.36.09 2 .27 1.53-1.04 2.2-.82 2.2-.82.44 1.1.16 1.92.08 2.12.51.56.82 1.27.82 2.15 0 3.07-1.87 3.75-3.65 3.95.29.25.54.73.54 1.48 0 1.07-.01 1.93-.01 2.2 0 .21.15.46.55.38A8.01 8.01 0 0 0 16 8c0-4.42-3.58-8-8-8Z"></path>
          </svg>
        </a>

        <button
          type="button"
          className="icon-button"
          onClick={toggleTheme}
          aria-label={theme === 'dark' ? t('common.themeLight') : t('common.themeDark')}
          title={theme === 'dark' ? t('common.themeLight') : t('common.themeDark')}
        >
          {theme === 'dark' ? '☀' : '☾'}
        </button>

        <LanguageSelector compact />

        <button type="button" className="icon-button" onClick={logout} aria-label={t('common.logout')} title={t('common.logout')}>
          <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
            <polyline points="16 17 21 12 16 7"></polyline>
            <line x1="21" y1="12" x2="9" y2="12"></line>
          </svg>
        </button>
      </div>
    </header>
  );
}

export default Topbar;
