import { useEffect, useState } from 'react';
import { Alert, Button, Empty, Input, Select, Spin, Tag } from 'antd';
import { ArrowLeftOutlined, BookOutlined, LogoutOutlined, MenuOutlined, ReloadOutlined, SearchOutlined } from '@ant-design/icons';
import { Link, useNavigate, useParams } from 'react-router-dom';
import Markdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { useAuth } from '../context/useAuth';
import { helpApi, helpEnabled, type HelpArticle, type HelpDetail } from '../services/help';
import { helpLink } from '../utils/helpLinks';
import logo from '../assets/ECF-Logo.png';
import './HelpCenter.css';

function HelpImage({ source, alt }: { source: string; alt: string }) {
  const [image, setImage] = useState('');
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    const abort = new AbortController();
    let url = '';
    setImage(''); setFailed(false);
    if (!helpLink(source, true)) { setFailed(true); return; }
    void helpApi.image(source.slice(6), abort.signal).then(response => {
      if (abort.signal.aborted) return;
      url = URL.createObjectURL(response.data);
      setImage(url);
    }).catch(() => { if (!abort.signal.aborted) setFailed(true); });
    return () => { abort.abort(); if (url) URL.revokeObjectURL(url); };
  }, [source]);
  if (failed) return <span className="help-image-error">No se pudo cargar la captura. El procedimiento sigue disponible por texto.</span>;
  return image ? <a href={image} target="_blank" rel="noopener noreferrer" title="Abrir captura en tamaño completo"><img src={image} alt={alt} className="help-image" /></a> : <span className="help-image-loading" role="status">Cargando captura...</span>;
}

