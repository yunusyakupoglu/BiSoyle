export type MenuItem = {
  key?: string
  label?: string
  icon?: string
  link?: string
  collapsed?: boolean
  subMenu?: any
  isTitle?: boolean
  badge?: any
  parentKey?: string
  disabled?: boolean
  roles?: string[] // Hangi roller görebilir
}

export const MENU: MenuItem[] = [
  // ======================
  // SUPERADMIN ONLY
  // ======================
  {
    key: 'superadmin-section',
    label: 'SİSTEM YÖNETİMİ',
    isTitle: true,
    roles: ['SuperAdmin'],
  },
  {
    key: 'firmalar',
    icon: 'iconamoon:building-duotone',
    label: 'Firmalar',
    link: '/firmalar',
    badge: {
      variant: 'danger',
      text: 'SuperAdmin',
    },
    roles: ['SuperAdmin'],
  },
  {
    key: 'abonelikler',
    icon: 'iconamoon:certificate-badge-duotone',
    label: 'Abonelik Planları',
    link: '/abonelikler',
    badge: {
      variant: 'danger',
      text: 'SuperAdmin',
    },
    roles: ['SuperAdmin'],
  },

  // ======================
  // ADMIN & USER COMMON
  // ======================
  {
    key: 'general',
    label: 'GENEL',
    isTitle: true,
    roles: ['Admin', 'User'],
  },
  {
    key: 'speakerid',
    icon: 'iconamoon:microphone-duotone',
    label: 'Ses Kaydı (Speaker ID)',
    link: '/speakerid',
    badge: {
      variant: 'success',
      text: 'AI',
    },
    roles: ['Admin', 'User'],
  },
  {
    key: 'islemler',
    icon: 'iconamoon:invoice-duotone',
    label: 'İşlemler',
    link: '/islemler',
    roles: ['Admin', 'User'],
  },

  // ======================
  // ADMIN ONLY
  // ======================
  {
    key: 'yonetim',
    label: 'FİRMA YÖNETİMİ',
    isTitle: true,
    roles: ['Admin'],
  },
  {
    key: 'kategoriler',
    icon: 'iconamoon:folder-duotone',
    label: 'Kategoriler',
    link: '/kategoriler',
    badge: {
      variant: 'info',
      text: 'Admin',
    },
    roles: ['Admin'],
  },
  {
    key: 'urunler',
    icon: 'iconamoon:box-duotone',
    label: 'Ürünler',
    link: '/urunler',
    badge: {
      variant: 'info',
      text: 'Admin',
    },
    roles: ['Admin'],
  },
  {
    key: 'olcu-birimleri',
    icon: 'iconamoon:ruler-duotone',
    label: 'Ölçü Birimleri',
    link: '/olcu-birimleri',
    badge: {
      variant: 'info',
      text: 'Admin',
    },
    roles: ['Admin'],
  },
  {
    key: 'kullanicilar',
    icon: 'iconamoon:users-duotone',
    label: 'Kullanıcılar',
    link: '/kullanicilar',
    badge: {
      variant: 'info',
      text: 'Admin',
    },
    roles: ['Admin'],
  },
  {
    key: 'cihazlar',
    icon: 'iconamoon:device-duotone',
    label: 'Cihazlar',
    link: '/cihazlar',
    badge: {
      variant: 'info',
      text: 'Admin',
    },
    roles: ['Admin'],
  },
]
