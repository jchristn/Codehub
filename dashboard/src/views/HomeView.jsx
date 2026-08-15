import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import PageHeader from '../components/PageHeader';
import KpiTile from '../components/KpiTile';
import StatusBadge from '../components/StatusBadge';
import DataTable from '../components/DataTable';
import { formatRelativeTime, formatDateTime, formatNumber } from '../i18n/formatters';

/**
 * Overview command center: KPI tiles, a health-distribution visual, and an
 * attention list linking into the filtered repositories table.
 */
function HomeView({ overview, onRefreshOverview, onScanNow, isScanning, lastScannedUtc }) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  const goToRepos = (params = {}) => {
    const search = new URLSearchParams(params).toString();
    navigate(`/repositories${search ? `?${search}` : ''}`);
  };

  const kpis = [
    { label: t('home.kpiTotal'), value: overview?.totalRepositories ?? 0, tone: 'neutral', onClick: () => goToRepos() },
    { label: t('home.kpiAttention'), value: overview?.needsAttention ?? 0, tone: 'danger', onClick: () => goToRepos({ health: 'Red' }) },
    { label: t('home.kpiGreen'), value: overview?.greenCount ?? 0, tone: 'success', onClick: () => goToRepos({ health: 'Green' }) },
    { label: t('home.kpiYellow'), value: overview?.yellowCount ?? 0, tone: 'warning', onClick: () => goToRepos({ health: 'Yellow' }) },
    { label: t('home.kpiRed'), value: overview?.redCount ?? 0, tone: 'danger', onClick: () => goToRepos({ health: 'Red' }) },
    { label: t('home.kpiNoTests'), value: overview?.reposWithoutTests ?? 0, tone: 'warning', onClick: () => goToRepos({ signalType: 'TestInfra', signalStatus: 'Red' }) },
    { label: t('home.kpiNoTelemetry'), value: overview?.webServicesWithoutTelemetry ?? 0, tone: 'warning', onClick: () => goToRepos({ signalType: 'Telemetry', signalStatus: 'Red' }) },
    { label: t('home.kpiHighCves'), value: overview?.reposWithHighCves ?? 0, tone: 'danger', onClick: () => goToRepos({ signalType: 'Vulnerabilities', signalStatus: 'Red' }) },
    { label: t('home.kpiOutdated'), value: overview?.reposWithOutdatedDeps ?? 0, tone: 'warning', onClick: () => goToRepos({ signalType: 'OutdatedDependencies', signalStatus: 'Red' }) }
  ];

  const total = overview?.totalRepositories ?? 0;
  const dist = [
    { key: 'Green', tone: 'success', count: overview?.greenCount ?? 0 },
    { key: 'Yellow', tone: 'warning', count: overview?.yellowCount ?? 0 },
    { key: 'Red', tone: 'danger', count: overview?.redCount ?? 0 }
  ];

  const attentionRows = overview?.attentionList || [];
  const hasScan = Boolean(lastScannedUtc);

  const columns = [
    {
      key: 'name',
      label: t('repositories.colRepository'),
      render: (row) => (
        <div className="repo-cell">
          <span className="repo-name">{row.repository?.name}</span>
          <span className="repo-path mono">{row.repository?.path}</span>
        </div>
      )
    },
    {
      key: 'overall',
      label: t('repositories.colOverall'),
      className: 'cell-center',
      render: (row) => <StatusBadge status={row.repository?.overallHealth} />
    }
  ];

  return (
    <div className="view home-view">
      <PageHeader
        title={t('home.title')}
        subtitle={t('home.subtitle')}
        actions={
          <div className="header-actions-inline">
            <span className="last-scanned-chip">
              {t('common.lastScanned')}: {isScanning ? t('common.scanning') : formatRelativeTime(lastScannedUtc)}
            </span>
            <button type="button" className="button-secondary" onClick={onRefreshOverview}>
              {t('common.refresh')}
            </button>
            <button type="button" className="button-primary" onClick={onScanNow} disabled={isScanning}>
              {isScanning ? t('common.scanning') : t('common.scanNow')}
            </button>
          </div>
        }
      />

      {!hasScan && <div className="banner banner-info">{t('home.noScan')}</div>}

      <section className="kpi-grid">
        {kpis.map((kpi) => (
          <KpiTile key={kpi.label} {...kpi} />
        ))}
      </section>

      <section className="panel">
        <h2 className="panel-title">{t('home.distribution')}</h2>
        <div className="health-distribution">
          <div className="dist-bar" role="img" aria-label={t('home.distribution')}>
            {dist.map((seg) => {
              const pct = total > 0 ? (seg.count / total) * 100 : 0;
              return seg.count > 0 ? (
                <span
                  key={seg.key}
                  className={`dist-segment tone-${seg.tone}`}
                  style={{ width: `${pct}%` }}
                  title={`${seg.key}: ${seg.count}`}
                />
              ) : null;
            })}
          </div>
          <div className="dist-legend">
            {dist.map((seg) => (
              <div key={seg.key} className="dist-legend-item">
                <StatusBadge status={seg.key} />
                <span className="dist-count">{formatNumber(seg.count)}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="panel-title">{t('home.attentionList')}</h2>
          <button type="button" className="button-secondary" onClick={() => goToRepos()}>
            {t('home.viewAll')}
          </button>
        </div>
        <DataTable
          columns={columns}
          rows={attentionRows}
          rowKey={(row) => row.repository?.id}
          onRowClick={(row) => goToRepos({ q: row.repository?.name })}
          emptyMessage={t('home.attentionEmpty')}
        />
        {lastScannedUtc && (
          <p className="panel-footnote">
            {t('common.lastScanned')}: {formatDateTime(lastScannedUtc)}
          </p>
        )}
      </section>
    </div>
  );
}

export default HomeView;
