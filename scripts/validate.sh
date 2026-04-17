#!/usr/bin/env bash
set -euo pipefail

if ! command -v redocly >/dev/null 2>&1; then
  echo "redocly CLI not found. Install with: npm i -g @redocly/cli" >&2
  exit 1
fi

redocly lint openapi/openapi.yaml
