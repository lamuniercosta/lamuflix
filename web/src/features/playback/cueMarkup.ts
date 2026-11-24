export type CueMarkupNode =
  { kind: 'text'; text: string } | { kind: 'i' | 'b' | 'u'; children: CueMarkupNode[] };

type FormatKind = 'i' | 'b' | 'u';

type Token =
  | { type: 'text'; text: string }
  | { type: 'open'; kind: FormatKind }
  | { type: 'close'; kind: FormatKind };

interface OpenFormat {
  kind: FormatKind;
  children: CueMarkupNode[];
}

interface TreeBuilder {
  root: CueMarkupNode[];
  open: OpenFormat[];
}

const NAMED_ENTITIES: Readonly<Record<string, string>> = {
  amp: '&',
  lt: '<',
  gt: '>',
  quot: '"',
  apos: "'",
  nbsp: ' ',
};

const ENTITY_PATTERN = /&(?:#[xX]([0-9a-fA-F]+)|#([0-9]+)|(amp|lt|gt|quot|apos|nbsp));/g;
const TAG_PATTERN = /<(\/?)([a-zA-Z][a-zA-Z0-9]*)(?:\s[^<>]*)?\/?>/g;

function decodeNumeric(digits: string, radix: number): string | null {
  const codePoint = Number.parseInt(digits, radix);
  if (codePoint > 0x10ffff) return null;
  if (codePoint >= 0xd800 && codePoint <= 0xdfff) return null;
  return String.fromCodePoint(codePoint);
}

function decodeEntity(
  match: string,
  hex: string | undefined,
  decimal: string | undefined,
  named: string | undefined,
): string {
  if (hex !== undefined) return decodeNumeric(hex, 16) ?? match;
  if (decimal !== undefined) return decodeNumeric(decimal, 10) ?? match;
  return named === undefined ? match : (NAMED_ENTITIES[named] ?? match);
}

// Single pass, so an already-escaped entity such as &amp;lt; decodes once to &lt;.
function decodeEntities(raw: string): string {
  return raw.replace(ENTITY_PATTERN, decodeEntity);
}

function isFormatKind(name: string): name is FormatKind {
  return name === 'i' || name === 'b' || name === 'u';
}

// Every tag except i/b/u is dropped; its attributes are never read.
function tagToken(slash: string, name: string): Token | null {
  const lower = name.toLowerCase();
  if (!isFormatKind(lower)) return null;
  return slash === '/' ? { type: 'close', kind: lower } : { type: 'open', kind: lower };
}

function tokenize(decoded: string): Token[] {
  const tokens: Token[] = [];
  let cursor = 0;
  for (const match of decoded.matchAll(TAG_PATTERN)) {
    const index = match.index ?? 0;
    tokens.push({ type: 'text', text: decoded.slice(cursor, index) });
    const token = tagToken(match[1] ?? '', match[2] ?? '');
    if (token) tokens.push(token);
    cursor = index + match[0].length;
  }
  tokens.push({ type: 'text', text: decoded.slice(cursor) });
  return tokens;
}

function currentChildren(builder: TreeBuilder): CueMarkupNode[] {
  const top = builder.open[builder.open.length - 1];
  return top ? top.children : builder.root;
}

// Appends a node, merging adjacent text and omitting empty text.
function append(children: CueMarkupNode[], node: CueMarkupNode): void {
  if (node.kind !== 'text') {
    children.push(node);
    return;
  }
  if (node.text === '') return;
  const last = children[children.length - 1];
  if (last?.kind === 'text') {
    children[children.length - 1] = { kind: 'text', text: last.text + node.text };
    return;
  }
  children.push(node);
}

function closeTop(builder: TreeBuilder): void {
  const frame = builder.open.pop();
  if (!frame) return;
  append(currentChildren(builder), { kind: frame.kind, children: frame.children });
}

function lastOpenIndex(open: readonly OpenFormat[], kind: FormatKind): number {
  for (let index = open.length - 1; index >= 0; index -= 1) {
    if (open[index]?.kind === kind) return index;
  }
  return -1;
}

// Closes the nearest open node of this kind and anything opened after it.
// A close tag with no matching open is ignored.
function closeFormat(builder: TreeBuilder, kind: FormatKind): void {
  const index = lastOpenIndex(builder.open, kind);
  if (index === -1) return;
  while (builder.open.length > index) closeTop(builder);
}

function applyToken(builder: TreeBuilder, token: Token): void {
  if (token.type === 'text') {
    append(currentChildren(builder), { kind: 'text', text: token.text });
  } else if (token.type === 'open') {
    builder.open.push({ kind: token.kind, children: [] });
  } else {
    closeFormat(builder, token.kind);
  }
}

export function parseCueMarkup(raw: string): CueMarkupNode[] {
  const builder: TreeBuilder = { root: [], open: [] };
  for (const token of tokenize(decodeEntities(raw))) applyToken(builder, token);
  while (builder.open.length > 0) closeTop(builder);
  return builder.root;
}
