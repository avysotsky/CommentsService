export type PublicConfiguration = {
  rootPageSize: number; defaultListLimit: number; maximumListLimit: number;
  userNameMaxLength: number; emailMaxLength: number; homePageMaxLength: number; textMaxLength: number;
  imageUploadMaxBytes: number; imageMaxWidth: number; imageMaxHeight: number; textFileMaxBytes: number;
  captchaCodeLength: number; captchaAlphabet: string; captchaLifetimeSeconds: number;
  realtimeEnabled: boolean; reconnectDelayMilliseconds: number; maximumReconnectDelayMilliseconds: number;
};

let current: PublicConfiguration | undefined;
export function configure(value: PublicConfiguration) {
  const keys: (keyof PublicConfiguration)[] = ['rootPageSize', 'defaultListLimit', 'maximumListLimit',
    'userNameMaxLength', 'emailMaxLength', 'homePageMaxLength', 'textMaxLength',
    'imageUploadMaxBytes', 'imageMaxWidth', 'imageMaxHeight', 'textFileMaxBytes', 'captchaCodeLength', 'captchaLifetimeSeconds',
    'reconnectDelayMilliseconds', 'maximumReconnectDelayMilliseconds'];
  if (!value || keys.some(key => !Number.isSafeInteger(value[key]) || Number(value[key]) < 1) ||
      typeof value.realtimeEnabled !== 'boolean' || value.maximumReconnectDelayMilliseconds < value.reconnectDelayMilliseconds ||
      typeof value.captchaAlphabet !== 'string' || !/^[A-Z0-9]+$/.test(value.captchaAlphabet))
    throw new Error('Invalid public configuration');
  current = Object.freeze({ ...value });
}

// Initialized before rendering; failed loading never silently uses different limits.
export const limits = new Proxy({} as PublicConfiguration, {
  get: (_, key: keyof PublicConfiguration) => {
    if (!current) throw new Error('Configuration not loaded');
    return current[key];
  },
});

export async function loadConfiguration(request: typeof fetch = (...args) => fetch(...args)) {
  const response = await request('/api/config', { cache: 'no-store', signal: AbortSignal.timeout(10000) });
  if (!response.ok) throw new Error('Configuration unavailable');
  configure(await response.json());
}
