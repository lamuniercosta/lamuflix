import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { MovieCarousel } from './MovieCarousel';

type Layout = {
  scrollWidth: number;
  clientWidth: number;
  scrollLeft: number;
};

let layout: Layout = { scrollWidth: 0, clientWidth: 0, scrollLeft: 0 };
let scrollDeltas: number[] = [];
let restoreLayout: () => void = () => {};
const resizeObservers: ResizeObserverStub[] = [];

class ResizeObserverStub {
  private readonly callback: ResizeObserverCallback;

  constructor(callback: ResizeObserverCallback) {
    this.callback = callback;
    resizeObservers.push(this);
  }

  observe(): void {}

  unobserve(): void {}

  disconnect(): void {}

  trigger(): void {
    this.callback([], this as unknown as ResizeObserver);
  }
}

function replaceProperty(
  target: object,
  property: string,
  descriptor: PropertyDescriptor,
): () => void {
  const saved = Object.getOwnPropertyDescriptor(target, property);
  Object.defineProperty(target, property, { configurable: true, ...descriptor });
  return () => {
    if (saved) {
      Object.defineProperty(target, property, saved);
    } else {
      Reflect.deleteProperty(target, property);
    }
  };
}

function movieCard(title: string, href = `/movies/${title.toLowerCase().replaceAll(' ', '-')}`) {
  return (
    <article key={title}>
      <a href={href}>
        <span>{title}</span>
      </a>
    </article>
  );
}

function renderCarousel(count = 5, label = 'More like this') {
  return render(
    <MovieCarousel label={label}>
      {Array.from({ length: count }, (_, index) => movieCard(`Movie ${index + 1}`))}
    </MovieCarousel>,
  );
}

beforeEach(() => {
  layout = { scrollWidth: 0, clientWidth: 0, scrollLeft: 0 };
  scrollDeltas = [];
  resizeObservers.length = 0;
  const restores = [
    replaceProperty(Element.prototype, 'scrollWidth', {
      get: () => layout.scrollWidth,
    }),
    replaceProperty(Element.prototype, 'clientWidth', {
      get: () => layout.clientWidth,
    }),
    replaceProperty(Element.prototype, 'scrollLeft', {
      get: () => layout.scrollLeft,
      set: (value: number) => {
        layout.scrollLeft = value;
      },
    }),
    replaceProperty(Element.prototype, 'scrollBy', {
      value(this: Element, options: ScrollToOptions = {}) {
        const delta = options.left ?? 0;
        scrollDeltas.push(delta);
        const maxScroll = Math.max(0, layout.scrollWidth - layout.clientWidth);
        layout.scrollLeft = Math.min(Math.max(0, layout.scrollLeft + delta), maxScroll);
        this.dispatchEvent(new Event('scroll'));
      },
    }),
  ];
  restoreLayout = () => {
    for (const restore of restores.reverse()) {
      restore();
    }
  };
});

