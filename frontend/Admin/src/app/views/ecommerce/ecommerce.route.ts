import { Route } from '@angular/router'
import { ProductsComponent } from './products/products.component'
import { ProductDetailsComponent } from './product-details/product-details.component'
import { CreateComponent } from './create/create.component'
import { CustomersComponent } from './customers/customers.component'
import { SellersComponent } from './sellers/sellers.component'
import { OrdersComponent } from './orders/orders.component'
import { OrderDetailsComponent } from './order-details/order-details.component'
import { InventoryComponent } from './inventory/inventory.component'

export const ECOMMERCE_ROUTES: Route[] = [
  {
    path: 'products',
    component: ProductsComponent,
    data: { title: 'Products' },
  },
  {
    path: 'product/:id',
    component: ProductDetailsComponent,
    data: { title: 'Product Details' },
  },
  {
    path: 'create',
    component: CreateComponent,
    data: { title: 'Create Product' },
  },
  {
    path: 'customers',
    component: CustomersComponent,
    data: { title: 'Customers List' },
  },
  {
    path: 'sellers',
    component: SellersComponent,
    data: { title: 'Sellers List' },
  },
  {
    path: 'orders',
    component: OrdersComponent,
    data: { title: 'Orders List' },
  },
  {
    path: 'orders/:id',
    component: OrderDetailsComponent,
    data: { title: 'Order details' },
  },
  {
    path: 'inventory',
    component: InventoryComponent,
    data: { title: 'Inventory' },
  },
]
