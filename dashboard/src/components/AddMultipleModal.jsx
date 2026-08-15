import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { useToast } from '../context/ToastContext';

/**
 * Paste-a-list modal for including many directories at once. Lines are sent as-is;
 * the server ignores empty lines, duplicates, non-existent directories, and paths
 * outside the configured roots, and reports how many were added versus ignored.
 */
function AddMultipleModal({ apiClient, onClose, onChanged }) {
  const { t } = useTranslation();
  const toast = useToast();
  const [text, setText] = useState('');
  const [busy, setBusy] = useState(false);

  const lineCount = text.split('\n').filter((l) => l.trim().length > 0).length;

  const submit = async () => {
    const paths = text.split(/\r?\n/);
    setBusy(true);
    try {
      const result = (await apiClient.addSelections(paths)) || { added: 0, ignored: 0 };
      toast.success(t('addMultiple.result', { added: result.added || 0, ignored: result.ignored || 0 }));
      if ((result.added || 0) > 0 && onChanged) onChanged();
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
      title={t('addMultiple.title')}
      subtitle={t('addMultiple.subtitle')}
      size="lg"
      footer={
        <div className="picker-footer">
          <span className="picker-hint">{t('addMultiple.count', { count: lineCount })}</span>
          <div className="picker-actions">
            <button type="button" className="button-secondary" onClick={onClose} disabled={busy}>
              {t('common.cancel')}
            </button>
            <button type="button" className="button-primary" onClick={submit} disabled={busy || lineCount === 0}>
              {busy ? t('common.loading') : t('addMultiple.add')}
            </button>
          </div>
        </div>
      }
    >
      <div className="add-multiple-body">
        <textarea
          className="add-multiple-textarea"
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder={t('addMultiple.placeholder')}
          spellCheck={false}
          autoFocus
        />
      </div>
    </Modal>
  );
}

export default AddMultipleModal;
