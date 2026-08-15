import CopyButton from './CopyButton';

/**
 * Read-only JSON / code block with a copy control. Accepts an object or a
 * pre-stringified value.
 */
function JsonBlock({ value, label }) {
  const text = typeof value === 'string' ? value : JSON.stringify(value, null, 2);
  return (
    <div className="json-block">
      <div className="json-block-toolbar">
        <CopyButton value={text} label={label} size="sm" showLabel />
      </div>
      <pre className="json-content mono">{text}</pre>
    </div>
  );
}

export default JsonBlock;
