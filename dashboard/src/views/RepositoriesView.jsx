import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useToast } from '../context/ToastContext';
import PageHeader from '../components/PageHeader';
import DataTable from '../components/DataTable';
import Pagination from '../components/Pagination';
import StatusBadge from '../components/StatusBadge';
import ActionMenu from '../components/ActionMenu';
import RepositoryDetailModal from '../components/RepositoryDetailModal';
import DirectoryPicker from '../components/DirectoryPicker';
import AddMultipleModal from '../components/AddMultipleModal';
import LaunchToolModal from '../components/LaunchToolModal';
import ColumnPicker from '../components/ColumnPicker';
import useDebounce from '../hooks/useDebounce';
import { SIGNAL_TYPES, STORAGE, DEFAULT_PAGE_SIZE } from '../utils/constants';
import { formatRelativeTime, formatDateTime } from '../i18n/formatters';

const EMPTY_FILTERS = {
  q: '',
  language: '',
  version: '',
  frameworks: '',
  branch: '',
  commits: '',
  updated: '',
  tests: '',
  telemetry: '',
  dependencies: '',
  security: '',
  issuepr: '',
  overall: ''
};

const SIGNAL_FILTER_KEY = {
  TestInfra: 'tests',
  Telemetry: 'telemetry',
  OutdatedDependencies: 'dependencies',
  Vulnerabilities: 'security',
  IssuesAndPullRequests: 'issuepr'
};

const LANG_SHORT = { CSharp: 'C#', Node: 'Node', Python: 'Py', PowerShell: 'PS', Unknown: '?' };

/**
 * THE main table — one row per repository. Per-column filters (text + dropdown)
 * live in a filter row under the header; sorting, filtering and paging are all
 * backend-driven.
 */
