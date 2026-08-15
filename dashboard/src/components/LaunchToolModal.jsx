import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { useToast } from '../context/ToastContext';

const FLAGS = {
  claude: '--dangerously-skip-permissions',
  codex: '--yolo'
};

const LABELS = {
  claude: 'Claude',
  codex: 'Codex'
};

/**
 * Confirmation modal for launching Claude or Codex in a repository. Asks whether
 * to pass the tool's dangerous flag before launching on the server host.
 */
function LaunchToolModal({ apiClient, repository, tool, onClose }) {
  const { t } = useTranslation();
  const toast = useToast();
  const [dangerous, setDangerous] = useState(false);
  const [busy, setBusy] = useState(false);

  const flag = FLAGS[tool];
  const label = LABELS[tool];

  const launch = async () => {
    setBusy(true);
    try {
      await apiClient.openRepository(repository.id, tool, dangerous);
      toast.success(t('launch.started', { tool: label }));
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
      title={t('launch.title', { tool: label })}
      subtitle={repository?.name}
      size="sm"
      footer={
        <div className="launch-footer">
          <button type="button" className="button-secondary" onClick={onClose} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button type="button" className={dangerous ? 'button-danger' : 'button-primary'} onClick={launch} disabled={busy}>
            {t('launch.open', { tool: label })}
          </button>
        </div>
      }
    >
      <div className="launch-body">
        <p className="mono launch-path">{repository?.path}</p>
        <label className="launch-flag">
          <input type="checkbox" checked={dangerous} onChange={(e) => setDangerous(e.target.checked)} />
          <span>
            {t('launch.useFlag')} <code>{flag}</code>
          </span>
        </label>
        {dangerous && <p className="launch-warning">{t('launch.warning')}</p>}
      </div>
    </Modal>
  );
}

export default LaunchToolModal;
