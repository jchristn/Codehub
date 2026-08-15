import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { useToast } from '../context/ToastContext';
import { AGENTS, agentLabel, agentDangerousFlag } from '../utils/constants';

/**
 * Invoke a custom action on a repository. The agent, dangerous flag, and prompt are
 * pre-filled from the action but editable before launching; on confirm a terminal opens
 * and the agent runs with the (possibly edited) prompt.
 */
function InvokeCustomActionModal({ apiClient, repository, action, onClose }) {
  const { t } = useTranslation();
  const toast = useToast();
  const [agent, setAgent] = useState(action.agent || 'claude');
  const [dangerous, setDangerous] = useState(action.dangerous || false);
  const [prompt, setPrompt] = useState(action.prompt || '');
  const [busy, setBusy] = useState(false);

  const flag = agentDangerousFlag(agent);

  const run = async () => {
    setBusy(true);
    try {
      await apiClient.runAgent(repository.id, { agent, dangerous: flag ? dangerous : false, prompt });
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
            rows={6}
          />
        </label>
      </div>
    </Modal>
  );
}

export default InvokeCustomActionModal;
