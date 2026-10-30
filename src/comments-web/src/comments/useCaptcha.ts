import { useCallback, useEffect, useRef, useState } from 'react';
import type { CaptchaChallenge, CommentsClient } from './model';

export function useCaptcha(client: CommentsClient) {
  const [challenge, setChallenge] = useState<CaptchaChallenge | null>(null);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(true);
  const [failed, setFailed] = useState(false);
  const [invalid, setInvalid] = useState(false);
  const [now, setNow] = useState(Date.now());
  const request = useRef<AbortController | null>(null), previousId = useRef<string | undefined>(undefined);
  const renew = useCallback(async () => {
    request.current?.abort(); const controller = new AbortController(); request.current = controller;
    setBusy(true); setFailed(false); setChallenge(null); setCode('');
    try {
      const next = await client.captcha(previousId.current, controller.signal);
      if (!controller.signal.aborted) { previousId.current = next.id; setChallenge(next); setNow(Date.now()); }
    } catch { if (!controller.signal.aborted) setFailed(true); }
    finally { if (!controller.signal.aborted) setBusy(false); }
  }, [client]);
  useEffect(() => { void renew(); return () => request.current?.abort(); }, [renew]);
  useEffect(() => { const timer = setInterval(() => setNow(Date.now()), 1000); return () => clearInterval(timer); }, []);
  const expired = !!challenge && new Date(challenge.expiresAt).getTime() <= now;
  return { challenge, code, setCode, busy, failed, invalid, setInvalid, expired, renew };
}
