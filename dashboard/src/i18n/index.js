import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';
import resources from './resources';
import {
  STORAGE_KEY,
  DEFAULT_LOCALE,
  SUPPORTED_CODES,
  canonicalizeLocale,
  getDirection
} from './localeRegistry';

i18n
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    resources,
    fallbackLng: DEFAULT_LOCALE,
    supportedLngs: SUPPORTED_CODES,
    load: 'languageOnly',
    interpolation: { escapeValue: false },
    detection: {
      order: ['localStorage', 'navigator', 'htmlTag'],
      lookupLocalStorage: STORAGE_KEY,
      caches: ['localStorage']
    }
  });

/**
 * Apply lang + dir to the document root for the active locale.
 */
function applyDocumentLocale(locale) {
  const canonical = canonicalizeLocale(locale);
  document.documentElement.lang = canonical;
  document.documentElement.dir = getDirection(canonical);
}

// Sync document attributes on init and on every change.
applyDocumentLocale(i18n.language || DEFAULT_LOCALE);
i18n.on('languageChanged', applyDocumentLocale);

/**
 * Change the active locale, canonicalizing aliases and persisting the choice.
 */
export function setActiveLocale(locale) {
  const canonical = canonicalizeLocale(locale);
  i18n.changeLanguage(canonical);
  try {
    localStorage.setItem(STORAGE_KEY, canonical);
  } catch {
    /* storage may be unavailable */
  }
}

export default i18n;
