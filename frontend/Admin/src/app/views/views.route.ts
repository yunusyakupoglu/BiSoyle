import { Route } from '@angular/router'
import { WidgetsComponent } from './apps/widgets/widgets.component'
import { InvoiceDetailsComponent } from './invoices/invoice-details/invoice-details.component'
import { InvoicesComponent } from './invoices/invoices/invoices.component'
import { SpeakerIDComponent } from './speakerid/speakerid.component'
import { KategorilerComponent } from './kategoriler/kategoriler.component'
import { UrunlerComponent } from './urunler/urunler.component'
import { OlcuBirimleriComponent } from './olcu-birimleri/olcu-birimleri.component'
import { IslemlerComponent } from './islemler/islemler.component'
import { KullanicilarComponent } from './kullanicilar/kullanicilar.component'
import { CihazlarComponent } from './cihazlar/cihazlar.component'
import { AuthGuard } from '../guards/auth.guard'

export const VIEW_ROUTES: Route[] = [
  // Protected routes - AuthGuard will redirect to /auth/login if not authenticated
  {
    path: 'speakerid',
    component: SpeakerIDComponent,
    canActivate: [AuthGuard],
    data: { title: 'Speaker ID' },
  },
  {
    path: 'kategoriler',
    component: KategorilerComponent,
    canActivate: [AuthGuard],
    data: { title: 'Kategoriler' },
  },
  {
    path: 'urunler',
    component: UrunlerComponent,
    canActivate: [AuthGuard],
    data: { title: 'Ürünler' },
  },
  {
    path: 'olcu-birimleri',
    component: OlcuBirimleriComponent,
    canActivate: [AuthGuard],
    data: { title: 'Ölçü Birimleri' },
  },
  {
    path: 'islemler',
    component: IslemlerComponent,
    canActivate: [AuthGuard],
    data: { title: 'İşlemler' },
  },
  {
    path: 'kullanicilar',
    component: KullanicilarComponent,
    canActivate: [AuthGuard],
    data: { title: 'Kullanıcılar' },
  },
  {
    path: 'cihazlar',
    component: CihazlarComponent,
    canActivate: [AuthGuard],
    data: { title: 'Cihazlar' },
  },
  {
    path: 'pages',
    loadChildren: () =>
      import('./pages/pages.route').then((mod) => mod.PAGES_ROUTES),
  },
  {
    path: 'dashboard',
    loadChildren: () =>
      import('./dashboards/dashboards.route').then(
        (mod) => mod.DASHBOARD_ROUTES
      ),
  },
  {
    path: 'ecommerce',
    loadChildren: () =>
      import('./ecommerce/ecommerce.route').then((mod) => mod.ECOMMERCE_ROUTES),
  },
  {
    path: 'apps',
    loadChildren: () =>
      import('./apps/apps.route').then((mod) => mod.APPS_ROUTES),
  },
  {
    path: 'calendar',
    loadChildren: () =>
      import('./calendar/calendar.route').then((mod) => mod.CALENDAR_ROUTES),
  },
  {
    path: 'invoices',
    component: InvoicesComponent,
    data: { title: 'Invoices' },
  },
  {
    path: 'invoice/:id',
    component: InvoiceDetailsComponent,
    data: { title: 'Invoice Details' },
  },

  {
    path: 'widgets',
    component: WidgetsComponent,
    data: { title: 'Widgets' },
  },
  {
    path: 'ui',
    loadChildren: () => import('./ui/ui.route').then((mod) => mod.UI_ROUTES),
  },
  {
    path: 'advanced',
    loadChildren: () =>
      import('./advanced-ui/advanced.route').then((mod) => mod.ADVANCED_ROUTES),
  },
  {
    path: 'charts',
    loadChildren: () =>
      import('./charts/charts.route').then((mod) => mod.CHART_ROUTE),
  },
  {
    path: 'forms',
    loadChildren: () =>
      import('./forms/forms.route').then((mod) => mod.FOMRS_ROUTE),
  },
  {
    path: 'tables',
    loadChildren: () =>
      import('./tables/table.route').then((mod) => mod.TABLES_ROUTE),
  },
  {
    path: 'icons',
    loadChildren: () =>
      import('./icons/icons.route').then((mod) => mod.ICONS_ROUTES),
  },
  {
    path: 'maps',
    loadChildren: () => import('./map/map.route').then((mod) => mod.MAPS_ROUTE),
  },
]
