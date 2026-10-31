\set ON_ERROR_STOP on

BEGIN;

INSERT INTO movies (
    library_path,
    title,
    format,
    is_in_watchlist,
    status,
    enrichment_attempts
)
SELECT
    '/dev427/movie-' || lpad(i::text, 4, '0') || '.mp4',
    'DEV427 Group' || lpad(((i - 1) % 10)::text, 2, '0') || ' Movie' || lpad(i::text, 4, '0'),
    'mp4',
    false,
    1,
    0
FROM generate_series(1, 500) AS series(i)
ON CONFLICT (library_path) DO NOTHING;

COMMIT;
