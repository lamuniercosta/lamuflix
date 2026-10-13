import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';
import './index.css';
import { maybeStartWorker } from './mocks/startup';

const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Root element #root not found');
}

await maybeStartWorker();

createRoot(rootElement).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
