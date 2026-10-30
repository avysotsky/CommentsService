import { FileViewer } from '../files/FileViewer';
import { attachments } from '../files/model';
import type { FileClient } from '../files/model';
import { useCallback, useEffect, useRef, useState } from 'react';
import { CommentText } from './CommentText';
import type { Language, Messages } from '../messages';
import { safeHomePage } from './model';
import type { Comment, CommentsClient } from './model';

type Branch = { items: Comment[]; nextAfter: string | null; open: boolean; loaded: boolean; busy: boolean; failed: boolean; retryAfter: string | null };
const emptyBranch = (): Branch => ({ items: [], nextAfter: null, open: true, loaded: false, busy: false, failed: false, retryAfter: null });
export function CommentsTable({ comments, labels: t, language, client, onReply, lastCreated, refreshVersion, fileClient }: {
  fileClient: FileClient; comments: Comment[]; labels: Messages; language: Language; client: CommentsClient;
  onReply: (comment: Comment) => void; lastCreated: Comment | null; refreshVersion: number;
}) {
  const [viewer, setViewer] = useState<{ files: ReturnType<typeof attachments>; initial: number } | null>(null);
  const [branches, setBranches] = useState<Record<string, Branch>>({});
  const branchSnapshot = useRef(branches); branchSnapshot.current = branches;
  const pending = useRef(new Map<string, AbortController>());
  useEffect(() => () => { pending.current.forEach(controller => controller.abort()); }, []);
  const load = useCallback(async (parentId: string, after: string | null = null) => {
    pending.current.get(parentId)?.abort();
    const controller = new AbortController(); pending.current.set(parentId, controller);
    setBranches(previous => ({ ...previous, [parentId]: { ...(previous[parentId] ?? emptyBranch()), open: true, busy: true, failed: false, retryAfter: after } }));
    try {
      const page = await client.replies(parentId, after, controller.signal);
      if (!controller.signal.aborted) setBranches(previous => {
        const current = previous[parentId] ?? emptyBranch();
        const items = after ? [...current.items, ...page.items.filter(item => !current.items.some(old => old.id === item.id))] : page.items;
        return { ...previous, [parentId]: { ...current, items, nextAfter: page.nextAfter, loaded: true, busy: false, failed: false } };
      });
    } catch {
      if (!controller.signal.aborted) setBranches(previous => ({ ...previous, [parentId]: { ...previous[parentId], busy: false, failed: true } }));
    } finally { if (pending.current.get(parentId) === controller) pending.current.delete(parentId); }
  }, [client]);
  useEffect(() => { if (lastCreated?.parentId) void load(lastCreated.parentId); }, [lastCreated, load]);

  useEffect(() => {
    if (refreshVersion > 0) for (const [id, branch] of Object.entries(branchSnapshot.current)) {
      if (branch.open) void load(id);
    }
  }, [refreshVersion, load]);

  function toggle(id: string) {
    if (!branches[id]?.loaded && !branches[id]?.busy) { void load(id); return; }
    setBranches(previous => ({ ...previous, [id]: { ...previous[id], open: !previous[id].open } }));
  }

  const rows: { comment: Comment; depth: number; controls?: boolean }[] = [];
  const stack = comments.slice().reverse().map(comment => ({ comment, depth: 0, controls: false }));
  const seen = new Set<string>();
  while (stack.length) {
    const row = stack.pop()!, branch = branches[row.comment.id];
    if (row.controls) { rows.push(row); continue; }
    if (seen.has(row.comment.id)) continue;
    seen.add(row.comment.id); rows.push(row);
    if (branch?.open) {
      stack.push({ ...row, controls: true });
      for (const child of branch.items.slice().reverse()) stack.push({ comment: child, depth: row.depth + 1, controls: false });
    }
  }
  function openFile(id: string) {
    const files = attachments(rows.filter(row => !row.controls).map(row => row.comment));
    const initial = files.findIndex(file => file.id === id);
    if (initial >= 0) setViewer({ files, initial });
  }
  return <><div className="table-scroll"><table>
    <thead><tr><th scope="col">{t.author}</th><th scope="col">{t.text}</th><th scope="col">{t.posted}</th></tr></thead>
    <tbody>{rows.map(({ comment, depth, controls }) => {
      const branch = branches[comment.id];
      if (controls) return <tr key={`${comment.id}-controls`} className="thread-controls"><td colSpan={3}>
        {branch.busy && <span role="status">{t.loadingReplies}</span>}
        {branch.failed && <span role="alert" className="error">{t.repliesError} <button className="secondary" onClick={() => void load(comment.id, branch.retryAfter)}>{t.retry}</button></span>}
        {!branch.busy && !branch.failed && branch.loaded && branch.items.length === 0 && <span>{t.noReplies}</span>}
        {!branch.busy && !branch.failed && branch.nextAfter && <button className="secondary" onClick={() => void load(comment.id, branch.nextAfter)}>{t.moreReplies}</button>}
      </td></tr>;
      const home = safeHomePage(comment.homePage);
      return <tr key={comment.id} data-comment-id={comment.id} data-depth={depth}>
        <td style={{ paddingInlineStart: `${12 + Math.min(depth, 6) * 10}px` }}>
          {depth > 0 && <small>{t.replyLevel} {depth}</small>}
          <strong dir="auto">{comment.userName}</strong><span className="author-email" dir="auto">{comment.email}</span>
          {home && <a href={home} target="_blank" rel="noopener noreferrer">{t.website} ↗</a>}</td>
        <td><div className="comment-text" dir="auto"><CommentText text={comment.text} /></div>
          {comment.image && <figure className="image-attachment"><button className="attachment-open" aria-label={`${t.viewerOpen}: ${comment.image.fileName}`} onClick={() => openFile(comment.image!.id)}>
            <img src={`/api/images/${encodeURIComponent(comment.image.id)}`} width={comment.image.width} height={comment.image.height} alt={comment.image.fileName} loading="lazy" />
          </button><figcaption>{comment.image.fileName} · {comment.image.width} × {comment.image.height}</figcaption></figure>}
          {comment.textFile && <p className="text-attachment"><button className="attachment-open" onClick={() => openFile(comment.textFile!.id)}>
            {comment.textFile.fileName}</button> · {comment.textFile.sizeBytes.toLocaleString()} {t.fileBytes}</p>}
          <div className="thread-actions"><button className="secondary" onClick={() => onReply(comment)}>{t.reply}</button>
            <button className="secondary" aria-expanded={branch?.open ?? false} onClick={() => toggle(comment.id)}>{branch?.open ? t.hideReplies : t.showReplies}</button></div>
        </td>
        <td><time dateTime={comment.createdAt}>{new Date(comment.createdAt).toLocaleString(language)}</time></td>
      </tr>;
    })}</tbody>
  </table></div>{viewer && <FileViewer files={viewer.files} initial={viewer.initial} client={fileClient} labels={t} onClose={() => setViewer(null)} />}</>;
}
