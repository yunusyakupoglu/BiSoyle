import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core'
import { CommonModule } from '@angular/common'
import { NgbNavModule, NgbOffcanvas } from '@ng-bootstrap/ng-bootstrap'
import { TicketDetailComponent } from '../ticket-detail/ticket-detail.component'
import { HttpClient, HttpHeaders } from '@angular/common/http'
import { AuthService } from '@/app/services/auth.service'
import { environment } from '@/environments/environment'
import type { Ticket, TicketComment, TicketAttachment } from '@/app/views/ticket-management/components/ticket-list/ticket-list.component'

@Component({
  selector: 'admin-ticket-list',
  standalone: true,
  imports: [CommonModule, NgbNavModule],
  templateUrl: './ticket-list.component.html',
})
export class AdminTicketListComponent implements OnInit, OnDestroy, OnChanges {
  ticketList: Ticket[] = []
  ticketTab: string = 'Open'
  loading = false
  error: string | null = null
  selectedTicket: Ticket | null = null
  
  @Input() activeFilter: string = 'Open'
  
  // Pagination
  currentPage = 1
  itemsPerPage = 20
  totalItems = 0
  Math = Math
  
  public offcanvasService: NgbOffcanvas
  http: HttpClient
  authService: AuthService
  private detailRef: any = null

  constructor(
    offcanvasService: NgbOffcanvas,
    http: HttpClient,
    authService: AuthService
  ) {
    this.offcanvasService = offcanvasService
    this.http = http
    this.authService = authService
  }

  ngOnInit(): void {
    this.loadTickets()
    this.ticketTab = this.activeFilter || 'Open'
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['activeFilter'] && !changes['activeFilter'].firstChange) {
      this.ticketTab = this.activeFilter
      this.currentPage = 1
    }
  }

  ngOnDestroy(): void {
    if (this.detailRef) {
      this.detailRef.close()
    }
  }

  get headers() {
    const token = this.authService.getToken()
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    })
  }

  loadTickets(): void {
    this.loading = true
    this.error = null
    
    const user = this.authService.getUser()
    if (!user) {
      this.loading = false
      return
    }

    // Admin için sadece kendi ticket'larını getir
    const params = new URLSearchParams()
    params.append('tenantId', (user.tenantId || 0).toString())
    params.append('createdByUserId', (user.id || 0).toString())

    this.http.get<Ticket[]>(`${environment.apiUrl}/tickets?${params.toString()}`, { headers: this.headers }).subscribe({
      next: (tickets) => {
        this.ticketList = tickets || []
        // Tenant bilgilerini yükle
        this.loadTenantNames()
        this.filterTickets()
        this.loading = false
      },
      error: (err) => {
        console.error('Ticket yükleme hatası:', err)
        this.error = 'Ticket\'lar yüklenemedi'
        this.loading = false
      }
    })
  }

  loadTenantNames(): void {
    // Tenant ID'lerini topla
    const tenantIds = new Set<number>()
    this.ticketList.forEach(ticket => {
      if (ticket.tenantId && !ticket.tenantName) {
        tenantIds.add(ticket.tenantId)
      }
    })

    // Her tenant için bilgi çek
    tenantIds.forEach(tenantId => {
      this.http.get<any>(`${environment.apiUrl}/tenants/${tenantId}`, { headers: this.headers }).subscribe({
        next: (tenant) => {
          // Ticket'lara tenant adını ekle
          this.ticketList.forEach(ticket => {
            if (ticket.tenantId === tenantId) {
              ticket.tenantName = tenant.name || tenant.companyName || tenant.firmaAdi || `Firma ${tenantId}`
            }
          })
        },
        error: (err) => {
          console.error(`Tenant ${tenantId} yükleme hatası:`, err)
        }
      })
    })
  }

  filterTickets(): void {
    // Filtering is handled in getFilteredTickets
  }

  getFilteredTickets(): Ticket[] {
    let filtered: Ticket[] = []
    
    if (!this.ticketTab || this.ticketTab === 'Open') {
      filtered = this.ticketList.filter(t => t.status === 'Open')
    } else {
      const statusMap: { [key: string]: string } = {
        'InProgress': 'InProgress',
        'Resolved': 'Resolved',
        'Closed': 'Closed'
      }
      const mappedStatus = statusMap[this.ticketTab] || this.ticketTab
      filtered = this.ticketList.filter(t => t.status === mappedStatus)
    }
    
    this.totalItems = filtered.length
    return filtered
  }

  getPaginatedTickets(): Ticket[] {
    const filtered = this.getFilteredTickets()
    const startIndex = (this.currentPage - 1) * this.itemsPerPage
    const endIndex = startIndex + this.itemsPerPage
    return filtered.slice(startIndex, endIndex)
  }

  getTotalPages(): number {
    return Math.ceil(this.totalItems / this.itemsPerPage)
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--
    }
  }

  nextPage(): void {
    if (this.currentPage < this.getTotalPages()) {
      this.currentPage++
    }
  }

  openDetail(ticket: Ticket) {
    this.selectedTicket = ticket
    const divContainer = document.getElementById('adminDetailDiv')!
    
    if (this.detailRef) {
      this.detailRef.close()
    }
    
    this.detailRef = this.offcanvasService.open(TicketDetailComponent, {
      panelClass: 'mail-read position-absolute shadow show',
      backdrop: false,
      position: 'end',
      container: divContainer,
      scroll: true,
    })
    
    if (this.detailRef.componentInstance) {
      this.detailRef.componentInstance.ticket = ticket
      this.detailRef.componentInstance.loadTicketDetails()
    }

    this.detailRef.closed.subscribe(() => {
      this.selectedTicket = null
      this.detailRef = null
      this.loadTickets()
    })
  }

  trackByTicketId(index: number, ticket: Ticket): any {
    return ticket?.id || index
  }

  getPriorityColor(priority: string): string {
    const colors: { [key: string]: string } = {
      'Low': 'bg-secondary',
      'Medium': 'bg-info',
      'High': 'bg-warning',
      'Critical': 'bg-danger'
    }
    return colors[priority] || 'bg-secondary'
  }

  setFilter(status: string) {
    this.ticketTab = status
    this.currentPage = 1
  }

  toggleStar(ticket: Ticket, event: Event): void {
    event.stopPropagation()
    const newValue = !ticket.isStarred
    ticket.isStarred = newValue
    
    this.http.put(`${environment.apiUrl}/tickets/${ticket.id}`, {
      isStarred: newValue
    }, { headers: this.headers }).subscribe({
      next: () => {
        console.log(`Ticket ${ticket.id} starred: ${newValue}`)
      },
      error: (err) => {
        console.error('Star toggle hatası:', err)
        ticket.isStarred = !newValue // Revert on error
      }
    })
  }

  toggleImportant(ticket: Ticket, event: Event): void {
    event.stopPropagation()
    const newValue = !ticket.isImportant
    ticket.isImportant = newValue
    
    this.http.put(`${environment.apiUrl}/tickets/${ticket.id}`, {
      isImportant: newValue
    }, { headers: this.headers }).subscribe({
      next: () => {
        console.log(`Ticket ${ticket.id} important: ${newValue}`)
      },
      error: (err) => {
        console.error('Important toggle hatası:', err)
        ticket.isImportant = !newValue // Revert on error
      }
    })
  }
}





