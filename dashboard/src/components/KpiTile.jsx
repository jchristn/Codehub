import { formatNumber } from '../i18n/formatters';

/**
 * KPI tile. Clickable when it summarizes a navigable resource.
 */
function KpiTile({ label, value, tone = 'neutral', onClick, hint }) {
  const Tag = onClick ? 'button' : 'div';
  return (
    <Tag className={`kpi-tile tone-${tone} ${onClick ? 'clickable' : ''}`} onClick={onClick} type={onClick ? 'button' : undefined}>
      <span className="kpi-value">{typeof value === 'number' ? formatNumber(value) : value}</span>
      <span className="kpi-label">{label}</span>
      {hint && <span className="kpi-hint">{hint}</span>}
    </Tag>
  );
}

export default KpiTile;
