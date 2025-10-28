export type ProductType = {
  id: number
  productImage: string | string[]
  productName: string
  subtitle: string
  category: string
  price: string
  inventory: string
}

export const products: ProductType[] = [
  {
    id: 1,
    productImage: 'assets/images/products/product-1(1).png',
    productName: 'G15 Gaming Laptop',
    subtitle:
      'Power Your Laptop with a Long-Lasting and Fast-Charging Battery.',
    category: 'Computer',
    price: '240.59',
    inventory: 'Limited',
  },
  {
    id: 2,
    productImage: 'assets/images/products/product-2.png',
    productName: 'Sony Alpha ILCE 6000Y 24.3 MP Mirrorless Digital SLR Camera',
    subtitle: 'Capture special moments and portraits to remember and share.',
    category: 'Camera',
    price: '135.99',
    inventory: 'Limited',
  },
  {
    id: 3,
    productImage: 'assets/images/products/product-3.png',
    productName: 'Sony Over-Ear Wireless Headphone with Mic',
    subtitle:
      "  Headphones are a pair of small loudspeaker drivers worn on or around the head over a user's ears.",
    category: 'Headphones',
    price: '99.49',
    inventory: 'In Stock',
  },
  {
    id: 4,
    productImage: 'assets/images/products/product-4.png',
    productName: 'Apple iPad Pro with Apple M1 chip',
    subtitle: 'The new iPad mini and iPad.',
    category: 'Mobile',
    price: '27.59',
    inventory: 'Out of Stock',
  },
  {
    id: 5,
    productImage: 'assets/images/products/product-5.png',
    productName: 'Adam ROMA USB-C / USB-A 3.1 (2-in-1 Flash Drive) – 128GB',
    subtitle:
      'A USB flash drive is a data storage device that includes flash memory with an integrated USB interface.',
    category: 'Pendrive',
    price: '350.19',
    inventory: 'Limited',
  },
  {
    id: 6,
    productImage: 'assets/images/products/product-6.png',
    productName: 'Apple iPHone 13',
    subtitle: 'The new iPHone 1 and iPad.',
    category: 'Mobile',
    price: '75.59',
    inventory: 'Out of Stock',
  },
  {
    id: 7,
    productImage: 'assets/images/products/product-1(2).png',
    productName: 'Apple Mac',
    subtitle:
      'Power Your Laptop with a Long-Lasting and Fast-Charging Battery.',
    category: 'Computer',
    price: '350.00',
    inventory: 'Limited',
  },
]
