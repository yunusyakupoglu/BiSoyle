import { Component, inject } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { CommonModule } from '@angular/common'
import { HttpClient, HttpHeaders } from '@angular/common/http'
import { AuthService } from '@/app/services/auth.service'
import { environment } from '@/environments/environment'
import { PageTitleComponent } from '@/app/components/page-title.component'
import { NgbNavModule } from '@ng-bootstrap/ng-bootstrap'
import { TicketListComponent } from './components/ticket-list/ticket-list.component'

@Component({
  selector: 'app-ticket-management',
  standalone: true,
  imports: [FormsModule, CommonModule, PageTitleComponent, NgbNavModule, TicketListComponent],
  templateUrl: './ticket-management.component.html',
  styles: ``,
})
export class TicketManagementComponent {
  http = inject(HttpClient)
  authService = inject(AuthService)
  
  activeTab: string = 'list'
  activeFilter: string = 'Open'
}

