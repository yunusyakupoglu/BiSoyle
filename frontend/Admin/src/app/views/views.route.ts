import { Route } from '@angular/router'
import { SpeakerIDComponent } from './speakerid/speakerid.component'
import { KategorilerComponent } from './kategoriler/kategoriler.component'
import { UrunlerComponent } from './urunler/urunler.component'
import { OlcuBirimleriComponent } from './olcu-birimleri/olcu-birimleri.component'
import { IslemlerComponent } from './islemler/islemler.component'
import { KullanicilarComponent } from './kullanicilar/kullanicilar.component'
import { CihazlarComponent } from './cihazlar/cihazlar.component'
import { AuthGuard } from '../guards/auth.guard'
import { adminGuard, userGuard, superAdminGuard } from '../guards/role.guard'

export const VIEW_ROUTES: Route[] = [
  // ========================
  // SUPERADMIN ONLY ROUTES
  // ========================
  {
    path: 'firmalar',
    loadChildren: () =>
      import('./firmalar/firmalar.route').then((mod) => mod.FIRMALAR_ROUTES),
    canActivate: [superAdminGuard],
    data: { title: 'Firmalar', roles: ['SuperAdmin'] },
  },
  {
    path: 'abonelikler',
    loadChildren: () =>
      import('./abonelikler/abonelikler.route').then((mod) => mod.ABONELIKLER_ROUTES),
    canActivate: [superAdminGuard],
    data: { title: 'Abonelik Planları', roles: ['SuperAdmin'] },
  },

  // ========================
  // ADMIN & USER COMMON
  // ========================
  {
    path: 'speakerid',
    component: SpeakerIDComponent,
    canActivate: [userGuard],
    data: { title: 'Speaker ID', roles: ['Admin', 'User'] },
  },
  {
    path: 'islemler',
    component: IslemlerComponent,
    canActivate: [userGuard],
    data: { title: 'İşlemler', roles: ['Admin', 'User'] },
  },

  // ========================
  // ADMIN ONLY ROUTES
  // ========================
  {
    path: 'kategoriler',
    component: KategorilerComponent,
    canActivate: [adminGuard],
    data: { title: 'Kategoriler', roles: ['Admin'] },
  },
  {
    path: 'urunler',
    component: UrunlerComponent,
    canActivate: [adminGuard],
    data: { title: 'Ürünler', roles: ['Admin'] },
  },
  {
    path: 'olcu-birimleri',
    component: OlcuBirimleriComponent,
    canActivate: [adminGuard],
    data: { title: 'Ölçü Birimleri', roles: ['Admin'] },
  },
  {
    path: 'kullanicilar',
    component: KullanicilarComponent,
    canActivate: [adminGuard],
    data: { title: 'Kullanıcılar', roles: ['Admin'] },
  },
  {
    path: 'cihazlar',
    component: CihazlarComponent,
    canActivate: [adminGuard],
    data: { title: 'Cihazlar', roles: ['Admin'] },
  },
]
