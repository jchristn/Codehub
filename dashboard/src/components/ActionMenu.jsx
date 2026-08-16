import { useState, useRef, useEffect, useLayoutEffect } from 'react';
import { createPortal } from 'react-dom';
import { useTranslation } from 'react-i18next';

/**
 * Row action menu that portals to the document body so it is never clipped by
 * table overflow containers.
 *
 * items: [{ label, onClick, tone?, disabled?, header?, submenu? }]
 * A `submenu` item renders as a single entry that expands a flyout to the side on hover.
 */
function ActionMenu({ items }) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [coords, setCoords] = useState({ top: 0, left: 0 });
  const [openSub, setOpenSub] = useState(null); // index of the open submenu, or null
  const [subCoords, setSubCoords] = useState({ top: 0, left: 0 });
  const triggerRef = useRef(null);
  const menuRef = useRef(null);
  const flyoutRef = useRef(null);
  const triggerRectRef = useRef(null);
  const subTimer = useRef(null);

  const close = () => {
    setOpen(false);
    setOpenSub(null);
  };

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
    const inMenus = (el) =>
      menuRef.current?.contains(el) || triggerRef.current?.contains(el) || flyoutRef.current?.contains(el);
    const handleClick = (e) => {
      if (inMenus(e.target)) return;
      close();
    };
    const handleKey = (e) => {
      if (e.key === 'Escape') close();
    };
    const handleScroll = (e) => {
      if (menuRef.current?.contains(e.target) || flyoutRef.current?.contains(e.target)) return;
      close();
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

  useEffect(() => () => clearTimeout(subTimer.current), []);

  const toggle = (e) => {
    e.stopPropagation();
    if (!open && triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      triggerRectRef.current = rect;
      const menuWidth = 200;
      setCoords({
        top: rect.bottom + 4,
        left: Math.max(8, Math.min(rect.right - menuWidth, window.innerWidth - menuWidth - 8))
      });
    }
    setOpen((prev) => !prev);
    setOpenSub(null);
  };

  const openSubmenu = (index, el, count) => {
    clearTimeout(subTimer.current);
    const rect = el.getBoundingClientRect();
    const width = 200;
    const estHeight = count * 34 + 10;
    const margin = 8;
    let left = rect.right - 2;
    if (left + width > window.innerWidth - margin) left = Math.max(margin, rect.left - width + 2);
    let top = rect.top - 4;
    top = Math.max(margin, Math.min(top, window.innerHeight - estHeight - margin));
    setSubCoords({ top, left });
    setOpenSub(index);
  };
  const scheduleCloseSub = () => {
    clearTimeout(subTimer.current);
    subTimer.current = setTimeout(() => setOpenSub(null), 180);
  };
  const cancelCloseSub = () => clearTimeout(subTimer.current);

  const renderItem = (item, i) => {
    if (item.header) {
      return (
        <div key={i} className="action-menu-header" role="presentation">
          {item.label}
        </div>
      );
    }
    if (item.submenu) {
      return (
        <div
          key={i}
          className={`action-menu-item action-menu-parent ${openSub === i ? 'active-sub' : ''}`}
          role="menuitem"
          aria-haspopup="menu"
          aria-expanded={openSub === i}
          onMouseEnter={(e) => openSubmenu(i, e.currentTarget, item.submenu.length)}
          onMouseLeave={scheduleCloseSub}
        >
          <span>{item.label}</span>
          <span className="submenu-caret" aria-hidden="true">▸</span>
        </div>
      );
    }
    return (
      <button
        key={i}
        type="button"
        role="menuitem"
        className={`action-menu-item ${item.tone === 'danger' ? 'danger' : ''}`}
        disabled={item.disabled}
        onClick={(e) => {
          e.stopPropagation();
          close();
          item.onClick();
        }}
      >
        {item.label}
      </button>
    );
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
            {items.map((item, i) => renderItem(item, i))}
          </div>,
          document.body
        )}
      {open &&
        openSub !== null &&
        items[openSub]?.submenu &&
        createPortal(
          <div
            ref={flyoutRef}
            className="action-menu action-menu-flyout"
            role="menu"
            style={{ top: subCoords.top, left: subCoords.left }}
            onMouseEnter={cancelCloseSub}
            onMouseLeave={scheduleCloseSub}
          >
            {items[openSub].submenu.map((sub, j) => (
              <button
                key={j}
                type="button"
                role="menuitem"
                className={`action-menu-item ${sub.tone === 'danger' ? 'danger' : ''}`}
                disabled={sub.disabled}
                onClick={(e) => {
                  e.stopPropagation();
                  close();
                  sub.onClick();
                }}
              >
                {sub.label}
              </button>
            ))}
          </div>,
          document.body
        )}
    </>
  );
}

export default ActionMenu;
