import assert from 'node:assert/strict';
import { test } from 'node:test';
import { connectComments } from '../src/comments/live.ts';

function harness(enabled = true) {
  const sockets = [], timers = [], states = []; let ready = 0, changed = 0;
  const stop = connectComments({ ready: () => ready++, changed: () => changed++, state: state => states.push(state) },
    { enabled, delay: 100, maximumDelay: 400 }, {
      url: 'wss://example.test/api/comments/live', socket: url => {
        assert.equal(url, 'wss://example.test/api/comments/live');
        const socket = { close() { this.closed = true; this.onclose?.(); } }; sockets.push(socket); return socket;
      }, later: (fn, delay) => { const timer = { fn, delay }; timers.push(timer); return timer; },
      cancel: timer => { timer.cancelled = true; },
    });
  return { sockets, timers, states, stop, get ready() { return ready; }, get changed() { return changed; },
    retry() { const timer = timers.shift(); timer.fn(); return timer.delay; } };
}

test('ready and change hints are separate; malformed/unrecognised data is ignored', () => {
  const h = harness(); const socket = h.sockets[0];
  socket.onmessage({ data: '{broken' }); socket.onmessage({ data: 'null' }); socket.onmessage({ data: '<script>' });
  assert.equal(h.changed, 0);
  socket.onmessage({ data: '{"type":"connected"}' }); assert.equal(h.ready, 1);
  socket.onmessage({ data: '{"type":"commentsChanged"}' }); assert.equal(h.changed, 1);
  assert.equal(h.states.at(-1), 'connected'); h.stop();
});

test('reconnect backs off, caps delay and resets after server handshake', () => {
  const h = harness();
  h.sockets.at(-1).close(); assert.equal(h.retry(), 100);
  h.sockets.at(-1).close(); assert.equal(h.retry(), 200);
  h.sockets.at(-1).close(); assert.equal(h.retry(), 400);
  h.sockets.at(-1).close(); assert.equal(h.retry(), 400);
  h.sockets.at(-1).onmessage({ data: '{"type":"connected"}' });
  assert.equal(h.ready, 1);
  h.sockets.at(-1).close(); assert.equal(h.retry(), 100); h.stop();
});

test('cleanup closes socket/cancels retry; disabled mode never connects', () => {
  const h = harness(); h.sockets[0].close(); const timer = h.timers[0]; h.stop();
  assert.equal(timer.cancelled, true); timer.fn(); assert.equal(h.sockets.length, 1);
  const disabled = harness(false); assert.equal(disabled.sockets.length, 0); assert.deepEqual(disabled.states, ['disabled']);
});
