import { useTranslation } from 'react-i18next';

/**
 * Generic filter row. `fields` describe the controls; `values` and `onChange`
 * are controlled by the parent, which translates them into backend query
 * parameters. A clear button resets everything.
 *
 * field: { name, type: 'text'|'select'|'datetime', label, placeholder, options?[] }
 */
function FilterBar({ fields, values, onChange, onClear, children }) {
  const { t } = useTranslation();

  const hasActive = fields.some((f) => values[f.name] !== undefined && values[f.name] !== '' && values[f.name] !== null);

  return (
    <div className="filter-bar">
      {fields.map((field) => (
        <div className="filter-field" key={field.name}>
          <label className="filter-label" htmlFor={`filter-${field.name}`}>
            {field.label}
          </label>
          {field.type === 'select' ? (
            <select
              id={`filter-${field.name}`}
              value={values[field.name] ?? ''}
              onChange={(e) => onChange(field.name, e.target.value)}
            >
              <option value="">{field.anyLabel || t('common.all')}</option>
              {field.options.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          ) : field.type === 'datetime' ? (
            <input
              id={`filter-${field.name}`}
              type="datetime-local"
              value={values[field.name] ?? ''}
              onChange={(e) => onChange(field.name, e.target.value)}
            />
          ) : (
            <input
              id={`filter-${field.name}`}
              type="text"
              value={values[field.name] ?? ''}
              placeholder={field.placeholder}
              onChange={(e) => onChange(field.name, e.target.value)}
            />
          )}
        </div>
      ))}

      {children}

      <div className="filter-actions">
        <button type="button" className="button-secondary" onClick={onClear} disabled={!hasActive}>
          {t('common.clear')}
        </button>
      </div>
    </div>
  );
}

export default FilterBar;
