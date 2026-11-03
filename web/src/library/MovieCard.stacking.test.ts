import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';

const here = dirname(fileURLToPath(import.meta.url));

function rule(css: string, selector: string): string | null {
  const start = css.indexOf(selector);
  if (start < 0) {
    return null;
  }
  const open = css.indexOf('{', start);
  const close = css.indexOf('}', open);
  if (open < 0 || close < 0) {
    return null;
  }
  return css.slice(open + 1, close);
}

describe('MovieCard stacking', () => {
  it('keeps feedback actions above the title link overlay', () => {
    const actionsCss = readFileSync(join(here, 'MovieFeedbackActions.css'), 'utf8');
    const libraryCss = readFileSync(join(here, 'library.css'), 'utf8');
    const actions = rule(actionsCss, '.movie-feedback-actions');
    const overlay = rule(libraryCss, '.movie-card-title a::after');
    expect(actions).not.toBeNull();
    expect(overlay).not.toBeNull();
    expect(overlay).toMatch(/z-index\s*:\s*1/);
    expect(actions).toMatch(/position\s*:\s*relative/);
    const z = actions!.match(/z-index\s*:\s*(\d+)/);
    expect(z).not.toBeNull();
    expect(Number(z![1])).toBeGreaterThan(1);
  });
});
