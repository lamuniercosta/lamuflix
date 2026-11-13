import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import CaptionOverlay from './CaptionOverlay';
import { DEFAULT_SUBTITLE_STYLE, type SubtitleStyleProfile } from './subtitlePreferences';
import { SUBTITLE_PREVIEW_SAMPLE_COPY } from './subtitleCopy';

function style(overrides: Partial<SubtitleStyleProfile> = {}): SubtitleStyleProfile {
  return { ...DEFAULT_SUBTITLE_STYLE, ...overrides };
}

function overlay(container: HTMLElement): HTMLElement {
  const element = container.querySelector<HTMLElement>('[data-testid="caption-overlay"]');
  if (!element) throw new Error('expected the overlay to render');
  return element;
}

function boxes(container: HTMLElement): HTMLElement[] {
  return Array.from(container.querySelectorAll<HTMLElement>('[data-testid="caption-box"]'));
}

function renderLive(profile: SubtitleStyleProfile, surfaceWidthPx: number) {
  return render(
    <CaptionOverlay
      surface="windowed"
      cues={[{ id: 'a', text: 'Caption' }]}
      style={profile}
      surfaceWidthPx={surfaceWidthPx}
    />,
  );
}

describe('CaptionOverlay sizing', () => {
  it('scales from the preview reference width before applying the user percentage', () => {
    const widths = [50, 100, 200].map((textSize) => {
      const { container } = render(
        <CaptionOverlay
          surface="preview"
          cues={[]}
          style={style({ textSize })}
          surfaceWidthPx={320}
        />,
      );
      return boxes(container)[0].style.fontSize;
    });
    expect(widths).toEqual(['7px', '14px', '28px']);
  });

  it('clamps the surface base only, so the user percentage always changes the result', () => {
    const windowed = [50, 100, 200].map(
      (textSize) => boxes(renderLive(style({ textSize }), 640).container)[0].style.fontSize,
    );
    expect(windowed).toEqual(['10px', '20px', '40px']);

    const fullscreen = [50, 100, 200].map((textSize) => {
      const { container } = render(
        <CaptionOverlay
          surface="fullscreen"
          cues={[{ id: 'a', text: 'Caption' }]}
          style={style({ textSize })}
          surfaceWidthPx={1920}
        />,
      );
      return boxes(container)[0].style.fontSize;
    });
    expect(fullscreen).toEqual(['24px', '48px', '96px']);
  });
});

describe('CaptionOverlay styling', () => {
  it('maps font tokens to the ratified local stacks', () => {
    expect(boxes(renderLive(style(), 640).container)[0].style.fontFamily).toBe(
      'system-ui, sans-serif',
    );
    const serif = boxes(renderLive(style({ font: 'serif' }), 640).container)[0].style.fontFamily;
    expect(serif).toContain('Georgia');
    expect(serif).toContain('Times New Roman');
    expect(serif.endsWith('serif')).toBe(true);
    expect(boxes(renderLive(style({ font: 'monospace' }), 640).container)[0].style.fontFamily).toBe(
      'ui-monospace, monospace',
    );
  });

  it('keeps text and background opacity as independent layers', () => {
    const independent = boxes(
      renderLive(style({ textOpacity: 40, bgOpacity: 100 }), 640).container,
    )[0];
    expect(independent.style.color).toBe('rgba(255, 255, 255, 0.4)');
    expect(independent.style.background).toBe('rgb(0, 0, 0)');

    const defaultBackground = boxes(renderLive(style(), 640).container)[0];
    expect(defaultBackground.style.background).toBe('rgba(0, 0, 0, 0.75)');
    expect(boxes(renderLive(style({ textColor: '#3366FF' }), 640).container)[0].style.color).toBe(
      'rgb(51, 102, 255)',
    );
  });

  it('maps all five edge tokens to distinct CSS text shadows', () => {
    const shadows = (['none', 'drop-shadow', 'raised', 'depressed', 'outline'] as const).map(
      (edge) => boxes(renderLive(style({ edge }), 640).container)[0].style.textShadow,
    );
    expect(new Set(shadows).size).toBe(5);
    expect(shadows[0]).toBe('none');
  });
});

