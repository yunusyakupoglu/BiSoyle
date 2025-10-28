export type SellerType = {
  id: number
  image: string
  name: string
  store_name: string
  rating: number
  products: number
  wallet_balance: number
  create_date: string
  revenue: number
}

export const sellersList: SellerType[] = [
  {
    id: 1,
    image: 'assets/images/users/avatar-1.jpg',
    name: 'Anna M. Hines',
    store_name: 'Acme',
    rating: 4.9,
    products: 356,
    wallet_balance: 256.45,
    create_date: '09/04/2021',
    revenue: 269.56,
  },
  {
    id: 2,
    image: 'assets/images/users/avatar-2.jpg',
    name: 'Candice F. Gilmore',
    store_name: 'Globex',
    rating: 3.2,
    products: 289,
    wallet_balance: 156.98,
    create_date: '13/05/2021',
    revenue: 89.75,
  },
  {
    id: 3,
    image: 'assets/images/users/avatar-3.jpg',
    name: 'Vanessa R. Davis',
    store_name: 'Soylent',
    rating: 4.1,
    products: 71,
    wallet_balance: 859.5,
    create_date: '21/02/2020',
    revenue: 452.5,
  },
  {
    id: 4,
    image: 'assets/images/users/avatar-4.jpg',
    name: 'Judith H. Fritsche',
    store_name: 'Initech',
    rating: 2.5,
    products: 125,
    wallet_balance: 163.75,
    create_date: '09/05/2020',
    revenue: 365,
  },
  {
    id: 5,
    image: 'assets/images/users/avatar-5.jpg',
    name: 'Peter T. Smith',
    store_name: 'Hooli',
    rating: 3.7,
    products: 265,
    wallet_balance: 545,
    create_date: '15/06/2019',
    revenue: 465.59,
  },
  {
    id: 6,
    image: 'assets/images/users/avatar-6.jpg',
    name: 'Emmanuel J. Delcid',
    store_name: 'Vehement',
    rating: 4.3,
    products: 68,
    wallet_balance: 136.54,
    create_date: '11/12/2019',
    revenue: 278.95,
  },
  {
    id: 7,
    image: 'assets/images/users/avatar-7.jpg',
    name: 'William J. Cook',
    store_name: 'Massive',
    rating: 1.8,
    products: 550,
    wallet_balance: 365.85,
    create_date: '26/1/2021',
    revenue: 475.96,
  },
  {
    id: 8,
    image: 'assets/images/users/avatar-8.jpg',
    name: 'Martin R. Peters',
    store_name: 'Fringe',
    rating: 3.5,
    products: 123,
    wallet_balance: 95.7,
    create_date: '13/04/2020',
    revenue: 142,
  },
  {
    id: 9,
    image: 'assets/images/users/avatar-9.jpg',
    name: 'Paul M. Schubert',
    store_name: 'Weeds',
    rating: 2.4,
    products: 789,
    wallet_balance: 423.4,
    create_date: '05/07/2020',
    revenue: 652.9,
  },
  {
    id: 10,
    image: 'assets/images/users/avatar-10.jpg',
    name: 'Janet J. Champine',
    store_name: 'Soylent',
    rating: 4.6,
    products: 75,
    wallet_balance: 216.8,
    create_date: '17/03/2019',
    revenue: 180.75,
  },
]
