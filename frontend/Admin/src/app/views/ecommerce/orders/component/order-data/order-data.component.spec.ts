import { ComponentFixture, TestBed } from '@angular/core/testing'

import { OrderDataComponent } from './order-data.component'

describe('OrderDataComponent', () => {
  let component: OrderDataComponent
  let fixture: ComponentFixture<OrderDataComponent>

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OrderDataComponent],
    }).compileComponents()

    fixture = TestBed.createComponent(OrderDataComponent)
    component = fixture.componentInstance
    fixture.detectChanges()
  })

  it('should create', () => {
    expect(component).toBeTruthy()
  })
})
