import { useEffect, useState } from 'react';
import type { Messages } from '../messages';
import type { RootPage, RootPageQuery, RootSort } from './model';

export function RootPagination({ query, data, busy, labels: t, onChange }: {
  query: RootPageQuery; data: RootPage | null; busy: boolean; labels: Messages; onChange: (query: RootPageQuery) => void;
}) {
  const [jump, setJump] = useState(String(query.page));
  useEffect(() => setJump(String(query.page)), [query.page]);
  const page = data?.page ?? query.page, total = data?.totalPages ?? 0;
  function go(value: number) {
    if (Number.isInteger(value) && value >= 1 && value <= Math.max(1, total)) onChange({ ...query, page: value });
  }
  return <div className="root-controls">
    <div className="sort-controls">
      <label>{t.sortBy} <select aria-label={t.sortBy} value={query.sort} disabled={busy} onChange={event => onChange({ ...query, page: 1, sort: event.target.value as RootSort })}>
        <option value="createdAt">{t.posted}</option><option value="userName">{t.name}</option><option value="email">{t.email}</option>
      </select></label>
      <label>{t.sortDirection} <select aria-label={t.sortDirection} value={query.direction} disabled={busy} onChange={event => onChange({ ...query, page: 1, direction: event.target.value as 'asc' | 'desc' })}>
        <option value="desc">{t.descending}</option><option value="asc">{t.ascending}</option>
      </select></label>
    </div>
    <nav className="pagination" aria-label={t.pages}>
      <button className="secondary" disabled={busy || page <= 1} onClick={() => go(1)}>{t.firstPage}</button>
      <button className="secondary" disabled={busy || page <= 1} onClick={() => go(page - 1)}>{t.previousPage}</button>
      <span aria-live="polite">{t.page} {page.toLocaleString()} / {Math.max(1, total).toLocaleString()} · {t.rootCount} {(data?.totalCount ?? 0).toLocaleString()}</span>
      <button className="secondary" disabled={busy || page >= total} onClick={() => go(page + 1)}>{t.nextPage}</button>
      <button className="secondary" disabled={busy || page >= total} onClick={() => go(total)}>{t.lastPage}</button>
      <label>{t.goToPage} <input aria-label={t.goToPage} type="number" min={1} max={Math.max(1, total)} value={jump} disabled={busy || !total}
        onChange={event => setJump(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); go(Number(jump)); } }} /></label>
      <button className="secondary" disabled={busy || !Number.isInteger(Number(jump)) || Number(jump) < 1 || Number(jump) > total} onClick={() => go(Number(jump))}>{t.go}</button>
    </nav>
  </div>;
}
