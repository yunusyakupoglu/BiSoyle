import { ComponentFixture, TestBed } from '@angular/core/testing'

import { SellerGridComponent } from './seller-grid.component'

describe('SellerGridComponent', () => {
  let component: SellerGridComponent
  let fixture: ComponentFixture<SellerGridComponent>

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SellerGridComponent],
    }).compileComponents()

    fixture = TestBed.createComponent(SellerGridComponent)
    component = fixture.componentInstance
    fixture.detectChanges()
  })

  it('should create', () => {
    expect(component).toBeTruthy()
  })
})
