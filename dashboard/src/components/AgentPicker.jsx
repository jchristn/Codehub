import { useTranslation } from 'react-i18next';
import { AGENTS, agentDangerousFlag } from '../utils/constants';

/**
 * Choose the agent a custom action runs under, plus its dangerous flag when the agent has one.
 */
function AgentPicker({ agent, dangerous, onAgentChange, onDangerousChange }) {
  const { t } = useTranslation();
  const flag = agentDangerousFlag(agent);

  return (
    <>
      <label className="ca-field">
        <span className="ca-label">{t('customActions.agent')}</span>
        <select value={agent} onChange={(e) => onAgentChange(e.target.value)}>
          {AGENTS.map((a) => (
            <option key={a.value} value={a.value}>{a.label}</option>
          ))}
        </select>
        <p className="ca-hint">{t('customActions.agentHint')}</p>
      </label>

      {flag && (
        <label className="ca-flag">
          <input type="checkbox" checked={dangerous} onChange={(e) => onDangerousChange(e.target.checked)} />
          <span>
            {t('customActions.dangerous')} <code>{flag}</code>
          </span>
        </label>
      )}
    </>
  );
}

export default AgentPicker;
