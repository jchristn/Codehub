import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';

/**
 * Reusable copy-to-clipboard control. Briefly switches to a checkmark on
 * success without causing layout shift. Preserves the exact value (URLs,
 * ports, query strings) verbatim.
 */
function CopyButton({ value, label, size = 'md', showLabel = false }) {
  const { t } = useTranslation();
  const [copied, setCopied] = useState(false);

  const copy = useCallback(
    async (e) => {
      e.stopPropagation();
      try {
        await navigator.clipboard.writeText(String(value ?? ''));
      } catch {
        // Fallback for insecure contexts.
        const ta = document.createElement('textarea');
        ta.value = String(value ?? '');
        document.body.appendChild(ta);
        ta.select();
        try {
          document.execCommand('copy');
        } catch {
          /* ignore */
        }
        document.body.removeChild(ta);
      }
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    },
    [value]
  );

  const aria = copied ? t('common.copied') : label ? t('common.copyValue', { label }) : t('common.copy');

  return (
    <button
      type="button"
      className={`copy-button ${size} ${copied ? 'copied' : ''}`}
      onClick={copy}
      aria-label={aria}
      title={aria}
    >
      <span className="copy-icon" aria-hidden="true">
        {copied ? '✓' : '⧉'}
      </span>
      {showLabel && <span className="copy-text">{copied ? t('common.copied') : t('common.copy')}</span>}
    </button>
  );
}

export default CopyButton;
