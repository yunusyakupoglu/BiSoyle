import { PageTitleComponent } from '@/app/components/page-title.component'
import { Component, inject, ViewChildren, type QueryList } from '@angular/core'
import { OrderDataComponent } from './component/order-data/order-data.component'
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap'
import { TableService } from '@/app/core/services/table.service'
import type { OrderType } from './data'
import { FormsModule } from '@angular/forms'
import { NgbdSortableHeader } from '@/app/directive/sortable.directive'

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [
    PageTitleComponent,
    OrderDataComponent,
    NgbDropdownModule,
    FormsModule,
  ],
  templateUrl: './orders.component.html',
  styles: ``,
})
export class OrdersComponent {
  @ViewChildren(NgbdSortableHeader) headers!: QueryList<
    NgbdSortableHeader<OrderType>
  >

  public tableService = inject(TableService<OrderType>)

  onSort(column: '' | keyof OrderType) {
    this.headers.forEach((header) => {
      if (header.sortable !== column) {
        header.direction = ''
      }
    })

    this.tableService.sortColumn = column
  }
}
