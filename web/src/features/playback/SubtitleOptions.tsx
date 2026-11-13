import { useId, type ChangeEvent } from 'react';
import CaptionOverlay from './CaptionOverlay';
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
  SUBTITLE_EDGE_OPTION_LABELS,
  SUBTITLE_FONT_LABEL,
  SUBTITLE_FONT_OPTION_LABELS,
  SUBTITLE_PREVIEW_LABEL,
  SUBTITLE_TEXT_COLOR_LABEL,
  SUBTITLE_TEXT_OPACITY_LABEL,
  SUBTITLE_TEXT_SIZE_LABEL,
  SUBTITLE_TIMING_LABEL,
} from './subtitleCopy';
import {
  SUBTITLE_EDGE_TOKENS,
  SUBTITLE_FONT_TOKENS,
  SUBTITLE_OPACITY_MAX,
  SUBTITLE_OPACITY_MIN,
  SUBTITLE_OPACITY_STEP,
  SUBTITLE_TEXT_SIZE_MAX,
  SUBTITLE_TEXT_SIZE_MIN,
  SUBTITLE_TEXT_SIZE_STEP,
  type SubtitleEdgeToken,
  type SubtitleFontToken,
  type SubtitleStyleProfile,
} from './subtitlePreferences';
import { formatSubtitleDelay, type SubtitleDelayDirection } from './subtitleTiming';
import './subtitleOptions.css';

export interface SubtitleOptionsProps {
  delayMs: number;
  timingEnabled: boolean;
  style: SubtitleStyleProfile;
  onAdjustDelay: (direction: SubtitleDelayDirection, shift: boolean) => void;
  onResetDelay: () => void;
  onUpdateStyle: (patch: Partial<SubtitleStyleProfile>) => void;
  onResetStyle: () => void;
}

const PREVIEW_SURFACE_WIDTH_PX = 320;

function numericValue(event: ChangeEvent<HTMLInputElement>): number {
  return Number(event.target.value);
}

interface StyleRangeRowProps {
  label: string;
  inputId: string;
  min: number;
  max: number;
  step: number;
  value: number;
  onChange: (value: number) => void;
}

function StyleRangeRow({ label, inputId, min, max, step, value, onChange }: StyleRangeRowProps) {
  return (
    <div className="subtitle-options-row">
      <label className="subtitle-options-label" htmlFor={inputId}>
        {label}
      </label>
      <div className="subtitle-options-control">
        <input
          id={inputId}
          className="subtitle-options-range"
          type="range"
          min={min}
          max={max}
          step={step}
          value={value}
          onChange={(event) => onChange(numericValue(event))}
        />
        <output className="subtitle-options-value" aria-live="off">
          {`${value}%`}
        </output>
      </div>
    </div>
  );
}

