import { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import './style.css';

type ApplicationStatus = { application: string; version: string; stage: string };

function App() {
  const [status, setStatus] = useState<ApplicationStatus | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    fetch('/api/status', { signal: controller.signal })
      .then(response => {
        if (!response.ok) throw new Error('API unavailable');
        return response.json() as Promise<ApplicationStatus>;
      })
      .then(setStatus)
      .catch(error => { if (error.name !== 'AbortError') setFailed(true); });
    return () => controller.abort();
  }, []);

  return <main>
    <header><span className="mark" aria-hidden="true">c</span><span>Comments<span className="light">Service</span></span></header>
    <section aria-labelledby="title">
      <p className="eyebrow">1.1.0.2 / DOCKER</p>
      <h1 id="title">A starting point<br />for better conversations.</h1>
      <p className="intro">The frontend and API now run together in Docker. Persistent databases are provisioned for the next iteration.</p>
      <dl>
        <div><dt>Frontend</dt><dd>React build</dd></div>
        <div><dt>Backend</dt><dd role="status">{status ? 'Connected' : failed ? 'Unavailable — start the API' : 'Checking connection…'}</dd></div>
        <div><dt>Release</dt><dd>{status ? `${status.version} · ${status.stage}` : '1.1.0.2 · Docker'}</dd></div>
      </dl>
      <p className="note">Application data integration and comments will be added next.</p>
    </section>
    <footer>CommentsService · One step at a time.</footer>
  </main>;
}

createRoot(document.getElementById('root')!).render(<App />);
