import type { FileClient } from './files/model';
import { useCallback, useEffect, useRef, useState } from 'react';
import { configuredMessages } from './messages';
import type { Language } from './messages';
import { CommentForm } from './comments/CommentForm';
import { CommentsTable } from './comments/CommentsTable';
import { RootPagination } from './comments/RootPagination';
import { defaultRootQuery, readRootQuery } from './comments/model';
import type { Comment, CommentsClient, RootPage, RootPageQuery } from './comments/model';

export function App({ client, fileClient }: { client: CommentsClient; fileClient: FileClient }) {
  const [language, setLanguage] = useState<Language>('en');
  const [query, setQuery] = useState<RootPageQuery>(() => readRootQuery(location.search));
  const [data, setData] = useState<RootPage | null>(null);
  const [loadedQuery, setLoadedQuery] = useState<RootPageQuery | null>(null);
  const [replyTo, setReplyTo] = useState<Comment | null>(null);
  const [lastCreated, setLastCreated] = useState<Comment | null>(null);
  const [threadRefresh, setThreadRefresh] = useState(0);
  const [loading, setLoading] = useState(true);
  const [failed, setFailed] = useState(false);
  const current = useRef<AbortController | null>(null);
  const t = configuredMessages(language);

  useEffect(() => { document.documentElement.lang = language; }, [language]);
  const load = useCallback(async (requested: RootPageQuery) => {
    current.current?.abort();
    const controller = new AbortController(); current.current = controller;
    setLoading(true); setFailed(false);
    try {
      const result = await client.page(requested, controller.signal);
      if (!controller.signal.aborted) {
        // A stale bookmarked page can become empty after deletion by an operator.
        if (requested.page > Math.max(1, result.totalPages)) {
          setQuery(previous => ({ ...previous, page: Math.max(1, result.totalPages) })); return;
        }
        setData(result); setLoadedQuery(requested);
      }
    } catch { if (!controller.signal.aborted) setFailed(true); }
    finally { if (!controller.signal.aborted) setLoading(false); }
  }, [client]);

  useEffect(() => {
    const url = new URL(location.href);
    url.searchParams.set('page', String(query.page)); url.searchParams.set('sort', query.sort); url.searchParams.set('direction', query.direction);
    history.replaceState(null, '', url.pathname + url.search + url.hash);
    void load(query); return () => current.current?.abort();
  }, [query, load]);

  function created(comment: Comment) {
    current.current?.abort(); setLoading(false); setFailed(false);
    setLastCreated(comment);
    setReplyTo(previous => previous?.id === comment.parentId ? null : previous);
    if (comment.parentId) { void load(query); return; }
    // New roots are immediately findable under the default newest-first order.
    if (query.page === 1 && query.sort === 'createdAt' && query.direction === 'desc') void load(defaultRootQuery);
    else setQuery({ ...defaultRootQuery });
  }

  return <>
    <a className="skip-link" href="#recent-title">{t.skipComments}</a>
    <main>
    <header><a className="brand" href="/" aria-label="CommentsService"><span className="mark" aria-hidden="true">c</span>
      <span>Comments<span className="light">Service</span></span></a>
      <label className="language" htmlFor="language">{t.language}<select id="language" aria-label={t.language} value={language} onChange={event => setLanguage(event.target.value as Language)}>
        <option value="en">English</option><option value="ru">Русский</option><option value="uk">Українська</option>
      </select></label></header>
    <div className="hero"><p className="eyebrow">1.2.1.1</p><h1>{t.title}</h1><p className="intro">{t.intro}</p></div>
    <CommentForm client={client} labels={t} onCreated={created} replyTo={replyTo} onCancelReply={() => setReplyTo(null)} />
    <section className="panel comments" aria-labelledby="recent-title" aria-busy={loading}>
      <div className="section-heading"><h2 id="recent-title" tabIndex={-1}>{t.recent}</h2><button className="secondary" onClick={() => { setThreadRefresh(value => value + 1); void load(query); }} disabled={loading}>{t.refresh}</button></div>
      <p className="muted">{t.window}</p>
      <RootPagination query={query} data={data} busy={loading} labels={t} onChange={setQuery} />
      <p role="status">{loading ? t.loading : !failed && data?.items.length === 0 ? t.empty : ''}</p>
      {failed && <p role="alert" className="error">{t.loadError} {data && t.previousResults}</p>}
      {data && data.items.length > 0 && <CommentsTable key={`${loadedQuery?.page}:${loadedQuery?.sort}:${loadedQuery?.direction}`}
        comments={data.items} fileClient={fileClient} labels={t} language={language} client={client} lastCreated={lastCreated} refreshVersion={threadRefresh} onReply={comment => {
          setReplyTo(comment); document.querySelector('#compose-title')?.scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth', block: 'start' });
          requestAnimationFrame(() => document.getElementById('text')?.focus());
        }} />}
    </section>
    <footer>CommentsService · 1.2.1.1</footer>
  </main></>;
}
