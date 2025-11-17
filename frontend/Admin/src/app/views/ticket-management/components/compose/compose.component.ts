import { Component } from '@angular/core'
import { TicketSharedDataService } from '../filter.service'
import { CommonModule } from '@angular/common'

@Component({
  selector: 'ticket-compose',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './compose.component.html',
  styles: [`
    :host(ticket-compose) {
      display: contents !important;
    }
  `],
})
export class TicketComposeComponent {
  public sharedDataService: TicketSharedDataService

  constructor(sharedDataService: TicketSharedDataService) {
    this.sharedDataService = sharedDataService
  }

  updateData(key: string, type: string) {
    this.sharedDataService.updateData(key, type)
  }
}

import { CommonModule } from '@angular/common'

@Component({
  selector: 'ticket-compose',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './compose.component.html',
  styles: [`
    :host(ticket-compose) {
      display: contents !important;
    }
  `],
})
export class TicketComposeComponent {
  public sharedDataService: TicketSharedDataService

  constructor(sharedDataService: TicketSharedDataService) {
    this.sharedDataService = sharedDataService
  }

  updateData(key: string, type: string) {
    this.sharedDataService.updateData(key, type)
  }
}


