import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { apiFailureToastQueue, type ApiFailureToastQueue } from '../ui/apiFailureToastQueue';
import { reportApiFailure } from './reportApiFailure';

export function createQueryClient(queue: ApiFailureToastQueue = apiFailureToastQueue): QueryClient {
  const report = (error: unknown) => {
    reportApiFailure(error, queue);
  };

  return new QueryClient({
    queryCache: new QueryCache({ onError: report }),
    mutationCache: new MutationCache({ onError: report }),
    defaultOptions: { queries: { retry: false } },
  });
}
