import { PageTitleComponent } from '@/app/components/page-title.component'
import { TableService } from '@/app/core/services/table.service'
import { NgbdSortableHeader } from '@/app/directive/sortable.directive'
import { Component, inject, ViewChildren, type QueryList } from '@angular/core'
import { FormsModule } from '@angular/forms'
import {
  NgbDropdownModule,
  NgbNavModule,
  NgbTooltipModule,
} from '@ng-bootstrap/ng-bootstrap'
import { CustomerGridComponent } from './components/customer-grid/customer-grid.component'
import { CustomerListComponent } from './components/customer-list/customer-list.component'
import { type CustomerType } from './data'

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [
    PageTitleComponent,
    CustomerListComponent,
    CustomerGridComponent,
    NgbNavModule,
    NgbDropdownModule,
    FormsModule,
    NgbTooltipModule,
  ],
  templateUrl: './customers.component.html',
  styles: ``,
})
export class CustomersComponent {
  active = 2
  sort!: '' | keyof CustomerType

  @ViewChildren(NgbdSortableHeader) headers!: QueryList<
    NgbdSortableHeader<CustomerType>
  >

  public tableService = inject(TableService<CustomerType>)

  sortData(column: any) {
    this.sort = column
    this.onSort()
  }

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
