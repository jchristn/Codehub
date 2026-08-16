import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { useToast } from '../context/ToastContext';

const OVERRIDABLE = [
  { column: 'TestInfra', labelKey: 'signals.testInfra' },
  { column: 'Telemetry', labelKey: 'signals.telemetry' },
  { column: 'OutdatedDependencies', labelKey: 'signals.outdatedDeps' },
  { column: 'Vulnerabilities', labelKey: 'signals.vulnerabilities' },
  { column: 'IssuesAndPullRequests', labelKey: 'signals.issuesAndPullRequests' },
  { column: 'Overall', labelKey: 'repositories.colOverall' }
];

const STATUSES = ['Green', 'Yellow', 'Red', 'NotApplicable'];

/**
 * Override a repository's signal/overall values with a note. An override value is shown
 * (and filtered/sorted on) instead of the computed one; leaving a column as "No override"
 * removes any override for it.
 */
function AnnotationsModal({ apiClient, repository, onClose, onDone }) {
  const { t } = useTranslation();
  const toast = useToast();
  const [state, setState] = useState(null); // { [column]: { status: '', note: '' } }
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    apiClient
      .getAnnotations(repository.id)
      .then((res) => {
        const map = {};
        OVERRIDABLE.forEach((o) => {
          map[o.column] = { status: '', note: '' };
        });
        (res || []).forEach((a) => {
          map[a.column] = { status: a.status || '', note: a.note || '' };
        });
        setState(map);
      })
      .catch(() => {
        const map = {};
        OVERRIDABLE.forEach((o) => {
          map[o.column] = { status: '', note: '' };
        });
        setState(map);
      });
  }, [apiClient, repository.id]);

  const set = (column, key, value) =>
    setState((prev) => ({ ...prev, [column]: { ...prev[column], [key]: value } }));

  const save = async () => {
    setBusy(true);
    try {
      const existing = await apiClient.getAnnotations(repository.id);
      const existingCols = new Set((existing || []).map((a) => a.column));
      const ops = [];
      OVERRIDABLE.forEach((o) => {
        const v = state[o.column];
        if (v.status) {
          ops.push(apiClient.setAnnotation(repository.id, { column: o.column, status: v.status, note: v.note }));
        } else if (existingCols.has(o.column)) {
          ops.push(apiClient.deleteAnnotation(repository.id, o.column));
        }
      });
      await Promise.all(ops);
      toast.success(t('annotations.saved'));
      if (onDone) onDone();
      onClose();
    } catch (e) {
      toast.error(e?.body || t('common.error'));
      setBusy(false);
    }
  };

  return (
    <Modal
      open
      onClose={onClose}
      title={t('annotations.title')}
      subtitle={repository?.name}
      size="lg"
      footer={
        <>
          <button type="button" className="button-secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button type="button" className="button-primary" onClick={save} disabled={busy || !state}>
            {busy ? t('common.loading') : t('common.save')}
          </button>
        </>
      }
    >
      <p className="settings-note">{t('annotations.help')}</p>
      {!state ? (
        <div className="table-state"><div className="loading-spinner" /><span>{t('common.loading')}</span></div>
      ) : (
        <table className="data-table annotations-table">
          <thead>
            <tr>
              <th>{t('annotations.colColumn')}</th>
              <th>{t('annotations.colOverride')}</th>
              <th>{t('annotations.colNote')}</th>
            </tr>
          </thead>
          <tbody>
            {OVERRIDABLE.map((o) => (
              <tr key={o.column}>
                <td>{t(o.labelKey)}</td>
                <td>
                  <select value={state[o.column].status} onChange={(e) => set(o.column, 'status', e.target.value)}>
                    <option value="">{t('annotations.noOverride')}</option>
                    {STATUSES.map((s) => (
                      <option key={s} value={s}>{t(`health.${s.toLowerCase()}`, s)}</option>
                    ))}
                  </select>
                </td>
                <td>
                  <input
                    type="text"
                    className="annotation-note"
                    value={state[o.column].note}
                    placeholder={t('annotations.notePlaceholder')}
                    disabled={!state[o.column].status}
                    onChange={(e) => set(o.column, 'note', e.target.value)}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </Modal>
  );
}

export default AnnotationsModal;
