import { useRef, useEffect } from 'react';

/**
 * A checkbox that supports an indeterminate state, used as the "select all" box
 * in a table header.
 */
function SelectAllCheckbox({ checked, indeterminate, onChange, title }) {
  const ref = useRef(null);
  useEffect(() => {
    if (ref.current) ref.current.indeterminate = Boolean(indeterminate);
  }, [indeterminate]);
  return (
    <input
      ref={ref}
      type="checkbox"
      checked={checked}
      onChange={onChange}
      data-no-row-click
      title={title}
      aria-label={title}
    />
  );
}

export default SelectAllCheckbox;
