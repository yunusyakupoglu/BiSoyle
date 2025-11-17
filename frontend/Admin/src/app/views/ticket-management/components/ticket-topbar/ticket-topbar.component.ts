import { Component } from '@angular/core'
import { NgbDropdownModule, NgbTooltipModule, NgbOffcanvas } from '@ng-bootstrap/ng-bootstrap'
import { CommonModule } from '@angular/common'
import { TicketListComponent } from '../ticket-list/ticket-list.component'

@Component({
  selector: 'ticket-topbar',
  standalone: true,
  imports: [CommonModule, NgbDropdownModule, NgbTooltipModule, TicketListComponent],
  templateUrl: './ticket-topbar.component.html',
})
export class TicketTopbarComponent {
  public offcanvasService: NgbOffcanvas

  constructor(offcanvasService: NgbOffcanvas) {
    this.offcanvasService = offcanvasService
  }

  openSidebar() {
    // Sidebar açma işlemi (gerekirse)
  }
}

import { CommonModule } from '@angular/common'
import { TicketListComponent } from '../ticket-list/ticket-list.component'

@Component({
  selector: 'ticket-topbar',
  standalone: true,
  imports: [CommonModule, NgbDropdownModule, NgbTooltipModule, TicketListComponent],
  templateUrl: './ticket-topbar.component.html',
})
export class TicketTopbarComponent {
  public offcanvasService: NgbOffcanvas

  constructor(offcanvasService: NgbOffcanvas) {
    this.offcanvasService = offcanvasService
  }

  openSidebar() {
    // Sidebar açma işlemi (gerekirse)
  }
}


