import { Component, inject } from '@angular/core'
import { orderList, type OrderType } from '../../data'
import { CommonModule } from '@angular/common'
import type { Observable } from 'rxjs'
import { TableService } from '@/app/core/services/table.service'
import { NgbPaginationModule } from '@ng-bootstrap/ng-bootstrap'
import { RouterLink } from '@angular/router'

@Component({
  selector: 'order-data',
  standalone: true,
  imports: [CommonModule, NgbPaginationModule, RouterLink],
  templateUrl: './order-data.component.html',
  styles: ``,
})
export class OrderDataComponent {
  orders$: Observable<OrderType[]>
  total$: Observable<number>

  public tableService = inject(TableService<OrderType>)

  constructor() {
    this.orders$ = this.tableService.items$
    this.total$ = this.tableService.total$
  }

  ngOnInit(): void {
    this.tableService.setItems(orderList, 10)
  }
}
