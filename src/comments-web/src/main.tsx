import { HttpFileClient } from './files/client';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { HttpCommentsClient } from './comments/client';
import './style.css';
import { loadConfiguration } from './runtimeConfig';

const element = document.getElementById('root')!;
async function start() {
  element.textContent = 'Loading…';
  try {
    await loadConfiguration();
    createRoot(element).render(<App client={new HttpCommentsClient()} fileClient={new HttpFileClient()} />);
  } catch {
    element.textContent = 'Could not load application settings. ';
    const retry = document.createElement('button'); retry.textContent = 'Retry';
    retry.onclick = () => { void start(); }; element.append(retry);
  }
}
void start();
