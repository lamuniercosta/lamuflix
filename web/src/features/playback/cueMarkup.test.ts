import { describe, expect, it } from 'vitest';
import { parseCueMarkup } from './cueMarkup';

describe('parseCueMarkup', () => {
  it('returns a single text node for plain text', () => {
    expect(parseCueMarkup('Hello world')).toEqual([{ kind: 'text', text: 'Hello world' }]);
  });

  it('turns escaped i tags (as the backend sends them) into an i node', () => {
    expect(parseCueMarkup('&lt;i&gt;x&lt;/i&gt;')).toEqual([
      { kind: 'i', children: [{ kind: 'text', text: 'x' }] },
    ]);
  });

  it('also handles raw b tags, since decoding is a no-op for them', () => {
    expect(parseCueMarkup('<b>x</b>')).toEqual([
      { kind: 'b', children: [{ kind: 'text', text: 'x' }] },
    ]);
  });

  it('drops font tags and their attributes but keeps the text', () => {
    expect(parseCueMarkup('&lt;font color=#FFFF00&gt;Qual é a regra?&lt;/font&gt;')).toEqual([
      { kind: 'text', text: 'Qual é a regra?' },
    ]);
  });

  it('decodes ampersand', () => {
    expect(parseCueMarkup('Tom &amp; Jerry')).toEqual([{ kind: 'text', text: 'Tom & Jerry' }]);
  });

  it('decodes less-than and keeps comparisons as text', () => {
    expect(parseCueMarkup('a &lt; b')).toEqual([{ kind: 'text', text: 'a < b' }]);
    expect(parseCueMarkup('1 &lt; 2 &gt; 0')).toEqual([{ kind: 'text', text: '1 < 2 > 0' }]);
    expect(parseCueMarkup('I &lt;3 you')).toEqual([{ kind: 'text', text: 'I <3 you' }]);
  });

  it('decodes entities exactly once', () => {
    expect(parseCueMarkup('&amp;lt;i&amp;gt;')).toEqual([{ kind: 'text', text: '&lt;i&gt;' }]);
  });

  it('nests i and b tags', () => {
    expect(parseCueMarkup('<i><b>x</b></i>')).toEqual([
      { kind: 'i', children: [{ kind: 'b', children: [{ kind: 'text', text: 'x' }] }] },
    ]);
  });

  it('auto-closes unclosed i tags at end of input', () => {
    expect(parseCueMarkup('<i>abc')).toEqual([
      { kind: 'i', children: [{ kind: 'text', text: 'abc' }] },
    ]);
  });

  it('ignores a stray close tag', () => {
    expect(parseCueMarkup('</i>x')).toEqual([{ kind: 'text', text: 'x' }]);
  });

  it('closes the nearest open node and implicitly closes anything opened after it', () => {
    expect(parseCueMarkup('<i><b>x</i>y</b>')).toEqual([
      { kind: 'i', children: [{ kind: 'b', children: [{ kind: 'text', text: 'x' }] }] },
      { kind: 'text', text: 'y' },
    ]);
  });

  it('treats tag names case-insensitively', () => {
    expect(parseCueMarkup('<I>x</I>')).toEqual([
      { kind: 'i', children: [{ kind: 'text', text: 'x' }] },
    ]);
  });

  it('drops span tags and keeps their text', () => {
    expect(parseCueMarkup('<span class="a">hi</span>')).toEqual([{ kind: 'text', text: 'hi' }]);
  });

  it('drops img tags and keeps the surrounding text', () => {
    expect(parseCueMarkup('<img src=x onerror=alert(1)>text')).toEqual([
      { kind: 'text', text: 'text' },
    ]);
  });

  it('drops script tags but keeps their text and creates no script node', () => {
    expect(parseCueMarkup('<script>alert(1)</script>')).toEqual([
      { kind: 'text', text: 'alert(1)' },
    ]);
  });

  it('decodes decimal and hex numeric entities', () => {
    expect(parseCueMarkup('&#233;')).toEqual([{ kind: 'text', text: 'é' }]);
    expect(parseCueMarkup('&#xE9;')).toEqual([{ kind: 'text', text: 'é' }]);
  });

  it('leaves invalid numeric code points as typed', () => {
    expect(parseCueMarkup('&#1114112;')).toEqual([{ kind: 'text', text: '&#1114112;' }]);
  });

  it('decodes nbsp to a normal space', () => {
    expect(parseCueMarkup('a&nbsp;b')).toEqual([{ kind: 'text', text: 'a b' }]);
  });

  it('preserves newlines inside text', () => {
    expect(parseCueMarkup('Line one\nLine two')).toEqual([
      { kind: 'text', text: 'Line one\nLine two' },
    ]);
  });

  it('returns an empty array for empty input', () => {
    expect(parseCueMarkup('')).toEqual([]);
  });
});
