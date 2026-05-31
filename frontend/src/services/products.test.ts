import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { productApi } from './products'

describe('productApi', () => {
  const fetchMock = vi.fn<typeof fetch>()

  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock)
  })

  afterEach(() => {
    fetchMock.mockReset()
  })

  it('lists products from the API', async () => {
    const products = [{ id: 1, name: '測試商品' }]

    fetchMock.mockResolvedValue(
      new Response(JSON.stringify(products), {
        status: 200,
        headers: { 'Content-Type': 'application/json' },
      }),
    )

    await expect(productApi.list()).resolves.toEqual(products)
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/products',
      expect.objectContaining({
        headers: expect.objectContaining({
          'Content-Type': 'application/json',
        }),
      }),
    )
  })

  it('sends JSON when creating a product', async () => {
    fetchMock.mockResolvedValue(
      new Response(JSON.stringify({ id: 3, name: '建立商品' }), {
        status: 201,
        headers: { 'Content-Type': 'application/json' },
      }),
    )

    await productApi.create({
      name: '建立商品',
      description: '描述',
      price: 99,
      stock: 5,
    })

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/products',
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          name: '建立商品',
          description: '描述',
          price: 99,
          stock: 5,
        }),
      }),
    )
  })

  it('returns undefined for 204 delete responses', async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 204 }))

    await expect(productApi.remove(7)).resolves.toBeUndefined()
    expect(fetchMock).toHaveBeenCalledWith(
      '/api/products/7',
      expect.objectContaining({ method: 'DELETE' }),
    )
  })

  it('surfaces validation problem details as readable text', async () => {
    fetchMock.mockResolvedValue(
      new Response(
        JSON.stringify({
          errors: {
            name: ['Name is required'],
            price: ['Price must be greater than or equal to 0.'],
          },
        }),
        {
          status: 400,
          headers: { 'Content-Type': 'application/problem+json' },
        },
      ),
    )

    await expect(
      productApi.create({
        name: '',
        description: '',
        price: -1,
        stock: 0,
      }),
    ).rejects.toThrow('Name is required\nPrice must be greater than or equal to 0.')
  })

  it('falls back to raw error text when the response is not JSON', async () => {
    fetchMock.mockResolvedValue(
      new Response('Service unavailable', {
        status: 503,
        headers: { 'Content-Type': 'text/plain' },
      }),
    )

    await expect(productApi.list()).rejects.toThrow('Service unavailable')
  })
})
