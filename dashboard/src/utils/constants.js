/**
 * Application constants shared across views.
 */

export const STORAGE = {
  serverUrl: 'codehub.serverUrl',
  apiKey: 'codehub.apiKey',
  theme: 'codehub.theme',
  repoPageSize: 'codehub.repoPageSize',
  repoHiddenColumns: 'codehub.repoHiddenColumns',
  requestPageSize: 'codehub.requestPageSize',
  explorerHistory: 'codehub.explorerHistory'
};

// When the dashboard is served by the backend (at /dashboard) the origin is the backend.
// In the Vite dev server the origin is :3000 and /v1.0 is proxied to the backend.
export const DEFAULT_SERVER_URL =
  typeof window !== 'undefined' && window.location?.origin ? window.location.origin : 'http://127.0.0.1:8090';
export const DEFAULT_API_KEY_HINT = 'codehub-dev-key';

export const GITHUB_REPO_URL = 'https://github.com/jchristn/codehub';

export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100, 250, 500, 1000];
export const DEFAULT_PAGE_SIZE = 25;

/** Health status → semantic token mapping. Color is never the only signal. */
export const HEALTH = {
  Green: { tone: 'success', letter: 'G' },
  Yellow: { tone: 'warning', letter: 'Y' },
  Red: { tone: 'danger', letter: 'R' },
  NotApplicable: { tone: 'muted', letter: '–' },
  Unknown: { tone: 'neutral', letter: '?' }
};

/** The five signal columns, in display order. */
export const SIGNAL_TYPES = [
  { type: 'TestInfra', labelKey: 'signals.testInfra' },
  { type: 'Telemetry', labelKey: 'signals.telemetry' },
  { type: 'OutdatedDependencies', labelKey: 'signals.outdatedDeps' },
  { type: 'Vulnerabilities', labelKey: 'signals.vulnerabilities' },
  { type: 'IssuesAndPullRequests', labelKey: 'signals.issuesAndPullRequests' }
];

export const HEALTH_FILTER_OPTIONS = ['Green', 'Yellow', 'Red', 'NotApplicable', 'Unknown'];
export const VISIBILITY_OPTIONS = ['Open', 'Closed', 'Unknown'];
export const SIGNAL_STATUS_OPTIONS = ['Green', 'Yellow', 'Red', 'NotApplicable', 'Unknown'];

export const HTTP_METHODS = ['GET', 'POST', 'PUT', 'DELETE', 'PATCH', 'HEAD'];

/** Chart range presets — bucket counts must match the backend contract. */
export const CHART_RANGES = [
  { id: 'hour', labelKey: 'chart.rangeHour', bucketMinutes: 1, buckets: 60, windowMinutes: 60 },
  { id: 'day', labelKey: 'chart.rangeDay', bucketMinutes: 15, buckets: 96, windowMinutes: 24 * 60 },
  { id: 'week', labelKey: 'chart.rangeWeek', bucketMinutes: 120, buckets: 84, windowMinutes: 7 * 24 * 60 },
  { id: 'month', labelKey: 'chart.rangeMonth', bucketMinutes: 360, buckets: 120, windowMinutes: 30 * 24 * 60 }
];

/**
 * Compute {fromUtc, toUtc, bucketMinutes} ISO params for a chart range id.
 */
export function rangeToParams(rangeId) {
  const range = CHART_RANGES.find((r) => r.id === rangeId) || CHART_RANGES[1];
  const to = new Date();
  const from = new Date(to.getTime() - range.windowMinutes * 60 * 1000);
  return {
    fromUtc: from.toISOString(),
    toUtc: to.toISOString(),
    bucketMinutes: range.bucketMinutes
  };
}
