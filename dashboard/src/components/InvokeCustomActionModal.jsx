import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import AgentPicker from './AgentPicker';
import { useToast } from '../context/ToastContext';
import { agentLabel, agentDangerousFlag, getLastAgentRun, setLastAgentRun } from '../utils/constants';

/**
 * Invoke a custom action on a repository. Actions are agent-agnostic, so the agent (and its
 * dangerous flag) is chosen here, defaulting to the last choice made; the prompt is pre-filled
 * from the action but editable. On confirm a terminal opens and the agent runs with the prompt.
 */
function InvokeCustomActionModal({ apiClient, repository, action, onClose }) {
  const { t } = useTranslation();
  const toast = useToast();
  const [agent, setAgent] = useState(() => getLastAgentRun().agent);
  const [dangerous, setDangerous] = useState(() => getLastAgentRun().dangerous);
  const [prompt, setPrompt] = useState(action.prompt || '');
  const [busy, setBusy] = useState(false);

  const flag = agentDangerousFlag(agent);

  const run = async () => {
    setBusy(true);
    setLastAgentRun(agent, dangerous);
    try {
      const res = await apiClient.runCustomAction(action.id, {
        repositoryIds: [repository.id],
        agent,
        dangerous: flag ? dangerous : false,
        prompt
      });
      if (res && res.failed > 0) {
        toast.error(res.results?.[0]?.error || t('launch.failed'));
        setBusy(false);
        return;
      }
      toast.success(t('customActions.launched', { name: action.name, agent: agentLabel(agent) }));
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
      title={t('customActions.runTitle', { name: action.name })}
      subtitle={repository?.name}
      size="md"
      className="ca-modal-lg"
      footer={
        <>
          <button type="button" className="button-secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button type="button" className={dangerous && flag ? 'button-danger' : 'button-primary'} onClick={run} disabled={busy}>
            {busy ? t('common.loading') : t('customActions.run')}
          </button>
        </>
      }
    >
      <div className="ca-form">
        <p className="mono launch-path">{repository?.path}</p>

        <AgentPicker agent={agent} dangerous={dangerous} onAgentChange={setAgent} onDangerousChange={setDangerous} />

        <label className="ca-field ca-field-grow">
          <span className="ca-label">{t('customActions.prompt')}</span>
          <textarea
            className="ca-prompt mono"
            value={prompt}
            onChange={(e) => setPrompt(e.target.value)}
            placeholder={t('customActions.promptPlaceholder')}
            rows={6}
          />
        </label>
      </div>
    </Modal>
  );
}

export default InvokeCustomActionModal;
