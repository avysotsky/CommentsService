import { limits } from '../runtimeConfig.ts';
import { fieldError } from './validation';
import { useRef, useState } from 'react';
import type { FormEvent } from 'react';
import { useCaptcha } from './useCaptcha';
import { CommentText } from './CommentText';
import type { Messages } from '../messages';
import { CommentsRequestError } from './client';
import { emptyInput, validate } from './model';
import type { Comment, CommentsClient, Field, FieldErrors, ValidationCode } from './model';

export function CommentForm({ client, labels: t, onCreated, replyTo, onCancelReply }: {
  client: CommentsClient; labels: Messages; onCreated: (comment: Comment) => void;
  replyTo: Comment | null; onCancelReply: () => void;
}) {
  const [textFile, setTextFile] = useState<File | null>(null);
  const [textFileError, setTextFileError] = useState(false);
  const textFileInput = useRef<HTMLInputElement>(null);
  const [image, setImage] = useState<File | null>(null);
  const [imageError, setImageError] = useState(false);
  const imageInput = useRef<HTMLInputElement>(null);
  const captcha = useCaptcha(client);
  const [input, setInput] = useState(emptyInput);
  const [errors, setErrors] = useState<Partial<Record<Field, ValidationCode>>>({});
  const [serverErrors, setServerErrors] = useState<FieldErrors>({});
  const [notice, setNotice] = useState<'success' | 'sendError' | 'validationError' | null>(null);
  const [sending, setSending] = useState(false);
  const [preview, setPreview] = useState<{ source: string; html: string } | null>(null);
  const [previewing, setPreviewing] = useState(false);
  const [previewError, setPreviewError] = useState(false);
  const previewRequest = useRef<AbortController | null>(null);
  const textVersion = useRef(0);
  const locked = useRef(false);
  const form = useRef<HTMLFormElement>(null);

  function focusField(field?: string) {
    if (field) (form.current?.elements.namedItem(field) as HTMLElement | null)?.focus();
  }

  function changedText() {
    textVersion.current++; previewRequest.current?.abort(); setPreview(null); setPreviewError(false); setPreviewing(false);
  }

  function format(tag: 'strong' | 'i' | 'code' | 'a') {
    const textarea = form.current?.elements.namedItem('text') as HTMLTextAreaElement;
    const start = textarea.selectionStart, end = textarea.selectionEnd;
    const open = tag === 'a' ? '<a href="https://example.com/" title="">' : `<${tag}>`;
    const close = `</${tag}>`;
    const text = input.text.slice(0, start) + open + input.text.slice(start, end) + close + input.text.slice(end);
    if (text.length > limits.textMaxLength) return;
    changedText(); setInput(previous => ({ ...previous, text })); setNotice(null);
    setErrors(previous => ({ ...previous, text: undefined })); setServerErrors(previous => ({ ...previous, text: undefined }));
    requestAnimationFrame(() => { textarea.focus(); textarea.setSelectionRange(start + open.length, end + open.length); });
  }

  async function showPreview() {
    previewRequest.current?.abort();
    const controller = new AbortController(); previewRequest.current = controller;
    const version = textVersion.current, source = input.text;
    setPreview(null); setPreviewError(false); setPreviewing(true);
    try {
      const result = await client.preview(source, controller.signal);
      if (version === textVersion.current && !controller.signal.aborted) setPreview({ source, html: result.html });
    } catch {
      if (!controller.signal.aborted) setPreviewError(true);
    } finally { if (previewRequest.current === controller) setPreviewing(false); }
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (locked.current) return;
    const invalid = validate(input);
    setErrors(invalid); setServerErrors({}); setNotice(null);
    if (Object.keys(invalid).length) { setNotice('validationError'); focusField(Object.keys(invalid)[0]); return; }
    if (image && (image.size === 0 || image.size > limits.imageUploadMaxBytes || !/\.(jpe?g|gif|png)$/i.test(image.name))) {
      setImageError(true); imageInput.current?.focus(); return;
    }
    if (textFile && (textFile.size === 0 || textFile.size > limits.textFileMaxBytes || !/\.txt$/i.test(textFile.name))) {
      setTextFileError(true); textFileInput.current?.focus(); return;
    }
    if (!captcha.challenge || captcha.expired || (captcha.code.trim().length !== limits.captchaCodeLength || [...captcha.code.trim().toUpperCase()].some(character => !limits.captchaAlphabet.includes(character)))) {
      captcha.setInvalid(true); document.getElementById('captchaCode')?.focus(); return;
    }
    captcha.setInvalid(false);
    locked.current = true; setSending(true);
    try {
      const comment = await client.create({ ...input, parentId: replyTo?.id ?? null, captchaId: captcha.challenge.id, captchaCode: captcha.code.trim(), image, textFile });
      changedText(); setInput(previous => ({ ...previous, text: '' })); setImage(null); setImageError(false);
      if (imageInput.current) imageInput.current.value = '';
      setTextFile(null); setTextFileError(false); if (textFileInput.current) textFileInput.current.value = '';
      setNotice('success'); onCreated(comment);
    } catch (error) {
      const fields = error instanceof CommentsRequestError ? error.fields : {};
      setServerErrors(fields); if (fields.image) setImageError(true); if (fields.textFile) setTextFileError(true); if (fields.captchaCode) captcha.setInvalid(true);
      setNotice(Object.keys(fields).length ? 'validationError' : 'sendError');
      // Disabled fields can be focused after the pending state is released.
      requestAnimationFrame(() => focusField(Object.keys(fields)[0]));
    } finally { locked.current = false; setSending(false); void captcha.renew(); }
  }

  const field = (name: Field, title: string, hint?: string) => {
    const error = fieldError(name, errors[name], serverErrors, t);
    const props = {
      id: name, name, value: input[name], disabled: sending,
      'aria-invalid': !!error, 'aria-describedby': `${name}-hint ${name}-error`,
      onChange: (event: { target: { value: string } }) => {
        if (name === 'text') changedText();
        setInput(previous => ({ ...previous, [name]: event.target.value }));
        setErrors(previous => ({ ...previous, [name]: undefined }));
        setServerErrors(previous => ({ ...previous, [name]: undefined })); setNotice(null);
      },
    };
    return <div className={`field field-${name}`} key={name}>
      <label htmlFor={name}>{title}{name !== 'homePage' && <span aria-hidden="true"> *</span>}</label>
      {name === 'text' ? <textarea {...props} rows={6} required maxLength={limits.textMaxLength} dir="auto" /> :
        <input {...props} type={name === 'email' ? 'email' : name === 'homePage' ? 'url' : 'text'}
          required={name !== 'homePage'} maxLength={name === 'userName' ? limits.userNameMaxLength : name === 'email' ? limits.emailMaxLength : limits.homePageMaxLength}
          dir={name === 'userName' ? 'auto' : 'ltr'} autoCapitalize="none" spellCheck={false}
          autoComplete={name === 'userName' ? 'username' : name === 'email' ? 'email' : 'url'} />}
      <small id={`${name}-hint`}>{hint}</small>
      <small className="error" id={`${name}-error`}>{error}</small>
    </div>;
  };

  return <section className="panel" aria-labelledby="compose-title">
    <h2 id="compose-title">{t.compose}</h2>
    <p className="muted required-hint">{t.requiredHint}</p>
    <form noValidate onSubmit={submit} ref={form} aria-busy={sending}>
      {replyTo && <div className="reply-target" role="status">
        <span>{t.replyingTo} <strong dir="auto">{replyTo.userName}</strong></span>
        <button type="button" className="secondary" disabled={sending} onClick={onCancelReply}>{t.cancelReply}</button>
      </div>}
      {serverErrors.parentId && <p role="alert" className="error">{t.replyTargetError}</p>}
      <div className="form-grid">{field('userName', t.name, t.nameHint)}{field('email', t.email, t.emailHint)}</div>
      {field('homePage', t.home, t.homeHint)}
      <div className="editor-toolbar" role="group" aria-label={t.formatting}>
        {(['strong', 'i', 'code', 'a'] as const).map(tag => <button key={tag} type="button" disabled={sending}
          onMouseDown={event => event.preventDefault()} onClick={() => format(tag)}>{t[tag]}</button>)}
        <button type="button" disabled={sending || previewing} onClick={showPreview}>{previewing ? t.previewing : t.preview}</button>
      </div>
      {field('text', t.text, t.textHint)}
      {previewError && <p role="alert" className="error">{t.previewError}</p>}
      {preview && preview.source === input.text && <section className="comment-preview" aria-label={t.preview}>
        <h3>{t.preview}</h3><div className="comment-text" dir="auto"><CommentText text={preview.html} validated /></div>
      </section>}
      <div className="field image-field">
        <label htmlFor="image">{t.imageLabel}</label>
        <input ref={imageInput} id="image" name="image" type="file" accept=".jpg,.jpeg,.gif,.png,image/jpeg,image/gif,image/png" disabled={sending}
          aria-invalid={imageError} aria-describedby="image-hint image-error" onChange={event => {
            setImage(event.target.files?.[0] ?? null); setImageError(false); setNotice(null);
            setTextFile(null); setTextFileError(false); if (textFileInput.current) textFileInput.current.value = '';
          }} />
        <small id="image-hint">{t.imageHint}</small>
        {image && <button type="button" className="secondary" disabled={sending} onClick={() => { setImage(null); setImageError(false); if (imageInput.current) imageInput.current.value = ''; }}>{t.imageRemove}</button>}
        <small id="image-error" className="error" role="alert">{imageError ? t.imageError : ''}</small>
      </div>
      <div className="field text-file-field">
        <label htmlFor="textFile">{t.textFileLabel}</label>
        <input ref={textFileInput} id="textFile" name="textFile" type="file" accept=".txt,text/plain" disabled={sending}
          aria-invalid={textFileError} aria-describedby="textFile-hint textFile-error" onChange={event => {
            setTextFile(event.target.files?.[0] ?? null); setTextFileError(false); setNotice(null);
            setImage(null); setImageError(false); if (imageInput.current) imageInput.current.value = '';
          }} />
        <small id="textFile-hint">{t.textFileHint}</small>
        {textFile && <button type="button" className="secondary" disabled={sending} onClick={() => {
          setTextFile(null); setTextFileError(false); setNotice(null); if (textFileInput.current) textFileInput.current.value = '';
        }}>{t.textFileRemove}</button>}
        <small id="textFile-error" className="error" role="alert">{textFileError ? t.textFileError : ''}</small>
      </div>
      <div className="field captcha-field">
        <label htmlFor="captchaCode">{t.captchaLabel} <span aria-hidden="true">*</span></label>
        <div className="captcha-image-row">
          {captcha.challenge && <img width={limits.captchaCodeLength * 30 + 20} height={64} src={captcha.challenge.imageDataUrl} alt={t.captchaImage} />}
          <button type="button" className="secondary" disabled={sending || captcha.busy} onClick={() => { captcha.setInvalid(false); void captcha.renew(); }}>{t.captchaRefresh}</button>
        </div>
        <span role="status">{captcha.busy ? t.captchaLoading : ''}</span>
        <input id="captchaCode" name="captchaCode" value={captcha.code} maxLength={limits.captchaCodeLength} required autoComplete="off" spellCheck={false} dir="ltr" autoCapitalize="characters"
          disabled={sending || captcha.busy || !captcha.challenge} aria-invalid={captcha.invalid || captcha.expired}
          aria-describedby="captcha-hint captcha-error" onChange={event => { captcha.setCode(event.target.value); captcha.setInvalid(false); setNotice(null); }} />
        <small id="captcha-hint">{t.captchaHint}</small>
        <small id="captcha-error" className="error" role="alert">{captcha.failed ? t.captchaUnavailable : captcha.expired ? t.captchaExpired : captcha.invalid ? t.captchaInvalid : ''}</small>
      </div>
      <div className="form-bottom"><span className="counter"><span className="sr-only">{t.characterCount}: </span>{input.text.length.toLocaleString()} / {limits.textMaxLength.toLocaleString()}</span>
        <button type="submit" disabled={sending || captcha.busy || !captcha.challenge || captcha.expired}>{sending ? t.sending : t.submit} <span aria-hidden="true">↗</span></button></div>
      <div role={notice === 'success' ? 'status' : 'alert'} className={notice === 'success' ? 'success' : 'error'}>{notice && t[notice]}</div>
    </form>
  </section>;
}
