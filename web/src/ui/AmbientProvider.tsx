import { useCallback, useMemo, useState, type ReactNode } from 'react';
import { AmbientRegistryContext, normalizeAmbientUrls, type AmbientRegistry } from './ambient';

type OwnerPosters = ReadonlyMap<string, readonly string[]>;

function sameUrls(current: readonly string[] | undefined, next: readonly string[]): boolean {
  return (
    current !== undefined &&
    current.length === next.length &&
    current.every((url, index) => url === next[index])
  );
}

export function AmbientProvider({ children }: { children: ReactNode }) {
  const [owners, setOwners] = useState<OwnerPosters>(() => new Map());

  const setOwnerPosters = useCallback((ownerId: string, urls: readonly string[]) => {
    setOwners((previous) => {
      if (sameUrls(previous.get(ownerId), urls)) {
        return previous;
      }
      const next = new Map(previous);
      next.set(ownerId, urls);
      return next;
    });
  }, []);

  const removeOwner = useCallback((ownerId: string) => {
    setOwners((previous) => {
      if (!previous.has(ownerId)) {
        return previous;
      }
      const next = new Map(previous);
      next.delete(ownerId);
      return next;
    });
  }, []);

  const registry = useMemo<AmbientRegistry>(
    () => ({ setOwnerPosters, removeOwner }),
    [setOwnerPosters, removeOwner],
  );

  const posters = useMemo(() => normalizeAmbientUrls([...owners.values()].flat()), [owners]);

  return (
    <AmbientRegistryContext.Provider value={registry}>
      <div className="lamu-ambient" aria-hidden="true">
        {posters.map((url) => (
          <img key={url} src={url} alt="" decoding="async" />
        ))}
      </div>
      {children}
    </AmbientRegistryContext.Provider>
  );
}
