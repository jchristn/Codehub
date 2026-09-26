import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';

/**
 * Create/edit a custom action: a name and a default prompt. Actions are agent-agnostic; the
 * agent is chosen when the action is run.
 */
function CustomActionModal({ action, onSave, onClose, busy }) {
  const { t } = useTranslation();
  const [name, setName] = useState(action?.name || '');
  const [prompt, setPrompt] = useState(action?.prompt || '');

  const canSave = name.trim().length > 0 && !busy;

  const submit = () => {
    if (!canSave) return;
    onSave({ name: name.trim(), prompt });
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
      </div>
    </Modal>
  );
}

export default CustomActionModal;
