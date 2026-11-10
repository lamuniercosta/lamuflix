import { useQuery } from '@tanstack/react-query';
import { client } from '../../api/client';
import type { components } from '../../api/types';

type PlaybackCapabilitiesDto = components['schemas']['PlaybackCapabilitiesDto'];

export type PlaybackCapabilityState = 'loading' | 'enabled' | 'disabled' | 'failed';

export function usePlaybackCapabilities(): PlaybackCapabilityState {
  const query = useQuery<PlaybackCapabilitiesDto>({
    queryKey: ['playbackCapabilities'],
    queryFn: async ({ signal }) => {
      const result = await client.GET('/api/playback/capabilities', { signal });
      if (result.data === undefined) {
        throw new Error('Playback capability request failed');
      }
      return result.data;
    },
    retry: false,
  });

  if (query.isPending) return 'loading';
  if (query.isError || !query.data) return 'failed';
  return query.data.localPlayEnabled ? 'enabled' : 'disabled';
}
