import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import PageHeader from '../components/PageHeader';
import DataTable from '../components/DataTable';
import { formatDateTime, formatRelativeTime, formatNumber, formatDuration } from '../i18n/formatters';

/**
 * Scan operations view: live in-flight progress + scan run history.
 */
function ScansView({ apiClient, scanStatus, scanNonce, isScanning, onScanNow }) {
  const { t } = useTranslation();
  const [runs, setRuns] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const load = useCallback(() => {
    if (!apiClient) return;
    setLoading(true);
    setError(null);
    apiClient
      .getScanRuns(50)
      .then((res) => setRuns(Array.isArray(res) ? res : []))
      .catch(() => setError(t('common.error')))
      .finally(() => setLoading(false));
  }, [apiClient, t]);

  useEffect(() => {
    load();
  }, [load, scanNonce, isScanning]);

  const current = scanStatus?.current;
  const repoProgress = Array.isArray(scanStatus?.repositories) ? scanStatus.repositories : [];

  const statusIcon = (status) => {
    switch ((status || 'pending').toLowerCase()) {
      case 'done':
        return '✓';
      case 'scanning':
        return '⟳';
      case 'failed':
        return '✗';
      default:
        return '–';
    }
  };

  const columns = [
    { key: 'trigger', label: t('scans.colTrigger'), render: (r) => r.trigger },
    {
      key: 'status',
      label: t('scans.colStatus'),
      render: (r) => <span className={`run-status run-${(r.status || 'unknown').toLowerCase()}`}>{r.status}</span>
    },
    { key: 'scanned', label: t('scans.colScanned'), className: 'cell-right', render: (r) => `${formatNumber(r.reposScanned || 0)} / ${formatNumber(r.reposTotal || 0)}` },
    { key: 'started', label: t('scans.colStarted'), className: 'cell-nowrap', render: (r) => <span title={formatDateTime(r.startedUtc)}>{formatRelativeTime(r.startedUtc)}</span> },
    { key: 'completed', label: t('scans.colCompleted'), className: 'cell-nowrap', render: (r) => (r.completedUtc ? <span title={formatDateTime(r.completedUtc)}>{formatRelativeTime(r.completedUtc)}</span> : '—') },
    {
      key: 'duration',
      label: t('scans.colDuration'),
      className: 'cell-right',
      render: (r) => (r.completedUtc && r.startedUtc ? formatDuration(new Date(r.completedUtc) - new Date(r.startedUtc)) : '—')
    }
  ];

  const progressPct =
    current && current.reposTotal > 0 ? Math.round((current.reposScanned / current.reposTotal) * 100) : 0;

  return (
    <div className="view scans-view">
      <PageHeader
        title={t('scans.title')}
        subtitle={t('scans.subtitle')}
        actions={
          <button type="button" className="button-primary" onClick={onScanNow} disabled={isScanning}>
            {isScanning ? t('common.scanning') : t('common.scanNow')}
          </button>
        }
      />

      <section className="panel">
        <h2 className="panel-title">{isScanning ? t('scans.inFlight') : t('scans.idle')}</h2>
        {isScanning && current ? (
          <div className="scan-progress">
            <div className="scan-progress-bar">
              <span className="scan-progress-fill" style={{ width: `${progressPct}%` }} />
            </div>
            <div className="scan-progress-meta">
              <span>{t('scans.progress', { done: current.reposScanned || 0, total: current.reposTotal || 0 })}</span>
              <span>{progressPct}%</span>
            </div>
            {repoProgress.length > 0 && (
              <ul className="scan-repo-list">
                {repoProgress.map((r) => {
                  const s = (r.status || 'pending').toLowerCase();
                  return (
                    <li key={r.path} className={`scan-repo scan-repo-${s}`} title={r.path}>
                      <span className="scan-repo-icon" aria-hidden="true">{statusIcon(r.status)}</span>
                      <span className="scan-repo-name">{r.name}</span>
                      <span className="scan-repo-status">{t(`scans.status.${s}`)}</span>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        ) : (
          <p className="muted">
            {t('scans.repoCount', { count: scanStatus?.repositoryCount ?? 0 })}
          </p>
        )}
      </section>

      <section className="panel">
        <DataTable columns={columns} rows={runs} loading={loading} error={error} emptyMessage={t('scans.empty')} />
      </section>
    </div>
  );
}

export default ScansView;
