import { useState, useEffect, useCallback, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';
import { useToast } from '../context/ToastContext';

const ROOT_KEY = '__root__';

// --- Client-side selection-state computation -------------------------------
// Mirrors SelectionService.StateFor on the server so a single toggle needs only
// the include/exclude sets, not a re-browse of every loaded tree level.

function normPath(p) {
  return p ? p.replace(/[\\/]+$/, '') : '';
}
function pathEquals(a, b) {
  return normPath(a).toLowerCase() === normPath(b).toLowerCase();
}
function isStrictlyUnder(child, parent) {
  const c = normPath(child).toLowerCase();
  const p = normPath(parent).toLowerCase();
  if (!c || !p || c === p) return false;
  return c.startsWith(`${p}\\`) || c.startsWith(`${p}/`);
}
function computeState(path, sets) {
  const p = normPath(path);
  if (sets.excluded.some((e) => pathEquals(p, e) || isStrictlyUnder(p, e))) return 'Excluded';
  if (sets.included.some((i) => pathEquals(p, i) || isStrictlyUnder(p, i))) return 'Selected';
  if (sets.included.some((i) => isStrictlyUnder(i, p))) return 'Partial';
  return 'None';
}

/**
 * Tri-state checkbox that reflects selected / partial / excluded / none.
 */
function TriCheckbox({ state, onToggle }) {
  const ref = useRef(null);
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = state === 'Partial';
  }, [state]);
  return (
    <input
      ref={ref}
      type="checkbox"
      className={`tri-check ${state === 'Excluded' ? 'excluded' : ''}`}
      checked={state === 'Selected'}
      onChange={onToggle}
      data-no-row-click
      aria-checked={state === 'Partial' ? 'mixed' : state === 'Selected'}
    />
  );
}

/**
 * Lazy directory-tree picker. Each toggle persists immediately to the
 * scan_selections table; unchecking a folder inside a selected branch prunes it
 * via an exclude entry. Closing refreshes the parent view.
 */
