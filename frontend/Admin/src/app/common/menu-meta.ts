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
    icon: 'mdi:office-building',
    label: 'Firmalar',
    link: '/firmalar',
    roles: ['SuperAdmin'],
  },
  {
    key: 'abonelikler',
    icon: 'mdi:certificate',
    label: 'Abonelik Planları',
    link: '/abonelikler',
    roles: ['SuperAdmin'],
  },
  {
    key: 'lisans-anahtarlari',
    icon: 'mdi:key-variant',
    label: 'Lisans Anahtarları',
    link: '/lisans-anahtarlari',
    roles: ['SuperAdmin'],
  },
  {
    key: 'gateway-ayarlar',
    icon: 'mdi:credit-card-settings-outline',
    label: 'Sanal POS Ayarları',
    link: '/gateway-ayarlar',
    roles: ['SuperAdmin'],
  },
  {
    key: 'log-viewer',
    icon: 'mdi:file-eye',
    label: 'Log İzleme',
    link: '/log-viewer',
    roles: ['SuperAdmin'],
  },
  {
    key: 'duyurular',
    icon: 'mdi:bullhorn',
    label: 'Duyurular',
    link: '/duyurular',
    roles: ['SuperAdmin'],
  },
  {
    key: 'ticket-management-superadmin',
    icon: 'mdi:ticket-outline',
    label: 'Ticket Yönetimi',
    link: '/ticket-management',
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
    icon: 'mdi:microphone',
    label: 'Ses Kaydı (Speaker ID)',
    link: '/speakerid',
    roles: ['Admin'],
  },
  {
    key: 'islemler',
    icon: 'mdi:receipt-text',
    label: 'İşlemler',
    link: '/islemler',
    roles: ['Admin', 'User'],
  },
  {
    key: 'izinler',
    icon: 'mdi:shield-check',
    label: 'İzinler',
    link: '/izinler',
    roles: ['Admin', 'User'],
  },
  {
    key: 'task-management',
    icon: 'mdi:clipboard-check',
    label: 'Görev Yönetimi',
    link: '/task-management',
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
    icon: 'mdi:folder',
    label: 'Kategoriler',
    link: '/kategoriler',
    roles: ['Admin'],
  },
  {
    key: 'urunler',
    icon: 'mdi:package-variant',
    label: 'Ürünler',
    link: '/urunler',
    roles: ['Admin'],
  },
  {
    key: 'olcu-birimleri',
    icon: 'mdi:ruler',
    label: 'Ölçü Birimleri',
    link: '/olcu-birimleri',
    roles: ['Admin'],
  },
  {
    key: 'kullanicilar',
    icon: 'mdi:account-group',
    label: 'Kullanıcılar',
    link: '/kullanicilar',
    roles: ['Admin'],
  },
  {
    key: 'giderler',
    icon: 'mdi:currency-usd',
    label: 'Giderler',
    link: '/giderler',
    roles: ['Admin'],
  },
  {
    key: 'duyurular',
    icon: 'mdi:bullhorn',
    label: 'Duyurular',
    link: '/duyurular',
    roles: ['Admin'],
  },
  {
    key: 'destek-ticket',
    icon: 'mdi:ticket-outline',
    label: 'Destek Ticket Aç',
    link: '/destek-ticket',
    roles: ['Admin'],
  },
]
