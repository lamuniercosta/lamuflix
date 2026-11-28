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
const SCANNING_ENTRY = { banned: 'scanning', canonical: 'Import', term: 'Import', exceptions: [] };
const SCANNED_FILE_ENTRY = {
  banned: 'scanned file',
  canonical: 'Primary video',
  term: 'Primary video',
  exceptions: [],
};
const POSTER_ENTRY = {
  banned: 'poster',
  canonical: 'Thumbnail',
  term: 'Thumbnail',
  exceptions: [],
};
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

  it('skips log-level calls but keeps adjacent UI copy on the same line', async () => {
    // arrange
    const source =
      'logger.info("Could not scan the library."); const copy = "Retry scan";\nconsole.warn("scan failed");\n';
    // act
    const hits = scanSourceText(source, 'Copy.ts', [SCAN_ENTRY], false);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'Copy.ts', line: 1, canonical: 'Import' });
  });

  it('skips multiline log-level calls', async () => {
    // arrange
    const source =
      'logger.info(\n  "Could not scan the library.",\n  { retry: true },\n);\nconst copy = "Retry scan";\n';
    // act
    const hits = scanSourceText(source, 'Copy.ts', [SCAN_ENTRY], false);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'Copy.ts', line: 5, canonical: 'Import' });
  });

  it('flags Scanning progress copy via the scanning entry', async () => {
    // arrange
    const source = '<p className="lamu-dialog-hint">Scanning library…</p>\n';
    // act
    const hits = scanSourceText(source, 'BulkImportDialog.tsx', [SCANNING_ENTRY], true);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({
      file: 'BulkImportDialog.tsx',
      line: 1,
      canonical: 'Import',
    });
  });

  it('flags element text spread across lines at its own line', async () => {
    // arrange
    const source = '<p role="status">\n  Scanning library…\n</p>\n';
    // act
    const hits = scanSourceText(source, 'BulkImportDialog.tsx', [SCANNING_ENTRY], true);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'BulkImportDialog.tsx', line: 2 });
  });

  it('keeps whole-word matching for scan-adjacent copy', async () => {
    // arrange
    const entries = [SCAN_ENTRY, SCANNING_ENTRY];
    // act
    const rescan = findHitsInSegment('Rescan the library', 'Copy.ts', 1, entries);
    const retry = findHitsInSegment('Retry scan', 'Copy.ts', 2, [SCAN_ENTRY]);
    const scanningFiles = findHitsInSegment('scanning files', 'Copy.ts', 3, [SCANNED_FILE_ENTRY]);
    // assert
    expect(rescan).toEqual([]);
    expect(retry).toHaveLength(1);
    expect(scanningFiles).toEqual([]);
  });

  it('flags the scanned file selection phrase', async () => {
    // arrange
    const entries = [SCANNED_FILE_ENTRY];
    // act
    const hit = findHitsInSegment('Choose the scanned file to play', 'Copy.ts', 1, entries);
    // assert
    expect(hit).toHaveLength(1);
    expect(hit[0]).toMatchObject({ canonical: 'Primary video' });
  });

  it('flags poster copy without a broad file allowance', async () => {
    // arrange
    const source =
      '<img alt={`${title} poster`} />\n<div>Poster unavailable</div>\nconst hint = "See poster gallery";\n';
    // act
    const hits = scanSourceText(source, 'MoviePoster.tsx', [POSTER_ENTRY], true);
    // assert
    expect(hits).toHaveLength(3);
    expect(hits.map((hit) => hit.line).sort()).toEqual([1, 2, 3]);
  });

  it('skips data-testid values but keeps alt text on the same element', async () => {
    // arrange
    const source = '<img data-testid="poster-grid" alt="Movie poster" />\n';
    // act
    const hits = scanSourceText(source, 'MoviePoster.tsx', [POSTER_ENTRY], true);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'MoviePoster.tsx', line: 1 });
  });

  it('excludes web/src/test helpers when collecting targets', async () => {
    // arrange
    const root = await makeTempRoot();
    await mkdir(join(root, 'web', 'src', 'test'), { recursive: true });
    await writeFile(
      join(root, 'web', 'src', 'test', 'helper.ts'),
      'export const copy = "Retry scan";\n',
    );
    await writeFile(join(root, 'web', 'src', 'keep.ts'), 'export const ok = 1;\n');
    // act
    const { srcFiles } = await collectTargetFiles(root);
    const rel = srcFiles.map((abs) => toRepoPath(abs, root));
    // assert
    expect(rel).toContain('web/src/keep.ts');
    expect(rel).not.toContain('web/src/test/helper.ts');
  });

  it('reports each duplicate OpenAPI description at its own line', async () => {
    // arrange
    const raw = [
      '{',
      '  "paths": {',
      '    "/a": {',
      '      "get": {',
      '        "operationId": "scan",',
      '        "summary": "List movies",',
      '        "description": "First scan here."',
      '      }',
      '    },',
      '    "/b": {',
      '      "get": {',
      '        "summary": "List movies",',
      '        "description": "First scan here."',
      '      }',
      '    }',
      '  }',
      '}',
    ].join('\n');
    // act
    const hits = scanOpenApiText(raw, 'web/src/api/openapi.json', [SCAN_ENTRY]);
    // assert
    expect(hits).toHaveLength(2);
    expect(hits.map((hit) => hit.line)).toEqual([7, 13]);
  });

  it('ignores the same text in other keys and pins escaped values', async () => {
    // arrange
    const raw = [
      '{',
      '  "operationId": "scan",',
      '  "description": "Could not scan\\nthe library."',
      '}',
    ].join('\n');
    // act
    const hits = scanOpenApiText(raw, 'web/src/api/openapi.json', [SCAN_ENTRY]);
    // assert
    expect(hits).toHaveLength(1);
    expect(hits[0]).toMatchObject({ file: 'web/src/api/openapi.json', line: 3 });
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
