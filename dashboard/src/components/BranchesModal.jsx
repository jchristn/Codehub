import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import Modal from './Modal';

/**
 * Shows each local branch of a repository with its commits ahead/behind the base
 * branch, as captured on the last scan.
 */
function BranchesModal({ apiClient, repository, onClose }) {
  const { t } = useTranslation();
  const [branches, setBranches] = useState(null);
  const [error, setError] = useState(null);

  useEffect(() => {
    let active = true;
    apiClient
      .getBranches(repository.id)
      .then((res) => active && setBranches(res || []))
      .catch(() => active && setError(t('common.error')));
    return () => {
      active = false;
    };
  }, [apiClient, repository.id, t]);

  return (
    <Modal open onClose={onClose} title={t('branches.title')} subtitle={repository?.name} size="lg">
      {error ? (
        <div className="banner banner-danger">{error}</div>
      ) : !branches ? (
        <div className="table-state">
          <div className="loading-spinner" />
          <span>{t('common.loading')}</span>
        </div>
      ) : branches.length === 0 ? (
        <div className="table-state">{t('branches.empty')}</div>
      ) : (
        <div className="data-table-scroll">
          <table className="data-table">
            <thead>
              <tr>
                <th>{t('branches.name')}</th>
                <th className="cell-center">{t('branches.ahead')}</th>
                <th className="cell-center">{t('branches.behind')}</th>
              </tr>
            </thead>
            <tbody>
              {branches.map((b) => (
                <tr key={b.name}>
                  <td className="mono">
                    {b.name}
                    {b.isCurrent && <span className="branch-current">{t('branches.current')}</span>}
                  </td>
                  <td className="cell-center">
                    <span className={b.ahead > 0 ? 'ab-ahead active' : 'ab-ahead'}>↑{b.ahead}</span>
                  </td>
                  <td className="cell-center">
                    <span className={b.behind > 0 ? 'ab-behind active' : 'ab-behind'}>↓{b.behind}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </Modal>
  );
}

export default BranchesModal;
