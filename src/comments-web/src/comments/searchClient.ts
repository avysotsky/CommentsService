import type { Comment } from './model';
export type SearchPage = { items: Comment[]; page: number; pageSize: number; hasMore: boolean };
export class HttpSearchClient {
  private readonly request: typeof fetch;
  constructor(request: typeof fetch = (...args) => fetch(...args)) { this.request = request; }
  async search(query: string, page: number, signal?: AbortSignal): Promise<SearchPage> {
    return this.read(await this.request('/api/comments/search?' + new URLSearchParams({ query, page: String(page) }), { signal, cache: 'no-store' }));
  }
  async root(id: string, signal?: AbortSignal): Promise<Comment> {
    return this.read(await this.request('/api/comments/' + encodeURIComponent(id), { signal, cache: 'no-store' }));
  }
  private async read<T>(response: Response): Promise<T> {
    if (!response.ok) throw new Error('Search unavailable');
    return response.json() as Promise<T>;
  }
}
