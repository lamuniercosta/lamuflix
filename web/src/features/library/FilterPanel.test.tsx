import { render, screen } from '@testing-library/react';
import type { components } from '../../api/types';
import { describe, expect, it } from 'vitest';
import FilterPanel from './FilterPanel';

describe('FilterPanel', () => {
  it('renders search placeholder and clear button', () => {
    render(<FilterPanel />);

    expect(screen.getByPlaceholderText('Search titles')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /clear filters/i })).toBeInTheDocument();
  });

  it('renders genre counts in a fixed badge and accessible suffix', () => {
    render(
      <FilterPanel
        genres={[{ id: 1, name: 'Action', movieCount: 42 } as components['schemas']['GenreDto']]}
      />,
    );

    expect(screen.getByText('Action')).toBeInTheDocument();
    expect(screen.getByText(/, 42 movies/i)).toBeInTheDocument();
  });
});
