import { test } from 'node:test';
import assert from 'node:assert/strict';
import { validate, safeHomePage, readRootQuery } from '../src/comments/model.ts';
import { HttpCommentsClient, CommentsRequestError } from '../src/comments/client.ts';

const valid = { userName: 'Reader123', email: 'reader@example.test', homePage: '', text: 'Привіт 😀\n1 < 2' };
test('default HTTP transport does not bind fetch to the client instance', async () => {
  const original = globalThis.fetch;
  globalThis.fetch = async function () {
    assert(!(this instanceof HttpCommentsClient));
    return Response.json([]);
  };
  try { assert.deepEqual(await new HttpCommentsClient().latest(), []); }
  finally { globalThis.fetch = original; }
});
test('plain Unicode input and trimmed author fields are valid', () => {
  assert.deepEqual(validate({ ...valid, userName: ' Reader123 ', email: ' reader@example.test ' }), {});
});
test('required and bounded inputs reject invalid names, email, markup and text', () => {
  for (const userName of ['', 'a b', 'Ж', 'a'.repeat(101)]) assert.equal(validate({ ...valid, userName }).userName, 'nameError');
  for (const email of ['', 'x', 'Name <a@b.test>', 'a\n@b.test']) assert.equal(validate({ ...valid, email }).email, 'emailError');
  for (const text of ['', '   ', '\0', '\ud800', 'a'.repeat(65537)]) assert.equal(validate({ ...valid, text }).text, 'textError');
  for (const text of ['<script>x</script>', '<b>hi</b>', '<!-- hi -->', '<Ж>']) assert.deepEqual(validate({ ...valid, text }), {});
  assert.deepEqual(validate({ ...valid, text: 'a'.repeat(65536) }), {});
});
test('homepage links restrict schemes, credentials and controls', () => {
  for (const homePage of ['javascript:alert(1)', 'data:text/html,x', 'https://user:pass@example.test', 'https://example.test/\nx', 'relative']) {
    assert.equal(validate({ ...valid, homePage }).homePage, 'homeError');
    assert.equal(safeHomePage(homePage), undefined);
  }
  assert.equal(safeHomePage('https://example.test/path'), 'https://example.test/path');
});
test('HTTP client sends only normalized public input and retains text whitespace', async () => {
  const client = new HttpCommentsClient(async (url, options) => {
    assert.equal(url, '/api/comments'); assert.equal(options.method, 'POST');
    assert.deepEqual(JSON.parse(options.body), { ...valid, homePage: null, text: '  hello\n' });
    return Response.json({ id: 'saved' }, { status: 201 });
  });
  assert.deepEqual(await client.create({ ...valid, userName: ' Reader123 ', text: '  hello\n' }), { id: 'saved' });
});
test('list request propagates abort and bypasses browser cache', async () => {
  const controller = new AbortController();
  const client = new HttpCommentsClient(async (url, options) => {
    assert.equal(url, '/api/comments?limit=25'); assert.equal(options.signal, controller.signal);
    assert.equal(options.cache, 'no-store'); return Response.json([]);
  });
  assert.deepEqual(await client.latest(controller.signal), []);
});
test('validation response exposes known fields only, unavailable errors stay generic', async () => {
  const client = new HttpCommentsClient(async () => Response.json({ errors: { text: ['Rejected'], connection: ['secret'], email: 5 } }, { status: 400 }));
  await assert.rejects(client.create(valid), error => error instanceof CommentsRequestError && JSON.stringify(error.fields) === '{"text":["Rejected"]}');
  const unavailable = new HttpCommentsClient(async () => new Response('private provider details', { status: 503 }));
  await assert.rejects(unavailable.latest(), error => error.status === 503 && !error.message.includes('private'));
});

test('preview sends text only and propagates cancellation and validation', async () => {
  const controller = new AbortController();
  const client = new HttpCommentsClient(async (url, options) => {
    assert.equal(url, '/api/comments/preview');
    assert.equal(options.signal, controller.signal);
    assert.deepEqual(JSON.parse(options.body), { text: '<strong>x</strong>' });
    return Response.json({ html: '<strong>x</strong>' });
  });
  assert.deepEqual(await client.preview('<strong>x</strong>', controller.signal), { html: '<strong>x</strong>' });
});

test('reply client encodes parent and cursor, aborts and bypasses cache', async () => {
  const cancel = new AbortController();
  const client = new HttpCommentsClient(async (url, options) => {
    assert.equal(url, '/api/comments/parent/replies?limit=25&after=cursor');
    assert.equal(options.signal, cancel.signal); assert.equal(options.cache, 'no-store');
    return Response.json({ items: [], nextAfter: null });
  });
  assert.deepEqual(await client.replies('parent', 'cursor', cancel.signal), { items: [], nextAfter: null });
});
test('create carries only requested parent and exposes missing-target errors', async () => {
  const client = new HttpCommentsClient(async (url, options) => {
    assert.equal(JSON.parse(options.body).parentId, 'parent');
    return Response.json({ errors: { parentId: ['Not found'] } }, { status: 400 });
  });
  await assert.rejects(client.create({ ...valid, parentId: 'parent' }), error => error.fields.parentId[0] === 'Not found');
});

test('page transport sends bounded query fields and cancellation without cache', async () => {
  const controller = new AbortController();
  const client = new HttpCommentsClient(async (url, options) => {
    assert.equal(url, '/api/comments/page?page=2&sort=email&direction=asc');
    assert.equal(options.signal, controller.signal); assert.equal(options.cache, 'no-store');
    return Response.json({ items: [], page: 2, pageSize: 25, totalCount: 0, totalPages: 0 });
  });
  assert.equal((await client.page({ page: 2, sort: 'email', direction: 'asc' }, controller.signal)).page, 2);
});
test('bookmarked paging input validates ranges and allowlists', () => {
  assert.deepEqual(readRootQuery('?page=2&sort=userName&direction=asc'), { page: 2, sort: 'userName', direction: 'asc' });
  for (const page of ['-1', '0', '1.5', 'NaN', '85899346']) assert.equal(readRootQuery('?page=' + page).page, 1);
  assert.deepEqual(readRootQuery('?sort=drop&direction=evil'), { page: 1, sort: 'createdAt', direction: 'desc' });
});
