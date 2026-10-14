import { useRef, useState } from 'react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import LibraryErrorBoundary from './library/LibraryErrorBoundary';
import LibraryView from './library/LibraryView';

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: false } },
});

export default function App() {
  const [resetKey, setResetKey] = useState('library');
  const refetchRef = useRef<(() => void) | null>(null);
  const clearFiltersRef = useRef<(() => void) | null>(null);

  return (
    <QueryClientProvider client={queryClient}>
      <LibraryErrorBoundary
        resetKey={resetKey}
        onRetry={() => {
          refetchRef.current?.();
        }}
        clearFilters={() => {
          clearFiltersRef.current?.();
        }}
      >
        <LibraryView
          onResetKeyChange={(value: unknown) => setResetKey(String(value ?? 'library'))}
          onRefetchReady={(value: unknown) => {
            refetchRef.current = typeof value === 'function' ? (value as () => void) : null;
          }}
          onClearReady={(value: unknown) => {
            clearFiltersRef.current = typeof value === 'function' ? (value as () => void) : null;
          }}
        />
      </LibraryErrorBoundary>
    </QueryClientProvider>
  );
}
