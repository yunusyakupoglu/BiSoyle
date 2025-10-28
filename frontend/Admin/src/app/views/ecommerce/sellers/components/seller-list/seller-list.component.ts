import { Component, inject } from '@angular/core'
import { sellersList, type SellerType } from '../../data'
import { CommonModule } from '@angular/common'
import type { Observable } from 'rxjs'
import { TableService } from '@/app/core/services/table.service'
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap'

@Component({
  selector: 'seller-list',
  standalone: true,
  imports: [CommonModule, NgbPaginationModule],
  templateUrl: './seller-list.component.html',
  styles: ``,
})
export class SellerListComponent {
  sellers$: Observable<SellerType[]>
  total$: Observable<number>

  public tableService = inject(TableService<SellerType>)

  constructor() {
    this.sellers$ = this.tableService.items$
    this.total$ = this.tableService.total$
  }

  ngOnInit(): void {
    this.tableService.setItems(sellersList, 10)
  }
}
