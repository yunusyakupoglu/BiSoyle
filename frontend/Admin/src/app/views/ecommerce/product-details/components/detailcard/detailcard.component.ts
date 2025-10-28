import { Component, Input } from '@angular/core'
import { productDetail } from '../../data'
import { NgbNavModule, NgbRatingModule } from '@ng-bootstrap/ng-bootstrap'
import { products, type ProductType } from '../../../products/data'
import { currency } from '@/app/common/constants'

@Component({
  selector: 'detail-card',
  standalone: true,
  imports: [NgbNavModule, NgbRatingModule],
  templateUrl: './detailcard.component.html',
  styles: ``,
})
export class DetailcardComponent {
  productDetail!: ProductType
  discountPrice: number = 0
  currency = currency

  @Input() productId: number = 1

  ngOnInit() {
    products.forEach((product) => {
      if (product.id == this.productId) {
        this.productDetail = product
      }
    })
  }
}
