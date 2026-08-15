import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useToast } from '../context/ToastContext';
import PageHeader from '../components/PageHeader';
import DataTable from '../components/DataTable';
import ActionMenu from '../components/ActionMenu';
import CustomActionModal from '../components/CustomActionModal';
import ConfirmModal from '../components/ConfirmModal';
import { agentLabel } from '../utils/constants';

/**
 * Manage custom actions (CRUD). Defined actions appear in the repository actions menu
 * under a "Custom Actions" section.
 */
function CustomActionsView({ apiClient }) {
  const { t } = useTranslation();
  const toast = useToast();

  const [actions, setActions] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [editing, setEditing] = useState(null); // action | {} (new) | null
  const [saving, setSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);

  const load = useCallback(() => {
    setLoading(true);
    apiClient
      .getCustomActions()
      .then((res) => setActions(res || []))
      .catch(() => setError(t('common.error')))
      .finally(() => setLoading(false));
  }, [apiClient, t]);

  useEffect(() => {
    load();
  }, [load]);

  const save = async (payload) => {
    setSaving(true);
    try {
      if (editing && editing.id) await apiClient.updateCustomAction(editing.id, payload);
      else await apiClient.createCustomAction(payload);
      toast.success(t('customActions.saved'));
      setEditing(null);
      load();
    } catch (e) {
      toast.error(e?.body || t('common.error'));
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = async () => {
    try {
      await apiClient.deleteCustomAction(deleteTarget.id);
      toast.success(t('customActions.deleted', { name: deleteTarget.name }));
      setDeleteTarget(null);
      load();
    } catch (e) {
      toast.error(e?.body || t('common.error'));
    }
  };

  const columns = [
    { key: 'name', label: t('customActions.name'), render: (row) => <span className="repo-name">{row.name}</span> },
    { key: 'agent', label: t('customActions.agent'), render: (row) => agentLabel(row.agent) },
    {
      key: 'dangerous',
      label: t('customActions.dangerous'),
      className: 'cell-center',
      render: (row) => (row.dangerous ? t('common.yes') : t('common.no'))
    },
    {
      key: 'prompt',
      label: t('customActions.prompt'),
      render: (row) => <span className="ca-prompt-preview" title={row.prompt}>{row.prompt || '—'}</span>
    },
    {
      key: 'actions',
      label: t('common.actions'),
      className: 'cell-actions',
      render: (row) => (
        <ActionMenu
          items={[
            { label: t('common.edit'), onClick: () => setEditing(row) },
            { label: t('common.delete'), tone: 'danger', onClick: () => setDeleteTarget(row) }
          ]}
        />
      )
    }
  ];

  return (
    <div className="view custom-actions-view">
      <PageHeader
        title={t('customActions.title')}
        subtitle={t('customActions.subtitle')}
        actions={
          <button type="button" className="button-primary" onClick={() => setEditing({})}>
            {t('customActions.new')}
          </button>
        }
      />

      <DataTable
        columns={columns}
        rows={actions}
        rowKey={(row) => row.id}
        loading={loading}
        error={error}
        emptyMessage={t('customActions.empty')}
      />

      {editing && (
        <CustomActionModal
          action={editing.id ? editing : null}
          onSave={save}
          onClose={() => setEditing(null)}
          busy={saving}
        />
      )}

      <ConfirmModal
        open={Boolean(deleteTarget)}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={confirmDelete}
        title={t('customActions.deleteTitle')}
        confirmLabel={t('common.delete')}
        body={t('customActions.deleteBody', { name: deleteTarget?.name })}
      />
    </div>
  );
}

export default CustomActionsView;
