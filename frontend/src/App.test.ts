import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import type { Product } from './types/product'

const productApiMocks = vi.hoisted(() => ({
  list: vi.fn(),
  create: vi.fn(),
  update: vi.fn(),
  remove: vi.fn(),
}))

vi.mock('./services/products', () => ({
  productApi: productApiMocks,
}))

import App from './App.vue'

function makeProduct(overrides: Partial<Product> = {}): Product {
  return {
    id: 1,
    name: 'LLM 評測套件',
    description: '商品描述',
    price: 1200,
    stock: 2,
    createdAt: '2026-05-30T00:00:00Z',
    updatedAt: '2026-05-30T00:00:00Z',
    ...overrides,
  }
}

async function mountApp() {
  const wrapper = mount(App)
  await flushPromises()
  return wrapper
}

describe('App', () => {
  beforeEach(() => {
    productApiMocks.list.mockReset()
    productApiMocks.create.mockReset()
    productApiMocks.update.mockReset()
    productApiMocks.remove.mockReset()
  })

  afterEach(() => {
    vi.restoreAllMocks()
  })

  it('loads products on mount and renders the inventory summary', async () => {
    const products = [
      makeProduct(),
      makeProduct({ id: 2, name: '第二筆商品', price: 600, stock: 1 }),
    ]
    productApiMocks.list.mockResolvedValue(products)

    const wrapper = await mountApp()
    const inventoryValue = new Intl.NumberFormat('zh-TW', {
      style: 'currency',
      currency: 'TWD',
      maximumFractionDigits: 0,
    }).format(3000)

    expect(productApiMocks.list).toHaveBeenCalledTimes(1)
    expect(wrapper.text()).toContain('LLM 評測套件')
    expect(wrapper.text()).toContain('第二筆商品')
    expect(wrapper.text()).toContain(inventoryValue)
  })

  it('shows an error when the initial load fails', async () => {
    productApiMocks.list.mockRejectedValue(new Error('API 連線失敗'))

    const wrapper = await mountApp()

    expect(wrapper.text()).toContain('API 連線失敗')
  })

  it('validates name, price, and stock before submitting', async () => {
    productApiMocks.list.mockResolvedValue([])

    const wrapper = await mountApp()
    const nameInput = wrapper.get('input[placeholder="例如：LLM 評測套件"]')
    const numberInputs = wrapper.findAll('input[type="number"]')

    await wrapper.get('form').trigger('submit')
    expect(wrapper.text()).toContain('商品名稱必填')

    await nameInput.setValue('新商品')
    await numberInputs[0].setValue('')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.text()).toContain('價格必填')

    await numberInputs[0].setValue('100')
    await numberInputs[1].setValue('-1.5')
    await wrapper.get('form').trigger('submit')
    expect(wrapper.text()).toContain('庫存必須為大於或等於 0 的整數')
  })

  it('creates a product with trimmed values and refreshes the list', async () => {
    const created = makeProduct({
      id: 3,
      name: '新商品',
      description: '建立成功',
      price: 900,
      stock: 4,
    })

    productApiMocks.list
      .mockResolvedValueOnce([])
      .mockResolvedValueOnce([created])
    productApiMocks.create.mockResolvedValue(created)

    const wrapper = await mountApp()
    const nameInput = wrapper.get('input[placeholder="例如：LLM 評測套件"]')
    const descriptionInput = wrapper.get('textarea')
    const numberInputs = wrapper.findAll('input[type="number"]')

    await nameInput.setValue('  新商品  ')
    await descriptionInput.setValue('  建立成功  ')
    await numberInputs[0].setValue('900')
    await numberInputs[1].setValue('4')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(productApiMocks.create).toHaveBeenCalledWith({
      name: '新商品',
      description: '建立成功',
      price: 900,
      stock: 4,
    })
    expect(productApiMocks.list).toHaveBeenCalledTimes(2)
    expect((nameInput.element as HTMLInputElement).value).toBe('')
    expect(wrapper.text()).toContain('新商品')
  })

  it('updates a product after entering edit mode', async () => {
    const existing = makeProduct()
    const updated = makeProduct({
      name: '更新後商品',
      description: '更新後描述',
      price: 1500,
      stock: 9,
      updatedAt: '2026-05-31T00:00:00Z',
    })

    productApiMocks.list
      .mockResolvedValueOnce([existing])
      .mockResolvedValueOnce([updated])
    productApiMocks.update.mockResolvedValue(updated)

    const wrapper = await mountApp()
    const editButton = wrapper.findAll('button').find((button) => button.text() === '編輯')

    expect(editButton).toBeDefined()
    await editButton!.trigger('click')

    const nameInput = wrapper.get('input[placeholder="例如：LLM 評測套件"]')
    const descriptionInput = wrapper.get('textarea')
    const numberInputs = wrapper.findAll('input[type="number"]')

    await nameInput.setValue('  更新後商品  ')
    await descriptionInput.setValue('  更新後描述  ')
    await numberInputs[0].setValue('1500')
    await numberInputs[1].setValue('9')
    await wrapper.get('form').trigger('submit')
    await flushPromises()

    expect(productApiMocks.update).toHaveBeenCalledWith(1, {
      name: '更新後商品',
      description: '更新後描述',
      price: 1500,
      stock: 9,
    })
    expect(wrapper.text()).toContain('更新後商品')
  })

  it('does not delete when the confirmation dialog is cancelled', async () => {
    productApiMocks.list.mockResolvedValue([makeProduct()])
    vi.spyOn(window, 'confirm').mockReturnValue(false)

    const wrapper = await mountApp()
    const deleteButton = wrapper.findAll('button').find((button) => button.text() === '刪除')

    expect(deleteButton).toBeDefined()
    await deleteButton!.trigger('click')

    expect(productApiMocks.remove).not.toHaveBeenCalled()
  })

  it('deletes a product when confirmed and refreshes the list', async () => {
    productApiMocks.list
      .mockResolvedValueOnce([makeProduct()])
      .mockResolvedValueOnce([])
    productApiMocks.remove.mockResolvedValue(undefined)
    vi.spyOn(window, 'confirm').mockReturnValue(true)

    const wrapper = await mountApp()
    const deleteButton = wrapper.findAll('button').find((button) => button.text() === '刪除')

    expect(deleteButton).toBeDefined()
    await deleteButton!.trigger('click')
    await flushPromises()

    expect(productApiMocks.remove).toHaveBeenCalledWith(1)
    expect(productApiMocks.list).toHaveBeenCalledTimes(2)
    expect(wrapper.text()).toContain('尚無商品，請先新增一筆資料。')
  })
})
