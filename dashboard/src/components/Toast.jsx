import { createPortal } from 'react-dom';

/**
 * Toast stack rendered via portal so it escapes any overflow container.
 */
function ToastContainer({ toasts, onDismiss }) {
  if (!toasts || toasts.length === 0) return null;
  return createPortal(
    <div className="toast-stack" role="region" aria-live="polite">
      {toasts.map((toast) => (
        <div key={toast.id} className={`toast tone-${toast.tone}`}>
          <span className="toast-message">{toast.message}</span>
          <button
            type="button"
            className="toast-close"
            onClick={() => onDismiss(toast.id)}
            aria-label="Dismiss"
          >
            ×
          </button>
        </div>
      ))}
    </div>,
    document.body
  );
}

export default ToastContainer;
