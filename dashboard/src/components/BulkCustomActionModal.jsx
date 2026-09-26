import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import AgentPicker from './AgentPicker';
import { useToast } from '../context/ToastContext';
import { agentDangerousFlag, getLastAgentRun, setLastAgentRun } from '../utils/constants';

/**
 * Apply a custom action to several repositories at once. Actions are agent-agnostic, so the
 * agent (and its dangerous flag) is chosen here, defaulting to the last choice made; the prompt
 * is pre-filled from the chosen action but editable. On run, each repository launches the agent
 * in its own terminal window.
 */
function BulkCustomActionModal({ apiClient, repositories, customActions, onClose, onDone }) {
  const { t } = useTranslation();
  const toast = useToast();

  const [actionId, setActionId] = useState(customActions[0]?.id || '');
  const [agent, setAgent] = useState(() => getLastAgentRun().agent);
  const [dangerous, setDangerous] = useState(() => getLastAgentRun().dangerous);
  const [prompt, setPrompt] = useState(customActions[0]?.prompt || '');
  const [busy, setBusy] = useState(false);

  const flag = agentDangerousFlag(agent);

  const pickAction = (id) => {
    setActionId(id);
    const a = customActions.find((x) => x.id === id);
    if (a) setPrompt(a.prompt);
  };

  // With a saved action, one request runs it everywhere; without one (no actions defined yet),
  // launch the ad-hoc prompt per repository.
  const launch = async (options) => {
    if (actionId) {
      const res = await apiClient.runCustomAction(actionId, { ...options, repositoryIds: repositories.map((r) => r.id) });
      return { ok: res?.launched || 0, failed: res?.failed || 0 };
    }
    const results = await Promise.allSettled(repositories.map((repo) => apiClient.runAgent(repo.id, options)));
    const ok = results.filter((r) => r.status === 'fulfilled').length;
    return { ok, failed: results.length - ok };
  };

  const run = async () => {
    setBusy(true);
    setLastAgentRun(agent, dangerous);
    try {
      const { ok, failed } = await launch({ agent, dangerous: flag ? dangerous : false, prompt });
      if (failed === 0) toast.success(t('bulkAction.launched', { count: ok }));
      else toast.warning(t('bulkAction.partial', { ok, failed }));
      if (onDone) onDone();
      onClose();
    } catch (e) {
      toast.error(e?.body || t('launch.failed'));
      setBusy(false);
    }
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

        <AgentPicker agent={agent} dangerous={dangerous} onAgentChange={setAgent} onDangerousChange={setDangerous} />

        <label className="ca-field ca-field-grow">
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
