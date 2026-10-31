import { useEffect, useRef, useState } from 'react';
import type { Language, Messages } from '../messages';
import type { FileClient } from '../files/model';
import type { Comment, CommentsClient } from './model';
import { CommentText } from './CommentText';
import { CommentsTable } from './CommentsTable';
import { HttpSearchClient } from './searchClient';
import type { SearchPage } from './searchClient';
import { limits } from '../runtimeConfig';
const searchClient = new HttpSearchClient();
export function CommentSearch({ labels: t, language, client, fileClient, onReply, lastCreated, refreshVersion }: {
  labels: Messages; language: Language; client: CommentsClient; fileClient: FileClient; onReply: (comment: Comment) => void;
  lastCreated: Comment | null; refreshVersion: number;
}) {
  const [input, setInput] = useState('');
  const [loadedQuery, setLoadedQuery] = useState('');
  const [data, setData] = useState<SearchPage | null>(null);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);
  const [invalid, setInvalid] = useState(false);
  const [selected, setSelected] = useState<Comment | null>(null);
  const [root, setRoot] = useState<Comment | null>(null);
  const [opening, setOpening] = useState(false);
  const [openFailed, setOpenFailed] = useState(false);
  const pending = useRef<AbortController | null>(null), pendingRoot = useRef<AbortController | null>(null);
  const discussion = useRef<HTMLElement | null>(null);
  useEffect(() => () => { pending.current?.abort(); pendingRoot.current?.abort(); }, []);
  async function load(query: string, page: number) {
    pending.current?.abort();
    if (!query.trim() || query.trim().length > limits.searchQueryMaxLength) { setInvalid(true); return; }
    setInvalid(false); setBusy(true); setFailed(false);
    const cancel = new AbortController(); pending.current = cancel;
    try {
      const result = await searchClient.search(query.trim(), page, cancel.signal);
      if (!cancel.signal.aborted) { setData(result); setLoadedQuery(query.trim()); }
    } catch { if (!cancel.signal.aborted) setFailed(true); }
    finally { if (!cancel.signal.aborted) setBusy(false); }
  }
  async function open(comment: Comment) {
    pendingRoot.current?.abort(); const cancel = new AbortController(); pendingRoot.current = cancel;
    setOpening(true); setOpenFailed(false);
    try {
      const result = await searchClient.root(comment.rootId, cancel.signal);
      if (!cancel.signal.aborted) { setRoot(result); setSelected(comment); requestAnimationFrame(() => discussion.current?.scrollIntoView({ block: 'nearest' })); }
    } catch { if (!cancel.signal.aborted) setOpenFailed(true); }
    finally { if (!cancel.signal.aborted) setOpening(false); }
  }
  return <section className="panel search" aria-labelledby="search-title">
    <h2 id="search-title">{t.searchTitle}</h2><p className="muted">{t.searchHint}</p>
    <form onSubmit={event => { event.preventDefault(); void load(input, 1); }}>
      <label htmlFor="search-query">{t.searchQuery}</label>
      <input id="search-query" type="search" value={input} maxLength={limits.searchQueryMaxLength} onChange={event => setInput(event.target.value)} />
      <button type="submit" disabled={busy}>{t.searchButton}</button>
    </form>
    <p role="status">{busy ? t.loading : data?.items.length === 0 ? t.searchEmpty : ''}</p>
    {invalid && <p role="alert">{t.searchInvalid}</p>}
    {failed && <p role="alert" className="error">{t.searchUnavailable} {data && t.previousResults}</p>}
    {data && <div className="search-results" aria-busy={busy}>
      {data.items.map(comment => <article key={comment.id} data-search-id={comment.id}>
        <strong>{comment.userName}</strong><div className="search-excerpt"><CommentText text={comment.text} /></div>
        <button type="button" className="secondary" disabled={opening} onClick={() => void open(comment)}>{t.openDiscussion}</button>{' '}
        <button type="button" className="secondary" onClick={() => onReply(comment)}>{t.reply}</button>
      </article>)}
      <nav aria-label={t.pages}><button type="button" className="secondary" disabled={busy || data.page <= 1} onClick={() => void load(loadedQuery, data.page - 1)}>{t.previousPage}</button>
        {' '}<span>{t.page} {data.page}</span>{' '}<button type="button" className="secondary" disabled={busy || !data.hasMore} onClick={() => void load(loadedQuery, data.page + 1)}>{t.nextPage}</button></nav>
    </div>}
    {openFailed && <p role="alert" className="error">{t.loadError}</p>}
    {root && selected && <section ref={discussion} className="search-discussion" aria-label={t.discussion}>
      <h3>{t.discussion}</h3>
      {selected.id !== root.id && <article data-selected-search-id={selected.id}><h4>{t.selectedComment}</h4><strong>{selected.userName}</strong><CommentText text={selected.text} />
        <button type="button" className="secondary" onClick={() => onReply(selected)}>{t.reply}</button></article>}
      <CommentsTable key={root.id} comments={[root]} labels={t} language={language} client={client} fileClient={fileClient}
        lastCreated={lastCreated} refreshVersion={refreshVersion} onReply={onReply} />
    </section>}
  </section>;
}
