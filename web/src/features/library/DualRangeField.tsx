import type { CSSProperties } from 'react';

export interface RangeThumb {
  id: string;
  accessibleName: string;
  valueText: string;
  value: number;
  onValueChange: (value: string) => void;
}

export interface DualRangeFieldProps {
  min: number;
  max: number;
  step: number;
  low: RangeThumb;
  high: RangeThumb;
}

// Two overlapping range inputs share one track; each thumb is clamped so the
// pair can never cross, and the amber fill spans the selected interval.
export function DualRangeField({ min, max, step, low, high }: DualRangeFieldProps) {
  const span = max - min || 1;
  const fill = {
    '--range-from': (low.value - min) / span,
    '--range-to': (high.value - min) / span,
  } as CSSProperties;
  // Keep the low thumb reachable when both thumbs meet near the top end.
  const lowOnTop = low.value > min + span / 2;
  return (
    <div className="lamu-range-field">
      <div className="lamu-range-caption" aria-hidden="true">
        <span>{low.valueText}</span>
        <span>{high.valueText}</span>
      </div>
      <div className="lamu-dual-range" style={fill}>
        <input
          id={low.id}
          type="range"
          className="lamu-range"
          aria-label={low.accessibleName}
          min={min}
          max={max}
          step={step}
          value={low.value}
          style={lowOnTop ? { zIndex: 2 } : undefined}
          onChange={(event) =>
            low.onValueChange(String(Math.min(Number(event.target.value), high.value)))
          }
        />
        <input
          id={high.id}
          type="range"
          className="lamu-range"
          aria-label={high.accessibleName}
          min={min}
          max={max}
          step={step}
          value={high.value}
          onChange={(event) =>
            high.onValueChange(String(Math.max(Number(event.target.value), low.value)))
          }
        />
      </div>
    </div>
  );
}

export default DualRangeField;
