import { currentYear } from '@/app/common/constants'

export type CustomerType = {
  id: number
  image: string
  name: string
  date: string
  email: string
  phone: string
  location: string
  orders: number
}

export const customerList: CustomerType[] = [
  {
    id: 1,
    image: 'assets/images/users/avatar-1.jpg',
    name: 'Anna M. Hines',
    date: '23 April ' + currentYear,
    email: 'anna.hines@mail.com',
    phone: '(+1)-555-1564-261',
    location: 'Burr Ridge/Illinois',
    orders: 15,
  },
  {
    id: 2,
    image: 'assets/images/users/avatar-2.jpg',
    name: 'Candice F. Gilmore',
    date: '12 April ' + currentYear,
    email: 'candice.gilmore@mail.com',
    phone: '(+257)-755-5532-588',
    location: 'Roselle/Illinois',
    orders: 215,
  },
  {
    id: 3,
    image: 'assets/images/users/avatar-3.jpg',
    name: 'Vanessa R. Davis',
    date: '15 March ' + currentYear,
    email: 'vanessa.davis@mail.com',
    phone: '(+1)-441-5558-183',
    location: 'Wann/Oklahoma',
    orders: 125,
  },
  {
    id: 4,
    image: 'assets/images/users/avatar-4.jpg',
    name: 'Judith H. Fritsche',
    date: '11 January ' + currentYear,
    email: 'judith.fritsche.com',
    phone: '(+57)-305-5579-759',
    location: 'SULLIVAN/Kentucky',
    orders: 5,
  },
  {
    id: 5,
    image: 'assets/images/users/avatar-5.jpg',
    name: 'Peter T. Smith',
    date: '03 December ' + currentYear,
    email: 'peter.smith@mail.com',
    phone: '(+33)-655-5187-93',
    location: 'Yreka/California',
    orders: 15,
  },
  {
    id: 6,
    image: 'assets/images/users/avatar-6.jpg',
    name: 'Emmanuel J. Delcid',
    date: '12 April ' + currentYear,
    email: 'emmanuel.delicid@mail.com',
    phone: '(+30)-693-5553-637',
    location: 'Atlanta/Georgia',
    orders: 10,
  },
  {
    id: 7,
    image: 'assets/images/users/avatar-7.jpg',
    name: 'William J. Cook',
    date: '13 November ' + currentYear,
    email: 'william.cook@mail.com',
    phone: '(+91)-855-5446-150',
    location: 'Rosenberg/Texas',
    orders: 85,
  },
  {
    id: 8,
    image: 'assets/images/users/avatar-8.jpg',
    name: 'Martin R. Peters',
    date: '25 August ' + currentYear,
    email: 'martin.peters@mail.com',
    phone: '(+61)-455-5943-13',
    location: 'Youngstown/Ohio',
    orders: 3,
  },
  {
    id: 9,
    image: 'assets/images/users/avatar-9.jpg',
    name: 'Paul M. Schubert',
    date: '28 April ' + currentYear,
    email: 'paul.schubert@mail.com',
    phone: '(+61)-035-5531-64',
    location: 'Austin/Texas',
    orders: 181,
  },
  {
    id: 10,
    image: 'assets/images/users/avatar-10.jpg',
    name: 'Janet J. Champine',
    date: '06 May ' + currentYear,
    email: 'janet.champine@mail.com',
    phone: '(+880)-115-5592-916',
    location: 'Nashville/Tennessee',
    orders: 521,
  },
]
