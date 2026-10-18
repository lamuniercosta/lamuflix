import { fireEvent, render, screen } from '@testing-library/react';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import { AmbientProvider } from './AmbientProvider';
import { AMBIENT_LIMIT, normalizeAmbientUrls, useAmbientPosters } from './ambient';

function Posters({ urls }: { urls: readonly (string | null | undefined)[] }) {
  useAmbientPosters(urls);
  return null;
}

function Toggleable() {
  const [visible, setVisible] = useState(true);
  return (
    <>
      <button type="button" onClick={() => setVisible(false)}>
        Hide posters
      </button>
      {visible ? <Posters urls={['/gone.jpg']} /> : null}
    </>
  );
}

function posterSources(container: HTMLElement): string[] {
  return Array.from(
    container.querySelectorAll('.lamu-ambient img'),
    (img) => img.getAttribute('src') ?? '',
  );
}

describe('normalizeAmbientUrls', () => {
  it('drops empty and missing URLs and removes duplicates', () => {
    expect(normalizeAmbientUrls(['/a.jpg', null, '', '/a.jpg', undefined, '/b.jpg'])).toEqual([
      '/a.jpg',
      '/b.jpg',
    ]);
  });

  it('keeps at most AMBIENT_LIMIT URLs', () => {
    const urls = Array.from({ length: AMBIENT_LIMIT + 3 }, (_, index) => `/poster-${index}.jpg`);
    expect(normalizeAmbientUrls(urls)).toEqual(urls.slice(0, AMBIENT_LIMIT));
  });
});

describe('useAmbientPosters', () => {
  it('renders a hidden ambient layer with empty-alt posters', () => {
    const { container } = render(
      <AmbientProvider>
        <Posters urls={['/a.jpg', '/b.jpg']} />
      </AmbientProvider>,
    );

    expect(container.querySelector('.lamu-ambient')).toHaveAttribute('aria-hidden', 'true');
    expect(posterSources(container)).toEqual(['/a.jpg', '/b.jpg']);
    expect(container.querySelectorAll('.lamu-ambient img[alt=""]')).toHaveLength(2);
  });

  it('combines owners, removes duplicates and caps the total at AMBIENT_LIMIT', () => {
    const { container } = render(
      <AmbientProvider>
        <Posters urls={['/1.jpg', '/2.jpg', '/3.jpg', '/4.jpg']} />
        <Posters urls={['/3.jpg', '/5.jpg', '/6.jpg', '/7.jpg', '/8.jpg']} />
      </AmbientProvider>,
    );

    expect(posterSources(container)).toEqual([
      '/1.jpg',
      '/2.jpg',
      '/3.jpg',
      '/4.jpg',
      '/5.jpg',
      '/6.jpg',
    ]);
  });

  it('replaces posters when the URLs change', () => {
    const { container, rerender } = render(
      <AmbientProvider>
        <Posters urls={['/a.jpg']} />
      </AmbientProvider>,
    );

    rerender(
      <AmbientProvider>
        <Posters urls={['/b.jpg', null]} />
      </AmbientProvider>,
    );

    expect(posterSources(container)).toEqual(['/b.jpg']);
  });

  it('removes an owner posters when it unmounts', () => {
    const { container } = render(
      <AmbientProvider>
        <Toggleable />
      </AmbientProvider>,
    );
    expect(posterSources(container)).toEqual(['/gone.jpg']);

    fireEvent.click(screen.getByRole('button', { name: 'Hide posters' }));

    expect(posterSources(container)).toEqual([]);
  });

  it('does nothing outside an AmbientProvider', () => {
    expect(() => render(<Posters urls={['/a.jpg']} />)).not.toThrow();
  });
});
