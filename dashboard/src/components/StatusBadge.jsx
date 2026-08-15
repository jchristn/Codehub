import { useTranslation } from 'react-i18next';
import { HEALTH } from '../utils/constants';

/**
 * Traffic-light status badge: colored dot + short label + tooltip with the
 * signal's evidence. Color is never the only signal — a letter/label and a
 * tooltip always accompany it (accessibility requirement).
 */
function StatusBadge({ status, detail, short = false }) {
  const { t } = useTranslation();
  const meta = HEALTH[status] || HEALTH.Unknown;
  const label = t(`health.${lower(status)}`, { defaultValue: status || t('common.unknown') });
  const tooltip = detail && detail.trim() ? detail : `${label}`;

  return (
    <span className={`status-badge tone-${meta.tone}`} title={tooltip} role="status" aria-label={`${label}. ${tooltip}`}>
      <span className="status-dot" aria-hidden="true" />
      <span className="status-label">{short ? meta.letter : label}</span>
    </span>
  );
}

function lower(status) {
  switch (status) {
    case 'Green':
      return 'green';
    case 'Yellow':
      return 'yellow';
    case 'Red':
      return 'red';
    case 'NotApplicable':
      return 'notApplicable';
    default:
      return 'unknown';
  }
}

export default StatusBadge;
