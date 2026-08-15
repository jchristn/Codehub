import { useTranslation } from 'react-i18next';
import { LOCALES, canonicalizeLocale } from './localeRegistry';
import { setActiveLocale } from './index';

/**
 * Shared locale selector. Rendered in login, topbar, and settings. Shows each
 * language as its autonym.
 */
function LanguageSelector({ compact = false }) {
  const { t, i18n } = useTranslation();
  const current = canonicalizeLocale(i18n.language);

  return (
    <label className={`language-selector ${compact ? 'compact' : ''}`}>
      <span className="sr-only">{t('common.language')}</span>
      <select
        aria-label={t('common.language')}
        value={current}
        onChange={(e) => setActiveLocale(e.target.value)}
      >
        {LOCALES.map((locale) => (
          <option key={locale.code} value={locale.code}>
            {locale.nativeName}
          </option>
        ))}
      </select>
    </label>
  );
}

export default LanguageSelector;
