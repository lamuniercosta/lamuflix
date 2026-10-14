import { useCallback, useEffect, useRef, useState } from 'react';

export function useDebouncedSearch<T = string>(
  arg: T | ((value: T) => void),
  delay: number = 300,
  onCommit?: (value: T) => void,
): T | ((value: T) => void) {
  const isCallback = typeof arg === 'function';

  const [debounced, setDebounced] = useState<T>(() => arg as T);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current);
      }
    };
  }, []);

  const target = isCallback ? (arg as (value: T) => void) : onCommit;

  const commit = useCallback(
    (value: T) => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current);
      }
      timerRef.current = setTimeout(() => {
        timerRef.current = null;
        if (target !== undefined) {
          target(value);
        } else if (!isCallback) {
          setDebounced(() => value);
        }
      }, delay);
    },
    [target, delay, isCallback],
  );

  useEffect(() => {
    if (isCallback) {
      return;
    }
    if (timerRef.current !== null) {
      clearTimeout(timerRef.current);
    }
    timerRef.current = setTimeout(() => {
      timerRef.current = null;
      setDebounced(() => arg as T);
    }, delay);
    return () => {
      if (timerRef.current !== null) {
        clearTimeout(timerRef.current);
      }
    };
  }, [arg, delay, isCallback]);

  if (isCallback) {
    return commit;
  }

  return debounced;
}

export function useDebouncedValue<T>(value: T, delay: number = 300): T {
  return useDebouncedSearch(value, delay) as T;
}

export const useDebounce = useDebouncedSearch;
export const useDebouncedCallback = useDebouncedSearch;
export const debounce = (
  fn: (value: string) => void,
  delay: number = 300,
): ((value: string) => void) => {
  let timer: ReturnType<typeof setTimeout> | null = null;
  return (value: string) => {
    if (timer !== null) {
      clearTimeout(timer);
    }
    timer = setTimeout(() => {
      timer = null;
      fn(value);
    }, delay);
  };
};
