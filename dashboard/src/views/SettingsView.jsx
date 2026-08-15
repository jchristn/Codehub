import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import PageHeader from '../components/PageHeader';
import CopyButton from '../components/CopyButton';
import DirectoryPicker from '../components/DirectoryPicker';
import LanguageSelector from '../i18n/LanguageSelector';

const API_TYPES = ['OpenAI', 'Gemini', 'Ollama'];
const DB_TYPES = ['Sqlite', 'Mysql', 'Postgresql', 'SqlServer'];

/**
 * Server settings form. Loads the full settings, lets the operator edit every section,
 * and PUTs the result back to codehub.json. Includes the Model Runner section.
 */
function SettingsView({ apiClient }) {
  const { t } = useTranslation();
  const { serverUrl } = useAuth();
  const toast = useToast();

  const [s, setS] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState(null);
  const [showPicker, setShowPicker] = useState(false);

  const load = useCallback(() => {
    setLoading(true);
    apiClient
      .getEditableSettings()
      .then(setS)
      .catch(() => setError(t('common.error')))
      .finally(() => setLoading(false));
  }, [apiClient, t]);

  useEffect(() => {
    load();
  }, [load]);

  const set = (section, key, value) =>
    setS((prev) => ({ ...prev, [section]: { ...prev[section], [key]: value } }));

  const save = async () => {
    setSaving(true);
    try {
      const updated = await apiClient.updateSettings(s);
      setS(updated);
      toast.success(t('settingsForm.saved'));
    } catch (e) {
      toast.error(e?.body || t('common.error'));
    } finally {
      setSaving(false);
    }
  };

  if (loading || !s) {
    return (
      <div className="view settings-view">
        <PageHeader title={t('settings.title')} subtitle={t('settings.subtitle')} />
        {error ? <div className="banner banner-danger">{error}</div> : <div className="panel"><div className="loading-spinner" /></div>}
      </div>
    );
  }

  const arr = (v) => (v || []).join('\n');
  const toArr = (text) => text.split('\n').map((x) => x.trim()).filter(Boolean);

  return (
    <div className="view settings-view">
      <PageHeader
        title={t('settings.title')}
        subtitle={t('settings.subtitle')}
        actions={
          <button type="button" className="button-primary" onClick={save} disabled={saving}>
            {saving ? t('settingsForm.saving') : t('settingsForm.save')}
          </button>
        }
      />

      <p className="settings-note">{t('settingsForm.restartNote')}</p>

      <div className="settings-grid">
        <Section title={t('settingsForm.webserver')}>
          <Field label={t('settingsForm.hostname')}><input value={s.webserver.hostname || ''} onChange={(e) => set('webserver', 'hostname', e.target.value)} /></Field>
          <Field label={t('settingsForm.port')}><input type="number" value={s.webserver.port} onChange={(e) => set('webserver', 'port', Number(e.target.value))} /></Field>
          <Check label={t('settingsForm.ssl')} checked={s.webserver.ssl} onChange={(v) => set('webserver', 'ssl', v)} />
          <Check label={t('settingsForm.enableOpenApi')} checked={s.webserver.enableOpenApi} onChange={(v) => set('webserver', 'enableOpenApi', v)} />
        </Section>

        <Section title={t('settingsForm.authentication')}>
          <Field label={t('settingsForm.apiKey')}>
            <span className="id-cell">
              <input type="password" value={s.authentication.apiKey || ''} onChange={(e) => set('authentication', 'apiKey', e.target.value)} />
              <CopyButton value={s.authentication.apiKey} label="API key" size="sm" />
            </span>
          </Field>
        </Section>

        <Section title={t('settingsForm.directories')}>
          <Field label={t('settingsForm.rootPaths')}>
            <textarea rows={3} className="mono" value={arr(s.directories.rootPaths)} onChange={(e) => set('directories', 'rootPaths', toArr(e.target.value))} />
          </Field>
          <Field label={t('settingsForm.excludeDirectories')}>
            <textarea rows={3} className="mono" value={arr(s.directories.excludeDirectories)} onChange={(e) => set('directories', 'excludeDirectories', toArr(e.target.value))} />
          </Field>
          <Field label={t('settingsForm.selection')}>
            <button type="button" className="button-secondary tiny" onClick={() => setShowPicker(true)}>{t('picker.launch')}</button>
          </Field>
        </Section>

        <Section title={t('settingsForm.scan')}>
          <Field label={t('settingsForm.intervalHours')}><input type="number" value={s.scan.intervalHours} onChange={(e) => set('scan', 'intervalHours', Number(e.target.value))} /></Field>
          <Field label={t('settingsForm.maxConcurrency')}><input type="number" value={s.scan.maxConcurrency} onChange={(e) => set('scan', 'maxConcurrency', Number(e.target.value))} /></Field>
          <Check label={t('settingsForm.scanOnStartup')} checked={s.scan.scanOnStartup} onChange={(v) => set('scan', 'scanOnStartup', v)} />
          <Check label={t('settingsForm.dependencyCheck')} checked={s.scan.dependencyCheck} onChange={(v) => set('scan', 'dependencyCheck', v)} />
        </Section>

        <Section title={t('settingsForm.github')}>
          <Field label={t('settingsForm.pat')}><input type="password" value={s.github.personalAccessToken || ''} onChange={(e) => set('github', 'personalAccessToken', e.target.value)} /></Field>
          <Field label={t('settingsForm.owner')}><input value={s.github.owner || ''} onChange={(e) => set('github', 'owner', e.target.value)} /></Field>
        </Section>

        <Section title={t('settingsForm.database')}>
          <Field label={t('settingsForm.dbType')}>
            <select value={s.database.type} onChange={(e) => set('database', 'type', e.target.value)}>
              {DB_TYPES.map((x) => <option key={x} value={x}>{x}</option>)}
            </select>
          </Field>
          <Field label={t('settingsForm.dbFilename')}><input className="mono" value={s.database.filename || ''} onChange={(e) => set('database', 'filename', e.target.value)} /></Field>
        </Section>

        <Section title={t('settingsForm.cors')}>
          <Check label={t('settingsForm.corsEnable')} checked={s.cors.enable} onChange={(v) => set('cors', 'enable', v)} />
          <Field label={t('settingsForm.allowOrigin')}><input value={s.cors.allowOrigin || ''} onChange={(e) => set('cors', 'allowOrigin', e.target.value)} /></Field>
          <Field label={t('settingsForm.allowMethods')}><input value={s.cors.allowMethods || ''} onChange={(e) => set('cors', 'allowMethods', e.target.value)} /></Field>
          <Field label={t('settingsForm.allowHeaders')}><input value={s.cors.allowHeaders || ''} onChange={(e) => set('cors', 'allowHeaders', e.target.value)} /></Field>
        </Section>

        <Section title={t('settingsForm.requestHistory')}>
          <Check label={t('settingsForm.rhEnabled')} checked={s.requestHistory.enabled} onChange={(v) => set('requestHistory', 'enabled', v)} />
          <Field label={t('settingsForm.retentionDays')}><input type="number" value={s.requestHistory.retentionDays} onChange={(e) => set('requestHistory', 'retentionDays', Number(e.target.value))} /></Field>
          <Field label={t('settingsForm.maxReqBytes')}><input type="number" value={s.requestHistory.maxRequestBodyBytes} onChange={(e) => set('requestHistory', 'maxRequestBodyBytes', Number(e.target.value))} /></Field>
          <Field label={t('settingsForm.maxRespBytes')}><input type="number" value={s.requestHistory.maxResponseBodyBytes} onChange={(e) => set('requestHistory', 'maxResponseBodyBytes', Number(e.target.value))} /></Field>
        </Section>

        <Section title={t('settingsForm.logging')}>
          <Check label={t('settingsForm.consoleLogging')} checked={s.logging.consoleLogging} onChange={(v) => set('logging', 'consoleLogging', v)} />
          <Check label={t('settingsForm.fileLogging')} checked={s.logging.fileLogging} onChange={(v) => set('logging', 'fileLogging', v)} />
          <Field label={t('settingsForm.logDirectory')}><input className="mono" value={s.logging.logDirectory || ''} onChange={(e) => set('logging', 'logDirectory', e.target.value)} /></Field>
          <Field label={t('settingsForm.minimumSeverity')}><input type="number" min={0} max={5} value={s.logging.minimumSeverity} onChange={(e) => set('logging', 'minimumSeverity', Number(e.target.value))} /></Field>
        </Section>

        <Section title={t('settingsForm.modelRunner')} subtitle={t('settingsForm.modelRunnerHint')}>
          <Field label={t('settingsForm.endpointBaseUrl')}><input className="mono" placeholder="http://127.0.0.1:11434" value={s.modelRunner.endpointBaseUrl || ''} onChange={(e) => set('modelRunner', 'endpointBaseUrl', e.target.value)} /></Field>
          <Field label={t('settingsForm.apiType')}>
            <select value={s.modelRunner.apiType} onChange={(e) => set('modelRunner', 'apiType', e.target.value)}>
              {API_TYPES.map((x) => <option key={x} value={x}>{x}</option>)}
            </select>
          </Field>
          <Field label={t('settingsForm.modelApiKey')}><input type="password" value={s.modelRunner.apiKey || ''} onChange={(e) => set('modelRunner', 'apiKey', e.target.value)} /></Field>
          <Field label={t('settingsForm.modelName')}><input value={s.modelRunner.modelName || ''} onChange={(e) => set('modelRunner', 'modelName', e.target.value)} /></Field>
        </Section>

        <Section title={t('settingsForm.environment')}>
          <Field label={t('settings.serverUrl')}><span className="id-cell"><span className="mono">{serverUrl}</span><CopyButton value={serverUrl} label="server URL" size="sm" /></span></Field>
          <Field label={t('common.language')}><LanguageSelector /></Field>
        </Section>
      </div>

      <div className="settings-save-bar">
        <button type="button" className="button-primary" onClick={save} disabled={saving}>
          {saving ? t('settingsForm.saving') : t('settingsForm.save')}
        </button>
      </div>

      {showPicker && <DirectoryPicker apiClient={apiClient} onClose={() => setShowPicker(false)} onChanged={load} />}
    </div>
  );
}

function Section({ title, subtitle, children }) {
  return (
    <section className="panel settings-section">
      <h2 className="panel-title">{title}</h2>
      {subtitle && <p className="settings-section-hint">{subtitle}</p>}
      <div className="settings-fields">{children}</div>
    </section>
  );
}

function Field({ label, children }) {
  return (
    <label className="settings-field">
      <span className="settings-field-label">{label}</span>
      {children}
    </label>
  );
}

function Check({ label, checked, onChange }) {
  return (
    <label className="settings-check">
      <input type="checkbox" checked={!!checked} onChange={(e) => onChange(e.target.checked)} />
      <span>{label}</span>
    </label>
  );
}

export default SettingsView;
