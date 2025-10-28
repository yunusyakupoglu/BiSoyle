import { PageTitleComponent } from '@/app/components/page-title.component'
import { Component, inject, ViewChildren, type QueryList } from '@angular/core'
import { SellerListComponent } from './components/seller-list/seller-list.component'
import { SellerGridComponent } from './components/seller-grid/seller-grid.component'
import { NgbNavModule, NgbTooltipModule } from '@ng-bootstrap/ng-bootstrap'
import type { SellerType } from './data'
import { TableService } from '@/app/core/services/table.service'
import { FormsModule } from '@angular/forms'
import { NgbdSortableHeader } from '@/app/directive/sortable.directive'

@Component({
  selector: 'app-sellers',
  standalone: true,
  imports: [
    PageTitleComponent,
    SellerListComponent,
    SellerGridComponent,
    NgbNavModule,
    FormsModule,
    NgbTooltipModule,
  ],
  templateUrl: './sellers.component.html',
  styles: ``,
})
export class SellersComponent {
  sort!: '' | keyof SellerType

  @ViewChildren(NgbdSortableHeader) headers!: QueryList<
    NgbdSortableHeader<SellerType>
  >

  public tableService = inject(TableService<SellerType>)

  onSort() {
    const column = this.sort
    this.headers.forEach((header) => {
      if (header.sortable !== column) {
        header.direction = ''
      }
    })

    this.tableService.sortColumn = column
  }
}
