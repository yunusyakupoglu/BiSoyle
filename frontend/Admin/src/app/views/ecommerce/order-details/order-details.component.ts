import { PageTitleComponent } from '@/app/components/page-title.component'
import { Component } from '@angular/core'
import { ProductsComponent } from './components/products/products.component'
import { ShippingComponent } from './components/shipping/shipping.component'
import { BillingComponent } from './components/billing/billing.component'
import { DeliveryComponent } from './components/delivery/delivery.component'

@Component({
  selector: 'app-order-details',
  standalone: true,
  imports: [
    PageTitleComponent,
    ProductsComponent,
    ShippingComponent,
    BillingComponent,
    DeliveryComponent,
  ],
  templateUrl: './order-details.component.html',
  styles: ``,
})
export class OrderDetailsComponent {}
