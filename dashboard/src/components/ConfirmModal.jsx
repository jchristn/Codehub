import { useTranslation } from 'react-i18next';
import Modal from './Modal';

/**
 * Custom confirmation modal for destructive actions. Never use browser
 * confirm/alert. Backdrop + ESC dismiss cancels.
 */
function ConfirmModal({ open, onConfirm, onCancel, title, body, confirmLabel, tone = 'danger', busy = false }) {
  const { t } = useTranslation();
  return (
    <Modal
      open={open}
      onClose={onCancel}
      title={title}
      size="sm"
      footer={
        <>
          <button type="button" className="button-secondary" onClick={onCancel} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button
            type="button"
            className={tone === 'danger' ? 'button-danger' : 'button-primary'}
            onClick={onConfirm}
            disabled={busy}
          >
            {busy ? t('common.loading') : confirmLabel || t('common.confirm')}
          </button>
        </>
      }
    >
      <p className="confirm-body">{body}</p>
    </Modal>
  );
}

export default ConfirmModal;
