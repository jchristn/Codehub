import { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';

/**
 * A "Columns" button with a popover of checkboxes to toggle which table columns
 * are shown. `columns` is [{ key, label }]; `hidden` is a Set of hidden keys.
 */
function ColumnPicker({ columns, hidden, onToggle, onReset }) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const ref = useRef(null);

  useEffect(() => {
    if (!open) return undefined;
    const onDocClick = (e) => {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    };
    const onKey = (e) => {
      if (e.key === 'Escape') setOpen(false);
    };
    document.addEventListener('mousedown', onDocClick);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDocClick);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const visibleCount = columns.filter((c) => !hidden.has(c.key)).length;

  return (
    <div className="column-picker" ref={ref}>
      <button
        type="button"
        className="button-secondary tiny"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="true"
        aria-expanded={open}
      >
        {t('repositories.columns')} ({visibleCount}/{columns.length})
      </button>
      {open && (
        <div className="column-picker-menu" role="menu">
          <div className="column-picker-header">
            <span>{t('repositories.columnsShow')}</span>
            <button type="button" className="link-button" onClick={onReset}>
              {t('repositories.columnsReset')}
            </button>
          </div>
          {columns.map((col) => (
            <label key={col.key} className="column-picker-item">
              <input
                type="checkbox"
                checked={!hidden.has(col.key)}
                onChange={() => onToggle(col.key)}
              />
              <span>{col.label}</span>
            </label>
          ))}
        </div>
      )}
    </div>
  );
}

export default ColumnPicker;
