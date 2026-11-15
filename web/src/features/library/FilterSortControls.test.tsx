import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { FilterSortControls } from './FilterSortControls';

describe('FilterSortControls', () => {
  it('renders sort fields and the direction toggle only', () => {
    render(
      <FilterSortControls
        sort="Title"
        direction="Ascending"
        onSortChange={() => undefined}
        onDirectionToggle={() => undefined}
      />,
    );

    const group = screen.getByRole('group', { name: 'Sort' });
    expect(group).toBeInTheDocument();
    expect(screen.getAllByRole('button')).toHaveLength(5);
    expect(screen.queryByRole('slider')).not.toBeInTheDocument();
    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
  });

  it('marks the active sort field and reports a field change', () => {
    const onSortChange = vi.fn();
    render(
      <FilterSortControls
        sort="Year"
        direction="Ascending"
        onSortChange={onSortChange}
        onDirectionToggle={() => undefined}
      />,
    );

    expect(screen.getByRole('button', { name: 'Year' })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: 'Title' })).toHaveAttribute('aria-pressed', 'false');

    fireEvent.click(screen.getByRole('button', { name: 'Rating' }));
    expect(onSortChange).toHaveBeenCalledWith('Rating');
  });

  it('labels the direction toggle with the current direction and reports a toggle', () => {
    const onDirectionToggle = vi.fn();
    render(
      <FilterSortControls
        sort="Title"
        direction="Descending"
        onSortChange={() => undefined}
        onDirectionToggle={onDirectionToggle}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Descending' }));
    expect(onDirectionToggle).toHaveBeenCalledTimes(1);
  });
});
