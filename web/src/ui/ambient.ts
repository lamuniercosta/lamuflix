import { createContext, useContext, useEffect, useId } from 'react';

export const AMBIENT_LIMIT = 6;

export interface AmbientRegistry {
  setOwnerPosters: (ownerId: string, urls: readonly string[]) => void;
  removeOwner: (ownerId: string) => void;
}

const ignore = (): void => undefined;

export const AmbientRegistryContext = createContext<AmbientRegistry>({
  setOwnerPosters: ignore,
  removeOwner: ignore,
});

export function normalizeAmbientUrls(urls: readonly (string | null | undefined)[]): string[] {
  const unique = new Set<string>();
  for (const url of urls) {
    if (url) {
      unique.add(url);
    }
  }
  return [...unique].slice(0, AMBIENT_LIMIT);
}

export function useAmbientPosters(urls: readonly (string | null | undefined)[]): void {
  const ownerId = useId();
  const { setOwnerPosters, removeOwner } = useContext(AmbientRegistryContext);

  const key = JSON.stringify(normalizeAmbientUrls(urls));

  useEffect(() => {
    setOwnerPosters(ownerId, JSON.parse(key) as string[]);
  }, [key, ownerId, setOwnerPosters]);

  useEffect(() => () => removeOwner(ownerId), [ownerId, removeOwner]);
}
