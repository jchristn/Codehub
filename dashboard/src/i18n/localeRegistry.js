/**
 * Central locale registry using canonical BCP 47 identifiers.
 * Adding a new locale means adding a row here plus a resource bundle — no
 * application logic changes required.
 */

export const STORAGE_KEY = 'codehub.locale';
export const DEFAULT_LOCALE = 'en';

/**
 * Supported locales. `pseudo` is an expansion/bidi test locale.
 */
export const LOCALES = [
  { code: 'en', englishName: 'English', nativeName: 'English', dir: 'ltr', fallback: 'en' },
  { code: 'es', englishName: 'Spanish', nativeName: 'Español', dir: 'ltr', fallback: 'en' },
  { code: 'fr', englishName: 'French', nativeName: 'Français', dir: 'ltr', fallback: 'en' },
  { code: 'de', englishName: 'German', nativeName: 'Deutsch', dir: 'ltr', fallback: 'en' },
  { code: 'pt', englishName: 'Portuguese', nativeName: 'Português', dir: 'ltr', fallback: 'en' }
];

export const SUPPORTED_CODES = LOCALES.map((l) => l.code);

/**
 * Alias normalization: map product labels / region variants onto real codes.
 */
const ALIASES = {
  english: 'en',
  spanish: 'es',
  espanol: 'es',
  'español': 'es',
  french: 'fr',
  francais: 'fr',
  'français': 'fr',
  german: 'de',
  deutsch: 'de',
  portuguese: 'pt',
  'português': 'pt',
  portugues: 'pt',
  'pt-br': 'pt'
};

/**
 * Normalize an arbitrary locale-ish string to a supported canonical code.
 */
export function canonicalizeLocale(input) {
  if (!input) return DEFAULT_LOCALE;
  const lower = String(input).toLowerCase();
  if (ALIASES[lower]) return ALIASES[lower];
  const base = lower.split('-')[0];
  if (SUPPORTED_CODES.includes(lower)) return lower;
  if (SUPPORTED_CODES.includes(base)) return base;
  return DEFAULT_LOCALE;
}

export function getLocaleMeta(code) {
  const canonical = canonicalizeLocale(code);
  return LOCALES.find((l) => l.code === canonical) || LOCALES[0];
}

export function getDirection(code) {
  return getLocaleMeta(code).dir;
}
