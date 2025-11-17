import { Route } from '@angular/router'
import { SpeakerIDComponent } from './speakerid/speakerid.component'
import { KategorilerComponent } from './kategoriler/kategoriler.component'
import { UrunlerComponent } from './urunler/urunler.component'
import { OlcuBirimleriComponent } from './olcu-birimleri/olcu-birimleri.component'
import { IslemlerComponent } from './islemler/islemler.component'
import { KullanicilarComponent } from './kullanicilar/kullanicilar.component'
import { IzinlerComponent } from './izinler/izinler.component'
import { LogViewerComponent } from './log-viewer/log-viewer.component'
import { GiderlerComponent } from './giderler/giderler.component'
import { TaskManagementComponent } from './task-management/task-management.component'
import { DuyurularComponent } from './duyurular/duyurular.component'
import { DashboardComponent } from './dashboard/dashboard.component'
import { DestekTicketComponent } from './destek-ticket/destek-ticket.component'
import { TicketManagementComponent } from './ticket-management/ticket-management.component'
import { AuthGuard } from '../guards/auth.guard'
import { adminGuard, userGuard, superAdminGuard } from '../guards/role.guard'

export const VIEW_ROUTES: Route[] = [
  // ========================
  // DASHBOARD (DEFAULT)
  // ========================
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full',
  },
  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [userGuard],
    data: { title: 'Dashboard', roles: ['Admin', 'User', 'SuperAdmin'] },
  },
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
  {
    path: 'lisans-anahtarlari',
    loadChildren: () =>
      import('./lisans-anahtarlari/lisans-anahtarlari.route').then((mod) => mod.LISANS_ANAHTARLARI_ROUTES),
    canActivate: [superAdminGuard],
    data: { title: 'Lisans Anahtarları', roles: ['SuperAdmin'] },
  },
  {
    path: 'gateway-ayarlar',
    loadChildren: () =>
      import('./gateway-settings/gateway-settings.route').then((mod) => mod.GATEWAY_SETTINGS_ROUTES),
    canActivate: [superAdminGuard],
    data: { title: 'Sanal POS Ayarları', roles: ['SuperAdmin'] },
  },

  // ========================
  // ADMIN & USER COMMON
  // ========================
  {
    path: 'speakerid',
    component: SpeakerIDComponent,
    canActivate: [adminGuard],
    data: { title: 'Speaker ID', roles: ['Admin'] },
  },
  {
    path: 'islemler',
    component: IslemlerComponent,
    canActivate: [userGuard],
    data: { title: 'İşlemler', roles: ['Admin', 'User'] },
  },
  {
    path: 'izinler',
    component: IzinlerComponent,
    canActivate: [userGuard],
    data: { title: 'İzinler', roles: ['Admin', 'User'] },
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
    path: 'giderler',
    component: GiderlerComponent,
    canActivate: [adminGuard],
    data: { title: 'Giderler', roles: ['Admin'] },
  },
  {
    path: 'task-management',
    component: TaskManagementComponent,
    canActivate: [userGuard],
    data: { title: 'Görev Yönetimi', roles: ['Admin', 'User'] },
  },
  {
    path: 'duyurular',
    component: DuyurularComponent,
    canActivate: [userGuard],
    data: { title: 'Duyurular', roles: ['SuperAdmin', 'Admin'] },
  },
  {
    path: 'ticket-management',
    component: TicketManagementComponent,
    canActivate: [superAdminGuard],
    data: { title: 'Ticket Yönetimi', roles: ['SuperAdmin'] },
  },
  {
    path: 'destek-ticket',
    component: DestekTicketComponent,
    canActivate: [adminGuard],
    data: { title: 'Destek Ticket Aç', roles: ['Admin'] },
  },
  // Cihazlar bölümü kaldırıldı
  {
    path: 'license-activation',
    loadChildren: () =>
      import('./license-activation/license-activation.route').then((mod) => mod.LICENSE_ACTIVATION_ROUTES),
    canActivate: [AuthGuard],
    data: { title: 'License Activation' },
  },
  {
    path: 'log-viewer',
    component: LogViewerComponent,
    canActivate: [superAdminGuard],
    data: { title: 'Log İzleme', roles: ['SuperAdmin'] },
  },
]
