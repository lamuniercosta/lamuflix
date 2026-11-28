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
const CLASSNAME_PATTERN =
  /className\s*=\s*("(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*'|\{(?:`(?:[^`\\]|\\.)*`|"(?:[^"\\\n]|\\.)*"|'(?:[^'\\\n]|\\.)*')\})/g;

function literalParts(literal) {
  const inner = literal.slice(1, -1);
  if (literal.startsWith('`')) {
    return inner.split(/\$\{[^}]*\}/);
  }
  return [inner];
}

export function extractSegments(text, isTsx) {
  const segments = [];
  const lines = text.split('\n');
  let inBlockComment = false;
  lines.forEach((rawLine, index) => {
    const line = index + 1;
    let code = rawLine;
    if (inBlockComment) {
      const end = code.indexOf('*/');
      if (end === -1) {
        return;
      }
      code = code.slice(end + 2);
      inBlockComment = false;
    }
    const literals = [];
    code = code.replace(CLASSNAME_PATTERN, (match) => ' '.repeat(match.length));
    code = code.replace(STRING_PATTERN, (match) => {
      literals.push(match);
      return ' '.repeat(match.length);
    });
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
    if (isTsx) {
      const jsxPattern = />((?:[^<>{}]|\{[^{}]*\})+)</g;
      let jsxMatch;
      while ((jsxMatch = jsxPattern.exec(stripped)) !== null) {
        const inner = jsxMatch[1].replace(/\{[^{}]*\}/g, ' ').trim();
        if (inner !== '') {
          segments.push({ text: inner, line });
        }
      }
    }
  });
  return segments;
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
        if (name.name !== '__tests__') {
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

function collectOpenApiStrings(node, out) {
  if (Array.isArray(node)) {
    for (const item of node) {
      collectOpenApiStrings(item, out);
    }
  } else if (node !== null && typeof node === 'object') {
    for (const [key, item] of Object.entries(node)) {
      if ((key === 'description' || key === 'summary') && typeof item === 'string') {
        out.push(item);
      } else {
        collectOpenApiStrings(item, out);
      }
    }
  }
}

export function scanOpenApiText(raw, repoPath, entries) {
  const hits = [];
  let parsed;
  try {
    parsed = JSON.parse(raw);
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
  const strings = [];
  collectOpenApiStrings(parsed, strings);
  const seen = new Set();
  for (const value of strings) {
    if (typeof value !== 'string' || value.trim() === '' || seen.has(value)) {
      continue;
    }
    seen.add(value);
    const at = raw.indexOf(value.slice(0, 60));
    const line = at === -1 ? 1 : raw.slice(0, at).split('\n').length;
    hits.push(...findHitsInSegment(value, repoPath, line, entries));
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
