import { useCallback, useEffect, useRef, useState } from 'react';
import { messages } from './messages';
import type { Language } from './messages';
import { CommentForm } from './comments/CommentForm';
import { CommentsTable } from './comments/CommentsTable';
import type { Comment, CommentsClient } from './comments/model';

export function App({ client }: { client: CommentsClient }) {
  const [language, setLanguage] = useState<Language>('en');
  const [comments, setComments] = useState<Comment[]>([]);
  const [loading, setLoading] = useState(true);
  const [failed, setFailed] = useState(false);
  const current = useRef<AbortController | null>(null);
  const t = messages[language];

  useEffect(() => { document.documentElement.lang = language; }, [language]);

  const load = useCallback(async () => {
    current.current?.abort();
    const controller = new AbortController(); current.current = controller;
    setLoading(true); setFailed(false);
    try {
      const values = await client.latest(controller.signal);
      if (!controller.signal.aborted) setComments(values);
    } catch {
      if (!controller.signal.aborted) setFailed(true);
    } finally { if (!controller.signal.aborted) setLoading(false); }
  }, [client]);

  useEffect(() => { void load(); return () => current.current?.abort(); }, [load]);

  function created(comment: Comment) {
    // Cancel stale list responses so they cannot hide a newly confirmed comment.
    current.current?.abort(); setLoading(false); setFailed(false);
    setComments(previous => [comment, ...previous.filter(value => value.id !== comment.id)].slice(0, 25));
    void load();
  }

  return <main>
    <header><a className="brand" href="/" aria-label="CommentsService"><span className="mark" aria-hidden="true">c</span>
      <span>Comments<span className="light">Service</span></span></a>
      <label className="language" htmlFor="language">{t.language}<select id="language" aria-label={t.language} value={language} onChange={event => setLanguage(event.target.value as Language)}>
        <option value="en">English</option><option value="ru">Русский</option><option value="uk">Українська</option>
      </select></label></header>
    <div className="hero"><p className="eyebrow">1.1.0.6 / XHTML EDITOR</p><h1>{t.title}</h1><p className="intro">{t.intro}</p></div>
    <CommentForm client={client} labels={t} onCreated={created} />
    <section className="panel comments" aria-labelledby="recent-title" aria-busy={loading}>
      <div className="section-heading"><h2 id="recent-title">{t.recent}</h2><button className="secondary" onClick={() => void load()} disabled={loading}>{t.refresh}</button></div>
      <p className="muted">{t.window}</p>
      <p role="status">{loading ? t.loading : !failed && comments.length === 0 ? t.empty : ''}</p>
      {failed && <p role="alert" className="error">{t.loadError}</p>}
      {comments.length > 0 && <CommentsTable comments={comments} labels={t} language={language} />}
    </section>
    <footer>CommentsService · 1.1.0.6</footer>
  </main>;
}
