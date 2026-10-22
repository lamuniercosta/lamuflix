#!/bin/sh
# DEV-349: one-shot Ollama model initializer.
#
# Waits for the Ollama server with a finite deadline, pulls the configured
# embedding model, and exits 0 only when that model is available. The model
# name is passed to `ollama pull` as one fixed quoted argument; unset or empty
# values are rejected and no eval or code interpolation is used.
set -eu

model="${OLLAMA_EMBEDDING_MODEL:-}"
readiness_timeout="${OLLAMA_READY_TIMEOUT_SECONDS:-120}"
pull_timeout="${OLLAMA_PULL_TIMEOUT_SECONDS:-900}"
poll_interval="${OLLAMA_POLL_INTERVAL_SECONDS:-2}"

if [ -z "$model" ]; then
  echo "ollama-init: OLLAMA_EMBEDDING_MODEL must be set and non-empty" >&2
  exit 1
fi

for value in "$readiness_timeout" "$pull_timeout" "$poll_interval"; do
  case "$value" in
    '' | *[!0-9]*)
      echo "ollama-init: OLLAMA_READY_TIMEOUT_SECONDS, OLLAMA_PULL_TIMEOUT_SECONDS, and OLLAMA_POLL_INTERVAL_SECONDS must be whole seconds" >&2
      exit 1
      ;;
  esac
done

if [ "$poll_interval" -lt 1 ] || [ "$readiness_timeout" -lt 1 ] || [ "$pull_timeout" -lt 1 ]; then
  echo "ollama-init: timeout and poll values must be at least 1 second" >&2
  exit 1
fi

echo "ollama-init: waiting up to ${readiness_timeout}s for the Ollama server"
elapsed=0
ready=0
while [ "$elapsed" -lt "$readiness_timeout" ]; do
  if ollama list >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep "$poll_interval"
  elapsed=$((elapsed + poll_interval))
done

if [ "$ready" -ne 1 ]; then
  echo "ollama-init: Ollama server was not ready within ${readiness_timeout}s" >&2
  exit 1
fi

echo "ollama-init: pulling model '$model' (budget ${pull_timeout}s)"
if ! timeout "$pull_timeout" ollama pull "$model"; then
  echo "ollama-init: failed to pull model '$model'" >&2
  exit 1
fi

if ! ollama show "$model" >/dev/null 2>&1; then
  echo "ollama-init: model '$model' is not available after pull" >&2
  exit 1
fi

echo "ollama-init: model '$model' is ready"
