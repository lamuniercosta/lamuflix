import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { QueryClientProvider } from '@tanstack/react-query';
import App from './App';
import '@fontsource-variable/sofia-sans';
import '@fontsource-variable/sofia-sans-extra-condensed';
import './index.css';
import { useEnrichmentStatus } from './library/useEnrichmentStatus';
import { maybeStartWorker } from './mocks/startup';
import ApiFailureToastHost from './ui/ApiFailureToastHost';
import { apiFailureToastQueue } from './ui/apiFailureToastQueue';
import { createQueryClient } from './api/queryClient';

const queryClient = createQueryClient();

const rootElement = document.getElementById('root');

if (!rootElement) {
  throw new Error('Root element #root not found');
}

await maybeStartWorker();

// eslint-disable-next-line react-refresh/only-export-components -- Vite entry module has no exports to fast-refresh
function EnrichmentStatusHost() {
  const enrichmentStatus = useEnrichmentStatus();
  return <App enrichmentStatus={enrichmentStatus} />;
}

createRoot(rootElement).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <ApiFailureToastHost queue={apiFailureToastQueue} />
      <EnrichmentStatusHost />
    </QueryClientProvider>
  </StrictMode>,
);
