# CrudApi MCP 設計稿

## 目標

把現有 `backend/CrudApi` 的 REST API 封裝成一個可被 MCP client 使用的遠端 MCP server，優先支援：

- 查詢與管理商品資料
- 提供 API schema / 範例作為 context
- 後續可擴充認證、審計、分權與多端點拆分

本設計刻意參考 Cloudflare 的兩種 MCP 端點思路：

1. **Docs 型 MCP**：提供文件、schema、範例
2. **API 型 MCP**：提供可執行動作

但不直接照搬 Cloudflare API 的 `search()` + `execute()` Code Mode，因為目前專案 API 面很小，直接做明確的 typed tools 會更簡潔、可測、可控。

---

## 現況盤點

目前後端 API：

- `GET /health`
- `GET /api/products`
- `GET /api/products/{id}`
- `POST /api/products`
- `PUT /api/products/{id}`
- `DELETE /api/products/{id}`

主要 DTO 與限制：

- `Name`: required, 不可空白, 最長 120
- `Description`: optional, 最長 1000
- `Price`: `0 ~ 999_999_999`
- `Stock`: `0 ~ int.MaxValue`

---

## 外部參考結論

### MCP 官方能力模型

MCP server 主要暴露三類能力：

- **Tools**：讓模型執行動作
- **Resources**：提供唯讀資料/上下文
- **Prompts**：提供可重用提示模板

### 官方 C# SDK 可直接用

C# 是官方 Tier 1 SDK，適合直接為現有 .NET 後端建立 MCP server。

### Remote MCP 傳輸建議

Remote MCP 現在建議使用 **Streamable HTTP**；SSE 已被官方與 Cloudflare 視為舊方案。

### Cloudflare 可借鏡的兩個重點

#### 1. 兩類端點分工

Cloudflare 官方自營 MCP server 至少有兩條很值得參考的線：

- Docs server：`https://docs.mcp.cloudflare.com/mcp`
- API server：`https://mcp.cloudflare.com/mcp`

這代表文件查詢與實際 API 操作可以拆開。

#### 2. Code Mode 適合超大 API，不適合你目前這個小型 CRUD

Cloudflare API MCP 只暴露 `search()` 與 `execute()` 兩個工具，是因為它要覆蓋 2500+ endpoints，主要是在省 token。你目前只有少數 CRUD endpoint，直接定義原生 tools 比較合理。

---

## 設計決策

### 決策 1：先做單一 MCP server，內部分成 docs / actions 兩類能力

第一版先不拆成兩個部署單位，而是在同一個 MCP server 中提供：

- actions tools
- docs/resources
- 少量 prompts

好處：

- 開發成本低
- 本地測試簡單
- 後續仍可無痛拆成 `CrudApi.Mcp.Actions` 與 `CrudApi.Mcp.Docs`

### 決策 2：採用 typed tools，而不是 generic search/execute

第一版直接定義：

- `health_check`
- `products_list`
- `products_get`
- `products_create`
- `products_update`
- `products_delete`

原因：

- 工具名稱清楚
- schema 明確
- 權限邊界簡單
- 測試容易寫
- 比較符合目前 API 規模

### 決策 3：保留未來切換到 Code Mode 的升級路徑

當 API 規模成長到多模組、多資源、大量 endpoints 時，再考慮新增：

- `api_search`
- `api_execute`

並以 OpenAPI spec 驅動 discovery / invocation。

---

## 建議專案結構

```text
backend/
  CrudApi/                 # 現有 REST API
  CrudApi.Mcp/             # 新增 MCP server 專案
  CrudApi.Tests/           # 現有 API 測試
  CrudApi.Mcp.Tests/       # 新增 MCP tool/resource 測試
```

### `CrudApi.Mcp` 職責

- 對外暴露 MCP 能力
- 內部透過 `HttpClient` 呼叫 `CrudApi`
- 不直接存取資料庫
- 不複製商業邏輯

這樣可保持：

- REST API 是單一真實來源
- MCP 只是另一層 adapter
- 未來可獨立部署到不同 host

---

## MCP 能力設計

## 1. Tools

### `health_check`

**用途**
- 確認底層 REST API 可用

**輸入**
- 無

**行為**
- 呼叫 `GET /health`

**輸出**
```json
{
  "status": "ok",
  "service": "CrudApi"
}
```

