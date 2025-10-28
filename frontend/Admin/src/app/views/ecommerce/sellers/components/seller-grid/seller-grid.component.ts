import { Component, inject } from '@angular/core'
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap'
import { sellersList, type SellerType } from '../../data'
import type { Observable } from 'rxjs'
import { TableService } from '@/app/core/services/table.service'
import { CommonModule } from '@angular/common'

@Component({
  selector: 'seller-grid',
  standalone: true,
  imports: [NgbDropdownModule, CommonModule],
  templateUrl: './seller-grid.component.html',
  styles: ``,
})
export class SellerGridComponent {
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
