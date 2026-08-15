import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import CopyButton from './CopyButton';
import JsonBlock from './JsonBlock';
import { formatDateTime, formatDuration, formatBytes } from '../i18n/formatters';

/**
 * Request inspector: metadata, request/response headers + bodies, and raw JSON.
 * The list endpoint omits bodies, so the full entry is fetched on open.
 */
function RequestDetailsModal({ apiClient, entryId, onClose }) {
  const { t } = useTranslation();
  const [entry, setEntry] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const load = useCallback(() => {
    setLoading(true);
    setError(null);
    apiClient
      .getRequestHistoryEntry(entryId)
      .then(setEntry)
      .catch(() => setError(t('common.error')))
      .finally(() => setLoading(false));
  }, [apiClient, entryId, t]);

  useEffect(() => {
    load();
  }, [load]);

  const statusClass = entry ? (entry.statusCode >= 400 ? 'tone-danger' : 'tone-success') : '';

  return (
    <Modal
      open
      onClose={onClose}
      size="xxl"
      title={t('requestDetail.title')}
      subtitle={entry ? <span className="mono">{entry.method} {entry.path}</span> : null}
      footer={
        <button type="button" className="button-secondary" onClick={onClose}>
          {t('common.close')}
        </button>
      }
    >
      {loading ? (
        <div className="modal-loading"><div className="loading-spinner" /></div>
      ) : error ? (
        <div className="table-state error">{error}</div>
      ) : entry ? (
        <div className="request-detail">
          <section className="detail-section">
            <h3 className="detail-section-title">{t('requestDetail.metadata')}</h3>
            <div className="kv-grid">
              <div className="kv"><span className="kv-key">ID</span><span className="kv-val mono id-cell">{entry.id}<CopyButton value={entry.id} label="ID" size="sm" /></span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.method')}</span><span className="kv-val">{entry.method}</span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.status')}</span><span className={`kv-val ${statusClass}`}>{entry.statusCode}</span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.duration')}</span><span className="kv-val">{formatDuration(entry.durationMs)}</span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.created')}</span><span className="kv-val">{formatDateTime(entry.createdUtc)}</span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.completed')}</span><span className="kv-val">{formatDateTime(entry.completedUtc)}</span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.sourceIp')}</span><span className="kv-val mono">{entry.sourceIp || '—'}</span></div>
              <div className="kv"><span className="kv-key">{t('requestDetail.path')}</span><span className="kv-val mono">{entry.path}</span></div>
            </div>
          </section>

          <HeaderSection title={t('requestDetail.requestHeaders')} headers={entry.requestHeaders} emptyLabel={t('requestDetail.empty')} />
          <BodySection title={t('requestDetail.requestBody')} body={entry.requestBody} emptyLabel={t('requestDetail.empty')} />
          <HeaderSection title={t('requestDetail.responseHeaders')} headers={entry.responseHeaders} emptyLabel={t('requestDetail.empty')} />
          <BodySection title={t('requestDetail.responseBody')} body={entry.responseBody} emptyLabel={t('requestDetail.empty')} />

          <section className="detail-section">
            <h3 className="detail-section-title">{t('common.rawJson')}</h3>
            <JsonBlock value={entry} label="JSON" />
          </section>
        </div>
      ) : null}
    </Modal>
  );
}

function HeaderSection({ title, headers, emptyLabel }) {
  const entries = headers ? (typeof headers === 'string' ? tryParse(headers) : headers) : null;
  const rows = entries && typeof entries === 'object' ? Object.entries(entries) : [];
  const copyText = rows.map(([k, v]) => `${k}: ${v}`).join('\n');
  return (
    <section className="detail-section">
      <h3 className="detail-section-title">
        {title}
        {rows.length > 0 && <CopyButton value={copyText} label={title} size="sm" />}
      </h3>
      {rows.length === 0 ? (
        <p className="muted">{emptyLabel}</p>
      ) : (
        <table className="data-table compact kv-table">
          <tbody>
            {rows.map(([k, v]) => (
              <tr key={k}>
                <td className="mono kv-table-key">{k}</td>
                <td className="mono">{String(v)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}

function BodySection({ title, body, emptyLabel }) {
  if (!body) {
    return (
      <section className="detail-section">
        <h3 className="detail-section-title">{title}</h3>
        <p className="muted">{emptyLabel}</p>
      </section>
    );
  }
  const parsed = typeof body === 'string' ? tryParse(body) : body;
  const display = parsed && typeof parsed === 'object' ? JSON.stringify(parsed, null, 2) : String(body);
  return (
    <section className="detail-section">
      <h3 className="detail-section-title">{title} <span className="muted small">({formatBytes(new Blob([String(body)]).size)})</span></h3>
      <JsonBlock value={display} label={title} />
    </section>
  );
}

function tryParse(str) {
  try {
    return JSON.parse(str);
  } catch {
    return str;
  }
}

export default RequestDetailsModal;
