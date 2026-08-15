import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useToast } from '../context/ToastContext';
import PageHeader from '../components/PageHeader';
import DataTable from '../components/DataTable';
import Pagination from '../components/Pagination';
import FilterBar from '../components/FilterBar';
import KpiTile from '../components/KpiTile';
import ActionMenu from '../components/ActionMenu';
import ActivityChart, { ChartRangeSelector } from '../components/ActivityChart';
import RequestDetailsModal from '../components/RequestDetailsModal';
import ConfirmModal from '../components/ConfirmModal';
import useDebounce from '../hooks/useDebounce';
import { ApiError } from '../utils/api';
import { HTTP_METHODS, STORAGE, DEFAULT_PAGE_SIZE, rangeToParams } from '../utils/constants';
import { formatDateTime, formatDuration } from '../i18n/formatters';

/**
 * Request History investigation workspace: KPI strip + activity chart +
 * backend filters + paginated table + request inspector modal.
 */
function RequestHistoryView({ apiClient }) {
  const { t } = useTranslation();
  const toast = useToast();

  const [rangeId, setRangeId] = useState('day');
  const [summary, setSummary] = useState(null);
  const [chartLoading, setChartLoading] = useState(false);

  const [filters, setFilters] = useState({ method: '', statusCode: '', pathContains: '', fromUtc: '', toUtc: '' });
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(() => Number(localStorage.getItem(STORAGE.requestPageSize)) || DEFAULT_PAGE_SIZE);
  const [data, setData] = useState({ items: [], totalCount: 0 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [disabled, setDisabled] = useState(false);
  const [selectedId, setSelectedId] = useState(null);
  const [deleteId, setDeleteId] = useState(null);
  const [deleting, setDeleting] = useState(false);

  const debouncedPath = useDebounce(filters.pathContains, 350);

  const loadChart = useCallback(() => {
    if (!apiClient) return;
    setChartLoading(true);
    const params = rangeToParams(rangeId);
    apiClient
      .getRequestHistorySummary(params)
      .then(setSummary)
      .catch(() => setSummary(null))
      .finally(() => setChartLoading(false));
  }, [apiClient, rangeId]);

  useEffect(() => {
    loadChart();
  }, [loadChart]);

  const loadTable = useCallback(() => {
    if (!apiClient) return;
    setLoading(true);
    setError(null);
    apiClient
      .getRequestHistory({
        method: filters.method,
        statusCode: filters.statusCode,
        pathContains: debouncedPath,
        fromUtc: filters.fromUtc ? new Date(filters.fromUtc).toISOString() : '',
        toUtc: filters.toUtc ? new Date(filters.toUtc).toISOString() : '',
        pageNumber,
        pageSize
      })
      .then((res) => {
        setData(res || { items: [], totalCount: 0 });
        setDisabled(false);
      })
      .catch((err) => {
        if (err instanceof ApiError && (err.status === 404 || err.status === 501)) {
          setDisabled(true);
        } else {
          setError(t('common.error'));
        }
      })
      .finally(() => setLoading(false));
  }, [apiClient, filters.method, filters.statusCode, filters.fromUtc, filters.toUtc, debouncedPath, pageNumber, pageSize, t]);

  useEffect(() => {
    loadTable();
  }, [loadTable]);

  useEffect(() => {
    setPageNumber(1);
  }, [filters.method, filters.statusCode, debouncedPath, filters.fromUtc, filters.toUtc]);

  const updateFilter = (name, value) => setFilters((prev) => ({ ...prev, [name]: value }));
  const clearFilters = () => setFilters({ method: '', statusCode: '', pathContains: '', fromUtc: '', toUtc: '' });

  const handlePageSizeChange = (size) => {
    setPageSize(size);
    localStorage.setItem(STORAGE.requestPageSize, String(size));
    setPageNumber(1);
  };

  const handleBucketClick = (bucket) => {
    setFilters((prev) => ({
      ...prev,
      fromUtc: toLocalInput(bucket.bucketStartUtc),
      toUtc: toLocalInput(bucket.bucketEndUtc)
    }));
  };

  const confirmDelete = useCallback(async () => {
    if (!deleteId) return;
    setDeleting(true);
    try {
      await apiClient.deleteRequestHistoryEntry(deleteId);
      toast.success(t('requests.deleted'));
      setDeleteId(null);
      loadTable();
    } catch {
      toast.error(t('common.error'));
    } finally {
      setDeleting(false);
    }
  }, [apiClient, deleteId, toast, t, loadTable]);

  const filterFields = [
    { name: 'method', type: 'select', label: t('requests.filterMethod'), options: HTTP_METHODS.map((m) => ({ value: m, label: m })) },
    { name: 'statusCode', type: 'text', label: t('requests.filterStatus'), placeholder: '404' },
    { name: 'pathContains', type: 'text', label: t('requests.filterPath'), placeholder: '/v1.0/api' },
    { name: 'fromUtc', type: 'datetime', label: t('requests.filterFrom') },
    { name: 'toUtc', type: 'datetime', label: t('requests.filterTo') }
  ];

  const columns = [
    { key: 'time', label: t('requests.colTime'), className: 'cell-nowrap', render: (r) => formatDateTime(r.createdUtc) },
    { key: 'method', label: t('requests.colMethod'), render: (r) => <span className={`method-badge method-${(r.method || '').toLowerCase()}`}>{r.method}</span> },
    { key: 'path', label: t('requests.colPath'), className: 'mono cell-path', render: (r) => <span title={r.path}>{r.path}</span> },
    { key: 'status', label: t('requests.colStatus'), className: 'cell-center', render: (r) => <span className={`status-pill ${r.statusCode >= 400 ? 'tone-danger' : 'tone-success'}`}>{r.statusCode}</span> },
    { key: 'duration', label: t('requests.colDuration'), className: 'cell-right', render: (r) => formatDuration(r.durationMs) },
    {
      key: 'actions',
      label: t('common.actions'),
      className: 'cell-actions',
      render: (r) => (
        <ActionMenu
          items={[
            { label: t('common.viewDetails'), onClick: () => setSelectedId(r.id) },
            { label: t('common.delete'), tone: 'danger', onClick: () => setDeleteId(r.id) }
          ]}
        />
      )
    }
  ];

  const kpis = summary
    ? [
        { label: t('requests.kpiTotal'), value: summary.totalCount ?? 0, tone: 'neutral' },
        { label: t('requests.kpiSuccess'), value: summary.totalSuccess ?? 0, tone: 'success' },
        { label: t('requests.kpiFailure'), value: summary.totalFailure ?? 0, tone: 'danger' },
        { label: t('requests.kpiAvg'), value: formatDuration(summary.averageDurationMs ?? 0), tone: 'info' }
      ]
    : [];

  if (disabled) {
    return (
      <div className="view request-history-view">
        <PageHeader title={t('requests.title')} subtitle={t('requests.subtitle')} />
        <div className="banner banner-muted">{t('requests.disabled')}</div>
      </div>
    );
  }

  return (
    <div className="view request-history-view">
      <PageHeader title={t('requests.title')} subtitle={t('requests.subtitle')} />

      <section className="kpi-grid kpi-grid-compact">
        {kpis.map((kpi) => (
          <KpiTile key={kpi.label} {...kpi} />
        ))}
      </section>

      <section className="panel">
        <div className="panel-header">
          <h2 className="panel-title">{t('requests.title')}</h2>
          <ChartRangeSelector value={rangeId} onChange={setRangeId} onRefresh={loadChart} loading={chartLoading} />
        </div>
        <ActivityChart summary={summary} rangeId={rangeId} onBucketClick={handleBucketClick} height={330} />
      </section>

      <FilterBar fields={filterFields} values={filters} onChange={updateFilter} onClear={clearFilters} />

      <Pagination
        pageNumber={pageNumber}
        pageSize={pageSize}
        totalCount={data.totalCount}
        onPageChange={setPageNumber}
        onPageSizeChange={handlePageSizeChange}
        onRefresh={loadTable}
        loading={loading}
      />

      <DataTable
        columns={columns}
        rows={data.items}
        loading={loading}
        error={error}
        emptyMessage={data.totalCount === 0 && !anyFilter(filters) ? t('requests.empty') : t('requests.noMatches')}
        onRowClick={(r) => setSelectedId(r.id)}
      />

      {selectedId && <RequestDetailsModal apiClient={apiClient} entryId={selectedId} onClose={() => setSelectedId(null)} />}

      <ConfirmModal
        open={Boolean(deleteId)}
        onConfirm={confirmDelete}
        onCancel={() => setDeleteId(null)}
        title={t('requests.deleteConfirmTitle')}
        body={t('requests.deleteConfirmBody')}
        confirmLabel={t('common.delete')}
        busy={deleting}
      />
    </div>
  );
}

function anyFilter(filters) {
  return Object.values(filters).some((v) => v);
}

function toLocalInput(iso) {
  const d = new Date(iso);
  const pad = (n) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export default RequestHistoryView;
