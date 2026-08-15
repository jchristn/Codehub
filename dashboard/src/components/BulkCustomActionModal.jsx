import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { useToast } from '../context/ToastContext';
import { AGENTS, agentDangerousFlag } from '../utils/constants';

/**
 * Apply a custom action to several repositories at once. The agent, dangerous flag,
 * and prompt are pre-filled from the chosen action but editable; on run, each
 * repository launches the agent in its own terminal window concurrently.
 */
function BulkCustomActionModal({ apiClient, repositories, customActions, onClose, onDone }) {
  const { t } = useTranslation();
  const toast = useToast();

  const [actionId, setActionId] = useState(customActions[0]?.id || '');
  const [agent, setAgent] = useState(customActions[0]?.agent || 'claude');
  const [dangerous, setDangerous] = useState(customActions[0]?.dangerous || false);
  const [prompt, setPrompt] = useState(customActions[0]?.prompt || '');
  const [busy, setBusy] = useState(false);

  const flag = agentDangerousFlag(agent);

  const pickAction = (id) => {
    setActionId(id);
    const a = customActions.find((x) => x.id === id);
    if (a) {
      setAgent(a.agent);
      setDangerous(a.dangerous);
      setPrompt(a.prompt);
    }
  };

  const run = async () => {
    setBusy(true);
    const results = await Promise.allSettled(
      repositories.map((repo) => apiClient.runAgent(repo.id, { agent, dangerous: flag ? dangerous : false, prompt }))
    );
    const ok = results.filter((r) => r.status === 'fulfilled').length;
    const failed = results.length - ok;
    if (failed === 0) toast.success(t('bulkAction.launched', { count: ok }));
    else toast.warning(t('bulkAction.partial', { ok, failed }));
    if (onDone) onDone();
    onClose();
  };

  return (
    <Modal
      open
      onClose={onClose}
      title={t('bulkAction.title', { count: repositories.length })}
      size="md"
      className="ca-modal-lg"
      footer={
        <>
          <button type="button" className="button-secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button
            type="button"
            className={dangerous && flag ? 'button-danger' : 'button-primary'}
            onClick={run}
            disabled={busy || repositories.length === 0}
          >
            {busy ? t('common.loading') : t('bulkAction.run', { count: repositories.length })}
          </button>
        </>
      }
    >
      <div className="ca-form">
        {customActions.length > 0 && (
          <label className="ca-field">
            <span className="ca-label">{t('bulkAction.action')}</span>
            <select value={actionId} onChange={(e) => pickAction(e.target.value)}>
              {customActions.map((a) => (
                <option key={a.id} value={a.id}>{a.name}</option>
              ))}
            </select>
          </label>
        )}

        <label className="ca-field">
          <span className="ca-label">{t('customActions.agent')}</span>
          <select value={agent} onChange={(e) => setAgent(e.target.value)}>
            {AGENTS.map((a) => (
              <option key={a.value} value={a.value}>{a.label}</option>
            ))}
          </select>
        </label>

        {flag && (
          <label className="ca-flag">
            <input type="checkbox" checked={dangerous} onChange={(e) => setDangerous(e.target.checked)} />
            <span>
              {t('customActions.dangerous')} <code>{flag}</code>
            </span>
          </label>
        )}

        <label className="ca-field">
          <span className="ca-label">{t('customActions.prompt')}</span>
          <textarea
            className="ca-prompt mono"
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder={t('customActions.promptPlaceholder')}
            rows={5}
          />
        </label>

        <div className="ca-field">
          <span className="ca-label">{t('bulkAction.targets', { count: repositories.length })}</span>
          <ul className="bulk-targets">
            {repositories.map((r) => (
              <li key={r.id}>{r.name}</li>
            ))}
          </ul>
        </div>
      </div>
    </Modal>
  );
}

export default BulkCustomActionModal;
