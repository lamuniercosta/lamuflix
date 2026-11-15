import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { DualRangeField } from './DualRangeField';

function renderField(overrides: { low?: number; high?: number } = {}) {
  const onLow = vi.fn();
  const onHigh = vi.fn();
  render(
    <DualRangeField
      min={0}
      max={10}
      step={0.1}
      low={{
        id: 'low',
        accessibleName: 'Range minimum',
        valueText: (overrides.low ?? 0).toFixed(1),
        value: overrides.low ?? 0,
        onValueChange: onLow,
      }}
      high={{
        id: 'high',
        accessibleName: 'Range maximum',
        valueText: (overrides.high ?? 10).toFixed(1),
        value: overrides.high ?? 10,
        onValueChange: onHigh,
      }}
    />,
  );
  return { onLow, onHigh };
}

describe('DualRangeField', () => {
  it('renders both thumbs with the supplied extents and captions', () => {
    renderField({ low: 2, high: 8 });

    const low = screen.getByLabelText('Range minimum');
    const high = screen.getByLabelText('Range maximum');
    expect(low).toHaveAttribute('min', '0');
    expect(low).toHaveAttribute('max', '10');
    expect(low).toHaveAttribute('step', '0.1');
    expect(high).toHaveAttribute('min', '0');
    expect(high).toHaveAttribute('max', '10');
    expect(screen.getByText('2.0')).toBeInTheDocument();
    expect(screen.getByText('8.0')).toBeInTheDocument();
  });

  it('clamps the low thumb so the pair cannot cross', () => {
    const { onLow } = renderField({ low: 2, high: 5 });

    fireEvent.change(screen.getByLabelText('Range minimum'), { target: { value: '9' } });

    expect(onLow).toHaveBeenCalledWith('5');
  });

  it('clamps the high thumb so the pair cannot cross', () => {
    const { onHigh } = renderField({ low: 4, high: 8 });

    fireEvent.change(screen.getByLabelText('Range maximum'), { target: { value: '1' } });

    expect(onHigh).toHaveBeenCalledWith('4');
  });

  it('exposes only the two thumbs, not the fieldset content of its host', () => {
    renderField();

    expect(screen.getAllByRole('slider')).toHaveLength(2);
  });
});