---

### `products_list`

**用途**
- 列出所有商品

**輸入**
- 無

**行為**
- 呼叫 `GET /api/products`

**輸出**
- `ProductResponse[]`

**備註**
- 第一版維持與 REST API 相同，不額外加入篩選
- 若未來商品量變大，可擴充分頁、排序、搜尋參數

---

### `products_get`

**用途**
- 取得單一商品

**輸入**
```json
{
  "id": 123
}
```

**行為**
- 呼叫 `GET /api/products/{id}`

**輸出**
- 成功：`ProductResponse`
- 不存在：結構化錯誤訊息

**建議錯誤**
```json
{
  "error": "not_found",
  "message": "Product 123 was not found."
}
```

---

### `products_create`

**用途**
- 建立商品

**輸入**
```json
{
  "name": "LLM 評測套件",
  "description": "用來驗證 CRUD 流程",
  "price": 1200,
  "stock": 8
}
```

**行為**
- 呼叫 `POST /api/products`

**輸出**
- `ProductResponse`

**驗證規則**
- `name` 必填，不可空白，最長 120
- `description` 最長 1000
- `price >= 0`
- `stock >= 0`

---

### `products_update`

**用途**
- 更新商品

**輸入**
```json
{
  "id": 123,
  "name": "更新後商品",
  "description": "更新後描述",
  "price": 750,
  "stock": 10
}
```

**行為**
- 呼叫 `PUT /api/products/{id}`

**輸出**
- 成功：`ProductResponse`
- 不存在：`not_found`
- 驗證失敗：`validation_error`

---

### `products_delete`

**用途**
- 刪除商品

**輸入**
```json
{
  "id": 123
}
```

**行為**
- 呼叫 `DELETE /api/products/{id}`

**輸出**
```json
{
  "deleted": true,
  "id": 123
}
```

若不存在：
```json
{
  "deleted": false,
  "error": "not_found",
  "id": 123
}
```

---

## 2. Resources

Resources 的目的不是執行動作，而是給模型可靠上下文。

### `openapi://spec`

**內容**
- 開發環境 `MapOpenApi()` 產出的 OpenAPI JSON

**用途**
- 讓 client/agent 可讀 API schema
- 後續可作為 prompt / tool fallback context

---

### `crudapi://products/schema/request`

**內容**
- `CreateProductRequest` / `UpdateProductRequest` 的欄位與驗證規則摘要

**建議格式**
```json
{
  "name": {
    "type": "string",
    "required": true,
    "maxLength": 120,
    "notWhitespace": true
  },
  "description": {
    "type": "string|null",
    "required": false,
    "maxLength": 1000
  },
  "price": {
    "type": "decimal",
    "minimum": 0,
    "maximum": 999999999
  },
  "stock": {
    "type": "integer",
    "minimum": 0
  }
}
```

---

### `crudapi://products/schema/response`

**內容**
- `ProductResponse` 欄位摘要

---

### `crudapi://products/examples/create`

**內容**
- 合法建立商品 request 範例
- 典型 validation error 範例

---

### `crudapi://products/examples/update`

**內容**
- 合法更新商品 request 範例
- not found / validation 失敗範例

---

### `crudapi://products/examples/list`

**內容**
- 商品陣列回傳範例

---

## 3. Prompts（可選，但建議保留）

Prompts 不是第一版必要，但很適合當 UX 補強。

### `create_product_from_description`

**用途**
- 使用者只給自然語言描述時，幫助 client 形成標準建立商品流程

### `review_inventory`

**用途**
- 先抓 `products_list`，再產出低庫存觀察摘要

### `summarize_catalog`

**用途**
- 將商品清單整理成簡短摘要

> 注意：這些 prompts 的價值在於 workflow 引導，不應替代 tools。

---

## 資料契約建議

## ProductResponse

```json
{
  "id": 1,
  "name": "LLM 評測套件",
  "description": "用來驗證 CRUD 流程",
  "price": 1200,
  "stock": 8,
  "createdAt": "2026-05-31T08:00:00Z",
  "updatedAt": "2026-05-31T08:00:00Z"
}
```

## 統一錯誤格式

建議 MCP tools 在向 LLM 回傳時，不要只轉拋 HTTP 狀態碼；應包成一致格式：

