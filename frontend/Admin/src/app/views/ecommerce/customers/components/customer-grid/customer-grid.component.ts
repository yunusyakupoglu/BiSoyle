import { Component, inject } from '@angular/core'
import { customerList, type CustomerType } from '../../data'
import type { Observable } from 'rxjs'
import { TableService } from '@/app/core/services/table.service'
import { CommonModule } from '@angular/common'

@Component({
  selector: 'customer-grid',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './customer-grid.component.html',
  styles: ``,
})
export class CustomerGridComponent {
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
