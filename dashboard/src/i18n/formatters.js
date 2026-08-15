import i18n from './index';
import { canonicalizeLocale } from './localeRegistry';

/**
 * Locale-aware formatting helpers. Every helper resolves the active locale
 * explicitly (never relying on the browser default) so output stays consistent
 * with the selected language.
 */

function activeLocale() {
  return canonicalizeLocale(i18n.language || 'en');
}

export function formatNumber(value, options = {}) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '—';
  return new Intl.NumberFormat(activeLocale(), options).format(Number(value));
}

export function formatPercent(value, fractionDigits = 0) {
  if (value === null || value === undefined || Number.isNaN(Number(value))) return '—';
  return new Intl.NumberFormat(activeLocale(), {
    style: 'percent',
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits
  }).format(Number(value));
}

function toDate(value) {
  if (!value) return null;
  const d = value instanceof Date ? value : new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

export function formatDate(value) {
  const d = toDate(value);
  if (!d) return '—';
  return new Intl.DateTimeFormat(activeLocale(), { dateStyle: 'medium' }).format(d);
}

export function formatTime(value) {
  const d = toDate(value);
  if (!d) return '—';
  return new Intl.DateTimeFormat(activeLocale(), { timeStyle: 'medium' }).format(d);
}

export function formatDateTime(value) {
  const d = toDate(value);
  if (!d) return '—';
  return new Intl.DateTimeFormat(activeLocale(), {
    dateStyle: 'medium',
    timeStyle: 'short'
  }).format(d);
}

const RELATIVE_DIVISIONS = [
  { amount: 60, unit: 'second' },
  { amount: 60, unit: 'minute' },
  { amount: 24, unit: 'hour' },
  { amount: 7, unit: 'day' },
  { amount: 4.34524, unit: 'week' },
  { amount: 12, unit: 'month' },
  { amount: Number.POSITIVE_INFINITY, unit: 'year' }
];

export function formatRelativeTime(value) {
  const d = toDate(value);
  if (!d) return '—';
  const rtf = new Intl.RelativeTimeFormat(activeLocale(), { numeric: 'auto' });
  let duration = (d.getTime() - Date.now()) / 1000;
  for (const division of RELATIVE_DIVISIONS) {
    if (Math.abs(duration) < division.amount) {
      return rtf.format(Math.round(duration), division.unit);
    }
    duration /= division.amount;
  }
  return '—';
}

export function formatDuration(ms) {
  if (ms === null || ms === undefined || Number.isNaN(Number(ms))) return '—';
  const value = Number(ms);
  if (value < 1000) return `${formatNumber(Math.round(value))} ms`;
  const seconds = value / 1000;
  if (seconds < 60) return `${formatNumber(seconds, { maximumFractionDigits: 1 })} s`;
  const minutes = Math.floor(seconds / 60);
  const rem = Math.round(seconds % 60);
  return `${formatNumber(minutes)}m ${formatNumber(rem)}s`;
}

export function formatBytes(bytes) {
  if (bytes === null || bytes === undefined || Number.isNaN(Number(bytes))) return '—';
  const value = Number(bytes);
  if (value === 0) return '0 B';
  const units = ['B', 'KB', 'MB', 'GB', 'TB'];
  const exponent = Math.min(Math.floor(Math.log(Math.abs(value)) / Math.log(1024)), units.length - 1);
  const scaled = value / Math.pow(1024, exponent);
  return `${formatNumber(scaled, { maximumFractionDigits: 1 })} ${units[exponent]}`;
}

export function formatList(items) {
  const list = (items || []).filter(Boolean);
  if (list.length === 0) return '—';
  if (typeof Intl.ListFormat === 'function') {
    return new Intl.ListFormat(activeLocale(), { style: 'short', type: 'conjunction' }).format(list);
  }
  return list.join(', ');
}
