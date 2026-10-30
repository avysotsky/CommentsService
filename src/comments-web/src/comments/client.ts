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

  async latest(signal?: AbortSignal): Promise<Comment[]> {
    return this.read(await this.request('/api/comments?limit=25', { signal, cache: 'no-store' }));
  }

  async create(input: CommentInput): Promise<Comment> {
    return this.read(await this.request('/api/comments', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...input, userName: input.userName.trim(), email: input.email.trim(), homePage: input.homePage.trim() || null }),
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
      for (const key of ['userName', 'email', 'homePage', 'text'] as const) {
        const messages: unknown = problem?.errors?.[key];
        if (Array.isArray(messages) && messages.every(message => typeof message === 'string')) fields[key] = messages;
      }
    }
    throw new CommentsRequestError(response.status, fields);
  }
}
