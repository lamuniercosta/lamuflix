import { act, fireEvent, render, screen, within } from '@testing-library/react';
import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import ApiFailureToastHost from './ApiFailureToastHost';
import {
  createApiFailureToastQueue,
  type ApiFailureToastInput,
  type ApiFailureToastQueue,
} from './apiFailureToastQueue';

const here = dirname(fileURLToPath(import.meta.url));
const indexCss = readFileSync(join(here, '..', 'index.css'), 'utf8');
const mainSource = readFileSync(join(here, '..', 'main.tsx'), 'utf8');

function failure(title: string, status = '500'): ApiFailureToastInput {
  const slug = title.toLowerCase();
  return {
    title,
    status,
    reason: `Reason for ${slug}`,
    traceId: `trace-${slug}`,
  };
}

function toastElements(container: HTMLElement): HTMLElement[] {
  return Array.from(container.querySelectorAll<HTMLElement>('.lamu-toast'));
}

function toastTexts(container: HTMLElement): string[] {
  return toastElements(container).map((toast) => toast.textContent ?? '');
}

function publish(queue: ApiFailureToastQueue, value: ApiFailureToastInput): void {
  act(() => {
    queue.publish(value);
  });
}

let queue: ApiFailureToastQueue;

beforeEach(() => {
  vi.useFakeTimers();
  queue = createApiFailureToastQueue();
});

afterEach(() => {
  queue.reset();
  vi.restoreAllMocks();
  vi.useRealTimers();
});

