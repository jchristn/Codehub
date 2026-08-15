import { NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

/**
 * Grouped, workflow-oriented navigation. Groups: Overview, Inventory,
 * Operations, Observability, System.
 */

const NAV_GROUPS = [
  {
    labelKey: 'nav.groupOverview',
    items: [{ id: 'home', labelKey: 'nav.home', icon: '◧' }]
  },
  {
    labelKey: 'nav.groupInventory',
    items: [{ id: 'repositories', labelKey: 'nav.repositories', icon: '▤' }]
  },
  {
    labelKey: 'nav.groupOperations',
    items: [{ id: 'scans', labelKey: 'nav.scans', icon: '⟳' }]
  },
  {
    labelKey: 'nav.groupObservability',
    items: [
      { id: 'request-history', labelKey: 'nav.requestHistory', icon: '≣' },
      { id: 'api-explorer', labelKey: 'nav.apiExplorer', icon: '▷' }
    ]
  },
  {
    labelKey: 'nav.groupSystem',
    items: [{ id: 'settings', labelKey: 'nav.settings', icon: '⚙' }]
  }
];

function Sidebar({ collapsed, version }) {
  const { t } = useTranslation();

  return (
    <aside className={`sidebar ${collapsed ? 'collapsed' : ''}`}>
      <div className="sidebar-brand">
        <img src={`${import.meta.env.BASE_URL}logo.png`} alt="" className="sidebar-logo" />
        {!collapsed && <span className="sidebar-brand-name">{t('common.appName')}</span>}
      </div>

      <nav className="sidebar-nav" aria-label={t('common.appName')}>
        {NAV_GROUPS.map((group) => (
          <div className="nav-group" key={group.labelKey}>
            {!collapsed && <div className="nav-group-label">{t(group.labelKey)}</div>}
            {group.items.map((item) => (
              <NavLink
                key={item.id}
                to={`/dashboard/${item.id}`}
                className={({ isActive }) => `nav-item ${isActive ? 'active' : ''}`}
                title={t(item.labelKey)}
              >
                <span className="nav-icon" aria-hidden="true">
                  {item.icon}
                </span>
                {!collapsed && <span className="nav-label">{t(item.labelKey)}</span>}
              </NavLink>
            ))}
          </div>
        ))}
      </nav>

      <div className="sidebar-footer">
        {!collapsed && <span className="sidebar-version">v{version || '1.0.0'}</span>}
      </div>
    </aside>
  );
}

export default Sidebar;
