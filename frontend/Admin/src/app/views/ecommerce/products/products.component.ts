import { PageTitleComponent } from '@/app/components/page-title.component'
import { TableService } from '@/app/core/services/table.service'
import { CommonModule } from '@angular/common'
import { Component, type OnInit } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { RouterModule } from '@angular/router'
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap'
import type { Observable } from 'rxjs'
import { products, type ProductType } from './data'

@Component({
  selector: 'app-products',
  standalone: true,
  imports: [
    PageTitleComponent,
    CommonModule,
    RouterModule,
    NgbPaginationModule,
    FormsModule,
  ],
  templateUrl: './products.component.html',
  styles: ``,
})
export class ProductsComponent implements OnInit {
  products$: Observable<ProductType[]>
  total$: Observable<number>

  constructor(public tableService: TableService<ProductType>) {
    this.products$ = tableService.items$
    this.total$ = tableService.total$
  }

  ngOnInit(): void {
    this.tableService.setItems(products, 5)
  }
}
