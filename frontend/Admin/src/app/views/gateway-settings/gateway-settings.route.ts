import { Route } from '@angular/router';
import { GatewaySettingsComponent } from './gateway-settings.component';

export const GATEWAY_SETTINGS_ROUTES: Route[] = [
  {
    path: '',
    component: GatewaySettingsComponent,
    data: { title: 'Sanal POS Ayarları', roles: ['SuperAdmin'] }
  }
];










