import { useId, useRef, type KeyboardEvent as ReactKeyboardEvent } from 'react';
import {
  CLOSE_SUBTITLES_PANEL_LABEL,
  SUBTITLES_LABEL,
  SUBTITLES_OPTIONS_LABEL,
  subtitlePanelLabel,
} from './subtitleCopy';
import SubtitleOptions, { type SubtitleOptionsProps } from './SubtitleOptions';
import SubtitlesTab from './SubtitlesTab';
import type { SubtitlePlayback } from './useSubtitlePlayback';

export type SubtitlePanelTab = 'subtitles' | 'options';

export interface SubtitlePanelProps {
  title: string;
  panelId: string;
  activeTab: SubtitlePanelTab;
  movieId: number;
  playback: SubtitlePlayback;
  onTabChange: (tab: SubtitlePanelTab) => void;
  onClose: () => void;
  options: SubtitleOptionsProps;
}

const TABS: readonly SubtitlePanelTab[] = ['subtitles', 'options'];

export default function SubtitlePanel({
  title,
  panelId,
  activeTab,
  movieId,
  playback,
  onTabChange,
  onClose,
  options,
}: SubtitlePanelProps) {
  const baseId = useId();
  const tabRefs = useRef<Record<SubtitlePanelTab, HTMLButtonElement | null>>({
    subtitles: null,
    options: null,
  });

  const tabId = (tab: SubtitlePanelTab) => `${baseId}-${tab}-tab`;
  const tabPanelId = (tab: SubtitlePanelTab) => `${baseId}-${tab}-panel`;

  const activate = (tab: SubtitlePanelTab) => {
    onTabChange(tab);
    tabRefs.current[tab]?.focus();
  };

  const stepTab = (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    event.preventDefault();
    event.stopPropagation();
    activate(TABS[index]);
  };

  const handleTabKeyDown = (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    switch (event.key) {
      case 'ArrowRight':
        stepTab(event, (index + 1) % TABS.length);
        return;
      case 'ArrowLeft':
        stepTab(event, (index - 1 + TABS.length) % TABS.length);
        return;
      case 'Home':
        stepTab(event, 0);
        return;
      case 'End':
        stepTab(event, TABS.length - 1);
        return;
      default:
        return;
    }
  };

  return (
    <div
      id={panelId}
      className="subtitle-panel"
      role="region"
      aria-label={subtitlePanelLabel(title)}
      {...{ noautohide: '' }}
    >
      <div className="subtitle-panel-header">
        <div className="subtitle-panel-tabs" role="tablist" aria-label={subtitlePanelLabel(title)}>
          {TABS.map((tab, index) => {
            const selected = tab === activeTab;
            return (
              <button
                key={tab}
                ref={(element) => {
                  tabRefs.current[tab] = element;
                }}
                type="button"
                role="tab"
                id={tabId(tab)}
                aria-selected={selected}
                aria-controls={tabPanelId(tab)}
                tabIndex={selected ? 0 : -1}
                className="subtitle-panel-tab"
                onClick={() => onTabChange(tab)}
                onKeyDown={(event) => handleTabKeyDown(event, index)}
              >
                {tab === 'subtitles' ? SUBTITLES_LABEL : SUBTITLES_OPTIONS_LABEL}
              </button>
            );
          })}
        </div>
        <button
          type="button"
          className="subtitle-panel-close"
          aria-label={CLOSE_SUBTITLES_PANEL_LABEL}
          onClick={onClose}
        >
          <span aria-hidden="true">×</span>
        </button>
      </div>
      <div
        className="subtitle-panel-body"
        role="tabpanel"
        id={tabPanelId('subtitles')}
        aria-labelledby={tabId('subtitles')}
        hidden={activeTab !== 'subtitles'}
      >
        <SubtitlesTab title={title} movieId={movieId} playback={playback} />
      </div>
      <div
        className="subtitle-panel-body"
        role="tabpanel"
        id={tabPanelId('options')}
        aria-labelledby={tabId('options')}
        hidden={activeTab !== 'options'}
      >
        <SubtitleOptions {...options} />
      </div>
    </div>
  );
}
