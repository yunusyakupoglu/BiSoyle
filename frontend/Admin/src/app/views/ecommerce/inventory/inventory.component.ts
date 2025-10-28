import { PageTitleComponent } from '@/app/components/page-title.component'
import { Component } from '@angular/core'
import { FilterComponent } from './components/filter/filter.component'
import { InventoryProductComponent } from './components/inventory-product/inventory-product.component'

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [PageTitleComponent, FilterComponent, InventoryProductComponent],
  templateUrl: './inventory.component.html',
  styles: ``,
})
export class InventoryComponent {}
