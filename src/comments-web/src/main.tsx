import { HttpFileClient } from './files/client';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { HttpCommentsClient } from './comments/client';
import './style.css';

createRoot(document.getElementById('root')!).render(<App client={new HttpCommentsClient()} fileClient={new HttpFileClient()} />);
