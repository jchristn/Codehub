import { useState, useEffect, useRef, useCallback } from 'react';

/**
 * Poll the scan status endpoint. While a scan is running it polls every
 * `activeInterval` ms; when idle it refreshes on `idleInterval`. Invokes
 * `onComplete` once when a running scan transitions to idle so callers can
 * refresh their data.
 */
export function useScanStatus(apiClient, { activeInterval = 2500, onComplete } = {}) {
  const [status, setStatus] = useState(null);
  const wasScanning = useRef(false);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  const refresh = useCallback(async () => {
    if (!apiClient) return null;
    try {
      const next = await apiClient.getScanStatus();
      setStatus(next);
      if (wasScanning.current && next && !next.isScanning) {
        if (onCompleteRef.current) onCompleteRef.current(next);
      }
      wasScanning.current = Boolean(next && next.isScanning);
      return next;
    } catch {
      return null;
    }
  }, [apiClient]);

  useEffect(() => {
    if (!apiClient) return undefined;
    let cancelled = false;
    let timer = null;

    const tick = async () => {
      if (cancelled) return;
      const next = await refresh();
      const scanning = Boolean(next && next.isScanning);
      timer = setTimeout(tick, scanning ? activeInterval : 15000);
    };

    tick();
    return () => {
      cancelled = true;
      if (timer) clearTimeout(timer);
    };
  }, [apiClient, refresh, activeInterval]);

  return { status, refresh, isScanning: Boolean(status && status.isScanning) };
}

export default useScanStatus;
