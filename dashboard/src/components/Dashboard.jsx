import { useState, useEffect, useCallback } from 'react';
import { useParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { useTranslation } from 'react-i18next';
import { ApiError } from '../utils/api';
import useScanStatus from '../hooks/useScanStatus';
import Sidebar from './Sidebar';
import Topbar from './Topbar';
import HomeView from '../views/HomeView';
import RepositoriesView from '../views/RepositoriesView';
import ScansView from '../views/ScansView';
import RequestHistoryView from '../views/RequestHistoryView';
import ApiExplorerView from '../views/ApiExplorerView';
import SettingsView from '../views/SettingsView';
import CustomActionsView from '../views/CustomActionsView';
import NotFound from '../views/NotFound';

/**
 * Authenticated app shell: persistent sidebar + topbar + scrollable workspace.
 * Owns cross-view concerns — scan orchestration, overview health summary for
 * the topbar, and a scan-completion nonce that views watch to refresh.
 */
function Dashboard() {
  const { section = 'home' } = useParams();
  const { apiClient } = useAuth();
  const toast = useToast();
  const { t } = useTranslation();

  const [collapsed, setCollapsed] = useState(false);
  const [overview, setOverview] = useState(null);
  const [settings, setSettings] = useState(null);
  const [scanNonce, setScanNonce] = useState(0);

  // Refresh overview + bump nonce when a scan finishes, naming the repository/count scanned.
  const handleScanComplete = useCallback(
    (_status, repos) => {
      let message;
      if (repos && repos.length === 1) message = t('scans.scannedOne', { name: repos[0].name });
      else if (repos && repos.length > 1) message = t('scans.scannedMany', { count: repos.length });
      else message = t('scans.completeGeneric');
      toast.success(message);
      setScanNonce((n) => n + 1);
    },
    [toast, t]
  );

  const { status: scanStatus, refresh: refreshScan, isScanning } = useScanStatus(apiClient, {
    onComplete: handleScanComplete
  });

  const loadOverview = useCallback(() => {
    if (!apiClient) return;
    apiClient.getOverview().then(setOverview).catch(() => setOverview(null));
  }, [apiClient]);

  useEffect(() => {
    loadOverview();
  }, [loadOverview, scanNonce]);

  useEffect(() => {
    if (!apiClient) return;
    apiClient.getSettings().then(setSettings).catch(() => setSettings(null));
  }, [apiClient]);

  const handleScanNow = useCallback(async () => {
    try {
      await apiClient.startScan(null);
      refreshScan();
      // Briefly wait for the scan to report its in-flight repositories, then name the toast.
      let repos = [];
      for (let i = 0; i < 8; i += 1) {
        const st = await apiClient.getScanStatus();
        if (st && Array.isArray(st.repositories) && st.repositories.length) {
          repos = st.repositories;
          break;
        }
        if (st && !st.isScanning && i > 1) break;
        await new Promise((resolve) => {
          setTimeout(resolve, 250);
        });
      }
      if (repos.length === 1) toast.info(t('scans.scanningOne', { name: repos[0].name }));
      else if (repos.length > 1) toast.info(t('scans.scanningMany', { count: repos.length }));
      else toast.info(t('common.scanning'));
    } catch (err) {
      if (err instanceof ApiError && err.status === 409) {
        toast.warning(t('common.scanning'));
      } else {
        toast.error(t('common.error'));
      }
    }
  }, [apiClient, toast, t, refreshScan]);

  const lastScannedUtc = scanStatus?.lastScannedUtc || overview?.lastScannedUtc;
  const nextScanUtc = scanStatus?.nextScanUtc;
  const healthSummary = overview
    ? { green: overview.greenCount, yellow: overview.yellowCount, red: overview.redCount }
    : null;

  const renderView = () => {
    const shared = { apiClient, scanNonce, isScanning, onScanNow: handleScanNow, lastScannedUtc, nextScanUtc };
    switch (section) {
      case 'home':
        return <HomeView {...shared} overview={overview} onRefreshOverview={loadOverview} />;
      case 'repositories':
        return <RepositoriesView {...shared} />;
      case 'scans':
        return <ScansView {...shared} scanStatus={scanStatus} />;
      case 'request-history':
        return <RequestHistoryView apiClient={apiClient} />;
      case 'api-explorer':
        return <ApiExplorerView apiClient={apiClient} />;
      case 'settings':
        return <SettingsView apiClient={apiClient} settings={settings} />;
      case 'custom-actions':
        return <CustomActionsView apiClient={apiClient} />;
      default:
        return <NotFound />;
    }
  };

  return (
    <div className={`app-shell ${collapsed ? 'sidebar-collapsed' : ''}`}>
      <Sidebar collapsed={collapsed} version={settings?.version} />
      <div className="shell-main">
        <Topbar
          onToggleSidebar={() => setCollapsed((c) => !c)}
          rootPath={(settings?.rootPaths || []).join(', ')}
          lastScannedUtc={lastScannedUtc}
          healthSummary={healthSummary}
          isScanning={isScanning}
          onScanNow={handleScanNow}
          scanDisabled={!apiClient}
        />
        <main className="workspace">{renderView()}</main>
      </div>
    </div>
  );
}

export default Dashboard;
