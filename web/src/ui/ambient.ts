import { createContext, useContext, useEffect, useId, type CSSProperties } from 'react';

// One slot per ambient poster; the limit follows the slot count so every
// rendered poster has a position.
export const AMBIENT_POSITIONS: readonly CSSProperties[] = [
  { top: '-4%', left: '-4%' },
  { top: '-4%', right: '-4%' },
  { top: '34%', left: '28%' },
  { bottom: '-4%', left: '-4%' },
  { bottom: '-4%', right: '-4%' },
  { top: '30%', right: '8%' },
];

export const AMBIENT_LIMIT = AMBIENT_POSITIONS.length;

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
