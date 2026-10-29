import { useRef, useState } from 'react';
import type { FormEvent } from 'react';
import type { Messages } from '../messages';
import { CommentsRequestError } from './client';
import { emptyInput, validate } from './model';
import type { Comment, CommentsClient, Field, FieldErrors, ValidationCode } from './model';

export function CommentForm({ client, labels: t, onCreated }: {
  client: CommentsClient; labels: Messages; onCreated: (comment: Comment) => void;
}) {
  const [input, setInput] = useState(emptyInput);
  const [errors, setErrors] = useState<Partial<Record<Field, ValidationCode>>>({});
  const [serverErrors, setServerErrors] = useState<FieldErrors>({});
  const [notice, setNotice] = useState<'success' | 'sendError' | 'validationError' | null>(null);
  const [sending, setSending] = useState(false);
  const locked = useRef(false);
  const form = useRef<HTMLFormElement>(null);

  function focusField(field?: string) {
    if (field) (form.current?.elements.namedItem(field) as HTMLElement | null)?.focus();
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (locked.current) return;
    const invalid = validate(input);
    setErrors(invalid); setServerErrors({}); setNotice(null);
    if (Object.keys(invalid).length) { setNotice('validationError'); focusField(Object.keys(invalid)[0]); return; }
    locked.current = true; setSending(true);
    try {
      const comment = await client.create(input);
      setInput(previous => ({ ...previous, text: '' }));
      setNotice('success'); onCreated(comment);
    } catch (error) {
      const fields = error instanceof CommentsRequestError ? error.fields : {};
      setServerErrors(fields);
      setNotice(Object.keys(fields).length ? 'validationError' : 'sendError');
      // Disabled fields can be focused after the pending state is released.
      requestAnimationFrame(() => focusField(Object.keys(fields)[0]));
    } finally { locked.current = false; setSending(false); }
  }

  const field = (name: Field, title: string, hint?: string) => {
    const error = errors[name] ? t[errors[name]!] : serverErrors[name]?.join(' ');
    const props = {
      id: name, name, value: input[name], disabled: sending,
      'aria-invalid': !!error, 'aria-describedby': `${name}-hint ${name}-error`,
      onChange: (event: { target: { value: string } }) => {
        setInput(previous => ({ ...previous, [name]: event.target.value }));
        setErrors(previous => ({ ...previous, [name]: undefined }));
        setServerErrors(previous => ({ ...previous, [name]: undefined })); setNotice(null);
      },
    };
    return <div className={`field field-${name}`} key={name}>
      <label htmlFor={name}>{title}{name !== 'homePage' && <span aria-hidden="true"> *</span>}</label>
      {name === 'text' ? <textarea {...props} rows={6} required maxLength={65536} dir="auto" /> :
        <input {...props} type={name === 'email' ? 'email' : name === 'homePage' ? 'url' : 'text'}
          required={name !== 'homePage'} maxLength={name === 'userName' ? 100 : name === 'email' ? 254 : 2048}
          autoComplete={name === 'userName' ? 'username' : name === 'email' ? 'email' : 'url'} />}
      <small id={`${name}-hint`}>{hint}</small>
      <small className="error" id={`${name}-error`}>{error}</small>
    </div>;
  };

  return <section className="panel" aria-labelledby="compose-title">
    <h2 id="compose-title">{t.compose}</h2>
    <form noValidate onSubmit={submit} ref={form} aria-busy={sending}>
      <div className="form-grid">{field('userName', t.name, t.nameHint)}{field('email', t.email, t.emailHint)}</div>
      {field('homePage', t.home)}{field('text', t.text, t.textHint)}
      <div className="form-bottom"><span className="counter">{input.text.length.toLocaleString()} / 65,536</span>
        <button type="submit" disabled={sending}>{sending ? t.sending : t.submit} <span aria-hidden="true">↗</span></button></div>
      <div role={notice === 'success' ? 'status' : 'alert'} className={notice === 'success' ? 'success' : 'error'}>{notice && t[notice]}</div>
    </form>
  </section>;
}
