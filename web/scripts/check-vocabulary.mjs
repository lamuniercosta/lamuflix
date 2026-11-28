import { readdir, readFile } from 'node:fs/promises';
import { dirname, join, relative, resolve, sep } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const SOURCE_EXTENSIONS = new Set(['.ts', '.tsx', '.js', '.jsx', '.mjs', '.mts', '.cts']);

export function escapeRegExp(value) {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

export function toRepoPath(absPath, repoRoot) {
  return relative(repoRoot, absPath).split(sep).join('/');
}

export async function loadConfig(configPath) {
  const raw = await readFile(configPath, 'utf8');
  const config = JSON.parse(raw);
  return config.entries ?? [];
}

export function isException(entry, repoPath, segment) {
  const lowered = segment.toLowerCase();
  return (entry.exceptions ?? []).some(
    (exception) =>
      (repoPath === exception.file || repoPath.endsWith(exception.file)) &&
      lowered.includes(exception.phrase.toLowerCase()),
  );
}

export function findHitsInSegment(segment, repoPath, line, entries) {
  const hits = [];
  for (const entry of entries) {
    const pattern = new RegExp(`\\b${escapeRegExp(entry.banned)}\\b`, 'i');
    const match = pattern.exec(segment);
    if (match && !isException(entry, repoPath, segment)) {
      hits.push({
        file: repoPath,
        line,
        matched: match[0],
        canonical: entry.canonical,
        term: entry.term,
      });
    }
  }
  return hits;
}

const STRING_PATTERN = /'(?:[^'\\\n]|\\.)*'|"(?:[^"\\\n]|\\.)*"|`(?:[^`\\]|\\.)*`/g;
const NON_COPY_ATTRIBUTE_PATTERN =
  /(?:className|data-testid)\s*=\s*("(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*'|\{(?:`(?:[^`\\]|\\.)*`|"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*')\})/g;
const LOG_CALL_PATTERN = /(?:\bconsole|\blogger)\s*\.\s*(?:log|debug|info|warn|error|trace)\s*\(/g;

function findLogRanges(text) {
  const ranges = [];
  LOG_CALL_PATTERN.lastIndex = 0;
  let open;
  while ((open = LOG_CALL_PATTERN.exec(text)) !== null) {
    let index = open.index + open[0].length;
    let depth = 1;
    let quote = null;
    while (index < text.length && depth > 0) {
      const ch = text[index];
      if (quote !== null) {
        if (ch === '\\') {
          index += 2;
          continue;
        }
        if (ch === quote) {
          quote = null;
        }
        index += 1;
        continue;
      }
      if (ch === '"' || ch === "'" || ch === '`') {
        quote = ch;
        index += 1;
        continue;
      }
      if (ch === '/' && text[index + 1] === '/') {
        const newline = text.indexOf('\n', index);
        index = newline === -1 ? text.length : newline;
        continue;
      }
      if (ch === '/' && text[index + 1] === '*') {
        const end = text.indexOf('*/', index + 2);
        index = end === -1 ? text.length : end + 2;
        continue;
      }
      if (ch === '(') {
        depth += 1;
      } else if (ch === ')') {
        depth -= 1;
      }
      index += 1;
    }
    ranges.push([open.index, index]);
  }
  return ranges;
}

function isInRanges(ranges, offset) {
  return ranges.some(([start, end]) => offset >= start && offset < end);
}

function literalParts(literal) {
  const inner = literal.slice(1, -1);
  if (literal.startsWith('`')) {
    return inner.split(/\$\{[^}]*\}/);
  }
  return [inner];
}

export function extractSegments(text, isTsx) {
  const segments = [];
  const strippedLines = [];
  const logRanges = findLogRanges(text);
  const lines = text.split('\n');
  let inBlockComment = false;
  let offset = 0;
  lines.forEach((rawLine, index) => {
    const line = index + 1;
    const lineStart = offset;
    offset += rawLine.length + 1;
    let code = rawLine;
    let shift = 0;
    if (inBlockComment) {
      const end = code.indexOf('*/');
      if (end === -1) {
        strippedLines.push('');
        return;
      }
      shift = end + 2;
      code = code.slice(shift);
      inBlockComment = false;
    }
    const literals = [];
    code = code.replace(NON_COPY_ATTRIBUTE_PATTERN, (match) => ' '.repeat(match.length));
    STRING_PATTERN.lastIndex = 0;
    let literalMatch;
    const literalAt = [];
    while ((literalMatch = STRING_PATTERN.exec(code)) !== null) {
      literalAt.push({ match: literalMatch[0], offset: lineStart + shift + literalMatch.index });
    }
    code = code.replace(STRING_PATTERN, (match) => ' '.repeat(match.length));
    for (const { match, offset: literalOffset } of literalAt) {
      if (!isInRanges(logRanges, literalOffset)) {
        literals.push(match);
      }
    }
    for (const literal of literals) {
      for (const part of literalParts(literal)) {
        if (part.trim() !== '') {
          segments.push({ text: part, line });
        }
      }
    }
    let stripped = code;
    for (;;) {
      const start = stripped.indexOf('/*');
      if (start === -1) {
        break;
      }
      const end = stripped.indexOf('*/', start + 2);
      if (end === -1) {
        stripped = stripped.slice(0, start);
        inBlockComment = true;
        break;
      }
      stripped = stripped.slice(0, start) + ' '.repeat(end + 2 - start) + stripped.slice(end + 2);
    }
    const commentAt = stripped.indexOf('//');
    if (commentAt !== -1) {
      stripped = stripped.slice(0, commentAt);
    }
    strippedLines.push(stripped);
  });
  if (isTsx) {
    const strippedFull = strippedLines.join('\n');
    for (const gap of collectJsxTextGaps(strippedFull)) {
      segments.push({ text: gap.text, line: gap.line });
    }
  }
  return segments;
}

function readTag(text, start) {
  let index = start + 1;
  let closing = false;
  if (text[index] === '/') {
    closing = true;
    index += 1;
  }
  if (text[index] === '>') {
    return { end: index + 1, closing, selfClosing: false };
  }
  if (!/[A-Za-z]/.test(text[index] ?? '')) {
    return null;
  }
  while (index < text.length && /[A-Za-z0-9.]/.test(text[index])) {
    index += 1;
  }
  let selfClosing = false;
  for (;;) {
    const ch = text[index];
    if (ch === undefined || ch === '<') {
      return null;
    }
    if (ch === '>') {
      index += 1;
      break;
    }
    if (ch === '"' || ch === "'" || ch === '`') {
      const quote = ch;
      index += 1;
      while (index < text.length && text[index] !== quote) {
        if (text[index] === '\\') {
          index += 1;
        }
        index += 1;
      }
      index += 1;
      continue;
    }
    if (ch === '{') {
      let depth = 1;
      index += 1;
      let quote = null;
      while (index < text.length && depth > 0) {
        const inner = text[index];
        if (quote !== null) {
          if (inner === '\\') {
            index += 2;
            continue;
          }
          if (inner === quote) {
            quote = null;
          }
          index += 1;
          continue;
        }
        if (inner === '"' || inner === "'" || inner === '`') {
          quote = inner;
          index += 1;
          continue;
        }
        if (inner === '{') {
          depth += 1;
        } else if (inner === '}') {
          depth -= 1;
        }
        index += 1;
      }
      if (depth !== 0) {
        return null;
      }
      continue;
    }
    if (ch === '/' && text[index + 1] === '>') {
      selfClosing = true;
      index += 2;
      break;
    }
    index += 1;
  }
  return { end: index, closing, selfClosing };
}

export function collectJsxTextGaps(text) {
  const tags = [];
  let index = 0;
  while (index < text.length) {
    if (text[index] === '<' && /[A-Za-z/>]/.test(text[index + 1] ?? '')) {
      const tag = readTag(text, index);
      if (tag !== null) {
        tags.push({ start: index, end: tag.end, closing: tag.closing });
        index = tag.end;
        continue;
      }
    }
    index += 1;
  }
  const gaps = [];
  for (let at = 1; at < tags.length; at += 1) {
    if (tags[at - 1].closing) {
      continue;
    }
    const raw = text.slice(tags[at - 1].end, tags[at].start);
    const withoutExpressions = raw.replace(/\{(?:[^{}]|\{[^{}]*\})*\}/g, (match) =>
      ' '.repeat(match.length),
    );
    if (/[{}]/.test(withoutExpressions)) {
      continue;
    }
    const first = withoutExpressions.search(/\S/);
    if (first === -1) {
      continue;
    }
    const line = text.slice(0, tags[at - 1].end + first).split('\n').length;
    gaps.push({ text: withoutExpressions.trim(), line });
  }
  return gaps;
}

export async function collectTargetFiles(repoRoot) {
  const srcFiles = [];
  const srcRoot = join(repoRoot, 'web', 'src');
  const queue = [srcRoot];
  while (queue.length > 0) {
    const dir = queue.pop();
    let names;
    try {
      names = await readdir(dir, { withFileTypes: true });
    } catch (err) {
      if (err?.code === 'ENOENT') {
        break;
      }
      throw err;
    }
    for (const name of names) {
      const abs = join(dir, name.name);
      if (name.isDirectory()) {
        if (name.name !== '__tests__' && name.name !== 'test') {
          queue.push(abs);
        }
      } else if (
        SOURCE_EXTENSIONS.has('.' + name.name.split('.').pop()) &&
        !/\.test\./.test(name.name) &&
        !/\.spec\./.test(name.name)
      ) {
        srcFiles.push(abs);
      }
    }
  }
  const mdFiles = [];
  const readme = join(repoRoot, 'README.md');
  mdFiles.push(readme);
  const docsRoot = join(repoRoot, 'docs');
  const docsQueue = [docsRoot];
  while (docsQueue.length > 0) {
    const dir = docsQueue.pop();
    let names;
    try {
      names = await readdir(dir, { withFileTypes: true });
    } catch (err) {
      if (err?.code === 'ENOENT') {
        break;
      }
      throw err;
    }
    for (const name of names) {
      const abs = join(dir, name.name);
      if (name.isDirectory()) {
        if (name.name !== 'adr') {
          docsQueue.push(abs);
        }
      } else if (name.name.endsWith('.md')) {
        mdFiles.push(abs);
      }
    }
  }
  return { srcFiles, openApi: join(repoRoot, 'web', 'src', 'api', 'openapi.json'), mdFiles };
}

export function scanSourceText(text, repoPath, entries, isTsx) {
  const hits = [];
  for (const segment of extractSegments(text, isTsx)) {
    hits.push(...findHitsInSegment(segment.text, repoPath, segment.line, entries));
  }
  return hits;
}

export function scanMarkdownText(text, repoPath, entries) {
  const hits = [];
  let inFence = false;
  text.split('\n').forEach((lineText, index) => {
    if (/^\s*(```|~~~)/.test(lineText)) {
      inFence = !inFence;
      return;
    }
    if (inFence || lineText.trim() === '') {
      return;
    }
    hits.push(...findHitsInSegment(lineText, repoPath, index + 1, entries));
  });
  return hits;
}

const JSON_STRING_PATTERN = /"(?:[^"\\]|\\(?:["\\/bfnrt]|u[0-9a-fA-F]{4}))*"/g;

export function scanOpenApiText(raw, repoPath, entries) {
  const hits = [];
  try {
    JSON.parse(raw);
  } catch {
    return [
      {
        file: repoPath,
        line: 1,
        matched: '',
        canonical: '',
        term: 'openapi.json is not valid JSON',
      },
    ];
  }
  JSON_STRING_PATTERN.lastIndex = 0;
  let tokenMatch;
  let pendingKey = null;
  while ((tokenMatch = JSON_STRING_PATTERN.exec(raw)) !== null) {
    const token = tokenMatch[0];
    if (/^\s*:/.test(raw.slice(tokenMatch.index + token.length))) {
      try {
        pendingKey = JSON.parse(token);
      } catch {
        pendingKey = null;
      }
      continue;
    }
    if (pendingKey === 'description' || pendingKey === 'summary') {
      let value;
      try {
        value = JSON.parse(token);
      } catch {
        continue;
      }
      if (typeof value === 'string' && value.trim() !== '') {
        const line = raw.slice(0, tokenMatch.index).split('\n').length;
        hits.push(...findHitsInSegment(value, repoPath, line, entries));
      }
    }
  }
  return hits;
}

export async function run(repoRoot, entries) {
  const hits = [];
  const { srcFiles, openApi, mdFiles } = await collectTargetFiles(repoRoot);
  for (const abs of srcFiles) {
    const repoPath = toRepoPath(abs, repoRoot);
    const text = await readFile(abs, 'utf8');
    const isTsx = abs.endsWith('.tsx') || abs.endsWith('.jsx');
    hits.push(...scanSourceText(text, repoPath, entries, isTsx));
  }
  try {
    const raw = await readFile(openApi, 'utf8');
    hits.push(...scanOpenApiText(raw, toRepoPath(openApi, repoRoot), entries));
  } catch (err) {
    if (err?.code !== 'ENOENT') {
      throw err;
    }
  }
  for (const abs of mdFiles) {
    const repoPath = toRepoPath(abs, repoRoot);
    let text;
    try {
      text = await readFile(abs, 'utf8');
    } catch (err) {
      if (err?.code === 'ENOENT') {
        continue;
      }
      throw err;
    }
    hits.push(...scanMarkdownText(text, repoPath, entries));
  }
  return hits;
}

export function formatHit(hit) {
  return `${hit.file}:${hit.line}: found "${hit.matched}" — use "${hit.canonical}" instead (${hit.term})`;
}

export async function main(argv = process.argv.slice(2)) {
  const scriptDir = dirname(fileURLToPath(import.meta.url));
  const repoRoot = resolve(scriptDir, '..', '..');
  let configPath = join(repoRoot, 'vocabulary-check.json');
  const flagAt = argv.indexOf('--config');
  if (flagAt !== -1 && argv[flagAt + 1]) {
    configPath = resolve(argv[flagAt + 1]);
  }
  const entries = await loadConfig(configPath);
  const hits = await run(repoRoot, entries);
  for (const hit of hits) {
    console.log(formatHit(hit));
  }
  if (hits.length > 0) {
    console.log(`${hits.length} banned word(s) found.`);
    process.exitCode = 1;
  }
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  await main();
}
