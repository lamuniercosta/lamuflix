import { copyFileSync, mkdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const fixtureDir = path.dirname(fileURLToPath(import.meta.url));
const sampleLibraryDir = path.resolve(fixtureDir, '../../../..', 'docker', 'sample-library');

export const DEV471_FIXTURE_FILE = path.join(fixtureDir, 'smoke-fixture.json');

export function loadDev471Fixture() {
  return JSON.parse(readFileSync(DEV471_FIXTURE_FILE, 'utf8'));
}

/**
 * Stages the fixture-correlated folder into the isolated runtime library. The media itself is
 * the existing sample already verified decodable by the T001 Chromium probe, so the smoke
 * reuses that file instead of committing a second copy of the same bytes.
 */
export function stageDev471Fixture({ libraryDir, fixture }) {
  const destination = path.join(libraryDir, fixture.folder);
  mkdirSync(destination, { recursive: true });
  copyFileSync(path.join(sampleLibraryDir, fixture.source), path.join(destination, fixture.source));
  return path.join(destination, fixture.source);
}
