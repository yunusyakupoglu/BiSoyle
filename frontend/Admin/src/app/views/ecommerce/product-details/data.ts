export type DetailType = {
  id: number
  category: string
  images: string[]
  title: string
  rating: number
  review: number
  discount: number
  price: number
  stock: string
  color: string[]
  size: string[]
  processorBrand: string
  processorName: string
  ssd: string
  ssdCapacity: string
  ram: string
  about: string[]
}

export const productDetail: DetailType = {
  id: 1,
  category: 'Apple MacBook Air M1',
  images: [
    'assets/images/products/product-1(2).png',
    'assets/images/products/product-1(1).png',
    'assets/images/products/product-1(3).png',
    'assets/images/products/product-1(4).png',
  ],
  title:
    '(8 GB/256 GB SSD/Mac OS Big Sur) MGN63HN/A (13.3 inch, Space Grey, 1.29 kg)',
  rating: 5,
  review: 36,
  discount: 20,
  price: 999,
  stock: 'Instock',
  color: ['Black', 'Blue', 'Midnight'],
  size: ['256 GB', '512 GB'],
  processorBrand: 'Apple',
  processorName: 'M1',
  ssd: 'Yes',
  ssdCapacity: '256 GB',
  ram: '8 GB',
  about: [
    'Quad LED Backlit IPS Display (227 PPI, 400 nits Brightness, Wide Colour (P3), True Tone Technology)',
    'Built-in Speakers',
    'Three-mic Array with Directional Beamforming',
    'Stereo Speakers, Wide Stereo Sound, Support for Dolby Atmos Playback',
    '49.9 WHr Li-polymer Battery',
    'Backlit Magic Keyboard',
  ],
}
