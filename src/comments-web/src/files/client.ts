import { fileUrl } from './model.ts';
import type { Attachment, FileClient } from './model';
export class HttpFileClient implements FileClient {
  private readonly request: typeof fetch;
  constructor(request: typeof fetch = (...args) => fetch(...args)) { this.request = request; }
  async load(file: Attachment, signal: AbortSignal): Promise<Blob | string> {
    const response = await this.request(fileUrl(file), { signal, cache: 'no-store' });
    if (!response.ok) throw new Error('File unavailable');
    const expected = file.kind === 'image' ? 'image/png' : 'text/plain';
    if (response.headers.get('content-type')?.split(';')[0].trim() !== expected) { await response.body?.cancel(); throw new Error('Invalid file type'); }
    const limit = file.kind === 'image' ? 5 * 1024 * 1024 : 102400;
    const reader = response.body?.getReader();
    if (!reader) throw new Error('Empty file');
    const chunks: Uint8Array[] = []; let length = 0;
    try {
      for (;;) {
        const { done, value } = await reader.read(); if (done) break;
        length += value.byteLength;
        if (length > limit) throw new Error('File too large');
        chunks.push(value);
      }
    } finally { await reader.cancel(); reader.releaseLock(); }
    if (!length) throw new Error('Empty file');
    const data = new Uint8Array(length); let offset = 0;
    for (const chunk of chunks) { data.set(chunk, offset); offset += chunk.length; }
    return file.kind === 'image' ? new Blob([data], { type: expected }) : new TextDecoder('utf-8', { fatal: true }).decode(data);
  }
}
