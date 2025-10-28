import { Component, inject } from '@angular/core'
import { customerList, type CustomerType } from '../../data'
import type { Observable } from 'rxjs'
import { TableService } from '@/app/core/services/table.service'
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap'
import { CommonModule } from '@angular/common'

@Component({
  selector: 'customer-list',
  standalone: true,
  imports: [NgbPaginationModule, CommonModule],
  templateUrl: './customer-list.component.html',
  styles: ``,
})
export class CustomerListComponent {
  customers$: Observable<CustomerType[]>
  total$: Observable<number>

  public tableService = inject(TableService<CustomerType>)

  constructor() {
    this.customers$ = this.tableService.items$
    this.total$ = this.tableService.total$
  }

  ngOnInit(): void {
    this.tableService.setItems(customerList, 10)
  }
}
