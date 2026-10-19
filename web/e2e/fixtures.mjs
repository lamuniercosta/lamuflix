import { copyFileSync, mkdirSync } from 'node:fs';
import path from 'node:path';

export const SEED_MOVIES = [
  {
    source: 'Big Buck Bunny (2008).mp4',
    relativePath: 'seeded/Big Buck Bunny (2008)',
    title: 'Big Buck Bunny',
    year: 2008,
    imdbId: 'tt2008001',
    genre: 'Animation, Comedy',
    runtimeMinutes: 10,
    rating: '8.0',
  },
  {
    source: 'Elephants Dream (2006).mp4',
    relativePath: 'seeded/Elephants Dream (2006)',
    title: 'Elephants Dream',
    year: 2006,
    imdbId: 'tt2006001',
    genre: 'Drama',
    runtimeMinutes: 11,
    rating: '7.6',
  },
];

export const RESERVED_MOVIE = {
  source: 'Sintel (2010).mp4',
  title: 'Sintel',
  year: 2010,
  imdbId: 'tt2010001',
  genre: 'Animation',
  runtimeMinutes: 15,
  rating: '7.7',
  folders: {
    initial: 'pending/Sintel (2010)',
    retry: 'pending-retry/Sintel (2010)',
  },
};

const OTHER_MOVIE = {
  source: 'Tears of Steel (2012).mp4',
  relativePath: 'available/Tears of Steel (2012)',
};

function copyMovie({ sampleLibraryDir, libraryDir, relativePath, source }) {
  const destination = path.join(libraryDir, relativePath);
  mkdirSync(destination, { recursive: true });
  copyFileSync(path.join(sampleLibraryDir, source), path.join(destination, source));
}

export function generateFixtures({ sampleLibraryDir, libraryDir }) {
  for (const movie of SEED_MOVIES) {
    copyMovie({
      sampleLibraryDir,
      libraryDir,
      relativePath: movie.relativePath,
      source: movie.source,
    });
  }

  for (const relativePath of Object.values(RESERVED_MOVIE.folders)) {
    copyMovie({ sampleLibraryDir, libraryDir, relativePath, source: RESERVED_MOVIE.source });
  }

  copyMovie({
    sampleLibraryDir,
    libraryDir,
    relativePath: OTHER_MOVIE.relativePath,
    source: OTHER_MOVIE.source,
  });

  return {
    seedFolders: SEED_MOVIES.map((movie) => movie.relativePath),
    reservedFolders: { ...RESERVED_MOVIE.folders },
  };
}
