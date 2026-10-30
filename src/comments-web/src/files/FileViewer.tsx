import { useEffect, useRef, useState } from 'react';
import type { Messages } from '../messages';
import { fileUrl } from './model';
import type { Attachment, FileClient } from './model';

// The native modal owns background inertness, focus containment and Escape handling.
export function FileViewer({ files, initial, client, labels: t, onClose }: {
  files: Attachment[]; initial: number; client: FileClient; labels: Messages; onClose: () => void;
}) {
  const dialog = useRef<HTMLDialogElement>(null);
  const [index, setIndex] = useState(initial);
  const [attempt, setAttempt] = useState(0);
  const [content, setContent] = useState<{ index: number; attempt: number; text?: string; url?: string } | null>(null);
  const [failed, setFailed] = useState(false);
  const [closing, setClosing] = useState(false);
  const closeTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);
  const file = files[index];
  function close() {
    if (closeTimer.current !== undefined) return;
    setClosing(true);
    closeTimer.current = setTimeout(onClose, matchMedia('(prefers-reduced-motion: reduce)').matches ? 0 : 160);
  }
  useEffect(() => {
    const element = dialog.current!, opener = document.activeElement;
    const overflow = document.body.style.overflow;
    element.showModal(); document.body.style.overflow = 'hidden';
    return () => {
      clearTimeout(closeTimer.current); element.close(); document.body.style.overflow = overflow;
      if (opener instanceof HTMLElement && opener.isConnected) opener.focus();
    };
  }, []);
  useEffect(() => {
    const controller = new AbortController(); let url: string | undefined;
    setContent(null); setFailed(false);
    void client.load(file, controller.signal).then(value => {
      if (controller.signal.aborted) return;
      if (typeof value === 'string') setContent({ index, attempt, text: value });
      else { url = URL.createObjectURL(value); setContent({ index, attempt, url }); }
    }).catch(() => { if (!controller.signal.aborted) setFailed(true); });
    return () => { controller.abort(); if (url) URL.revokeObjectURL(url); };
  }, [file, index, attempt, client]);
  const ready = content?.index === index && content.attempt === attempt ? content : null;
  const move = (offset: number) => { if (!closing) setIndex(value => (value + offset + files.length) % files.length); };
  return <dialog ref={dialog} className={`file-viewer${closing ? ' closing' : ''}`} aria-labelledby="file-title" aria-describedby="file-help"
    onCancel={event => { event.preventDefault(); close(); }}
    onClick={event => { if (event.target === event.currentTarget) { const r = event.currentTarget.getBoundingClientRect(); if (event.clientX < r.left || event.clientX > r.right || event.clientY < r.top || event.clientY > r.bottom) close(); } }}
    onKeyDown={event => {
      if (event.key === 'Tab') {
        const targets = Array.from(event.currentTarget.querySelectorAll<HTMLElement>('button:not(:disabled), a[href]'));
        const first = targets[0], last = targets[targets.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
      }
      if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') { event.preventDefault(); move(event.key === 'ArrowLeft' ? -1 : 1); } }}>
    <div className="viewer-heading"><h2 id="file-title" dir="auto">{file.fileName}</h2><button autoFocus onClick={close}>{t.viewerClose}</button></div>
    <p id="file-help" className="muted">{t.viewerHelp}</p>
    <div className="viewer-actions">
      <button className="secondary" disabled={files.length < 2 || closing} onClick={() => move(-1)}>{t.viewerPrevious}</button>
      <span aria-live="polite">{index + 1} / {files.length}</span>
      <button className="secondary" disabled={files.length < 2 || closing} onClick={() => move(1)}>{t.viewerNext}</button>
      <a href={fileUrl(file, true)} download>{t.viewerDownload}</a>
    </div>
    <div className="viewer-content" aria-busy={!ready && !failed}>
      {failed ? <p role="alert">{t.viewerError} <button onClick={() => { setFailed(false); setAttempt(value => value + 1); }}>{t.retry}</button></p>
        : !ready ? <p role="status">{t.viewerLoading}</p>
        : ready.url ? <img key={ready.url} src={ready.url} alt={file.fileName} onError={() => setFailed(true)} />
        : <pre key={`${index}:${attempt}`} dir="auto">{ready.text}</pre>}
    </div>
  </dialog>;
}
