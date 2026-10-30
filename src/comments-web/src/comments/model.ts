export type CommentInput = { userName: string; email: string; homePage: string; text: string; parentId?: string | null };
export type Comment = Omit<CommentInput, 'homePage' | 'parentId'> & { id: string; homePage: string | null; createdAt: string; parentId: string | null; rootId: string };
export type Field = 'userName' | 'email' | 'homePage' | 'text';
export type FieldErrors = Partial<Record<Field | 'parentId', string[]>>;
export type RepliesPage = { items: Comment[]; nextAfter: string | null };
export type RootSort = 'userName' | 'email' | 'createdAt';
export type RootPageQuery = { page: number; sort: RootSort; direction: 'asc' | 'desc' };
export type RootPage = { items: Comment[]; page: number; pageSize: number; totalCount: number; totalPages: number };
export const defaultRootQuery: RootPageQuery = { page: 1, sort: 'createdAt', direction: 'desc' };
export function readRootQuery(search: string): RootPageQuery {
  const params = new URLSearchParams(search), page = Number(params.get('page') ?? 1);
  const sort = params.get('sort'), direction = params.get('direction');
  return { page: Number.isInteger(page) && page >= 1 && page <= 85899345 ? page : 1,
    sort: sort === 'userName' || sort === 'email' ? sort : 'createdAt', direction: direction === 'asc' ? 'asc' : 'desc' };
}
export interface CommentsClient {
  page(query: RootPageQuery, signal?: AbortSignal): Promise<RootPage>;
  replies(parentId: string, after?: string | null, signal?: AbortSignal): Promise<RepliesPage>;
  latest(signal?: AbortSignal): Promise<Comment[]>;
  create(input: CommentInput): Promise<Comment>;
  preview(text: string, signal?: AbortSignal): Promise<{ html: string }>;
}

export const emptyInput = (): CommentInput => ({ userName: '', email: '', homePage: '', text: '' });

export function safeHomePage(value: string | null): string | undefined {
  if (!value || /[\u0000-\u0020\u007f]/u.test(value)) return undefined;
  try {
    const url = new URL(value);
    return ['http:', 'https:'].includes(url.protocol) && !url.username && !url.password ? url.href : undefined;
  } catch { return undefined; }
}

export type ValidationCode = 'nameError' | 'emailError' | 'homeError' | 'textError' | 'plainError';
export function validate(input: CommentInput): Partial<Record<Field, ValidationCode>> {
  const errors: Partial<Record<Field, ValidationCode>> = {};
  const name = input.userName.trim(), email = input.email.trim(), home = input.homePage.trim();
  if (!/^[A-Za-z0-9]{1,100}$/.test(name)) errors.userName = 'nameError';
  if (email.length > 254 || !/^[^\s<>@\u0000-\u001f\u007f]+@[^\s<>@\u0000-\u001f\u007f]+$/u.test(email)) errors.email = 'emailError';
  if (home && (home.length > 2048 || !safeHomePage(home))) errors.homePage = 'homeError';
  if (!input.text.trim() || input.text.length > 65536 || input.text.includes('\0') || !input.text.isWellFormed()) errors.text = 'textError';

  return errors;
}
