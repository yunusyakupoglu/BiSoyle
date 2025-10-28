export type InventoryProductType = {
  id: number
  name: string
  image: string
  added_date: string
}

export type InventoryType = {
  id: string
  product: InventoryProductType
  condition: string
  location: string
  available: number
  reserved: number
  on_hand: number
  modified: string
}

export const inventoryList = [
  {
    id: '#a113',
    product: {
      id: 1,
      name: 'G15 Gaming Laptop',
      image: 'assets/images/products/product-1(1).png',
      added_date: '12 April 2018',
    },
    condition: 'New',
    location: 'WareHouse 1',
    available: 3521,
    reserved: 6532,
    on_hand: 1236,
    modified: '12/03/2021',
  },
  {
    id: '#a123',
    product: {
      id: 2,
      name: 'Sony Alpha ILCE 6000Y',
      image: 'assets/images/products/product-2.png',
      added_date: '10 April 2018',
    },
    condition: 'New',
    location: 'WareHouse 2',
    available: 4562,
    reserved: 256,
    on_hand: 214,
    modified: '06/04/2021',
  },
  {
    id: '#a133',
    product: {
      id: 3,
      name: 'Sony Wireless Headphone',
      image: 'assets/images/products/product-3.png',
      added_date: '25 December 2017',
    },
    condition: 'Return',
    location: 'WareHouse 3',
    available: 125,
    reserved: 4512,
    on_hand: 412,
    modified: '21/05/2020',
  },
  {
    id: '#a143',
    product: {
      id: 4,
      name: 'Apple iPad Pro',
      image: 'assets/images/products/product-4.png',
      added_date: '05 May 2018',
    },
    condition: 'Damaged',
    location: 'WareHouse 1',
    available: 4523,
    reserved: 1241,
    on_hand: 852,
    modified: '15/03/2021',
  },
  {
    id: '#a153',
    product: {
      id: 5,
      name: 'Adam ROMA USB-C',
      image: 'assets/images/products/product-5.png',
      added_date: '31 March 2018',
    },
    condition: 'New',
    location: 'WareHouse 2',
    available: 1475,
    reserved: 2345,
    on_hand: 1256,
    modified: '15/10/2020',
  },
]
