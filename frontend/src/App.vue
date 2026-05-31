<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { productApi } from './services/products'
import type { Product, ProductForm } from './types/product'

interface ProductFormState {
  name: string
  description: string
  price: string | number
  stock: string | number
}

const emptyForm = (): ProductFormState => ({
  name: '',
  description: '',
  price: '',
  stock: '',
})

const products = ref<Product[]>([])
const form = reactive<ProductFormState>(emptyForm())
const editingId = ref<number | null>(null)
const loading = ref(false)
const saving = ref(false)
const error = ref<string | null>(null)

const isEditing = computed(() => editingId.value !== null)
const inventoryValue = computed(() =>
  products.value.reduce((total, product) => total + product.price * product.stock, 0),
)

async function loadProducts() {
  loading.value = true
  error.value = null

  try {
    products.value = await productApi.list()
  } catch (err) {
    error.value = err instanceof Error ? err.message : '載入商品失敗'
  } finally {
    loading.value = false
  }
}

function resetForm() {
  Object.assign(form, emptyForm())
  editingId.value = null
}

function editProduct(product: Product) {
  Object.assign(form, {
    name: product.name,
    description: product.description ?? '',
    price: String(product.price),
    stock: String(product.stock),
  })
  editingId.value = product.id
}

function parsePrice(value: string | number) {
  const normalized = String(value).trim()

  if (!normalized) {
    error.value = '價格必填'
    return null
  }

  const parsed = Number(normalized)
  if (!Number.isFinite(parsed) || parsed < 0) {
    error.value = '價格必須大於或等於 0'
    return null
  }

  return parsed
}

function parseStock(value: string | number) {
  const normalized = String(value).trim()

  if (!normalized) {
    error.value = '庫存必填'
    return null
  }

  const parsed = Number(normalized)
  if (!Number.isInteger(parsed) || parsed < 0) {
    error.value = '庫存必須為大於或等於 0 的整數'
    return null
  }

  return parsed
}

async function submitForm() {
  const name = form.name.trim()
  if (!name) {
    error.value = '商品名稱必填'
    return
  }

  const price = parsePrice(form.price)
  if (price === null) return

  const stock = parseStock(form.stock)
  if (stock === null) return

  saving.value = true
  error.value = null

  try {
    const payload: ProductForm = {
      name,
      description: form.description.trim(),
      price,
      stock,
    }

    if (editingId.value === null) {
      await productApi.create(payload)
    } else {
      await productApi.update(editingId.value, payload)
    }

    resetForm()
    await loadProducts()
  } catch (err) {
    error.value = err instanceof Error ? err.message : '儲存商品失敗'
  } finally {
    saving.value = false
  }
}

async function deleteProduct(product: Product) {
  const confirmed = window.confirm(`確定要刪除「${product.name}」嗎？`)
  if (!confirmed) return

  loading.value = true
  error.value = null

  try {
    await productApi.remove(product.id)
    await loadProducts()
  } catch (err) {
    error.value = err instanceof Error ? err.message : '刪除商品失敗'
  } finally {
    loading.value = false
  }
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('zh-TW', {
    style: 'currency',
    currency: 'TWD',
    maximumFractionDigits: 0,
  }).format(value)
}

onMounted(loadProducts)
</script>

