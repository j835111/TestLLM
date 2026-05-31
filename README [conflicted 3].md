# TestLLM

本專案用於規劃與記錄三個階段的實驗，核心主題是觀察同一個商品管理場景，從傳統前後端架構逐步演進到由 Agent 主導資料操作與資料處理時，系統形態、互動方式與責任分工會如何改變。

## Demo 專案場景

這個 demo 專案以「商品與庫存管理」為場景，模擬一個可同時提供人類操作介面與 LLM / Agent 工具呼叫的產品管理系統。

`frontend/` 提供一個 Vue 3 + Vite + Tailwind CSS 的商品管理介面，使用者可以查看商品列表、新增商品、編輯商品、刪除商品，並即時看到商品筆數與庫存總值。前端表單目前圍繞商品名稱、描述、價格與庫存四個欄位，且包含基本輸入驗證與錯誤訊息處理。

`backend/` 提供一個 ASP.NET Core .NET 10 的 CRUD API，負責商品資料的新增、查詢、更新與刪除，並透過 SQL Server 持久化資料。除了傳統的 `/api/products` REST 介面外，後端也暴露 `/mcp` 端點，提供 `products_list`、`products_get`、`products_create`、`products_update`、`products_delete` 等工具，讓同一套商品資料流程也能被 LLM / Agent 以工具呼叫方式操作。

## 用途導向資料夾結構

如果依「用途」而不是依「技術名稱」來看，這個專案目前可整理成下面幾層：

```text
TestLLM/
├─ frontend/                  # 第一階段的人類操作介面
├─ backend/                   # 第一、二階段共用的服務層與 MCP 能力
├─ experiments/
│  └─ phase3/                 # 第三階段：Agent 直接處理資料的實驗區
├─ docs/                      # 補充說明文件與 MCP 設計筆記
├─ scripts/                   # 啟動或整合流程用腳本
├─ .omx/ .codex/ .agents/     # Codex / OMX 協作與執行狀態
└─ README.md                  # 專案總說明與實驗摘要
```

各資料夾的目的可進一步理解為：

- `frontend/`：偏向「使用者互動層」，是第一階段對照組最重要的入口。
- `backend/`：偏向「服務與工具層」，同時支援傳統 REST API 與第二階段的 MCP 工具呼叫。
- `experiments/phase3/`：偏向「Agent 自主資料處理實驗」，是第三階段最核心的目錄，目前包含 functional brief、儲存策略參考與 file-backed prototype。
- `docs/`：偏向「設計與說明文件」，補充 MCP 設計與使用脈絡。
- `scripts/`：偏向「輔助操作」，放置啟動或整合用途的腳本，而不是核心業務邏輯。
- `.omx/`、`.codex/`、`.agents/`：偏向「代理協作與執行狀態」，屬於工具運行上下文，不是商品管理實驗本體。

另外，`frontend/` 內目前還有幾個應視為產物而非核心實驗內容的子目錄：

- `frontend/node_modules/`：前端依賴安裝結果。
- `frontend/dist/`：前端建置輸出。
- `frontend/coverage/`：前端測試覆蓋率報告。

因此，若從閱讀與維護角度出發，建議優先把注意力放在 `README.md`、`frontend/`、`backend/`、`experiments/phase3/` 與 `docs/`，而不要先從工具狀態目錄或建置產物開始。

## 實驗階段

### 第一階段：傳統前後端分離服務

- 研究動機：建立傳統 Web 應用架構的對照組，作為後續 Agent / MCP 導向方案的比較基準。
- 方法：保留 `frontend/` 與 `backend/` 的典型前後端分離架構，由使用者透過前端介面操作商品 CRUD，後端提供 REST API、資料驗證與資料庫存取能力。
- 觀察結果：此階段可完整支援商品列表、新增、編輯、刪除與庫存總值展示，互動流程清楚，適合作為後續實驗的 baseline。
- 限制：需要同時維護前端與後端兩層系統，功能變更通常必須跨層協調；此外，操作入口仍以人類使用者介面為主，Agent 並非主要互動角色。

### 第二階段：移除前端，改由 MCP 端點直接提供 Agent 操作

- 研究動機：驗證在移除傳統前端之後，是否能由 Agent 直接透過工具介面完成商品管理，並降低系統互動層的複雜度。
- 方法：去除前端互動層，僅保留後端服務與 MCP 端點，讓 Agent 透過 `products_list`、`products_get`、`products_create`、`products_update`、`products_delete` 等工具直接讀寫商品資料。
- 觀察結果：效果非常好。Agent 能直接以工具呼叫方式完成商品 CRUD，省去傳統前端介面後，整體流程更直接，對此類資料操作型任務特別有效。
- 限制：雖然前端已被移除，但系統仍依賴既有後端服務、資料庫與 MCP 契約；也就是說，Agent 是透過後端能力操作資料，而非直接承擔資料處理責任。

### 第三階段：完全去除既有後端，由 Agent 自主決定資料處理方式

- 研究動機：進一步探索在連既有後端都移除的情況下，Agent 是否能自行決定資料處理與持久化策略，直接承擔系統實作責任。
- 方法：依 `experiments/phase3/` 內的 functional brief 與 storage reference 進行實驗，將商品 CRUD、健康檢查、資料保存方式與介面路徑交由 Agent 自主設計；目前 `experiments/phase3/product_store.py` 示範以 `experiments/phase3/data/products.json` 作為單一真實資料來源，直接完成商品資料的讀取、建立、更新與刪除。
- 觀察結果：目前已具備一個可運作的 file-backed prototype，顯示在沒有傳統後端框架的前提下，Agent 仍可直接處理真實持久化資料，並維持商品 CRUD 的核心能力。
- 限制：此階段把更多系統責任轉移給 Agent，本身對 Agent 的決策品質、資料一致性驗證與儲存策略選型要求更高；另外，現行 `experiments/phase3/` 示例採用檔案型儲存，在規模、併發與 I/O 效能上仍有限制。
- 實驗內容位置：`experiments/phase3/`
