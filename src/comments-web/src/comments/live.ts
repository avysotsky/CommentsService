export type LiveState = 'connecting' | 'connected' | 'offline' | 'disabled';
type Socket = Pick<WebSocket, 'onopen' | 'onclose' | 'onerror' | 'onmessage' | 'close'>;
type Options = { enabled: boolean; delay: number; maximumDelay: number };
type Callbacks = { changed(): void; ready(): void; state(value: LiveState): void };
type Environment = {
  url: string; socket(url: string): Socket;
  later(action: () => void, delay: number): ReturnType<typeof setTimeout>;
  cancel(timer: ReturnType<typeof setTimeout>): void;
};

export function connectComments(callbacks: Callbacks, options: Options, environment: Environment = {
  url: `${location.protocol === 'https:' ? 'wss:' : 'ws:'}//${location.host}/api/comments/live`,
  socket: url => new WebSocket(url), later: (action, delay) => setTimeout(action, delay), cancel: timer => clearTimeout(timer),
}) {
  if (!options.enabled) { callbacks.state('disabled'); return () => {}; }
  let stopped = false, delay = options.delay;
  let socket: Socket | undefined, timer: ReturnType<typeof setTimeout> | undefined;
  function schedule() {
    if (stopped || timer !== undefined) return;
    callbacks.state('offline');
    timer = environment.later(() => { timer = undefined; open(); }, delay);
    delay = Math.min(options.maximumDelay, delay * 2);
  }
  function open() {
    if (stopped) return;
    callbacks.state('connecting');
    try {
      const connection = environment.socket(environment.url); socket = connection;
      connection.onmessage = event => {
        if (stopped || socket !== connection || typeof event.data !== 'string') return;
        try {
          const message = JSON.parse(event.data);
          if (message?.type === 'connected') {
            delay = options.delay; callbacks.state('connected'); callbacks.ready();
          } else if (message?.type === 'commentsChanged') callbacks.changed();
        } catch { /* Unknown/malformed hints never become rendered HTML. */ }
      };
      connection.onclose = () => { if (socket === connection) { socket = undefined; schedule(); } };
      connection.onerror = () => { connection.close(); };
    } catch { schedule(); }
  }
  open();
  return () => {
    stopped = true;
    if (timer !== undefined) environment.cancel(timer);
    if (socket) { socket.onmessage = socket.onclose = socket.onerror = null; socket.close(); }
  };
}
