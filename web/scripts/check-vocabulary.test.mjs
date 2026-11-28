import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';
import {
  collectTargetFiles,
  findHitsInSegment,
  scanMarkdownText,
  scanOpenApiText,
  scanSourceText,
  toRepoPath,
} from './check-vocabulary.mjs';

const SCAN_ENTRY = { banned: 'scan', canonical: 'Import', term: 'Import', exceptions: [] };
const WATCH_ENTRY = { banned: 'Watch', canonical: 'Playback', term: 'Playback', exceptions: [] };

let tempDirs = [];

afterEach(async () => {
  // arrange
  const dirs = tempDirs;
  tempDirs = [];
  // act
  await Promise.all(dirs.map((dir) => rm(dir, { recursive: true, force: true })));
  // assert
  expect(tempDirs).toEqual([]);
});

async function makeTempRoot() {
  // arrange
  const dir = await mkdtemp(join(tmpdir(), 'vocab-'));
  tempDirs.push(dir);
  // act
  return dir;
}

describe('check-vocabulary fixtures', () => {
  it('flags a banned word in a TSX string literal', async () => {
    // arrange
    const root = await makeTempRoot();
    const abs = join(root, 'Component.tsx');
    await writeFile(abs, 'export const copy = "Retry scan";\n');
    // act
    const hits = scanSourceText(
      'export const copy = "Retry scan";\n',
      'Component.tsx',
      [SCAN_ENTRY],
      true,
    );
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'Component.tsx', line: 1, canonical: 'Import' });
  });

  it('flags a banned word in an OpenAPI description but not in other fields', async () => {
    // arrange
    const raw = JSON.stringify({
      paths: {
        '/items': {
          get: {
            operationId: 'scanMovies',
            summary: 'List movies',
            description: 'Could not scan the library.',
          },
        },
      },
    });
    // act
    const hits = scanOpenApiText(raw, 'web/src/api/openapi.json', [SCAN_ENTRY]);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'web/src/api/openapi.json', canonical: 'Import' });
  });

  it('skips a listed exception', async () => {
    // arrange
    const entry = {
      ...SCAN_ENTRY,
      exceptions: [
        {
          file: 'Component.tsx',
          phrase: 'Retry scan',
          reason: 'Bulk discovery, not the single-folder case.',
        },
      ],
    };
    // act
    const hits = findHitsInSegment('Retry scan', 'Component.tsx', 3, [entry]);
    // assert
    expect(hits).toEqual([]);
  });

  it('matches Watch as a whole word only', async () => {
    // arrange
    const entries = [WATCH_ENTRY];
    // act
    const watched = findHitsInSegment('Because you watched Dune', 'Copy.ts', 1, entries);
    const watchlist = findHitsInSegment('On your watchlist', 'Copy.ts', 2, entries);
    const watch = findHitsInSegment('Watch now', 'Copy.ts', 3, entries);
    // assert
    expect(watched).toEqual([]);
    expect(watchlist).toEqual([]);
    expect(watch).toHaveLength(1);
  });

  it('ignores styling hooks in className literals', async () => {
    // arrange
    const source = '<div className="movie-row-poster">\n';
    // act
    const hits = scanSourceText(
      source,
      'MovieListRow.tsx',
      [{ banned: 'poster', canonical: 'Thumbnail', term: 'Thumbnail', exceptions: [] }],
      true,
    );
    // assert
    expect(hits).toEqual([]);
  });

  it('ignores fenced code blocks in markdown', async () => {
    // arrange
    const text = 'Intro.\n```mermaid\nscan --> fs\n```\nOutro.\n';
    // act
    const hits = scanMarkdownText(text, 'docs/c4.md', [SCAN_ENTRY]);
    // assert
    expect(hits).toEqual([]);
  });

  it('skips ADR files when collecting targets', async () => {
    // arrange
    const root = await makeTempRoot();
    await mkdir(join(root, 'web', 'src'), { recursive: true });
    await mkdir(join(root, 'docs', 'adr'), { recursive: true });
    await writeFile(
      join(root, 'docs', 'adr', 'ADR-0001-example.md'),
      'We chose scan for discovery.\n',
    );
    await writeFile(join(root, 'docs', 'guide.md'), 'Welcome.\n');
    await writeFile(join(root, 'README.md'), 'Welcome.\n');
    // act
    const { mdFiles } = await collectTargetFiles(root);
    const rel = mdFiles.map((abs) => toRepoPath(abs, root));
    // assert
    expect(rel).not.toContain('docs/adr/ADR-0001-example.md');
    expect(rel).toContain('docs/guide.md');
    expect(await readFile(join(root, 'docs', 'adr', 'ADR-0001-example.md'), 'utf8')).toContain(
      'scan',
    );
  });
});
