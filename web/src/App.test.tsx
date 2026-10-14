import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import App from './App';

describe('App host', () => {
  it('mounts the filter panel and movie results', async () => {
    render(<App />);

    expect(await screen.findByText('2 movies')).toBeInTheDocument();
    expect(await screen.findByText('Dune')).toBeInTheDocument();
    expect(screen.getByRole('region', { name: /filters/i })).toBeInTheDocument();
  });
});