<template>
  <main class="min-h-screen bg-slate-950 px-4 py-10 text-slate-100 sm:px-6 lg:px-8">
    <section class="mx-auto max-w-6xl space-y-8">
      <header class="rounded-3xl border border-white/10 bg-white/5 p-8 shadow-2xl shadow-cyan-950/30">
        <p class="text-sm font-semibold uppercase tracking-[0.3em] text-cyan-300">TestLLM CRUD Lab</p>
        <div class="mt-4 grid gap-6 lg:grid-cols-[1fr_auto] lg:items-end">
          <div>
            <h1 class="text-4xl font-black tracking-tight text-white sm:text-5xl">商品 CRUD 測試專案</h1>
            <p class="mt-4 max-w-2xl text-slate-300">
      Vue 3 + Vite + Tailwind CSS 前端，串接 ASP.NET Core .NET 10 MVC Web API 與 MSSQL。
            </p>
          </div>
          <div class="rounded-2xl bg-cyan-400/10 px-6 py-4 text-right ring-1 ring-cyan-300/20">
            <p class="text-sm text-cyan-200">庫存總值</p>
            <p class="text-2xl font-bold text-cyan-100">{{ formatCurrency(inventoryValue) }}</p>
          </div>
        </div>
      </header>

      <div v-if="error" class="whitespace-pre-line rounded-2xl border border-red-400/30 bg-red-500/10 p-4 text-red-100">
        {{ error }}
      </div>

      <div class="grid gap-8 lg:grid-cols-[380px_1fr]">
        <form class="rounded-3xl border border-white/10 bg-white/5 p-6 shadow-xl" @submit.prevent="submitForm">
          <h2 class="text-2xl font-bold text-white">{{ isEditing ? '編輯商品' : '新增商品' }}</h2>
          <div class="mt-6 space-y-5">
            <label class="block">
              <span class="text-sm font-medium text-slate-300">名稱</span>
              <input
                v-model="form.name"
                class="mt-2 w-full rounded-xl border border-white/10 bg-slate-900 px-4 py-3 text-white outline-none transition focus:border-cyan-300"
                maxlength="120"
                placeholder="例如：LLM 評測套件"
                required
              />
            </label>

            <label class="block">
              <span class="text-sm font-medium text-slate-300">描述</span>
              <textarea
                v-model="form.description"
                class="mt-2 min-h-28 w-full rounded-xl border border-white/10 bg-slate-900 px-4 py-3 text-white outline-none transition focus:border-cyan-300"
                maxlength="1000"
                placeholder="商品描述"
              />
            </label>

            <div class="grid grid-cols-2 gap-4">
              <label class="block">
                <span class="text-sm font-medium text-slate-300">價格</span>
                <input
                  v-model="form.price"
                  class="mt-2 w-full rounded-xl border border-white/10 bg-slate-900 px-4 py-3 text-white outline-none transition focus:border-cyan-300"
                  min="0"
                  step="1"
                  required
                  type="number"
                />
              </label>

              <label class="block">
                <span class="text-sm font-medium text-slate-300">庫存</span>
                <input
                  v-model="form.stock"
                  class="mt-2 w-full rounded-xl border border-white/10 bg-slate-900 px-4 py-3 text-white outline-none transition focus:border-cyan-300"
                  min="0"
                  step="1"
                  required
                  type="number"
                />
              </label>
            </div>
          </div>

          <div class="mt-8 flex gap-3">
            <button
              class="flex-1 rounded-xl bg-cyan-300 px-4 py-3 font-bold text-slate-950 transition hover:bg-cyan-200 disabled:cursor-not-allowed disabled:opacity-60"
              :disabled="saving"
              type="submit"
            >
              {{ saving ? '儲存中...' : isEditing ? '更新' : '建立' }}
            </button>
            <button
              v-if="isEditing"
              class="rounded-xl border border-white/10 px-4 py-3 font-semibold text-slate-200 transition hover:bg-white/10"
              type="button"
              @click="resetForm"
            >
              取消
            </button>
          </div>
        </form>

        <section class="rounded-3xl border border-white/10 bg-white/5 p-6 shadow-xl">
          <div class="flex items-center justify-between gap-4">
            <div>
              <h2 class="text-2xl font-bold text-white">商品列表</h2>
              <p class="text-sm text-slate-400">共 {{ products.length }} 筆資料</p>
            </div>
            <button
              class="rounded-xl border border-cyan-300/30 px-4 py-2 text-cyan-100 transition hover:bg-cyan-300/10 disabled:opacity-60"
              :disabled="loading"
              type="button"
              @click="loadProducts"
            >
              重新整理
            </button>
          </div>

          <div v-if="loading" class="mt-8 rounded-2xl bg-slate-900 p-8 text-center text-slate-300">載入中...</div>
          <div v-else-if="products.length === 0" class="mt-8 rounded-2xl bg-slate-900 p-8 text-center text-slate-300">
            尚無商品，請先新增一筆資料。
          </div>

          <div v-else class="mt-6 overflow-hidden rounded-2xl border border-white/10">
            <table class="min-w-full divide-y divide-white/10">
              <thead class="bg-slate-900/80 text-left text-xs uppercase tracking-wider text-slate-400">
                <tr>
                  <th class="px-4 py-3">商品</th>
                  <th class="px-4 py-3">價格</th>
                  <th class="px-4 py-3">庫存</th>
                  <th class="px-4 py-3 text-right">操作</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-white/10 bg-slate-900/40">
                <tr v-for="product in products" :key="product.id" class="align-top">
                  <td class="px-4 py-4">
                    <p class="font-semibold text-white">{{ product.name }}</p>
                    <p class="mt-1 line-clamp-2 text-sm text-slate-400">{{ product.description || '無描述' }}</p>
                  </td>
                  <td class="px-4 py-4 text-cyan-100">{{ formatCurrency(product.price) }}</td>
                  <td class="px-4 py-4">{{ product.stock }}</td>
                  <td class="px-4 py-4 text-right">
                    <div class="flex justify-end gap-2">
                      <button class="rounded-lg bg-white/10 px-3 py-2 text-sm hover:bg-white/20" type="button" @click="editProduct(product)">
                        編輯
                      </button>
                      <button class="rounded-lg bg-red-500/20 px-3 py-2 text-sm text-red-100 hover:bg-red-500/30" type="button" @click="deleteProduct(product)">
                        刪除
                      </button>
                    </div>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>
      </div>
    </section>
  </main>
</template>
