import assert from 'node:assert/strict';
import { test } from 'node:test';
import { defaults } from './configure.mjs';
import { configure, limits, loadConfiguration } from '../src/runtimeConfig.ts';
import { validate, readRootQuery } from '../src/comments/model.ts';
import { configuredMessages } from '../src/messages.ts';
import { HttpCommentsClient } from '../src/comments/client.ts';

test('runtime configuration controls validation, query limits and translated labels together', async () => {
  try {
    await loadConfiguration(async (url, options) => {
      assert.equal(url, '/api/config'); assert.equal(options.cache, 'no-store');
      return Response.json({ ...defaults, rootPageSize: 7, defaultListLimit: 3, userNameMaxLength: 4, textMaxLength: 12, captchaCodeLength: 8 });
    });
    assert.equal(limits.rootPageSize, 7);
    assert.equal(validate({ userName: 'LongName', email: 'a@b.test', homePage: '', text: 'hello' }).userName, 'nameError');
    assert.equal(validate({ userName: 'Name', email: 'a@b.test', homePage: '', text: 'a'.repeat(13) }).text, 'textError');
    assert.equal(readRootQuery('?page=100000000').page, 100000000);
    for (const language of ['en', 'ru', 'uk']) {
      const labels = configuredMessages(language);
      assert.match(labels.window, /7/); assert.match(labels.nameHint, /4/);
      assert.doesNotMatch(JSON.stringify(labels), /\{\w+\}/);
    }
    const client = new HttpCommentsClient(async url => { assert.match(url, /limit=3/); return Response.json([]); });
    await client.latest();
  } finally { configure(defaults); }
});

test('unavailable or malformed configuration fails instead of silently using different values', async () => {
  await assert.rejects(loadConfiguration(async () => new Response('', { status: 503 })));
  await assert.rejects(loadConfiguration(async () => Response.json({ rootPageSize: 7 })));
  assert.throws(() => configure({ ...defaults, textMaxLength: 0 }));
});