describe('ApiFailureToastHost lifecycle', () => {
  it('caps the stack at three, evicts the oldest first and clears the evicted timer', () => {
    const { container } = render(<ApiFailureToastHost queue={queue} />);
    const setTimeoutSpy = vi.spyOn(globalThis, 'setTimeout');
    const clearTimeoutSpy = vi.spyOn(globalThis, 'clearTimeout');

    publish(queue, failure('Toast one', '501'));
    publish(queue, failure('Toast two', '502'));
    publish(queue, failure('Toast three', '503'));
    publish(queue, failure('Toast four', '504'));

    const evictedTimer = setTimeoutSpy.mock.results[0]?.value;
    expect(evictedTimer).toBeDefined();
    expect(clearTimeoutSpy).toHaveBeenCalledWith(evictedTimer);
    expect(toastTexts(container)).toHaveLength(3);
    expect(toastTexts(container).join(' ')).not.toContain('Toast one');
    expect(toastTexts(container).join(' ')).toContain('Toast four');
    expect(vi.getTimerCount()).toBe(3);

    act(() => {
      vi.advanceTimersByTime(5000);
    });

    expect(toastElements(container)).toHaveLength(0);
  });

  it('auto-dismisses a toast after five seconds of unpaused time', () => {
    const { container } = render(<ApiFailureToastHost queue={queue} />);

    publish(queue, failure('Expiring toast', '500'));
    expect(toastTexts(container).join(' ')).toContain('Expiring toast');

    act(() => {
      vi.advanceTimersByTime(4999);
    });
    expect(toastElements(container)).toHaveLength(1);

    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(toastElements(container)).toHaveLength(0);
  });

  it('pauses on hover and resumes with only the remaining time', () => {
    const { container } = render(<ApiFailureToastHost queue={queue} />);

    publish(queue, failure('Hovered toast', '500'));
    const toast = toastElements(container)[0];
    fireEvent.mouseEnter(toast);

    act(() => {
      vi.advanceTimersByTime(10_000);
    });
    expect(toastElements(container)).toHaveLength(1);

    fireEvent.mouseLeave(toast);
    act(() => {
      vi.advanceTimersByTime(4999);
    });
    expect(toastElements(container)).toHaveLength(1);

    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(toastElements(container)).toHaveLength(0);
  });

  it('pauses on keyboard focus inside the toast and resumes on blur', () => {
    const { container } = render(<ApiFailureToastHost queue={queue} />);

    publish(queue, failure('Focused toast', '500'));
    const dismiss = within(toastElements(container)[0]).getByRole('button', { name: 'Dismiss' });
    dismiss.focus();
    expect(dismiss).toHaveFocus();

    act(() => {
      vi.advanceTimersByTime(10_000);
    });
    expect(toastElements(container)).toHaveLength(1);

    fireEvent.blur(dismiss);
    act(() => {
      vi.advanceTimersByTime(5000);
    });
    expect(toastElements(container)).toHaveLength(0);
  });

  it('keeps focus where the user left it while other toasts come and go', () => {
    const { container } = render(<ApiFailureToastHost queue={queue} />);

    publish(queue, failure('Oldest toast', '501'));
    publish(queue, failure('Focused toast', '502'));
    publish(queue, failure('Last toast', '503'));

    const focusedDismiss = within(toastElements(container)[1]).getByRole('button', {
      name: 'Dismiss',
    });
    focusedDismiss.focus();
    expect(focusedDismiss).toHaveFocus();

    publish(queue, failure('Burst toast', '504'));
    expect(toastElements(container)[0].textContent).toContain('Focused toast');
    expect(focusedDismiss).toHaveFocus();

    fireEvent.click(focusedDismiss);
    expect(toastElements(container).map((toast) => toast.textContent ?? '')).toEqual([
      expect.stringContaining('Last toast'),
      expect.stringContaining('Burst toast'),
    ]);
    expect(document.activeElement).not.toBe(
      within(toastElements(container)[0]).getByRole('button', { name: 'Dismiss' }),
    );
    expect(document.activeElement).not.toBe(
      within(toastElements(container)[1]).getByRole('button', { name: 'Dismiss' }),
    );
  });

  it('announces each addition once in a persistent polite live region', () => {
    render(<ApiFailureToastHost queue={queue} />);
    const liveRegion = screen.getByRole('status');

    expect(liveRegion).toHaveAttribute('aria-live', 'polite');
    expect(liveRegion.textContent).toBe('');

    publish(queue, failure('Announced first', '501'));
    publish(queue, failure('Announced second', '502'));
    expect(liveRegion.children).toHaveLength(2);
    expect(liveRegion.textContent).toContain('Announced first');
    expect(liveRegion.textContent).toContain('Announced second');

    act(() => {
      vi.advanceTimersByTime(5000);
    });
    expect(liveRegion.children).toHaveLength(2);

    publish(queue, failure('Announced third', '503'));
    expect(liveRegion.children).toHaveLength(3);
    expect(liveRegion.textContent).toContain('Announced third');
    expect(liveRegion.textContent?.match(/Announced first/g)).toHaveLength(1);
  });

  it('bounds announcements at three and evicts the oldest first', () => {
    render(<ApiFailureToastHost queue={queue} />);
    const liveRegion = screen.getByRole('status');

    publish(queue, failure('Overflow one', '501'));
    publish(queue, failure('Overflow two', '502'));
    publish(queue, failure('Overflow three', '503'));
    publish(queue, failure('Overflow four', '504'));
    publish(queue, failure('Overflow five', '505'));

    expect(liveRegion.children).toHaveLength(3);
    expect(queue.getSnapshot().announcements).toHaveLength(3);
    expect(liveRegion.textContent).not.toContain('Overflow one');
    expect(liveRegion.textContent).not.toContain('Overflow two');
    expect(liveRegion.textContent).toContain('Overflow five');
  });

  it('announces repeated identical failures once each without replaying evicted ones', () => {
    render(<ApiFailureToastHost queue={queue} />);
    const liveRegion = screen.getByRole('status');

    for (let index = 0; index < 5; index += 1) {
      publish(queue, failure('Repeated toast', '500'));
    }

    expect(liveRegion.children).toHaveLength(3);
    const announcements = queue.getSnapshot().announcements;
    expect(announcements).toHaveLength(3);
    expect(new Set(announcements.map((announcement) => announcement.id)).size).toBe(3);
    expect(liveRegion.textContent?.match(/Repeated toast/g)).toHaveLength(3);
  });
});

describe('ApiFailureToastHost styling intent', () => {
  it('uses house tokens, square corners, narrow-safe placement and reduced-motion rules', () => {
    const toastStart = indexCss.indexOf('.lamu-toast-host');
    const reducedMotionStart = indexCss.indexOf('@media (prefers-reduced-motion', toastStart);
    const toastCss = indexCss.slice(toastStart, reducedMotionStart);

    expect(toastStart).toBeGreaterThan(-1);
    expect(indexCss).toMatch(/@media \(prefers-reduced-motion: reduce\)[\s\S]*\.lamu-toast/);
    expect(toastCss).toContain('var(--color-booth)');
    expect(toastCss).toContain('var(--color-ink)');
    expect(toastCss).toContain('var(--color-accent)');
    expect(toastCss).toContain('var(--lamu-radius-control)');
    expect(toastCss).toContain('100vw');
    expect(toastCss).toContain('overflow-wrap: anywhere');
    expect(toastCss).not.toMatch(/backdrop-filter|filter:\s*blur/);
  });

  it('mounts one host above route content in the app entry module', () => {
    const hostMatches = mainSource.match(/<ApiFailureToastHost\b/g) ?? [];

    expect(hostMatches).toHaveLength(1);
    expect(mainSource).toContain('apiFailureToastQueue');
    expect(mainSource.indexOf('<ApiFailureToastHost')).toBeLessThan(
      mainSource.indexOf('<EnrichmentStatusHost'),
    );
  });
});
