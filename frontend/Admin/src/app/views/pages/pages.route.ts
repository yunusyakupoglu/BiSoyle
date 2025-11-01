import { Route } from '@angular/router';
import { FaqsComponent } from './faqs/faqs.component';
import { Error404AltComponent } from './error-404-alt/error-404-alt.component';

export const PAGES_ROUTES: Route[] = [
  {
    path: 'faqs',
    component: FaqsComponent,
    data: { title: 'FAQs' },
  },
  {
    path: 'error-404-alt',
    component: Error404AltComponent,
    data: { title: '404 Error' },
  },
];
