#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="/mnt/d/project/TestLLM"
MCP_URL="${CRUDAPI_MCP_URL:-http://127.0.0.1:5000/mcp}"

exec codex \
  -C "$PROJECT_ROOT" \
  -c "mcp_servers.crudApiLocal.url=\"$MCP_URL\"" \
  "$@"
