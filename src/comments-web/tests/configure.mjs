import { readFileSync } from 'node:fs';
import { configure } from '../src/runtimeConfig.ts';
const { Runtime: { Limits: limits, Captcha: captcha } } = JSON.parse(readFileSync(new URL('../../CommentsService.Api/config.json', import.meta.url)));
export const defaults = Object.fromEntries(Object.entries(limits).map(([key, value]) => [key[0].toLowerCase() + key.slice(1), value]));
defaults.captchaCodeLength = captcha.CodeLength;
defaults.captchaAlphabet = captcha.Alphabet;
defaults.captchaLifetimeSeconds = captcha.LifetimeSeconds;
configure(defaults);
