import { useState, useEffect } from 'react';

/**
 * Debounce a rapidly-changing value (e.g. a search box) so backend calls only
 * fire once input settles.
 */
export function useDebounce(value, delay = 350) {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const handle = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(handle);
  }, [value, delay]);
  return debounced;
}

export default useDebounce;
