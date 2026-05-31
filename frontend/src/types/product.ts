export interface Product {
  id: number
  name: string
  description: string | null
  price: number
  stock: number
  createdAt: string
  updatedAt: string
}

export interface ProductForm {
  name: string
  description: string
  price: number
  stock: number
}
