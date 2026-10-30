import type { Comment } from '../comments/model';
export type Attachment = { id: string; fileName: string; kind: 'image' | 'text' };
export interface FileClient { load(file: Attachment, signal: AbortSignal): Promise<Blob | string>; }
export function attachments(comments: Comment[]): Attachment[] {
  return comments.flatMap<Attachment>(c => c.image ? [{ ...c.image, kind: 'image' as const }] : c.textFile ? [{ ...c.textFile, kind: 'text' as const }] : []);
}
export function fileUrl(file: Attachment, download = false): string {
  if (!/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(file.id)) throw new Error('Invalid attachment ID');
  return `/api/${file.kind === 'image' ? 'images' : 'text-files'}/${file.id}${download && file.kind === 'image' ? '?download=true' : ''}`;
}
