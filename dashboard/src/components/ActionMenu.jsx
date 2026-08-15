import { useState, useRef, useEffect, useLayoutEffect } from 'react';
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
  const triggerRectRef = useRef(null);

  // After the menu renders, clamp it inside the viewport: flip above the trigger when
  // there isn't room below (e.g. the bottom row), and keep it within the top/left/right edges.
  useLayoutEffect(() => {
    if (!open || !menuRef.current || !triggerRectRef.current) return;
    const rect = triggerRectRef.current;
    const menu = menuRef.current.getBoundingClientRect();
    const margin = 8;
    const vw = window.innerWidth;
    const vh = window.innerHeight;

    let top = rect.bottom + 4;
    if (top + menu.height > vh - margin) {
      const above = rect.top - 4 - menu.height;
      top = above >= margin ? above : Math.max(margin, vh - menu.height - margin);
    }

    let left = rect.right - menu.width;
    left = Math.max(margin, Math.min(left, vw - menu.width - margin));

    if (Math.round(top) !== Math.round(coords.top) || Math.round(left) !== Math.round(coords.left)) {
      setCoords({ top, left });
    }
  }, [open, coords.top, coords.left]);

  useEffect(() => {
    if (!open) return undefined;
    const handleClick = (e) => {
      if (menuRef.current?.contains(e.target) || triggerRef.current?.contains(e.target)) return;
      setOpen(false);
    };
    const handleKey = (e) => {
      if (e.key === 'Escape') setOpen(false);
    };
    // Close when the page scrolls, but ignore scrolling within the menu itself.
    const handleScroll = (e) => {
      if (menuRef.current?.contains(e.target)) return;
      setOpen(false);
    };
    document.addEventListener('mousedown', handleClick);
    document.addEventListener('keydown', handleKey);
    window.addEventListener('scroll', handleScroll, true);
    return () => {
      document.removeEventListener('mousedown', handleClick);
      document.removeEventListener('keydown', handleKey);
      window.removeEventListener('scroll', handleScroll, true);
    };
  }, [open]);

  const toggle = (e) => {
    e.stopPropagation();
    if (!open && triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      triggerRectRef.current = rect;
      const menuWidth = 200;
      // Preliminary position; the layout effect refines it once the menu's real size is known.
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
