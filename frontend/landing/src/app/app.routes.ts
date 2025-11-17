import { Routes } from '@angular/router';
import { AppComponent } from './app';
import { HakkimizdaComponent } from './pages/hakkimizda.component';
import { TeslimatVeIadeComponent } from './pages/teslimat-ve-iade.component';
import { GizlilikSozlesmesiComponent } from './pages/gizlilik-sozlesmesi.component';
import { MesafeliSatisSozlesmesiComponent } from './pages/mesafeli-satis-sozlesmesi.component';

export const routes: Routes = [
  {
    path: '',
    component: AppComponent,
    title: 'De\'Bakiim | Sesli Sipariş Platformu'
  },
  {
    path: 'hakkimizda',
    component: HakkimizdaComponent,
    title: 'Hakkımızda'
  },
  {
    path: 'teslimat-ve-iade',
    component: TeslimatVeIadeComponent,
    title: 'Teslimat ve İade Şartları'
  },
  {
    path: 'gizlilik-sozlesmesi',
    component: GizlilikSozlesmesiComponent,
    title: 'Gizlilik Sözleşmesi'
  },
  {
    path: 'mesafeli-satis-sozlesmesi',
    component: MesafeliSatisSozlesmesiComponent,
    title: 'Mesafeli Satış Sözleşmesi'
  }
];
