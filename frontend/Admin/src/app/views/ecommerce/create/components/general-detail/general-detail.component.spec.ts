import { ComponentFixture, TestBed } from '@angular/core/testing'

import { GeneralDEtailComponent } from './general-detail.component'

describe('GeneralDEtailComponent', () => {
  let component: GeneralDEtailComponent
  let fixture: ComponentFixture<GeneralDEtailComponent>

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GeneralDEtailComponent],
    }).compileComponents()

    fixture = TestBed.createComponent(GeneralDEtailComponent)
    component = fixture.componentInstance
    fixture.detectChanges()
  })

  it('should create', () => {
    expect(component).toBeTruthy()
  })
})
