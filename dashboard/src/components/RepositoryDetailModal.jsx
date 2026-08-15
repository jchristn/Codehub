import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import StatusBadge from './StatusBadge';
import CopyButton from './CopyButton';
import LanguageBadges from './LanguageBadges';
import JsonBlock from './JsonBlock';
import { SIGNAL_TYPES } from '../utils/constants';
import { formatDateTime, formatRelativeTime, formatNumber } from '../i18n/formatters';

/**
 * Repository detail modal. Drills into one repository's projects,
 * dependencies, signals, and GitHub snapshot. Fetched fresh on open.
 */
function RepositoryDetailModal({ apiClient, repositoryId, onClose, onRepositoryUpdated }) {
  const { t } = useTranslation();
  const [detail, setDetail] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);

  const load = useCallback(() => {
    if (!apiClient || !repositoryId) return;
    setLoading(true);
    setError(null);
    apiClient
      .getRepository(repositoryId)
      .then((data) => setDetail(data))
      .catch(() => setError(t('common.error')))
      .finally(() => setLoading(false));
  }, [apiClient, repositoryId, t]);

  useEffect(() => {
    load();
  }, [load]);

  const repo = detail?.repository;
  const signals = detail?.signals || [];
  const projects = detail?.projects || [];
  const dependencies = detail?.dependencies || [];
  const gitHub = detail?.gitHub;

  const signalFor = (type) => signals.find((s) => s.signalType === type);

  const toggleInclusion = useCallback(async () => {
    if (!repo) return;
    setBusy(true);
    try {
      const updated = repo.isIncluded
        ? await apiClient.excludeRepository(repo.id)
        : await apiClient.includeRepository(repo.id);
      setDetail((prev) => ({ ...prev, repository: updated?.repository || updated || prev.repository }));
      if (onRepositoryUpdated) onRepositoryUpdated();
    } catch {
      /* surfaced via toast at call sites elsewhere; keep modal open */
    } finally {
      setBusy(false);
    }
  }, [apiClient, repo, onRepositoryUpdated]);

  return (
    <Modal
      open
      onClose={onClose}
      size="xl"
      title={loading ? t('common.loading') : repo?.name}
      subtitle={
        repo ? (
          <span className="modal-subtitle-path">
            <span className="mono">{repo.path}</span>
            <CopyButton value={repo.path} label="path" size="sm" />
          </span>
        ) : null
      }
      headerExtra={repo && <span className={`vis-badge vis-${(repo.visibility || 'unknown').toLowerCase()}`} title={t('repositories.colVisibility')}>{repo.visibility}</span>}
      footer={
        repo && (
          <>
            <button type="button" className="button-secondary" onClick={onClose}>
              {t('common.close')}
            </button>
            <button type="button" className={repo.isIncluded ? 'button-danger' : 'button-primary'} onClick={toggleInclusion} disabled={busy}>
              {repo.isIncluded ? t('repositories.excludeAction') : t('repositories.includeAction')}
            </button>
          </>
        )
      }
    >
      {loading ? (
        <div className="modal-loading">
          <div className="loading-spinner" />
        </div>
      ) : error ? (
        <div className="table-state error">
          {error}
          <button type="button" className="button-secondary" onClick={load}>
            {t('common.retry')}
          </button>
        </div>
      ) : (
        <div className="repo-detail">
          {/* Overview */}
          <section className="detail-section">
            <h3 className="detail-section-title">{t('detail.overview')}</h3>
            <div className="kv-grid">
              <div className="kv"><span className="kv-key">{t('detail.version')}</span><span className="kv-val mono">{repo.currentVersion || '—'}</span></div>
              <div className="kv"><span className="kv-key">{t('detail.lastCommit')}</span><span className="kv-val" title={formatDateTime(repo.lastUpdateUtc)}>{formatRelativeTime(repo.lastUpdateUtc)}</span></div>
              {repo.isGitRepository && (
                <div className="kv"><span className="kv-key">{t('repositories.colBranch')}</span><span className="kv-val mono">{repo.currentBranch || '—'}</span></div>
              )}
              {repo.isGitRepository && repo.currentBranch && (
                <div className="kv">
                  <span className="kv-key">{t('repositories.colAheadBehind')}</span>
                  <span className="kv-val ahead-behind" title={t('repositories.aheadBehindTitle', { ahead: repo.commitsAhead || 0, behind: repo.commitsBehind || 0, base: repo.baseBranch || 'main' })}>
                    <span className={(repo.commitsAhead || 0) > 0 ? 'ab-ahead active' : 'ab-ahead'}>↑{repo.commitsAhead || 0}</span>
                    <span className={(repo.commitsBehind || 0) > 0 ? 'ab-behind active' : 'ab-behind'}>↓{repo.commitsBehind || 0}</span>
                  </span>
                </div>
              )}
              <div className="kv"><span className="kv-key">{t('detail.overall')}</span><span className="kv-val"><StatusBadge status={repo.overallHealth} /></span></div>
              <div className="kv"><span className="kv-key">{t('repositories.colVisibility')}</span><span className="kv-val">{repo.visibility}</span></div>
              <div className="kv"><span className="kv-key">{t('detail.languages')}</span><span className="kv-val"><LanguageBadges languages={repo.languages} primary={repo.primaryLanguage} /></span></div>
              <div className="kv">
                <span className="kv-key">{t('detail.remote')}</span>
                <span className="kv-val">
                  {repo.remoteUrl ? (
                    <a href={repo.remoteUrl} target="_blank" rel="noreferrer" className="link">
                      {t('detail.openRemote')}
                    </a>
                  ) : (
                    '—'
                  )}
                </span>
              </div>
            </div>
          </section>

          {/* Signals breakdown */}
          <section className="detail-section">
            <h3 className="detail-section-title">{t('detail.signalsBreakdown')}</h3>
            <div className="signal-breakdown">
              {SIGNAL_TYPES.map((sig) => {
                const s = signalFor(sig.type);
                return (
                  <div className="signal-row" key={sig.type}>
                    <div className="signal-row-head">
                      <span className="signal-row-label">{t(sig.labelKey)}</span>
                      <StatusBadge status={s?.status || 'Unknown'} detail={s?.detail} />
                    </div>
                    <p className="signal-row-detail">{s?.detail || t('signals.noEvidence')}</p>
                  </div>
                );
              })}
            </div>
          </section>

          {/* Projects */}
          <section className="detail-section">
            <h3 className="detail-section-title">{t('detail.projects')} ({formatNumber(projects.length)})</h3>
            <div className="detail-table-scroll">
              <table className="data-table compact">
                <thead>
                  <tr>
                    <th>{t('detail.projName')}</th>
                    <th>{t('detail.projType')}</th>
                    <th>{t('detail.projVersion')}</th>
                    <th>{t('detail.projFramework')}</th>
                    <th className="cell-center">{t('detail.projWeb')}</th>
                    <th className="cell-center">{t('detail.projTouchstone')}</th>
                    <th className="cell-center">{t('detail.projTelemetry')}</th>
                    <th className="cell-right">{t('detail.projOutdated')}</th>
                    <th className="cell-right">{t('detail.projVulnerable')}</th>
                  </tr>
                </thead>
                <tbody>
                  {projects.length === 0 ? (
                    <tr><td colSpan={9} className="table-state">{t('detail.noProjects')}</td></tr>
                  ) : (
                    projects.map((p) => (
                      <tr key={p.id}>
                        <td>
                          <div className="repo-cell">
                            <span>{p.name}</span>
                            <span className="repo-path mono">{p.relativePath}</span>
                          </div>
                        </td>
                        <td>{p.type}</td>
                        <td className="mono">{p.version || '—'}</td>
                        <td className="mono">{p.targetFramework || '—'}</td>
                        <td className="cell-center">{boolMark(p.isWebService)}</td>
                        <td className="cell-center">{boolMark(p.hasTouchstone)}</td>
                        <td className="cell-center">{p.hasRadiant ? 'R' : ''}{p.hasWatson7 ? ' W7' : ''}{!p.hasRadiant && !p.hasWatson7 ? '—' : ''}</td>
                        <td className="cell-right">{formatNumber(p.outdatedCount || 0)}</td>
                        <td className="cell-right">{formatNumber(p.vulnerableCount || 0)}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>

          {/* Dependencies */}
          <section className="detail-section">
            <h3 className="detail-section-title">{t('detail.dependencies')} ({formatNumber(dependencies.length)})</h3>
            <div className="detail-table-scroll">
              <table className="data-table compact">
                <thead>
                  <tr>
                    <th>{t('detail.depPackage')}</th>
                    <th>{t('detail.depVersion')}</th>
                    <th>{t('detail.depDrift')}</th>
                    <th>{t('detail.depSeverity')}</th>
                  </tr>
                </thead>
                <tbody>
                  {dependencies.length === 0 ? (
                    <tr><td colSpan={4} className="table-state">{t('detail.noDependencies')}</td></tr>
                  ) : (
                    dependencies.map((d) => (
                      <tr key={d.id}>
                        <td>
                          <div className="repo-cell">
                            <span className="mono">{d.packageName}</span>
                            <span className="repo-path">{d.ecosystem}</span>
                          </div>
                        </td>
                        <td className="mono">{d.currentVersion} → {d.latestVersion || '—'}</td>
                        <td><span className={`drift-badge drift-${(d.drift || 'none').toLowerCase()}`}>{d.drift || 'None'}</span></td>
                        <td>{d.isVulnerable ? <span className={`sev-badge sev-${(d.severity || 'none').toLowerCase()}`}>{d.severity}</span> : <span className="muted">—</span>}</td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>

          {/* GitHub */}
          <section className="detail-section">
            <h3 className="detail-section-title">{t('detail.github')}</h3>
            {!gitHub || gitHub.error ? (
              <div className="banner banner-muted">{gitHub?.error || t('detail.githubNotConfigured')}</div>
            ) : (
              <div className="kv-grid">
                <div className="kv"><span className="kv-key">{t('detail.ghOpenIssues')}</span><span className="kv-val">{formatNumber(gitHub.openIssues)}</span></div>
                <div className="kv"><span className="kv-key">{t('detail.ghOpenPrs')}</span><span className="kv-val">{formatNumber(gitHub.openPullRequests)}</span></div>
                <div className="kv"><span className="kv-key">{t('detail.ghDependabot')}</span><span className="kv-val">{formatNumber(gitHub.dependabotOpen)}</span></div>
                <div className="kv"><span className="kv-key">{t('detail.ghDependabotHigh')}</span><span className="kv-val">{formatNumber(gitHub.dependabotHigh)}</span></div>
                <div className="kv"><span className="kv-key">{t('detail.ghDependabotCritical')}</span><span className="kv-val">{formatNumber(gitHub.dependabotCritical)}</span></div>
              </div>
            )}
          </section>

          {/* Raw JSON */}
          <section className="detail-section">
            <h3 className="detail-section-title">{t('common.rawJson')}</h3>
            <JsonBlock value={detail} label="JSON" />
          </section>
        </div>
      )}
    </Modal>
  );
}

function boolMark(value) {
  return value ? <span className="tone-success">✓</span> : <span className="muted">—</span>;
}

export default RepositoryDetailModal;
