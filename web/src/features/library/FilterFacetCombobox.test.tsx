import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { FilterFacetCombobox } from './FilterFacetCombobox';

const people = [
  { id: 1, name: 'Zack Snyder', count: 2 },
  { id: 2, name: 'Zoe Saldana', count: 1 },
];

function renderFacets() {
  const onCast = vi.fn();
  const onCollection = vi.fn();
  const onDirector = vi.fn();
  render(
    <>
      <FilterFacetCombobox
        kind="combobox"
        inputId="cast"
        label="Cast"
        placeholder="Search cast"
        listId="cast-list"
        optionId={(index) => `cast-option-${index}`}
        options={people}
        onSelect={onCast}
        emptyMessage="No matching cast members."
      />
      <FilterFacetCombobox
        kind="combobox"
        inputId="collection"
        label="Collection"
        placeholder="Search collections"
        listId="collection-list"
        optionId={(index) => `collection-option-${index}`}
        options={people}
        onSelect={onCollection}
      />
      <FilterFacetCombobox
        kind="combobox"
        inputId="director"
        label="Director"
        placeholder="Search directors"
        listId="director-list"
        optionId={(index) => `director-option-${index}`}
        options={people}
        onSelect={onDirector}
      />
    </>,
  );
  return { onCast, onCollection, onDirector };
}

describe('FilterFacetCombobox', () => {
  it('keeps each instance draft independent', () => {
    renderFacets();

    fireEvent.change(screen.getByRole('combobox', { name: 'Cast' }), { target: { value: 'zac' } });

    expect(screen.getByRole('combobox', { name: 'Cast' })).toHaveValue('zac');
    expect(screen.getByRole('combobox', { name: 'Collection' })).toHaveValue('');
    expect(screen.getByRole('combobox', { name: 'Director' })).toHaveValue('');
  });

  it('keeps each instance listbox and active index independent', () => {
    renderFacets();

    const cast = screen.getByRole('combobox', { name: 'Cast' });
    const director = screen.getByRole('combobox', { name: 'Director' });
    fireEvent.change(cast, { target: { value: 'z' } });
    fireEvent.change(director, { target: { value: 'z' } });

    fireEvent.keyDown(cast, { key: 'ArrowDown' });

    const castActive = cast.getAttribute('aria-activedescendant');
    expect(castActive).toBe('cast-option-0');
    expect(director).not.toHaveAttribute('aria-activedescendant');
    expect(screen.getAllByRole('listbox')).toHaveLength(2);
  });

  it('commits only the instance that received the keyboard event', () => {
    const { onCast, onDirector } = renderFacets();

    const cast = screen.getByRole('combobox', { name: 'Cast' });
    fireEvent.change(cast, { target: { value: 'z' } });
    fireEvent.change(screen.getByRole('combobox', { name: 'Director' }), {
      target: { value: 'z' },
    });

    fireEvent.keyDown(cast, { key: 'ArrowDown' });
    fireEvent.keyDown(cast, { key: 'Enter' });

    expect(onCast).toHaveBeenCalledWith(expect.objectContaining({ id: 1 }));
    expect(onDirector).not.toHaveBeenCalled();
  });

  it('closes only its own draft on Escape', () => {
    renderFacets();

    const cast = screen.getByRole('combobox', { name: 'Cast' });
    const collection = screen.getByRole('combobox', { name: 'Collection' });
    fireEvent.change(cast, { target: { value: 'z' } });
    fireEvent.change(collection, { target: { value: 'z' } });

    fireEvent.keyDown(cast, { key: 'Escape' });

    expect(cast).toHaveValue('');
    expect(collection).toHaveValue('z');
  });

  it('reports an empty result message through the instance that owns the draft', () => {
    render(
      <FilterFacetCombobox
        kind="suggestions"
        inputId="cast"
        label="Cast"
        placeholder="Search cast"
        listId="cast-list"
        options={[]}
        onSelect={() => undefined}
        emptyMessage="No matching cast members."
      />,
    );

    fireEvent.change(screen.getByLabelText('Cast'), { target: { value: 'zzz' } });

    expect(screen.getByText('No matching cast members.')).toBeInTheDocument();
  });

  it('focuses its own input when a selection is committed', () => {
    render(
      <FilterFacetCombobox
        kind="combobox"
        inputId="director"
        label="Director"
        placeholder="Search directors"
        listId="director-list"
        options={people}
        onSelect={() => undefined}
      />,
    );

    const input = screen.getByRole('combobox', { name: 'Director' });
    fireEvent.change(input, { target: { value: 'zoe' } });
    const listbox = screen.getByRole('listbox');
    fireEvent.click(within(listbox).getByRole('option', { name: /Zoe/ }));

    expect(input).toHaveFocus();
    expect(input).toHaveValue('');
  });
});
