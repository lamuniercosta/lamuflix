import { fireEvent, render, screen, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import SubtitleOptions, { type SubtitleOptionsProps } from './SubtitleOptions';
import {
  CAPTION_STYLE_LABEL,
  RESET_STYLE_LABEL,
  RESET_TIMING_LABEL,
  SUBTITLE_BACKGROUND_COLOR_LABEL,
  SUBTITLE_BACKGROUND_OPACITY_LABEL,
  SUBTITLE_DELAY_EARLIER_LABEL,
  SUBTITLE_DELAY_HELP_COPY,
  SUBTITLE_DELAY_LABEL,
  SUBTITLE_DELAY_LATER_LABEL,
  SUBTITLE_EDGE_LABEL,
  SUBTITLE_FONT_LABEL,
  SUBTITLE_PREVIEW_LABEL,
  SUBTITLE_PREVIEW_SAMPLE_COPY,
  SUBTITLE_TEXT_COLOR_LABEL,
  SUBTITLE_TEXT_OPACITY_LABEL,
  SUBTITLE_TEXT_SIZE_LABEL,
} from './subtitleCopy';
import { DEFAULT_SUBTITLE_STYLE, type SubtitleStyleProfile } from './subtitlePreferences';

function props(overrides: Partial<SubtitleOptionsProps> = {}): SubtitleOptionsProps {
  return {
    delayMs: 0,
    timingEnabled: true,
    style: DEFAULT_SUBTITLE_STYLE,
    onAdjustDelay: vi.fn(),
    onResetDelay: vi.fn(),
    onUpdateStyle: vi.fn(),
    onResetStyle: vi.fn(),
    ...overrides,
  };
}

function stepper(container: HTMLElement): { minus: HTMLElement; plus: HTMLElement; value: string } {
  const scope = within(container);
  const value = container.querySelector<HTMLElement>('[data-delay-value]')?.textContent ?? '';
  return {
    minus: scope.getByRole('button', { name: SUBTITLE_DELAY_EARLIER_LABEL }),
    plus: scope.getByRole('button', { name: SUBTITLE_DELAY_LATER_LABEL }),
    value,
  };
}

describe('SubtitleOptions timing cluster', () => {
  it('renders the exact labels, help line and a zero delay as 0.0 s', () => {
    const { container } = render(<SubtitleOptions {...props()} />);

    expect(screen.getByText(SUBTITLE_DELAY_LABEL)).toBeInTheDocument();
    expect(screen.getByText(SUBTITLE_DELAY_HELP_COPY)).toBeInTheDocument();
    expect(
      screen.getByText('Negative shows subtitles earlier, positive later.'),
    ).toBeInTheDocument();
    expect(stepper(container).value).toBe('0.0 s');
    expect(within(container).getByRole('button', { name: RESET_TIMING_LABEL })).toBeInTheDocument();
  });

  it('shows signed one-decimal values for earlier and later delays', () => {
    const later = render(<SubtitleOptions {...props({ delayMs: 500 })} />);
    expect(stepper(later.container).value).toBe('+0.5 s');
    later.unmount();

    const earlier = render(<SubtitleOptions {...props({ delayMs: -1250 })} />);
    expect(stepper(earlier.container).value).toBe('-1.3 s');
  });

  it('reports ordinary and Shift steps from the stepper buttons', () => {
    const onAdjustDelay = vi.fn();
    const { container } = render(<SubtitleOptions {...props({ onAdjustDelay })} />);
    const { minus, plus } = stepper(container);

    fireEvent.click(plus);
    expect(onAdjustDelay).toHaveBeenLastCalledWith('later', false);

    fireEvent.click(minus);
    expect(onAdjustDelay).toHaveBeenLastCalledWith('earlier', false);

    fireEvent.click(plus, { shiftKey: true });
    expect(onAdjustDelay).toHaveBeenLastCalledWith('later', true);

    fireEvent.click(minus, { shiftKey: true });
    expect(onAdjustDelay).toHaveBeenLastCalledWith('earlier', true);
    expect(onAdjustDelay).toHaveBeenCalledTimes(4);
  });

  it('reports a timing reset without claiming style changes', () => {
    const onResetDelay = vi.fn();
    const onResetStyle = vi.fn();
    render(<SubtitleOptions {...props({ onResetDelay, onResetStyle })} />);

    fireEvent.click(screen.getByRole('button', { name: RESET_TIMING_LABEL }));

    expect(onResetDelay).toHaveBeenCalledTimes(1);
    expect(onResetStyle).not.toHaveBeenCalled();
  });

  it('disables the timing cluster while keeping style editing available', () => {
    const onAdjustDelay = vi.fn();
    const onResetDelay = vi.fn();
    const onUpdateStyle = vi.fn();
    const { container } = render(
      <SubtitleOptions
        {...props({ timingEnabled: false, onAdjustDelay, onResetDelay, onUpdateStyle })}
      />,
    );
    const { minus, plus } = stepper(container);

    expect(minus).toBeDisabled();
    expect(plus).toBeDisabled();
    expect(screen.getByRole('button', { name: RESET_TIMING_LABEL })).toBeDisabled();

    fireEvent.click(plus);
    expect(onAdjustDelay).not.toHaveBeenCalled();
    expect(onResetDelay).not.toHaveBeenCalled();

    fireEvent.change(screen.getByLabelText(SUBTITLE_TEXT_SIZE_LABEL), {
      target: { value: '150' },
    });
    expect(onUpdateStyle).toHaveBeenCalledWith({ textSize: 150 });
  });
});

describe('SubtitleOptions caption style controls', () => {
  it('labels every control separately with the ticket wording', () => {
    render(<SubtitleOptions {...props()} />);

    expect(screen.getByText(CAPTION_STYLE_LABEL)).toBeInTheDocument();
    expect(screen.getByText(SUBTITLE_PREVIEW_LABEL)).toBeInTheDocument();
    expect(screen.getByText(SUBTITLE_PREVIEW_SAMPLE_COPY)).toBeInTheDocument();
    expect(screen.getByLabelText(SUBTITLE_FONT_LABEL)).toBeInTheDocument();
    expect(screen.getByLabelText('Text size (%)')).toBeInTheDocument();
    expect(screen.getByLabelText(SUBTITLE_TEXT_COLOR_LABEL)).toBeInTheDocument();
    expect(screen.getByLabelText(SUBTITLE_TEXT_OPACITY_LABEL)).toBeInTheDocument();
    expect(screen.getByLabelText(SUBTITLE_BACKGROUND_COLOR_LABEL)).toBeInTheDocument();
    expect(screen.getByLabelText(SUBTITLE_BACKGROUND_OPACITY_LABEL)).toBeInTheDocument();
    expect(screen.getByLabelText(SUBTITLE_EDGE_LABEL)).toBeInTheDocument();
    expect(SUBTITLE_TEXT_SIZE_LABEL).toBe('Text size (%)');
  });

  it('offers the ratified font, edge and range limits', () => {
    render(<SubtitleOptions {...props()} />);

    const font = screen.getByLabelText(SUBTITLE_FONT_LABEL) as HTMLSelectElement;
    expect(Array.from(font.options).map((option) => option.value)).toEqual([
      'system-sans',
      'serif',
      'monospace',
    ]);
    expect(Array.from(font.options).map((option) => option.textContent)).toEqual([
      'System sans-serif',
      'Serif',
      'Monospace',
    ]);

    const edge = screen.getByLabelText(SUBTITLE_EDGE_LABEL) as HTMLSelectElement;
    expect(Array.from(edge.options).map((option) => option.value)).toEqual([
      'none',
      'drop-shadow',
      'raised',
      'depressed',
      'outline',
    ]);

    const size = screen.getByLabelText(SUBTITLE_TEXT_SIZE_LABEL) as HTMLInputElement;
    expect([size.min, size.max, size.step, size.value]).toEqual(['50', '200', '10', '100']);

    const textOpacity = screen.getByLabelText(SUBTITLE_TEXT_OPACITY_LABEL) as HTMLInputElement;
    expect([textOpacity.min, textOpacity.max, textOpacity.step]).toEqual(['0', '100', '5']);

    const bgOpacity = screen.getByLabelText(SUBTITLE_BACKGROUND_OPACITY_LABEL) as HTMLInputElement;
    expect([bgOpacity.min, bgOpacity.max, bgOpacity.step, bgOpacity.value]).toEqual([
      '0',
      '100',
      '5',
      '75',
    ]);
  });

  it('reports each control change through its own patch', () => {
    const onUpdateStyle = vi.fn();
    render(<SubtitleOptions {...props({ onUpdateStyle })} />);

    fireEvent.change(screen.getByLabelText(SUBTITLE_FONT_LABEL), { target: { value: 'serif' } });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ font: 'serif' });

    fireEvent.change(screen.getByLabelText(SUBTITLE_TEXT_SIZE_LABEL), {
      target: { value: '170' },
    });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ textSize: 170 });

    fireEvent.change(screen.getByLabelText(SUBTITLE_TEXT_COLOR_LABEL), {
      target: { value: '#112233' },
    });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ textColor: '#112233' });

    fireEvent.change(screen.getByLabelText(SUBTITLE_TEXT_OPACITY_LABEL), {
      target: { value: '45' },
    });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ textOpacity: 45 });

    fireEvent.change(screen.getByLabelText(SUBTITLE_BACKGROUND_COLOR_LABEL), {
      target: { value: '#445566' },
    });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ bgColor: '#445566' });

    fireEvent.change(screen.getByLabelText(SUBTITLE_BACKGROUND_OPACITY_LABEL), {
      target: { value: '60' },
    });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ bgOpacity: 60 });

    fireEvent.change(screen.getByLabelText(SUBTITLE_EDGE_LABEL), {
      target: { value: 'outline' },
    });
    expect(onUpdateStyle).toHaveBeenLastCalledWith({ edge: 'outline' });
    expect(onUpdateStyle).toHaveBeenCalledTimes(7);
  });

  it('renders the preview through the shared overlay with the active profile', () => {
    const style: SubtitleStyleProfile = {
      ...DEFAULT_SUBTITLE_STYLE,
      font: 'monospace',
      textSize: 150,
      edge: 'outline',
    };
    const { container } = render(<SubtitleOptions {...props({ style })} />);

    const overlay = container.querySelector<HTMLElement>('[data-testid="caption-overlay"]');
    expect(overlay?.getAttribute('data-surface')).toBe('preview');
    const box = container.querySelector<HTMLElement>('[data-testid="caption-box"]');
    expect(box?.textContent).toBe(SUBTITLE_PREVIEW_SAMPLE_COPY);
    expect(box?.style.fontFamily).toBe('ui-monospace, monospace');
    expect(box?.style.fontSize).toBe('21px');
    expect(box?.style.textShadow).toContain('rgba(0, 0, 0, 0.9)');
  });

  it('reports a style reset without touching the timing reset', () => {
    const onResetStyle = vi.fn();
    const onResetDelay = vi.fn();
    render(<SubtitleOptions {...props({ onResetStyle, onResetDelay })} />);

    fireEvent.click(screen.getByRole('button', { name: RESET_STYLE_LABEL }));

    expect(onResetStyle).toHaveBeenCalledTimes(1);
    expect(onResetDelay).not.toHaveBeenCalled();
  });

  it('stays props-only and never touches browser storage or media', () => {
    const setItem = vi.spyOn(Storage.prototype, 'setItem');
    const getItem = vi.spyOn(Storage.prototype, 'getItem');
    const play = vi.spyOn(HTMLMediaElement.prototype, 'play');

    const { container } = render(<SubtitleOptions {...props({ style: DEFAULT_SUBTITLE_STYLE })} />);
    const { plus } = stepper(container);
    fireEvent.click(plus);
    fireEvent.change(screen.getByLabelText(SUBTITLE_EDGE_LABEL), { target: { value: 'raised' } });

    expect(setItem).not.toHaveBeenCalled();
    expect(getItem).not.toHaveBeenCalled();
    expect(play).not.toHaveBeenCalled();
    expect(container.querySelector('video')).toBeNull();
    expect(container.querySelector('audio')).toBeNull();

    setItem.mockRestore();
    getItem.mockRestore();
    play.mockRestore();
  });
});
