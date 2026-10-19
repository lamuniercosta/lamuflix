const IMDB_ID_PATTERN = /tt\d{7,}/;

/**
 * Pulls the first IMDb title id out of a pasted id or title URL, or returns null when the
 * input contains none.
 */
export function extractImdbId(input: string): string | null {
  const match = IMDB_ID_PATTERN.exec(input);
  return match ? match[0] : null;
}
