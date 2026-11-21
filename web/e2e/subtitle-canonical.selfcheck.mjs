import assert from 'node:assert/strict';
import { canonicalSubtitleQuery } from './stub.mjs';

const redirected = [
  ['query=13+Going+On+30', 'query=13+going+on+30'],
  ['query=AMELIE', 'query=amelie'],
  ['query=13%20going', 'query=13+going'],
  ['query=13%20Going', 'query=13+going'],
  ['languages=PT-BR', 'languages=pt-br'],
  ['Languages=pt-br', 'languages=pt-br'],
  ['type=Movie', 'type=movie'],
  ['moviehash=ABCDEF0123456789', 'moviehash=abcdef0123456789'],
  ['moviehash=ABCDEF0123456789&languages=pt-br', 'languages=pt-br&moviehash=abcdef0123456789'],
  ['query=13+going&languages=pt-br', 'languages=pt-br&query=13+going'],
];

const accepted = [
  'query=am%C3%A9lie',
  'query=am%c3%a9lie',
  'query=Am%C3%A9lie',
  'query=AM%C3%89LIE',
  'query=star+wars%3A+episode',
  'query=star+wars%3a+episode',
  'query=star+wars:+episode',
  'query=tom+%26+jerry',
  'query=x%2By',
  'languages=pt-br&query=amelie&type=movie',
  'languages=en,pt-br',
  'languages=pt-br,en',
];

for (const [raw, canonical] of redirected) {
  assert.equal(canonicalSubtitleQuery(raw), canonical, `redirect: ${raw}`);
  assert.notEqual(canonical, raw, `redirect rows must differ from raw: ${raw}`);
}

for (const raw of accepted) {
  assert.equal(canonicalSubtitleQuery(raw), raw, `accepted without redirect: ${raw}`);
}

console.log(
  `subtitle-canonical selfcheck: ${redirected.length} redirect rows and ${accepted.length} accepted rows passed`,
);
