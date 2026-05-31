import type { Product, ProductForm } from '../types/product'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const PRODUCTS_URL = `${API_BASE}/api/products`

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

function getProblemDetailsMessage(problem: ProblemDetails) {
  const fieldMessages = Object.values(problem.errors ?? {})
    .flat()
    .map((message) => message.trim())
    .filter(Boolean)

  if (fieldMessages.length > 0) {
    return fieldMessages.join('\n')
  }

  const detail = problem.detail?.trim()
  if (detail) {
    return detail
  }

  const title = problem.title?.trim()
  if (title) {
    return title
  }

  return null
}

async function buildError(response: Response) {
  const rawBody = (await response.text()).trim()
  if (!rawBody) {
    return new Error(`Request failed with HTTP ${response.status}`)
  }

  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('json')) {
    try {
      const payload = JSON.parse(rawBody) as ProblemDetails
      const message = getProblemDetailsMessage(payload)
      if (message) {
        return new Error(message)
      }
    } catch {
      // Fall back to the raw response body if the JSON payload is malformed.
    }
  }

  return new Error(rawBody)
}

async function request<T>(url: string, options?: RequestInit): Promise<T> {
  const response = await fetch(url, {
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    ...options,
  })

  if (!response.ok) {
    throw await buildError(response)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return response.json() as Promise<T>
}

export const productApi = {
  list: () => request<Product[]>(PRODUCTS_URL),
  create: (payload: ProductForm) =>
    request<Product>(PRODUCTS_URL, {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  update: (id: number, payload: ProductForm) =>
    request<Product>(`${PRODUCTS_URL}/${id}`, {
      method: 'PUT',
      body: JSON.stringify(payload),
    }),
  remove: (id: number) =>
    request<void>(`${PRODUCTS_URL}/${id}`, {
      method: 'DELETE',
    }),
}