describe('CaptionOverlay', () => {
  it('renders the preview sample through the shared renderer', () => {
    const { container } = render(
      <CaptionOverlay surface="preview" cues={[]} style={style()} surfaceWidthPx={320} />,
    );
    expect(overlay(container).textContent).toBe(SUBTITLE_PREVIEW_SAMPLE_COPY);
    expect(SUBTITLE_PREVIEW_SAMPLE_COPY).toBe('The quick brown fox jumps over the lazy dog.');
    expect(boxes(container)).toHaveLength(1);
  });

  it('renders nothing when live with no active cues, taking no space', () => {
    const { container } = render(
      <CaptionOverlay surface="windowed" cues={[]} style={style()} surfaceWidthPx={640} />,
    );
    expect(container.querySelector('[data-testid="caption-overlay"]')).toBeNull();
  });

  it('renders multiple cues in order and preserves line breaks', () => {
    const { container } = render(
      <CaptionOverlay
        surface="windowed"
        style={style()}
        surfaceWidthPx={640}
        cues={[
          { id: 'a', text: 'First cue' },
          { id: 'b', text: 'Line one\nLine two' },
        ]}
      />,
    );
    const rendered = boxes(container);
    expect(rendered).toHaveLength(2);
    expect(rendered[0].textContent).toBe('First cue');
    expect(rendered[1].textContent).toBe('Line one\nLine two');
    expect(rendered[1].style.whiteSpace).toBe('pre-wrap');
  });

  it('renders markup-looking text literally with no active elements or handlers', () => {
    const hostile = [
      '<i>italic</i> and <b>bold</b>',
      '<img src="x" onerror="alert(1)">',
      '<svg onload="alert(1)"></svg>',
      '<a href="javascript:alert(1)">link</a>',
      '<span style="background:url(javascript:alert(1))">styled</span>',
    ];
    const { container } = render(
      <CaptionOverlay
        surface="windowed"
        style={style()}
        surfaceWidthPx={640}
        cues={hostile.map((text, index) => ({ id: `cue-${index}`, text }))}
      />,
    );

    const rendered = boxes(container);
    expect(rendered.map((box) => box.textContent)).toEqual(hostile);
    expect(container.querySelector('img')).toBeNull();
    expect(container.querySelector('svg')).toBeNull();
    expect(container.querySelector('a')).toBeNull();
    expect(container.querySelector('i')).toBeNull();
    expect(container.querySelector('b')).toBeNull();
    expect(
      Array.from(container.querySelectorAll('[data-testid="caption-box"]')).some((node) =>
        node.getAttribute('style')?.includes('url('),
      ),
    ).toBe(false);
  });

  it('is pointer-transparent, non-interactive, aria-hidden and not an aria-live stream', () => {
    const { container } = render(
      <CaptionOverlay
        surface="windowed"
        cues={[{ id: 'a', text: 'Caption' }]}
        style={style()}
        surfaceWidthPx={640}
      />,
    );
    const region = overlay(container);
    expect(region.getAttribute('aria-hidden')).toBe('true');
    expect(region.hasAttribute('aria-live')).toBe(false);
    expect(region.style.pointerEvents).toBe('none');
    expect(region.style.userSelect).toBe('none');
    expect(region.querySelector('button')).toBeNull();
    expect(region.querySelector('[tabindex]')).toBeNull();
  });

  it('renders the same style math for preview and live at an equal surface', () => {
    const profile = style({ font: 'serif', textSize: 150, edge: 'outline' });
    const preview = render(
      <CaptionOverlay surface="preview" cues={[]} style={profile} surfaceWidthPx={640} />,
    );
    const live = render(
      <CaptionOverlay
        surface="windowed"
        cues={[{ id: 'a', text: 'Caption' }]}
        style={profile}
        surfaceWidthPx={640}
      />,
    );
    const previewBox = boxes(preview.container)[0];
    const liveBox = boxes(live.container)[0];
    expect(previewBox.style.fontFamily).toBe(liveBox.style.fontFamily);
    expect(previewBox.style.textShadow).toBe(liveBox.style.textShadow);
    expect(previewBox.style.background).toBe(liveBox.style.background);
    expect(previewBox.style.color).toBe(liveBox.style.color);
  });
});
