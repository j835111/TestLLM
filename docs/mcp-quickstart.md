# CrudApi MCP Quickstart

## What runs where

`backend/CrudApi` now serves both:

- REST API at `http://localhost:5000`
- MCP endpoint at `http://127.0.0.1:5000/mcp`

## Start the app

```bash
DOTNET_CLI_HOME=/tmp/dotnet-home HOME=/tmp/dotnet-home dotnet run --project backend/CrudApi/CrudApi.csproj --launch-profile http
```

## Sanity check

```bash
curl http://localhost:5000/health
```

Expected:

```json
{"status":"ok","service":"CrudApi"}
```

## Start Codex with this project's MCP only

```bash
./scripts/codex-with-crudapi-mcp.sh
```

## Verify the MCP server is wired in

```bash
./scripts/codex-with-crudapi-mcp.sh mcp list --json
```

Expected server:

- `crudApiLocal`
- `http://127.0.0.1:5000/mcp`

## Suggested first prompts

- `先讀 project overview，再列出目前商品`
- `先檢查 health，再建立一個名稱為「測試商品」的商品，價格 100，庫存 3`
- `查詢 id=1 的商品，如果不存在就告訴我`

## Useful MCP resources

- `crudapi://project/overview`
- `crudapi://products/schema/request`
- `crudapi://products/schema/response`
- `crudapi://products/examples/create`
- `crudapi://products/examples/update`
