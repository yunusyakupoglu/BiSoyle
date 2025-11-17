import { Component, inject } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { CommonModule } from '@angular/common'
import { HttpClient, HttpHeaders } from '@angular/common/http'
import { AuthService } from '@/app/services/auth.service'
import { environment } from '@/environments/environment'
import { Router } from '@angular/router'
import { PageTitleComponent } from '@/app/components/page-title.component'
import { NgbNavModule, NgbModal, NgbModalModule } from '@ng-bootstrap/ng-bootstrap'
import { AdminTicketListComponent } from './components/ticket-list/ticket-list.component'

@Component({
  selector: 'app-destek-ticket',
  standalone: true,
  imports: [FormsModule, CommonModule, PageTitleComponent, NgbNavModule, NgbModalModule, AdminTicketListComponent],
  templateUrl: './destek-ticket.component.html',
  styleUrls: ['./destek-ticket.component.scss']
})
export class DestekTicketComponent {
  http = inject(HttpClient)
  authService = inject(AuthService)
  router = inject(Router)
  private modalService = inject(NgbModal)
  
  selectedFiles: File[] = []
  saving = false
  success = false
  error: string | null = null
  activeTab: string = 'list'
  activeFilter: string = 'Open'
  formData: any = {
    title: '',
    description: '',
    priority: 'Medium'
  }

  get headers() {
    const token = this.authService.getToken()
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    })
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement
    if (input.files) {
      this.selectedFiles = Array.from(input.files)
    }
  }

  removeFile(index: number): void {
    this.selectedFiles.splice(index, 1)
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
  }

  openComposeModal(content: any): void {
    this.modalService.open(content, { size: 'lg', windowClass: 'compose-mail' })
  }

  saveTicket(): void {
    if (!this.formData.title || !this.formData.description) {
      this.error = 'Başlık ve açıklama gereklidir'
      return
    }

    this.saving = true
    this.error = null
    this.success = false
    
    const user = this.authService.getUser()
    
    const ticketData = {
      tenantId: user?.tenantId,
      createdByUserId: user?.id,
      title: this.formData.title,
      description: this.formData.description,
      priority: this.formData.priority || 'Medium',
      status: 'Open'
    }

    this.http.post<any>(`${environment.apiUrl}/tickets`, ticketData, { headers: this.headers }).subscribe({
      next: (response) => {
        const ticketId = response.id || response.Id
        if (ticketId && this.selectedFiles.length > 0) {
          this.uploadFiles(ticketId)
        } else {
          this.saving = false
          this.success = true
          this.modalService.dismissAll()
          this.resetForm()
          this.activeTab = 'list'
          setTimeout(() => {
            this.success = false
          }, 5000)
        }
      },
      error: (err) => {
        console.error('Ticket oluşturma hatası:', err)
        this.error = err.error?.detail || 'Ticket oluşturulurken bir hata oluştu'
        this.saving = false
      }
    })
  }

  uploadFiles(ticketId: number): void {
    if (this.selectedFiles.length === 0) {
      this.saving = false
      this.success = true
      this.modalService.dismissAll()
      this.resetForm()
      this.activeTab = 'list'
      setTimeout(() => {
        this.success = false
      }, 5000)
      return
    }

    console.log(`Starting file upload for ticket ${ticketId}, ${this.selectedFiles.length} files`)
    const formData = new FormData()
    this.selectedFiles.forEach((file, index) => {
      console.log(`Adding file ${index + 1}: ${file.name} (${file.size} bytes)`)
      formData.append('file', file)
    })

    // Content-Type header'ını ekleme, browser otomatik ekleyecek (multipart/form-data)
    const uploadHeaders = new HttpHeaders()
    const token = this.authService.getToken()
    if (token) {
      uploadHeaders.set('Authorization', `Bearer ${token}`)
    }

    const uploadUrl = `${environment.apiUrl}/tickets/${ticketId}/upload`
    console.log('Upload URL:', uploadUrl)

    this.http.post(uploadUrl, formData, { headers: uploadHeaders }).subscribe({
      next: (response) => {
        console.log('Dosya yükleme başarılı:', response)
        this.saving = false
        this.success = true
        this.modalService.dismissAll()
        this.resetForm()
        this.activeTab = 'list'
        setTimeout(() => {
          this.success = false
        }, 5000)
      },
      error: (err) => {
        console.error('Dosya yükleme hatası:', err)
        console.error('Error status:', err.status)
        console.error('Error details:', err.error)
        this.error = err.error?.detail || err.error?.message || 'Dosya yüklenirken bir hata oluştu'
        this.saving = false
      }
    })
  }

  resetForm(): void {
    this.formData = {
      title: '',
      description: '',
      priority: 'Medium'
    }
    this.selectedFiles = []
  }
}

