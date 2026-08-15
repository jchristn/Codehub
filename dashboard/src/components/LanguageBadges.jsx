/**
 * Render a repository's languages as compact badges.
 */
function LanguageBadges({ languages, primary }) {
  const list = languages && languages.length > 0 ? languages : primary ? [primary] : [];
  if (list.length === 0) return <span className="muted">—</span>;
  return (
    <span className="language-badges">
      {list.map((lang) => (
        <span key={lang} className={`lang-badge ${lang === primary ? 'primary' : ''}`}>
          {lang}
        </span>
      ))}
    </span>
  );
}

export default LanguageBadges;
