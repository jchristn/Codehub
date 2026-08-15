import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

/**
 * Themed 404 page. Rendered both inside the app shell (unknown section) and
 * standalone (any unmatched route).
 */
function NotFound() {
  const { t } = useTranslation();
  return (
    <div className="notfound">
      <div className="notfound-card">
        <div className="notfound-code">404</div>
        <h1 className="notfound-title">{t('notFound.title')}</h1>
        <p className="notfound-message">{t('notFound.message')}</p>
        <Link to="/home" className="button-primary notfound-link">
          {t('notFound.back')}
        </Link>
      </div>
    </div>
  );
}

export default NotFound;
