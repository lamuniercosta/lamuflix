import { useEffect, useMemo, useRef, type FocusEvent } from 'react';
import MovieCard from '../../library/MovieCard';
import type { MovieFeedbackClients } from '../../library/movieFeedbackStore';
import { MovieCarousel } from '../../ui/MovieCarousel';
import { useForYou } from './forYouApi';
import './ForYouRail.css';

const SECTION_LABEL = 'For You';

function findLibraryHeading(rail: Element | null): HTMLElement | null {
  return rail?.closest('.library-view')?.querySelector<HTMLElement>('h2.library-title') ?? null;
}

function focusHeading(heading: HTMLElement | null): void {
  if (!heading) return;
  heading.setAttribute('tabindex', '-1');
  heading.addEventListener('blur', () => heading.removeAttribute('tabindex'), { once: true });
  heading.focus();
}

export interface ForYouRailProps {
  onOpenMovie?: (id: number | string) => void;
  feedbackClients?: MovieFeedbackClients;
}

export default function ForYouRail({ onOpenMovie, feedbackClients }: ForYouRailProps) {
  const query = useForYou();
  const rows = useMemo(() => query.data ?? [], [query.data]);
  const sectionRef = useRef<HTMLElement>(null);
  const headingRef = useRef<HTMLHeadingElement>(null);
  const lastFocusedRef = useRef<HTMLElement | null>(null);
  const libraryHeadingRef = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const heading = findLibraryHeading(sectionRef.current);
    if (heading) libraryHeadingRef.current = heading;
  });

  const handleFocus = (event: FocusEvent<HTMLElement>) => {
    if (event.target instanceof HTMLElement && event.target !== headingRef.current) {
      lastFocusedRef.current = event.target;
    }
  };

  const handleBlur = (event: FocusEvent<HTMLElement>) => {
    const next = event.relatedTarget;
    if (next instanceof Node && !sectionRef.current?.contains(next)) {
      lastFocusedRef.current = null;
    }
  };

  useEffect(() => {
    const focused = lastFocusedRef.current;
    if (!focused || document.contains(focused)) return;
    lastFocusedRef.current = null;
    if (rows.length > 0) {
      headingRef.current?.focus();
    } else {
      focusHeading(libraryHeadingRef.current);
    }
  }, [rows]);

  useEffect(
    () => () => {
      const focused = lastFocusedRef.current;
      if (!focused) return;
      const section = sectionRef.current;
      if (document.contains(focused) && !section?.contains(focused)) return;
      lastFocusedRef.current = null;
      focusHeading(libraryHeadingRef.current);
    },
    [],
  );

  if (rows.length === 0) {
    return null;
  }

  return (
    <section ref={sectionRef} className="for-you-rail" onFocus={handleFocus} onBlur={handleBlur}>
      <h2 ref={headingRef} tabIndex={-1} className="for-you-heading">
        {SECTION_LABEL}
      </h2>
      <MovieCarousel label={SECTION_LABEL}>
        {rows.map((movie) => (
          <MovieCard
            key={String(movie.id)}
            movie={movie}
            reasons={movie.reasons}
            onOpenMovie={onOpenMovie}
            feedbackClients={feedbackClients}
            className="for-you-card"
          />
        ))}
      </MovieCarousel>
    </section>
  );
}
