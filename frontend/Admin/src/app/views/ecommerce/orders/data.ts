export type OrderType = {
  orderID: string
  date: string
  product: string
  name: string
  email: string
  phone: string
  address: string
  payment_type: string
  status: string
}

export const orderList: OrderType[] = [
  {
    orderID: '#RB5625',
    date: '23/07/2021',
    product: 'product-1(1).png',
    name: 'Anna M. Hines',
    email: 'anna.hines@mail.com',
    phone: '(+1)-555-1564-261',
    address: 'Burr Ridge/Illinois',
    payment_type: 'Credit Card',
    status: 'Completed',
  },
  {
    orderID: '#RB0025',
    date: '06/09/2018',
    product: 'product-2.png',
    name: 'Candice F. Gilmore',
    email: 'candice.gilmore@mail.com',
    phone: '(+257)-755-5532-588',
    address: 'Roselle/Illinois',
    payment_type: 'Credit Card',
    status: 'Processing',
  },
  {
    orderID: '#RB9064',
    date: '12/07/2019',
    product: 'product-3.png',
    name: 'Vanessa R. Davis',
    email: 'vanessa.davis@mail.com',
    phone: '(+1)-441-5558-183',
    address: 'Wann/Oklahoma',
    payment_type: 'Pay Pal',
    status: 'Cancel',
  },
  {
    orderID: '#RB9652',
    date: '31/12/2021',
    product: 'product-4.png',
    name: 'Judith H. Fritsche',
    email: 'judith.fritsche.com',
    phone: '(+57)-305-5579-759',
    address: 'SULLIVAN/Kentucky',
    payment_type: 'Credit Card',
    status: 'Completed',
  },
  {
    orderID: '#RB5984',
    date: '01/05/2018',
    product: 'product-5.png',
    name: 'Peter T. Smith',
    email: 'peter.smith@mail.com',
    phone: '(+33)-655-5187-93',
    address: 'Yreka/California',
    payment_type: 'Pay Pal',
    status: 'Completed',
  },
  {
    orderID: '#RB3625',
    date: '12/06/2020',
    product: 'product-6.png',
    name: 'Emmanuel J. Delcid',
    email: 'emmanuel.delicid@mail.com',
    phone: '(+30)-693-5553-637',
    address: 'Atlanta/Georgia',
    payment_type: 'Pay Pal',
    status: 'Processing',
  },
  {
    orderID: '#RB8652',
    date: '14/08/2017',
    product: 'product-1(2).png',
    name: 'William J. Cook',
    email: 'william.cook@mail.com',
    phone: '(+91)-855-5446-150',
    address: 'Rosenberg/Texas',
    payment_type: 'Credit Card',
    status: 'Processing',
  },
  {
    orderID: '#RB1002',
    date: '13/07/2018',
    product: 'product-1(3).png',
    name: 'Martin R. Peters',
    email: 'martin.peters@mail.com',
    phone: '(+61)-455-5943-13',
    address: 'Youngstown/Ohio',
    payment_type: 'Credit Card',
    status: 'Cancel',
  },
  {
    orderID: '#RB9622',
    date: '18/11/2019',
    product: 'product-3.png',
    name: 'Paul M. Schubert',
    email: 'paul.schubert@mail.com',
    phone: '(+61)-035-5531-64',
    address: 'Austin/Texas',
    payment_type: 'Google Pay',
    status: 'Completed',
  },
  {
    orderID: '#RB8745',
    date: '07/03/2019',
    product: 'product-4.png',
    name: 'Janet J. Champine',
    email: 'janet.champine@mail.com',
    phone: '(+880)-115-5592-916',
    address: 'Nashville/Tennessee',
    payment_type: 'Google Pay',
    status: 'Processing',
  },
]