```json
{
  "error": "validation_error",
  "message": "The request payload is invalid.",
  "details": {
    "name": [
      "Name is required and must be 120 characters or fewer."
    ]
  }
}
```

其他錯誤類型：

- `not_found`
- `validation_error`
- `upstream_unavailable`
- `unexpected_error`

---

## 實作建議

## 建議使用模式

### MCP server 專案

- .NET console 或 ASP.NET Core minimal host
- 使用官方 C# MCP SDK
- 先支援：
  - 本地 `stdio`
  - 遠端 `Streamable HTTP`

### 與 REST API 的整合方式

優先採用：

- `CrudApi.Mcp` 用 `HttpClient` 呼叫 `CrudApi`

而不是：

- 直接共享 DbContext
- 直接引用 ProductService 操作資料庫

原因：

- 能重用現有驗證與 HTTP 契約
- MCP 和 REST 行為更一致
- 部署邊界清楚

---

## 驗證與測試策略

### 單元測試

針對 tools：

- `products_get` 404 轉換正確
- `products_create` validation error 轉換正確
- `products_delete` 成功/失敗格式正確

### 整合測試

- 啟動 `CrudApiApplicationFactory`
- 啟動 `CrudApi.Mcp`
- 呼叫 MCP tool 並驗證輸出內容

### Inspector 測試

用 MCP Inspector 檢查：

- tools 可列出
- resources 可讀取
- prompts 可發現
- Streamable HTTP 可連線

---

## 安全與權限

第一版可先做無認證本地開發，但設計上要預留：

### Phase 1
- 本地/內網使用
- 無認證或固定 bearer token

### Phase 2
- 寫入類 tools 需驗證
- 區分 read-only / write scopes

建議 scope：

- `products.read`
- `products.write`
- `health.read`
- `docs.read`

### Phase 3
- 若要對外 remote 提供
- 再導入 OAuth / access control

這部分可直接參考 Cloudflare 對 remote MCP 的做法：遠端可用 OAuth，CI/CD 則可改 bearer token。

---

## 演進路線

### Phase A：最小可用版

- 建立 `backend/CrudApi.Mcp`
- 提供 6 個 tools
- 提供 3~5 個 resources
- 本地 `stdio` 測試通過

### Phase B：遠端可用版

- 加入 Streamable HTTP
- 用 MCP Inspector 驗證
- 補 logging / error mapping

### Phase C：雲端化 / 分拆版

- 拆成 docs MCP / actions MCP
- 加 auth / scopes
- 視需要加入 product search/filter/pagination

### Phase D：大規模 API 才考慮

- OpenAPI 驅動 discovery
- `search + execute` Code Mode 類設計

---

## 最後結論

對目前這個 CRUD 專案，最合適的方案是：

1. **建立獨立 `CrudApi.Mcp` 專案**
2. **先做 typed tools，不做 generic Code Mode**
3. **同一個 MCP server 先同時提供 actions + docs resources**
4. **傳輸優先用 Streamable HTTP，並保留 stdio 便於本地開發**
5. **等 API 規模變大，再考慮拆成 Cloudflare 風格的 docs/api 雙端點，或升級為 search/execute 模式**

---

## 參考資料

- Model Context Protocol 官方首頁  
  https://modelcontextprotocol.org/

- MCP SDK 官方列表（含 C# Tier 1）  
  https://modelcontextprotocol.io/docs/sdk

- MCP resources 概念  
  https://modelcontextprotocol.io/docs/concepts/resources

- MCP prompts 概念  
  https://modelcontextprotocol.io/docs/concepts/prompts

- MCP server concepts  
  https://modelcontextprotocol.io/docs/learn/server-concepts

- MCP Inspector  
  https://modelcontextprotocol.io/docs/tools

- Cloudflare: Cloudflare's own MCP servers  
  https://developers.cloudflare.com/agents/model-context-protocol/mcp-servers-for-cloudflare/

- Cloudflare: MCP transport  
  https://developers.cloudflare.com/agents/model-context-protocol/transport/

- Cloudflare Blog: Remote MCP servers  
  https://blog.cloudflare.com/remote-model-context-protocol-servers-mcp/

- Cloudflare Blog: Code Mode  
  https://blog.cloudflare.com/code-mode-mcp/

- Cloudflare API MCP repository  
  https://github.com/cloudflare/mcp