export default function SubtitleOptions({
  delayMs,
  timingEnabled,
  style,
  onAdjustDelay,
  onResetDelay,
  onUpdateStyle,
  onResetStyle,
}: SubtitleOptionsProps) {
  const id = useId();
  const timingId = `${id}-timing`;
  const styleId = `${id}-style`;
  return (
    <div className="subtitle-options">
      <section className="subtitle-options-section" aria-labelledby={timingId}>
        <h3 className="subtitle-options-heading" id={timingId}>
          {SUBTITLE_TIMING_LABEL}
        </h3>
        <div className="subtitle-options-row">
          <span className="subtitle-options-label">{SUBTITLE_DELAY_LABEL}</span>
          <div className="subtitle-options-stepper">
            <button
              type="button"
              className="subtitle-options-step"
              aria-label={SUBTITLE_DELAY_EARLIER_LABEL}
              disabled={!timingEnabled}
              onClick={(event) => onAdjustDelay('earlier', event.shiftKey)}
            >
              <span aria-hidden="true">−</span>
            </button>
            <output className="subtitle-options-value" data-delay-value="true" aria-live="off">
              {formatSubtitleDelay(delayMs)}
            </output>
            <button
              type="button"
              className="subtitle-options-step"
              aria-label={SUBTITLE_DELAY_LATER_LABEL}
              disabled={!timingEnabled}
              onClick={(event) => onAdjustDelay('later', event.shiftKey)}
            >
              <span aria-hidden="true">+</span>
            </button>
          </div>
        </div>
        <p className="subtitle-options-help">{SUBTITLE_DELAY_HELP_COPY}</p>
        <button
          type="button"
          className="subtitle-options-reset"
          disabled={!timingEnabled}
          onClick={onResetDelay}
        >
          {RESET_TIMING_LABEL}
        </button>
      </section>

      <section className="subtitle-options-section" aria-labelledby={styleId}>
        <h3 className="subtitle-options-heading" id={styleId}>
          {CAPTION_STYLE_LABEL}
        </h3>

        <p className="subtitle-options-label">{SUBTITLE_PREVIEW_LABEL}</p>
        <CaptionOverlay
          surface="preview"
          cues={[]}
          style={style}
          surfaceWidthPx={PREVIEW_SURFACE_WIDTH_PX}
        />

        <div className="subtitle-options-row">
          <label className="subtitle-options-label" htmlFor={`${styleId}-font`}>
            {SUBTITLE_FONT_LABEL}
          </label>
          <select
            id={`${styleId}-font`}
            className="subtitle-options-select"
            value={style.font}
            onChange={(event) => onUpdateStyle({ font: event.target.value as SubtitleFontToken })}
          >
            {SUBTITLE_FONT_TOKENS.map((token) => (
              <option key={token} value={token}>
                {SUBTITLE_FONT_OPTION_LABELS[token]}
              </option>
            ))}
          </select>
        </div>

        <StyleRangeRow
          label={SUBTITLE_TEXT_SIZE_LABEL}
          inputId={`${styleId}-size`}
          min={SUBTITLE_TEXT_SIZE_MIN}
          max={SUBTITLE_TEXT_SIZE_MAX}
          step={SUBTITLE_TEXT_SIZE_STEP}
          value={style.textSize}
          onChange={(textSize) => onUpdateStyle({ textSize })}
        />

        <div className="subtitle-options-row">
          <label className="subtitle-options-label" htmlFor={`${styleId}-text-color`}>
            {SUBTITLE_TEXT_COLOR_LABEL}
          </label>
          <input
            id={`${styleId}-text-color`}
            className="subtitle-options-color"
            type="color"
            value={style.textColor}
            onChange={(event) => onUpdateStyle({ textColor: event.target.value })}
          />
        </div>

        <StyleRangeRow
          label={SUBTITLE_TEXT_OPACITY_LABEL}
          inputId={`${styleId}-text-opacity`}
          min={SUBTITLE_OPACITY_MIN}
          max={SUBTITLE_OPACITY_MAX}
          step={SUBTITLE_OPACITY_STEP}
          value={style.textOpacity}
          onChange={(textOpacity) => onUpdateStyle({ textOpacity })}
        />

        <div className="subtitle-options-row">
          <label className="subtitle-options-label" htmlFor={`${styleId}-bg-color`}>
            {SUBTITLE_BACKGROUND_COLOR_LABEL}
          </label>
          <input
            id={`${styleId}-bg-color`}
            className="subtitle-options-color"
            type="color"
            value={style.bgColor}
            onChange={(event) => onUpdateStyle({ bgColor: event.target.value })}
          />
        </div>

        <StyleRangeRow
          label={SUBTITLE_BACKGROUND_OPACITY_LABEL}
          inputId={`${styleId}-bg-opacity`}
          min={SUBTITLE_OPACITY_MIN}
          max={SUBTITLE_OPACITY_MAX}
          step={SUBTITLE_OPACITY_STEP}
          value={style.bgOpacity}
          onChange={(bgOpacity) => onUpdateStyle({ bgOpacity })}
        />

        <div className="subtitle-options-row">
          <label className="subtitle-options-label" htmlFor={`${styleId}-edge`}>
            {SUBTITLE_EDGE_LABEL}
          </label>
          <select
            id={`${styleId}-edge`}
            className="subtitle-options-select"
            value={style.edge}
            onChange={(event) => onUpdateStyle({ edge: event.target.value as SubtitleEdgeToken })}
          >
            {SUBTITLE_EDGE_TOKENS.map((token) => (
              <option key={token} value={token}>
                {SUBTITLE_EDGE_OPTION_LABELS[token]}
              </option>
            ))}
          </select>
        </div>

        <button type="button" className="subtitle-options-reset" onClick={onResetStyle}>
          {RESET_STYLE_LABEL}
        </button>
      </section>
    </div>
  );
}
