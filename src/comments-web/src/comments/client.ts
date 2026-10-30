import type { Comment, CommentInput, CommentsClient, FieldErrors } from './model';

export class CommentsRequestError extends Error {
  readonly status: number;
  readonly fields: FieldErrors;
  constructor(status: number, fields: FieldErrors = {}) {
    super('Comments request failed');
    this.status = status;
    this.fields = fields;
  }
}

export class HttpCommentsClient implements CommentsClient {
  private readonly request: typeof fetch;
  constructor(request: typeof fetch = (...args) => fetch(...args)) { this.request = request; }

  async captcha(replace?: string, signal?: AbortSignal) {
    const query = replace ? '?' + new URLSearchParams({ replace }) : '';
    return this.read<import('./model').CaptchaChallenge>(await this.request('/api/captcha' + query, { signal, cache: 'no-store' }));
  }

  async page(query: import('./model').RootPageQuery, signal?: AbortSignal) {
    const params = new URLSearchParams({ page: String(query.page), sort: query.sort, direction: query.direction });
    return this.read<import('./model').RootPage>(await this.request(`/api/comments/page?${params}`, { signal, cache: 'no-store' }));
  }

  async replies(parentId: string, after?: string | null, signal?: AbortSignal) {
    const query = new URLSearchParams({ limit: '25' });
    if (after) query.set('after', after);
    return this.read<import('./model').RepliesPage>(await this.request(`/api/comments/${encodeURIComponent(parentId)}/replies?${query}`, { signal, cache: 'no-store' }));
  }

  async latest(signal?: AbortSignal): Promise<Comment[]> {
    return this.read(await this.request('/api/comments?limit=25', { signal, cache: 'no-store' }));
  }

  async create(input: CommentInput): Promise<Comment> {
    const { image, textFile, ...values } = input;
    if (image && textFile) throw new CommentsRequestError(400, { textFile: ['Choose either an image or TXT.'] });
    const normalized = { ...values, userName: values.userName.trim(), email: values.email.trim(), homePage: values.homePage.trim() || null };
    if (image || textFile) {
      const body = new FormData();
      for (const [key, value] of Object.entries(normalized)) if (value !== null && value !== undefined) body.append(key, String(value));
      if (image) body.append('image', image, image.name);
      if (textFile) body.append('textFile', textFile, textFile.name);
      return this.read(await this.request('/api/comments', { method: 'POST', body }));
    }
    return this.read(await this.request('/api/comments', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(normalized),
    }));
  }

  async preview(text: string, signal?: AbortSignal): Promise<{ html: string }> {
    return this.read(await this.request('/api/comments/preview', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ text }), signal,
    }));
  }

  private async read<T>(response: Response): Promise<T> {
    if (response.ok) return response.json() as Promise<T>;
    const fields: FieldErrors = {};
    if (response.status === 400) {
      const problem = await response.json().catch(() => null);
      for (const key of ['userName', 'email', 'homePage', 'text', 'parentId', 'captchaCode', 'image', 'textFile'] as const) {
        const messages: unknown = problem?.errors?.[key];
        if (Array.isArray(messages) && messages.every(message => typeof message === 'string')) fields[key] = messages;
      }
    }
    throw new CommentsRequestError(response.status, fields);
  }
}
