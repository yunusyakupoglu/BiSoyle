import { PageTitleComponent } from '@/app/components/page-title.component'
import { Component, type OnInit } from '@angular/core'
import { ActivatedRoute } from '@angular/router'
import { DetailcardComponent } from './components/detailcard/detailcard.component'

@Component({
  selector: 'app-product-details',
  standalone: true,
  imports: [PageTitleComponent, DetailcardComponent],
  templateUrl: './product-details.component.html',
  styles: ``,
})
export class ProductDetailsComponent implements OnInit {
  productId: any

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => {
      this.productId = params.get('id')
    })
  }
}
