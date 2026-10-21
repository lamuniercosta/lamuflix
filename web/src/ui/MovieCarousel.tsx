import {
  Children,
  isValidElement,
  useCallback,
  useEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type ReactNode,
} from 'react';
import { ArrowLeftIcon, ArrowRightIcon } from './icons';
import './MovieCarousel.css';

const EDGE_TOLERANCE = 1;
const PAGE_FRACTION = 0.9;

export interface MovieCarouselProps {
  label: string;
  children?: ReactNode;
}

type Bounds = {
  canScroll: boolean;
  atStart: boolean;
  atEnd: boolean;
};

const FITTED_BOUNDS: Bounds = { canScroll: false, atStart: true, atEnd: true };

function readBounds(viewport: HTMLElement): Bounds {
  const maxScroll = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
  return {
    canScroll: maxScroll > EDGE_TOLERANCE,
    atStart: viewport.scrollLeft <= EDGE_TOLERANCE,
    atEnd: viewport.scrollLeft >= maxScroll - EDGE_TOLERANCE,
  };
}

function sameBounds(current: Bounds, next: Bounds): boolean {
  return (
    current.canScroll === next.canScroll &&
    current.atStart === next.atStart &&
    current.atEnd === next.atEnd
  );
}

function pageStep(viewport: HTMLElement, maxScroll: number): number {
  const step = Math.round(viewport.clientWidth * PAGE_FRACTION);
  return Math.min(maxScroll, Math.max(1, step));
}

export function MovieCarousel({ label, children }: MovieCarouselProps) {
  const viewportRef = useRef<HTMLDivElement>(null);
  const trackRef = useRef<HTMLUListElement>(null);
  const [bounds, setBounds] = useState<Bounds>(FITTED_BOUNDS);

  const measure = useCallback(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;
    const next = readBounds(viewport);
    setBounds((current) => (sameBounds(current, next) ? current : next));
  }, []);

  useEffect(() => {
    measure();
  }, [measure, children]);

  useEffect(() => {
    const viewport = viewportRef.current;
    if (!viewport) return;
    viewport.addEventListener('scroll', measure, { passive: true });
    let observer: ResizeObserver | undefined;
    if (typeof ResizeObserver !== 'undefined') {
      observer = new ResizeObserver(measure);
      observer.observe(viewport);
      if (trackRef.current) observer.observe(trackRef.current);
    }
    return () => {
      viewport.removeEventListener('scroll', measure);
      observer?.disconnect();
    };
  }, [measure]);

  const scrollPage = useCallback(
    (direction: -1 | 1) => {
      const viewport = viewportRef.current;
      if (!viewport) return;
      const maxScroll = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
      if (maxScroll <= 0) return;
      const target = Math.min(
        Math.max(viewport.scrollLeft + direction * pageStep(viewport, maxScroll), 0),
        maxScroll,
      );
      const delta = target - viewport.scrollLeft;
      if (delta === 0) return;
      if (typeof viewport.scrollBy === 'function') {
        viewport.scrollBy({ left: delta });
      } else {
        viewport.scrollLeft = target;
      }
      measure();
    },
    [measure],
  );

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    if (event.target !== event.currentTarget) return;
    if (event.key !== 'ArrowLeft' && event.key !== 'ArrowRight') return;
    event.preventDefault();
    scrollPage(event.key === 'ArrowRight' ? 1 : -1);
  };

  const items = Children.toArray(children);

  return (
    <section className="movie-carousel" aria-label={label}>
      <div className="movie-carousel-controls">
        <button
          type="button"
          className="lamu-icon-button"
          aria-label="Previous"
          disabled={!bounds.canScroll || bounds.atStart}
          onClick={() => scrollPage(-1)}
        >
          <ArrowLeftIcon size={18} />
        </button>
        <button
          type="button"
          className="lamu-icon-button"
          aria-label="Next"
          disabled={!bounds.canScroll || bounds.atEnd}
          onClick={() => scrollPage(1)}
        >
          <ArrowRightIcon size={18} />
        </button>
      </div>
      <div
        className="movie-carousel-viewport"
        ref={viewportRef}
        role="group"
        aria-label={label}
        tabIndex={0}
        onKeyDown={handleKeyDown}
      >
        <ul className="movie-carousel-track" ref={trackRef}>
          {items.map((child, index) => (
            <li
              className="movie-carousel-item"
              key={isValidElement(child) ? (child.key ?? index) : index}
            >
              {child}
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
