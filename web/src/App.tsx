import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import LibraryErrorBoundary from './library/LibraryErrorBoundary';
import LibraryView from './library/LibraryView';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <LibraryErrorBoundary>
        <LibraryView />
      </LibraryErrorBoundary>
    </QueryClientProvider>
  );
}