function RepositoriesView({ apiClient, scanNonce, isScanning, onScanNow, lastScannedUtc, nextScanUtc }) {
  const { t } = useTranslation();
  const toast = useToast();

  const [filters, setFilters] = useState(EMPTY_FILTERS);
  const [includeExcluded, setIncludeExcluded] = useState(false);
  const [sort, setSort] = useState('overall');
  const [dir, setDir] = useState('desc');
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(() => Number(localStorage.getItem(STORAGE.repoPageSize)) || DEFAULT_PAGE_SIZE);

  const [data, setData] = useState({ items: [], totalCount: 0 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedId, setSelectedId] = useState(null);
  const [showPicker, setShowPicker] = useState(false);
  const [showAddMultiple, setShowAddMultiple] = useState(false);
  const [launch, setLaunch] = useState(null);
  const [hiddenColumns, setHiddenColumns] = useState(() => {
    try {
      return new Set(JSON.parse(localStorage.getItem(STORAGE.repoHiddenColumns) || '[]'));
    } catch {
      return new Set();
    }
  });

  const debouncedFilters = useDebounce(filters, 300);

  const persistHidden = (next) => {
    setHiddenColumns(next);
    localStorage.setItem(STORAGE.repoHiddenColumns, JSON.stringify([...next]));
  };
  const toggleColumn = (key) => {
    const next = new Set(hiddenColumns);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    persistHidden(next);
  };
  const resetColumns = () => persistHidden(new Set());

  const load = useCallback(() => {
    if (!apiClient) return;
    setLoading(true);
    setError(null);
    apiClient
      .getRepositories({
        ...debouncedFilters,
        includeExcluded: includeExcluded ? 'true' : '',
        sort,
        dir,
        pageNumber,
        pageSize
      })
      .then((res) => setData(res || { items: [], totalCount: 0 }))
      .catch(() => setError(t('common.error')))
      .finally(() => setLoading(false));
  }, [apiClient, debouncedFilters, includeExcluded, sort, dir, pageNumber, pageSize, t]);

  useEffect(() => {
    load();
  }, [load, scanNonce]);

  // Reset to page 1 when filters/sort change.
  useEffect(() => {
    setPageNumber(1);
  }, [debouncedFilters, includeExcluded, sort, dir]);

  const updateFilter = (name, value) => setFilters((prev) => ({ ...prev, [name]: value }));
  const clearFilters = () => setFilters(EMPTY_FILTERS);
  const hasActiveFilters = Object.values(filters).some((v) => v) || includeExcluded;

  const handleSortChange = (sortKey, direction) => {
    setSort(sortKey);
    setDir(direction);
  };

  const handlePageSizeChange = (size) => {
    setPageSize(size);
    localStorage.setItem(STORAGE.repoPageSize, String(size));
    setPageNumber(1);
  };

  const toggleInclusion = useCallback(
    async (repo) => {
      try {
        if (repo.isIncluded) await apiClient.excludeRepository(repo.id);
        else await apiClient.includeRepository(repo.id);
        toast.success(repo.isIncluded ? t('repositories.excluded') : t('repositories.included'));
        load();
      } catch {
        toast.error(t('common.error'));
      }
    },
    [apiClient, toast, t, load]
  );

  const openExternal = useCallback(
    async (repo, target) => {
      try {
        await apiClient.openRepository(repo.id, target);
        toast.success(t('launch.started', { tool: target }));
      } catch (e) {
        toast.error(e?.body || t('launch.failed'));
      }
    },
    [apiClient, toast, t]
  );

  const signalFor = (row, type) => (row.signals || []).find((s) => s.signalType === type);

  // Filter-control factories (read/write the live filter state).
  const textFilter = (key, placeholder) => (
    <input
      className="col-filter"
      type="text"
      value={filters[key]}
      placeholder={placeholder}
      onChange={(e) => updateFilter(key, e.target.value)}
    />
  );
  const selectFilter = (key, options) => (
    <select className="col-filter" value={filters[key]} onChange={(e) => updateFilter(key, e.target.value)}>
      <option value="">{t('common.all')}</option>
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );

  const RYG = [
    { value: 'Green', label: t('health.green') },
    { value: 'Yellow', label: t('health.yellow') },
    { value: 'Red', label: t('health.red') }
  ];
  const LANG_OPTIONS = [
    { value: 'CSharp', label: 'C#' },
    { value: 'Node', label: 'Node' },
    { value: 'Python', label: 'Python' },
    { value: 'PowerShell', label: 'PowerShell' }
  ];
  const COMMITS_OPTIONS = [
    { value: 'Ahead', label: t('repositories.commitsAhead') },
    { value: 'Behind', label: t('repositories.commitsBehind') },
    { value: 'Even', label: t('repositories.commitsEven') }
  ];
  const UPDATED_OPTIONS = [
    { value: 'None', label: t('repositories.updatedNone') },
    { value: 'LastDay', label: t('repositories.updatedDay') },
    { value: 'LastWeek', label: t('repositories.updatedWeek') },
    { value: 'LastMonth', label: t('repositories.updatedMonth') },
    { value: 'LastYear', label: t('repositories.updatedYear') },
    { value: 'Older', label: t('repositories.updatedOlder') }
  ];
  const YESNO = [
    { value: 'Yes', label: t('common.yes') },
    { value: 'No', label: t('common.no') }
  ];

  const signalColumns = SIGNAL_TYPES.map((sig) => ({
    key: sig.type,
    label: t(sig.labelKey),
    sortKey: sig.type,
    className: 'cell-center cell-signal',
    render: (row) => {
      const s = signalFor(row, sig.type);
      return <StatusBadge status={s?.status || 'Unknown'} detail={s?.detail} short />;
    },
    renderFilter: () =>
      sig.type === 'IssuesAndPullRequests'
        ? selectFilter('issuepr', YESNO)
        : selectFilter(SIGNAL_FILTER_KEY[sig.type], RYG)
  }));

  const columns = [
    {
      key: 'actions',
      label: t('common.actions'),
      className: 'cell-actions',
      render: (row) => (
        <ActionMenu
          items={[
            { label: t('common.viewDetails'), onClick: () => setSelectedId(row.repository?.id) },
            { label: t('actions.openExplorer'), onClick: () => openExternal(row.repository, 'explorer') },
            { label: t('actions.openTerminal'), onClick: () => openExternal(row.repository, 'terminal') },
            { label: t('actions.openClaude'), onClick: () => setLaunch({ repository: row.repository, tool: 'claude' }) },
            { label: t('actions.openCodex'), onClick: () => setLaunch({ repository: row.repository, tool: 'codex' }) },
            { label: t('actions.openMux'), onClick: () => setLaunch({ repository: row.repository, tool: 'mux' }) },
            { label: t('actions.openOpenCode'), onClick: () => setLaunch({ repository: row.repository, tool: 'opencode' }) },
            {
              label: row.repository?.isIncluded ? t('repositories.excludeAction') : t('repositories.includeAction'),
              onClick: () => toggleInclusion(row.repository)
            }
          ]}
        />
      )
    },
    {
      key: 'name',
      label: t('repositories.colRepository'),
      sortKey: 'name',
      renderFilter: () => textFilter('q', t('repositories.colRepository')),
      render: (row) => (
        <div className="repo-cell">
          <span className="repo-name">{row.repository?.name}</span>
          <span className="repo-path mono">{row.repository?.path}</span>
        </div>
      )
    },
    {
      key: 'languages',
      label: t('repositories.colLanguages'),
      sortKey: 'language',
      className: 'cell-nowrap',
      renderFilter: () => selectFilter('language', LANG_OPTIONS),
      render: (row) => shortLanguages(row.repository?.languages)
    },
    {
      key: 'version',
      label: t('repositories.colVersion'),
      sortKey: 'version',
      className: 'mono cell-nowrap',
      renderFilter: () => textFilter('version', t('repositories.colVersion')),
      render: (row) => row.repository?.currentVersion || '—'
    },
    {
      key: 'frameworks',
      label: t('repositories.colFrameworks'),
      sortKey: 'frameworks',
      className: 'mono',
      renderFilter: () => textFilter('frameworks', t('repositories.colFrameworks')),
      render: (row) => {
        const fw = row.targetFrameworks || [];
        if (fw.length === 0) return '—';
        const text = fw.join(', ');
        return (
          <span className="cell-truncate" title={text}>
            {text}
          </span>
        );
      }
    },
    {
      key: 'branch',
      label: t('repositories.colBranch'),
      sortKey: 'branch',
      className: 'mono cell-nowrap',
      renderFilter: () => textFilter('branch', t('repositories.colBranch')),
      render: (row) => row.repository?.currentBranch || '—'
    },
    {
      key: 'aheadBehind',
      label: t('repositories.colAheadBehind'),
      sortKey: 'divergence',
      className: 'cell-center cell-nowrap',
      renderFilter: () => selectFilter('commits', COMMITS_OPTIONS),
      render: (row) => {
        const repo = row.repository;
        if (!repo?.isGitRepository || !repo?.currentBranch) return '—';
        const ahead = repo.commitsAhead || 0;
        const behind = repo.commitsBehind || 0;
        const title = t('repositories.aheadBehindTitle', { ahead, behind, base: repo.baseBranch || 'main' });
        return (
          <span className="ahead-behind" title={title}>
            <span className={ahead > 0 ? 'ab-ahead active' : 'ab-ahead'}>↑{ahead}</span>
            <span className={behind > 0 ? 'ab-behind active' : 'ab-behind'}>↓{behind}</span>
          </span>
        );
      }
    },
    {
      key: 'lastUpdate',
      label: t('repositories.colLastUpdate'),
      sortKey: 'lastUpdate',
      className: 'cell-nowrap',
      renderFilter: () => selectFilter('updated', UPDATED_OPTIONS),
      render: (row) => (
        <span title={formatDateTime(row.repository?.lastUpdateUtc)}>{formatRelativeTime(row.repository?.lastUpdateUtc)}</span>
      )
    },
    ...signalColumns,
    {
      key: 'overall',
      label: t('repositories.colOverall'),
      sortKey: 'overall',
      className: 'cell-center',
      renderFilter: () => selectFilter('overall', RYG),
      render: (row) => <StatusBadge status={row.repository?.overallHealth} />
    }
  ];

  // The repository name and the actions menu are always shown; everything else is toggleable.
  const PINNED_COLUMNS = new Set(['name', 'actions']);
  const toggleableColumns = columns.filter((c) => !PINNED_COLUMNS.has(c.key)).map((c) => ({ key: c.key, label: c.label }));
  const visibleColumns = columns.filter((c) => PINNED_COLUMNS.has(c.key) || !hiddenColumns.has(c.key));

  const emptyMessage = hasActiveFilters ? t('repositories.noMatches') : t('repositories.empty');

  return (
    <div className="view repositories-view">
      <PageHeader
        title={t('repositories.title')}
        subtitle={t('repositories.subtitle')}
        actions={
          <div className="header-actions-inline">
            <span className="last-scanned-chip">
              {t('common.lastScanned')}: {isScanning ? t('common.scanning') : formatRelativeTime(lastScannedUtc)}
            </span>
            {nextScanUtc && !isScanning && (
              <span className="last-scanned-chip">
                {t('common.nextScan')}: {formatRelativeTime(nextScanUtc)}
              </span>
            )}
            <button type="button" className="button-secondary" onClick={() => setShowPicker(true)}>
              {t('picker.launch')}
            </button>
            <button type="button" className="button-secondary" onClick={() => setShowAddMultiple(true)}>
              {t('addMultiple.launch')}
            </button>
            <button type="button" className="button-primary" onClick={onScanNow} disabled={isScanning}>
              {isScanning ? t('common.scanning') : t('common.scanNow')}
            </button>
          </div>
        }
      />

      <div className="repo-toolbar">
        <label className="filter-checkbox">
          <input type="checkbox" checked={includeExcluded} onChange={(e) => setIncludeExcluded(e.target.checked)} />
          <span>{t('repositories.includeExcluded')}</span>
        </label>
        {hasActiveFilters && (
          <button type="button" className="button-secondary tiny" onClick={clearFilters}>
            {t('common.clear')}
          </button>
        )}
        <div className="repo-toolbar-spacer" />
        <ColumnPicker columns={toggleableColumns} hidden={hiddenColumns} onToggle={toggleColumn} onReset={resetColumns} />
      </div>

      <Pagination
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={data.totalCount}
        onPageChange={setPageNumber}
        onPageSizeChange={handlePageSizeChange}
        onRefresh={load}
        loading={loading}
      />

      <DataTable
        columns={visibleColumns}
        rows={data.items}
        rowKey={(row) => row.repository?.id}
        onRowClick={(row) => setSelectedId(row.repository?.id)}
        loading={loading}
        error={error}
        emptyMessage={emptyMessage}
        sort={sort}
        dir={dir}
        onSortChange={handleSortChange}
      />

      {selectedId && (
        <RepositoryDetailModal
          apiClient={apiClient}
          repositoryId={selectedId}
          onClose={() => setSelectedId(null)}
          onRepositoryUpdated={load}
        />
      )}

      {showPicker && <DirectoryPicker apiClient={apiClient} onClose={() => setShowPicker(false)} onChanged={load} />}

      {showAddMultiple && (
        <AddMultipleModal apiClient={apiClient} onClose={() => setShowAddMultiple(false)} onChanged={load} />
      )}

      {launch && (
        <LaunchToolModal apiClient={apiClient} repository={launch.repository} tool={launch.tool} onClose={() => setLaunch(null)} />
      )}
    </div>
  );
}

function shortLanguages(languages) {
  if (!languages || languages.length === 0) return '—';
  return languages.map((l) => LANG_SHORT[l] || l).join(', ');
}

export default RepositoriesView;
