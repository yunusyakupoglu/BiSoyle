import { Routes } from '@angular/router'
import { AuthLayoutComponent } from './layouts/auth-layout/auth-layout.component'
import { PrivateLayoutComponent } from './layouts/private-layout/private-layout.component'
import { AuthGuard } from './guards/auth.guard'
import { licenseGuard } from './guards/license.guard'

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'auth/sign-in',
    pathMatch: 'full',
  },
  {
    path: '',
    component: PrivateLayoutComponent,
    canActivate: [AuthGuard, licenseGuard],
    loadChildren: () =>
      import('./views/views.route').then((mod) => mod.VIEW_ROUTES),
  },
  {
    path: '',
    component: AuthLayoutComponent,
    loadChildren: () =>
      import('./views/other-pages/other-page.route').then(
        (mod) => mod.OTHER_PAGES_ROUTES
      ),
  },
  {
    path: 'auth',
    component: AuthLayoutComponent,
    loadChildren: () =>
      import('./views/auth/auth.route').then((mod) => mod.AUTH_ROUTES),
  },
]
