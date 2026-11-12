#!/bin/sh
# Runs inside the postgres:18 tools image, mounted read-only at /scripts. DATABASE_URL comes from
# --env-file, so it is never on a command line. Nothing here echoes the URL.
#   count              print the movie count (-1 when the movies table is absent)
#   restore clean|plain <dump-name>   pg_restore the dump from /restore into DATABASE_URL
set -eu

case "${1:-}" in
    count)
        psql "$DATABASE_URL" -tAc "select case when to_regclass('public.movies') is null then -1 else (select count(*) from movies) end"
        ;;
    restore)
        mode="${2:-plain}"
        name="${3:?dump name required}"
        case "$name" in
            */*|*..*) echo "dump name must be a file name" >&2; exit 2 ;;
        esac
        if [ "$mode" = "clean" ]; then
            pg_restore -d "$DATABASE_URL" --no-owner --no-acl --clean --if-exists "/restore/$name"
        else
            pg_restore -d "$DATABASE_URL" --no-owner --no-acl "/restore/$name"
        fi
        ;;
    *)
        echo "usage: neon-tools.sh count | restore clean|plain <dump-name>" >&2
        exit 2
        ;;
esac