function DirectoryPicker({ apiClient, onClose, onChanged }) {
  const { t } = useTranslation();
  const toast = useToast();

  // path -> node ({ name, path, isGitRepository, hasSubdirectories })
  const [nodes, setNodes] = useState({});
  // parent path (or ROOT_KEY) -> array of child paths
  const [children, setChildren] = useState({});
  // Current include/exclude sets; node tri-state is derived from these client-side.
  const [sets, setSets] = useState({ included: [], excluded: [] });
  const [expanded, setExpanded] = useState(new Set());
  const [loading, setLoading] = useState(new Set());
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  const [dirty, setDirty] = useState(false);

  const load = useCallback(
    async (path) => {
      const key = path || ROOT_KEY;
      setLoading((prev) => new Set(prev).add(key));
      try {
        const res = await apiClient.browseDirectory(path);
        const kids = res.children || [];
        setNodes((prev) => {
          const next = { ...prev };
          kids.forEach((c) => {
            next[c.path] = c;
          });
          return next;
        });
        setChildren((prev) => ({ ...prev, [key]: kids.map((c) => c.path) }));
      } catch {
        setError(t('common.error'));
      } finally {
        setLoading((prev) => {
          const next = new Set(prev);
          next.delete(key);
          return next;
        });
      }
    },
    [apiClient, t]
  );

  // Fetch the include/exclude sets once; tri-state for every node is derived from these.
  const loadSelections = useCallback(async () => {
    try {
      const list = (await apiClient.getSelection()) || [];
      const next = { included: [], excluded: [] };
      list.forEach((s) => {
        if (s.included) next.included.push(s.path);
        else next.excluded.push(s.path);
      });
      setSets(next);
    } catch {
      setError(t('common.error'));
    }
  }, [apiClient, t]);

  useEffect(() => {
    load(null);
    loadSelections();
  }, [load, loadSelections]);

  const toggleExpand = useCallback(
    (node) => {
      if (!node.hasSubdirectories) return;
      setExpanded((prev) => {
        const next = new Set(prev);
        if (next.has(node.path)) {
          next.delete(node.path);
        } else {
          next.add(node.path);
          if (!children[node.path]) load(node.path);
        }
        return next;
      });
    },
    [children, load]
  );

  const toggleSelect = useCallback(
    async (node) => {
      const currentState = computeState(node.path, sets);
      setBusy(true);
      try {
        await apiClient.setSelection(node.path, currentState !== 'Selected');
        setDirty(true);
        await loadSelections();
      } catch {
        toast.error(t('common.error'));
      } finally {
        setBusy(false);
      }
    },
    [apiClient, sets, loadSelections, toast, t]
  );

  const handleClose = useCallback(() => {
    if (dirty && onChanged) onChanged();
    onClose();
  }, [dirty, onChanged, onClose]);

  const scanNow = useCallback(async () => {
    try {
      await apiClient.startScan(null);
      toast.success(t('picker.scanStarted'));
      handleClose();
    } catch {
      toast.error(t('common.error'));
    }
  }, [apiClient, toast, t, handleClose]);

  const renderNode = (path, depth) => {
    const node = nodes[path];
    if (!node) return null;
    const isOpen = expanded.has(path);
    const kids = children[path];
    const isLoading = loading.has(path);
    const state = computeState(node.path, sets);
    return (
      <div key={path} className="dir-node-wrap">
        <div className="dir-node" style={{ paddingLeft: `${depth * 18 + 4}px` }}>
          <button
            type="button"
            className={`dir-caret ${node.hasSubdirectories ? '' : 'leaf'}`}
            onClick={() => toggleExpand(node)}
            aria-label={isOpen ? 'Collapse' : 'Expand'}
            data-no-row-click
          >
            {node.hasSubdirectories ? (isOpen ? '▾' : '▸') : ''}
          </button>
          <TriCheckbox state={state} onToggle={() => toggleSelect(node)} />
          <span className="dir-name" onClick={() => toggleExpand(node)}>
            {node.name}
          </span>
          {node.isGitRepository && <span className="dir-git-badge">git</span>}
          {state === 'Excluded' && <span className="dir-excluded-tag">{t('picker.excluded')}</span>}
        </div>
        {isOpen && (
          <div className="dir-children">
            {isLoading && !kids ? (
              <div className="dir-loading" style={{ paddingLeft: `${(depth + 1) * 18 + 22}px` }}>
                {t('common.loading')}
              </div>
            ) : kids && kids.length === 0 ? (
              <div className="dir-empty" style={{ paddingLeft: `${(depth + 1) * 18 + 22}px` }}>
                {t('picker.emptyDir')}
              </div>
            ) : (
              (kids || []).map((childPath) => renderNode(childPath, depth + 1))
            )}
          </div>
        )}
      </div>
    );
  };

  const rootKids = children[ROOT_KEY];

  return (
    <Modal
      open
      onClose={handleClose}
      title={t('picker.title')}
      subtitle={t('picker.subtitle')}
      size="lg"
      footer={
        <div className="picker-footer">
          <span className="picker-hint">{t('picker.hint')}</span>
          <div className="picker-actions">
            <button type="button" className="button-secondary" onClick={handleClose} disabled={busy}>
              {t('common.close')}
            </button>
            <button type="button" className="button-primary" onClick={scanNow} disabled={busy}>
              {t('picker.scanNow')}
            </button>
          </div>
        </div>
      }
    >
      <div className="dir-tree">
        {error && <div className="error-banner">{error}</div>}
        {!rootKids && loading.has(ROOT_KEY) ? (
          <div className="dir-loading">{t('common.loading')}</div>
        ) : rootKids && rootKids.length === 0 ? (
          <div className="dir-empty">{t('picker.noRoots')}</div>
        ) : (
          (rootKids || []).map((p) => renderNode(p, 0))
        )}
      </div>
    </Modal>
  );
}

export default DirectoryPicker;
