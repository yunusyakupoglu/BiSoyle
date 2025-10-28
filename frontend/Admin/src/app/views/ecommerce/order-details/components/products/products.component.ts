import { currency } from '@/app/common/constants'
import { Component } from '@angular/core'

@Component({
  selector: 'order-detail-products',
  standalone: true,
  imports: [],
  templateUrl: './products.component.html',
  styles: ``,
})
export class ProductsComponent {
  currency = currency
}