export default function HelpCenter() {
  const { articleId = 'inicio' } = useParams();
  const { user, token, branchId, logout } = useAuth();
  const navigate = useNavigate();
  const [articles, setArticles] = useState<HelpArticle[]>([]);
  const [results, setResults] = useState<HelpArticle[]>([]);
  const [edition, setEdition] = useState('Manuales del ERP');
  const [query, setQuery] = useState('');
  const [audience, setAudience] = useState('Todos');
  const [detail, setDetail] = useState<HelpDetail>();
  const [indexError, setIndexError] = useState(false);
  const [detailError, setDetailError] = useState(false);
  const [searchError, setSearchError] = useState(false);
  const [loading, setLoading] = useState(true);
  const [searching, setSearching] = useState(false);
  const [menuOpen, setMenuOpen] = useState(false);
  const [retry, setRetry] = useState(0);

  useEffect(() => {
    if (!helpEnabled) return;
    const abort = new AbortController();
    setArticles([]); setResults([]); setIndexError(false);
    void helpApi.index('', abort.signal).then(response => {
      if (!abort.signal.aborted) { setArticles(response.data.articles); setEdition(response.data.edition); }
    }).catch(() => { if (!abort.signal.aborted) setIndexError(true); });
    return () => abort.abort();
  }, [token, branchId, retry]);

  useEffect(() => {
    if (!helpEnabled) return;
    const abort = new AbortController();
    setDetail(undefined); setDetailError(false); setLoading(true);
    void helpApi.detail(articleId, abort.signal).then(response => {
      if (!abort.signal.aborted) setDetail(response.data);
    }).catch(() => { if (!abort.signal.aborted) setDetailError(true); })
      .finally(() => { if (!abort.signal.aborted) setLoading(false); });
    setMenuOpen(false);
    window.scrollTo(0, 0);
    return () => abort.abort();
  }, [articleId, token, branchId, retry]);

  useEffect(() => {
    const abort = new AbortController();
    const term = query.trim();
    setResults([]); setSearchError(false); setSearching(term.length > 0);
    if (!term || !helpEnabled) return () => abort.abort();
    const timer = window.setTimeout(() => {
      void helpApi.index(term, abort.signal).then(response => {
        if (!abort.signal.aborted) setResults(response.data.articles);
      }).catch(() => { if (!abort.signal.aborted) setSearchError(true); })
        .finally(() => { if (!abort.signal.aborted) setSearching(false); });
    }, 250);
    return () => { clearTimeout(timer); abort.abort(); };
  }, [query, token, branchId, retry]);

  const openArticle = () => { setQuery(''); setMenuOpen(false); };
  const filtered = articles.filter(article => audience === 'Todos'
    || article.section === audience
    || article.audience.toLocaleLowerCase('es').includes(audience.toLocaleLowerCase('es')));
  const sections = [...new Set(filtered.map(article => article.section))];

  if (!helpEnabled) return <main className="help-unavailable"><Alert type="info" title="El centro de ayuda todavía no está habilitado en este servidor." /><Link to="/dashboard">Volver al ERP</Link></main>;

  return <div className="help-center">
    <header className="help-header">
      <Link to="/ayuda/inicio" className="help-brand" onClick={openArticle}><img src={logo} alt="El Caldero Flameante" /><span>Grimorio<span>Ayuda y manuales</span></span></Link>
      <div className="help-header-actions">
        <Button icon={<ArrowLeftOutlined />} aria-label="Volver al ERP" onClick={() => navigate('/dashboard')}>Volver al ERP</Button>
        <Button icon={<LogoutOutlined />} aria-label="Cerrar sesión" title={`Cerrar sesión de ${user?.firstName ?? 'usuario'}`} onClick={() => { logout(); navigate('/login', { replace: true }); }} />
      </div>
    </header>
    <div className="help-layout">
      <aside className="help-sidebar">
        <Button className="help-mobile-toggle" icon={<MenuOutlined />} aria-label="Guías del ERP" aria-expanded={menuOpen} aria-controls="help-navigation" onClick={() => setMenuOpen(value => !value)}>Guías del ERP</Button>
        <nav id="help-navigation" aria-label="Manuales disponibles" className={menuOpen ? 'help-navigation help-navigation--open' : 'help-navigation'}>
          <p className="help-edition">{edition}</p>
          <Select aria-label="Filtrar guías por público" value={audience} onChange={setAudience} options={['Todos', 'Personal', 'Administración'].map(value => ({ value, label: value }))} />
          {sections.map(section => <section key={section}>
            <h2>{section}</h2>
            {filtered.filter(article => article.section === section).map(article => <Link key={article.id} to={`/ayuda/${article.id}`} aria-current={article.id === articleId ? 'page' : undefined} onClick={openArticle}>{article.title}</Link>)}
          </section>)}
          {indexError && <Alert type="warning" title="No se pudo cargar el índice." action={<Button icon={<ReloadOutlined />} aria-label="Reintentar índice" onClick={() => setRetry(value => value + 1)} />} />}
        </nav>
      </aside>
      <main id="help-content" className="help-main">
        <div className="help-search">
          <Input aria-label="Buscar en los manuales" placeholder="Buscar en los manuales" prefix={<SearchOutlined />} value={query} maxLength={120} allowClear onChange={event => setQuery(event.target.value)} />
        </div>
        {query.trim() ? <section className="help-results" aria-label="Resultados de búsqueda" aria-busy={searching}>
          <h1>Resultados de búsqueda</h1>
          {searching ? <Spin tip="Buscando"><div className="help-loading" /></Spin>
            : searchError ? <Alert type="error" title="No se pudo realizar la búsqueda." action={<Button icon={<ReloadOutlined />} aria-label="Reintentar" onClick={() => setRetry(value => value + 1)}>Reintentar</Button>} />
            : results.length === 0 ? <Empty description="No se encontraron guías con esos términos." />
            : <><p role="status">{results.length} {results.length === 1 ? 'guía encontrada' : 'guías encontradas'}</p><ul>{results.map(article => <li key={article.id}><Link to={`/ayuda/${article.id}`} onClick={openArticle}>{article.title}</Link><p>{article.summary}</p><span>{article.section}</span></li>)}</ul></>}
        </section> : loading ? <Spin tip="Cargando guía"><div className="help-loading" /></Spin>
          : detailError || !detail ? <section className="help-not-found"><Empty description="Esta guía no está disponible para tu sesión o no pudo cargarse." /><Button icon={<ReloadOutlined />} aria-label="Reintentar" onClick={() => setRetry(value => value + 1)}>Reintentar</Button><Link to="/ayuda/inicio">Ir al inicio</Link></section>
          : <article key={detail.article.id} className="help-article">
            <div className="help-article-meta"><span><BookOutlined /> {detail.article.section}</span><Tag>{detail.article.kind}</Tag></div>
            <h1>{detail.article.title}</h1><p className="help-summary">{detail.article.summary}</p>
            <div className="help-verification"><span>{detail.article.audience}</span><span>Revisado: {detail.article.verifiedOn.split('-').reverse().join('/')}</span><p>{detail.article.verification}</p></div>
            <Markdown remarkPlugins={[remarkGfm]} skipHtml urlTransform={(url, key) => helpLink(url, key === 'src')}
              components={{
                a: ({ href, children }) => !href ? <span>{children}</span> : href.startsWith('/ayuda')
                  ? <Link to={href} onClick={openArticle}>{children}</Link>
                  : <a href={href} target={href.startsWith('https:') ? '_blank' : undefined} rel={href.startsWith('https:') ? 'noopener noreferrer' : undefined}>{children}</a>,
                img: ({ src, alt }) => typeof src === 'string' ? <HelpImage source={src} alt={alt ?? 'Captura del procedimiento'} /> : null,
                table: ({ children }) => <div className="help-table-scroll" role="region" aria-label="Tabla del procedimiento" tabIndex={0}><table>{children}</table></div>,
              }}>{detail.markdown}</Markdown>
          </article>}
      </main>
    </div>
  </div>;
}
