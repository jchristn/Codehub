import { useState, useRef, useEffect } from 'react';
import { createPortal } from 'react-dom';
import { useTranslation } from 'react-i18next';

/**
 * Row action menu that portals to the document body so it is never clipped by
 * table overflow containers.
 *
 * items: [{ label, onClick, tone?, disabled? }]
 */
function ActionMenu({ items }) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [coords, setCoords] = useState({ top: 0, left: 0 });
  const triggerRef = useRef(null);
  const menuRef = useRef(null);

  useEffect(() => {
    if (!open) return undefined;
    const handleClick = (e) => {
      if (menuRef.current?.contains(e.target) || triggerRef.current?.contains(e.target)) return;
      setOpen(false);
    };
    const handleKey = (e) => {
      if (e.key === 'Escape') setOpen(false);
    };
    document.addEventListener('mousedown', handleClick);
    document.addEventListener('keydown', handleKey);
    window.addEventListener('scroll', () => setOpen(false), true);
    return () => {
      document.removeEventListener('mousedown', handleClick);
      document.removeEventListener('keydown', handleKey);
    };
  }, [open]);

  const toggle = (e) => {
    e.stopPropagation();
    if (!open && triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      const menuWidth = 200;
      setCoords({
        top: rect.bottom + 4,
        left: Math.max(8, Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 8))
      });
    }
    setOpen((prev) => !prev);
  };

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        className="icon-button action-trigger"
        onClick={toggle}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t('common.actions')}
        title={t('common.actions')}
      >
        ⋮
      </button>
      {open &&
        createPortal(
          <div ref={menuRef} className="action-menu" role="menu" style={{ top: coords.top, left: coords.left }}>
            {items.map((item, i) =>
              item.header ? (
                <div key={i} className="action-menu-header" role="presentation">
                  {item.label}
                </div>
              ) : (
                <button
                  key={i}
                  type="button"
                  role="menuitem"
                  className={`action-menu-item ${item.tone === 'danger' ? 'danger' : ''}`}
                  disabled={item.disabled}
                  onClick={(e) => {
                    e.stopPropagation();
                    setOpen(false);
                    item.onClick();
                  }}
                >
                  {item.label}
                </button>
              )
            )}
          </div>,
          document.body
        )}
    </>
  );
}

export default ActionMenu;