afterEach(() => {
  restoreLayout();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

describe('MovieCarousel', () => {
  it('renders a labelled region containing a semantic list of its children', () => {
    // arrange
    const label = 'More like this';

    // act
    renderCarousel(3, label);

    // assert
    const region = screen.getByRole('region', { name: label });
    const list = within(region).getByRole('list');
    expect(within(list).getAllByRole('listitem')).toHaveLength(3);
    expect(within(list).getByRole('link', { name: 'Movie 1' })).toBeInTheDocument();
  });

  it('disables Previous at the start and enables Next when the rail overflows', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };

    // act
    renderCarousel();

    // assert
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();
  });

  it('scrolls a page at a time and disables each control at its boundary', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    renderCarousel();
    const previous = screen.getByRole('button', { name: 'Previous' });
    const next = screen.getByRole('button', { name: 'Next' });

    // act
    fireEvent.click(next);

    // assert
    expect(scrollDeltas).toEqual([360]);
    expect(previous).toBeEnabled();
    expect(next).toBeDisabled();

    // act
    fireEvent.click(previous);

    // assert
    expect(scrollDeltas).toEqual([360, -360]);
    expect(previous).toBeDisabled();
    expect(next).toBeEnabled();
  });

  it('disables both controls when the content fits without overflow', () => {
    // arrange
    layout = { scrollWidth: 400, clientWidth: 400, scrollLeft: 0 };

    // act
    renderCarousel(2);

    // assert
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it('scrolls with arrow keys only while the viewport itself has focus', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    renderCarousel();
    const viewport = screen.getByRole('group', { name: 'More like this' });
    const link = screen.getByRole('link', { name: 'Movie 1' });

    // act
    viewport.focus();
    const viewportResult = fireEvent.keyDown(viewport, { key: 'ArrowRight' });

    // assert
    expect(viewportResult).toBe(false);
    expect(scrollDeltas).toEqual([360]);

    // act
    link.focus();
    const linkResult = fireEvent.keyDown(link, { key: 'ArrowRight' });

    // assert
    expect(linkResult).toBe(true);
    expect(scrollDeltas).toEqual([360]);
    expect(document.activeElement).toBe(link);
  });

  it('keeps native link Tab, Enter, and modified activation behavior', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    render(
      <MovieCarousel label="More like this">
        {movieCard('Movie 2')}
        {movieCard('Movie 3', '#movie-3')}
      </MovieCarousel>,
    );
    const link = screen.getByRole('link', { name: 'Movie 2' });
    const fragmentLink = screen.getByRole('link', { name: 'Movie 3' });
    const fragmentTitle = within(fragmentLink).getByText('Movie 3');
    window.history.replaceState(null, '', '/');

    // act
    const tabResult = fireEvent.keyDown(link, { key: 'Tab' });
    const enterResult = fireEvent.keyDown(link, { key: 'Enter' });
    const modifiedClickResult = fireEvent.click(fragmentTitle, { ctrlKey: true });

    // assert
    expect(link).toHaveAttribute('href', '/movies/movie-2');
    expect(link.tabIndex).toBe(0);
    expect(tabResult).toBe(true);
    expect(enterResult).toBe(true);
    expect(modifiedClickResult).toBe(true);
    expect(scrollDeltas).toEqual([]);
    expect(window.location.pathname).toBe('/');
    expect(window.location.hash).toBe('');
  });

  it('does not move card focus when the rail scrolls', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    renderCarousel();
    const link = screen.getByRole('link', { name: 'Movie 1' });
    const next = screen.getByRole('button', { name: 'Next' });
    link.focus();

    // act
    fireEvent.click(next);

    // assert
    expect(document.activeElement).toBe(link);
  });

  it('remeasures control boundaries after a scroll', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    renderCarousel();
    const viewport = screen.getByRole('group', { name: 'More like this' });

    // act
    layout.scrollLeft = 360;
    fireEvent.scroll(viewport);

    // assert
    expect(screen.getByRole('button', { name: 'Previous' })).toBeEnabled();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it('remeasures control boundaries when the viewport resizes', () => {
    // arrange
    vi.stubGlobal('ResizeObserver', ResizeObserverStub);
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    renderCarousel();
    expect(resizeObservers).toHaveLength(1);

    // act
    layout = { scrollWidth: 400, clientWidth: 400, scrollLeft: 0 };
    act(() => resizeObservers[0].trigger());

    // assert
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it('remeasures control boundaries when the children change', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 360 };
    const { rerender } = renderCarousel();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();

    // act
    layout = { scrollWidth: 400, clientWidth: 400, scrollLeft: 0 };
    rerender(
      <MovieCarousel label="More like this">
        {movieCard('Movie 1')}
        {movieCard('Movie 2')}
      </MovieCarousel>,
    );

    // assert
    expect(screen.getAllByRole('listitem')).toHaveLength(2);
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
  });

  it('lays out non-recommendation children without fetching anything', () => {
    // arrange
    layout = { scrollWidth: 760, clientWidth: 400, scrollLeft: 0 };
    const fetchSpy = vi.spyOn(globalThis, 'fetch');

    // act
    render(
      <MovieCarousel label="Cast">
        <p>Actor one</p>
        <p>Actor two</p>
      </MovieCarousel>,
    );

    // assert
    expect(typeof MovieCarousel).toBe('function');
    expect(screen.getByRole('region', { name: 'Cast' })).toBeInTheDocument();
    expect(screen.getAllByRole('listitem')).toHaveLength(2);
    expect(screen.getByText('Actor two')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next' })).toBeEnabled();
    expect(fetchSpy).not.toHaveBeenCalled();
  });
});
