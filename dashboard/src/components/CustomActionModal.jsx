import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { AGENTS, agentDangerousFlag } from '../utils/constants';

/**
 * Create/edit a custom action: name, agent, optional dangerous flag, and default prompt.
 */
function CustomActionModal({ action, onSave, onClose, busy }) {
  const { t } = useTranslation();
  const [name, setName] = useState(action?.name || '');
  const [agent, setAgent] = useState(action?.agent || 'claude');
  const [dangerous, setDangerous] = useState(action?.dangerous || false);
  const [prompt, setPrompt] = useState(action?.prompt || '');

  const flag = agentDangerousFlag(agent);
  const canSave = name.trim().length > 0 && !busy;

  const submit = () => {
    if (!canSave) return;
    onSave({ name: name.trim(), agent, dangerous: flag ? dangerous : false, prompt });
  };

  return (
    <Modal
      open
      onClose={onClose}
      title={action ? t('customActions.editTitle') : t('customActions.newTitle')}
      size="md"
      className="ca-modal-lg"
      footer={
        <>
          <button type="button" className="button-secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button type="button" className="button-primary" onClick={submit} disabled={!canSave}>
            {busy ? t('common.loading') : t('common.save')}
          </button>
        </>
      }
    >
      <div className="ca-form">
        <label className="ca-field">
          <span className="ca-label">{t('customActions.name')}</span>
          <input type="text" value={name} onChange={(e) => setName(e.target.value)} placeholder={t('customActions.namePlaceholder')} autoFocus />
        </label>

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
      </div>
    </Modal>
  );
}

export default CustomActionModal;
