import { useMemo } from 'react';
import type {
  SubtitleStyleProfile,
  SubtitleFontToken,
  SubtitleEdgeToken,
} from './subtitlePreferences';
import { SUBTITLE_PREVIEW_SAMPLE_COPY } from './subtitleCopy';
import './captionOverlay.css';

export interface RenderableSubtitleCue {
  id: string;
  text: string;
}

export type CaptionSurface = 'preview' | 'windowed' | 'fullscreen';

export interface CaptionOverlayProps {
  surface: CaptionSurface;
  cues: readonly RenderableSubtitleCue[];
  style: SubtitleStyleProfile;
  surfaceWidthPx: number;
  sampleText?: string;
}

const FONT_STACKS: Record<SubtitleFontToken, string> = {
  'system-sans': 'system-ui, sans-serif',
  serif: "Georgia, 'Times New Roman', serif",
  monospace: 'ui-monospace, monospace',
};

const EDGE_SHADOWS: Record<SubtitleEdgeToken, string> = {
  none: 'none',
  'drop-shadow': '2px 2px 2px rgba(0,0,0,0.9)',
  raised: '1px 1px 0 rgba(255,255,255,0.85), -1px -1px 1px rgba(0,0,0,0.85)',
  depressed: '-1px -1px 0 rgba(255,255,255,0.55), 1px 1px 1px rgba(0,0,0,0.9)',
  outline:
    '-1px 0 0 rgba(0,0,0,0.9), 1px 0 0 rgba(0,0,0,0.9), 0 -1px 0 rgba(0,0,0,0.9), 0 1px 0 rgba(0,0,0,0.9)',
};

const REFERENCE_SURFACE_WIDTH_PX = 640;
const BASE_CAPTION_PX = 20;
const LINE_HEIGHT_RATIO = 1.35;

const SURFACE_BASE_LIMITS: Record<CaptionSurface, readonly [number, number]> = {
  preview: [14, 20],
  windowed: [14, 34],
  fullscreen: [18, 48],
};

interface CaptionStyle {
  fontFamily: string;
  fontSizePx: number;
  lineHeightPx: number;
  color: string;
  background: string;
  textShadow: string;
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), max);
}

function parseHexColor(hex: string): [number, number, number] {
  const normalized = hex.replace('#', '');
  return [
    Number.parseInt(normalized.slice(0, 2), 16),
    Number.parseInt(normalized.slice(2, 4), 16),
    Number.parseInt(normalized.slice(4, 6), 16),
  ];
}

function rgba(hex: string, alphaPercent: number): string {
  const [red, green, blue] = parseHexColor(hex);
  const alpha = clamp(alphaPercent, 0, 100) / 100;
  return `rgba(${red}, ${green}, ${blue}, ${alpha})`;
}

function fontSizePx(surfaceWidthPx: number, textSizePct: number, surface: CaptionSurface): number {
  const [min, max] = SURFACE_BASE_LIMITS[surface];
  const base = BASE_CAPTION_PX * (surfaceWidthPx / REFERENCE_SURFACE_WIDTH_PX);
  return clamp(base, min, max) * (textSizePct / 100);
}

function captionStyle(
  style: SubtitleStyleProfile,
  surfaceWidthPx: number,
  surface: CaptionSurface,
): CaptionStyle {
  const sizePx = fontSizePx(surfaceWidthPx, style.textSize, surface);
  return {
    fontFamily: FONT_STACKS[style.font],
    fontSizePx: sizePx,
    lineHeightPx: sizePx * LINE_HEIGHT_RATIO,
    color: rgba(style.textColor, style.textOpacity),
    background: rgba(style.bgColor, style.bgOpacity),
    textShadow: EDGE_SHADOWS[style.edge],
  };
}

export default function CaptionOverlay({
  surface,
  cues,
  style,
  surfaceWidthPx,
  sampleText = SUBTITLE_PREVIEW_SAMPLE_COPY,
}: CaptionOverlayProps) {
  const computed = useMemo(
    () => captionStyle(style, surfaceWidthPx, surface),
    [style, surface, surfaceWidthPx],
  );

  const lines = useMemo<readonly RenderableSubtitleCue[]>(() => {
    if (surface === 'preview') return [{ id: 'preview', text: sampleText }];
    return cues;
  }, [cues, sampleText, surface]);

  if (lines.length === 0) return null;

  return (
    <div
      className="caption-overlay"
      data-testid="caption-overlay"
      data-surface={surface}
      aria-hidden="true"
      style={{ pointerEvents: 'none', userSelect: 'none' }}
      {...{ noautohide: '' }}
    >
      {lines.map((line) => (
        <span
          key={line.id}
          className="caption-overlay-box"
          data-testid="caption-box"
          style={{
            fontFamily: computed.fontFamily,
            fontSize: `${computed.fontSizePx}px`,
            lineHeight: `${computed.lineHeightPx}px`,
            color: computed.color,
            background: computed.background,
            textShadow: computed.textShadow,
            whiteSpace: 'pre-wrap',
          }}
        >
          {line.text}
        </span>
      ))}
    </div>
  );
}
