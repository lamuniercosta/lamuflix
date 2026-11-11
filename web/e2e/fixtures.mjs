import { copyFileSync, mkdirSync, writeFileSync } from 'node:fs';
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

export const MANUAL_MATCH_MOVIES = [
  {
    smokeId: 'SMOKE-001',
    source: 'Big Buck Bunny (2008).mp4',
    title: 'Memento',
    year: 2000,
    imdbId: 'tt0209144',
    rating: '8.4',
    director: 'Christopher Nolan',
    genre: 'Thriller',
    runtimeMinutes: 113,
  },
  {
    smokeId: 'SMOKE-002',
    source: 'Elephants Dream (2006).mp4',
    title: 'O Brother, Where Art Thou',
    year: 2000,
    imdbId: 'tt0190590',
    rating: '7.7',
    director: 'Joel Coen',
    genre: 'Comedy',
    runtimeMinutes: 107,
  },
].map((movie) => ({
  ...movie,
  relativePath: `manual-match/${movie.title} (${movie.year})`,
  filename: `${movie.title} (${movie.year}).mp4`,
}));

const OTHER_MOVIE = {
  source: 'Tears of Steel (2012).mp4',
  relativePath: 'available/Tears of Steel (2012)',
};

export const SUBTITLE_FIXTURES = {
  listed: {
    title: 'Big Buck Bunny',
    relativePath: 'seeded/Big Buck Bunny (2008)',
    primaryFileName: 'Big Buck Bunny (2008).mp4',
    tracks: [
      {
        fileName: 'Big Buck Bunny (2008).default.srt',
        label: 'Default',
        language: 'und',
        isDefault: true,
        cueText: 'Default subtitle cue.',
      },
      {
        fileName: 'Big Buck Bunny (2008).en.srt',
        label: 'English',
        language: 'en',
        isDefault: false,
        cueText: 'English subtitle cue.',
      },
      {
        fileName: 'Big Buck Bunny (2008).pob.srt',
        label: 'Portuguese (Brazil)',
        language: 'pt-BR',
        isDefault: false,
        cueText: 'Legenda em português: ação.',
      },
    ],
  },
  broken: {
    title: 'Elephants Dream',
    relativePath: 'seeded/Elephants Dream (2006)',
    primaryFileName: 'Elephants Dream (2006).mp4',
    tracks: [
      {
        fileName: 'Elephants Dream (2006).srt',
        label: 'Subtitles',
        language: 'und',
        isDefault: false,
        cueText: null,
      },
    ],
  },
};

const SRT_CUE_WINDOW = '00:00:00,000 --> 00:00:59,000';

function writeSidecar(directory, track) {
  const body =
    track.cueText === null
      ? 'this sidecar is not a subtitle document\n'
      : `1\n${SRT_CUE_WINDOW}\n${track.cueText}\n`;
  writeFileSync(path.join(directory, track.fileName), body, 'utf8');
}

function copyMovie({ sampleLibraryDir, libraryDir, relativePath, source, filename = source }) {
  const destination = path.join(libraryDir, relativePath);
  mkdirSync(destination, { recursive: true });
  copyFileSync(path.join(sampleLibraryDir, source), path.join(destination, filename));
}

export function generateFixtures({ sampleLibraryDir, libraryDir }) {
  for (const movie of [...SEED_MOVIES, ...MANUAL_MATCH_MOVIES]) {
    copyMovie({
      sampleLibraryDir,
      libraryDir,
      relativePath: movie.relativePath,
      source: movie.source,
      filename: movie.filename,
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

  for (const fixture of Object.values(SUBTITLE_FIXTURES)) {
    const directory = path.join(libraryDir, fixture.relativePath);
    for (const track of fixture.tracks) {
      writeSidecar(directory, track);
    }
  }

  return {
    seedFolders: SEED_MOVIES.map((movie) => movie.relativePath),
    reservedFolders: { ...RESERVED_MOVIE.folders },
  };
}
