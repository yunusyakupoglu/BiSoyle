import { Component } from '@angular/core'
import { FormsModule } from '@angular/forms'
import { CommonModule } from '@angular/common'
import {
  NgbActiveOffcanvas,
  NgbTooltipModule,
} from '@ng-bootstrap/ng-bootstrap'
import { HttpClient, HttpHeaders } from '@angular/common/http'
import { AuthService } from '@/app/services/auth.service'
import { environment } from '@/environments/environment'
import type { Ticket, TicketComment, TicketAttachment } from '@/app/views/ticket-management/components/ticket-list/ticket-list.component'

@Component({
  selector: 'admin-ticket-detail',
  standalone: true,
  imports: [FormsModule, CommonModule, NgbTooltipModule],
  templateUrl: './ticket-detail.component.html',
  styles: [`
    :host {
      display: contents !important;
    }
  `],
})
export class TicketDetailComponent {
  activeOffcanvas: NgbActiveOffcanvas
  http: HttpClient
  authService: AuthService
  
  ticket: Ticket | null = null
  commentText = ''
  commentFiles: File[] = []
  saving = false

  constructor(
    activeOffcanvas: NgbActiveOffcanvas,
    http: HttpClient,
    authService: AuthService
  ) {
    this.activeOffcanvas = activeOffcanvas
    this.http = http
    this.authService = authService
  }

  get headers() {
    const token = this.authService.getToken()
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    })
  }

  closeDetail(): void {
    this.activeOffcanvas.close()
  }

  loadTicketDetails(): void {
    if (!this.ticket?.id) return
    
    this.http.get<Ticket>(`${environment.apiUrl}/tickets/${this.ticket.id}`, { headers: this.headers }).subscribe({
      next: (ticket) => {
        this.ticket = ticket
        // Tenant adını yükle
        if (ticket.tenantId && !ticket.tenantName) {
          this.loadTenantName(ticket.tenantId)
        }
        // Yorumlardaki kullanıcı adlarını yükle
        if (ticket.comments) {
          this.loadUserNames(ticket.comments)
        }
      },
      error: (err) => {
        console.error('Ticket detay yükleme hatası:', err)
      }
    })
  }

  loadTenantName(tenantId: number): void {
    this.http.get<any>(`${environment.apiUrl}/tenants/${tenantId}`, { headers: this.headers }).subscribe({
      next: (tenant) => {
        if (this.ticket) {
          this.ticket.tenantName = tenant.name || tenant.companyName || tenant.firmaAdi || `Firma ${tenantId}`
        }
      },
      error: (err) => {
        console.error(`Tenant ${tenantId} yükleme hatası:`, err)
      }
    })
  }

  loadUserNames(comments: TicketComment[]): void {
    const userIds = new Set<number>()
    comments.forEach(comment => {
      if (comment.userId && !comment.userName) {
        userIds.add(comment.userId)
      }
    })

    userIds.forEach(userId => {
      this.http.get<any>(`${environment.apiUrl}/users/${userId}`, { headers: this.headers }).subscribe({
        next: (user) => {
          if (this.ticket && this.ticket.comments) {
            this.ticket.comments.forEach(comment => {
              if (comment.userId === userId && !comment.userName) {
                const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim()
                comment.userName = fullName || user.username || `Kullanıcı ${userId}`
              }
            })
          }
        },
        error: (err) => {
          console.error(`User ${userId} yükleme hatası:`, err)
        }
      })
    })
  }

  onCommentFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement
    if (input.files) {
      this.commentFiles = Array.from(input.files)
    }
  }

  removeCommentFile(index: number): void {
    this.commentFiles.splice(index, 1)
  }

  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes'
    const k = 1024
    const sizes = ['Bytes', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round(bytes / Math.pow(k, i) * 100) / 100 + ' ' + sizes[i]
  }

  addComment(): void {
    if (!this.ticket?.id || !this.commentText.trim()) return
    
    const user = this.authService.getUser()
    if (!user || !user.id) {
      console.error('Kullanıcı bilgisi bulunamadı')
      alert('Kullanıcı bilgisi bulunamadı. Lütfen tekrar giriş yapın.')
      return
    }
    
    this.saving = true
    
    const commentData = {
      userId: user.id,
      comment: this.commentText.trim()
    }

    console.log('Comment gönderiliyor:', commentData)

    this.http.post<any>(`${environment.apiUrl}/tickets/${this.ticket.id}/comments`, commentData, { headers: this.headers }).subscribe({
      next: (response) => {
        console.log('Comment response:', response)
        console.log('Comment response type:', typeof response)
        console.log('Comment response keys:', response ? Object.keys(response) : 'null')
        console.log('Comment files to upload:', this.commentFiles.length)
        console.log('Comment files:', this.commentFiles.map(f => f.name))
        
        // Response'dan commentId'yi al - farklı formatları kontrol et
        let commentId: number | null = null
        
        // Response Created (201) durumunda body'yi kontrol et
        if (response) {
          if (typeof response === 'object') {
            // Önce küçük harf 'id' kontrol et
            if ('id' in response && response.id != null) {
              commentId = Number(response.id)
              console.log('Found commentId from response.id:', commentId)
            }
            // Sonra büyük harf 'Id' kontrol et
            else if ('Id' in response && response.Id != null) {
              commentId = Number(response.Id)
              console.log('Found commentId from response.Id:', commentId)
            }
            // Sonra 'commentId' kontrol et
            else if ('commentId' in response && response.commentId != null) {
              commentId = Number(response.commentId)
              console.log('Found commentId from response.commentId:', commentId)
            }
            else {
              console.log('No id field found in response, available keys:', Object.keys(response))
            }
          } else if (typeof response === 'number') {
            commentId = response
            console.log('CommentId is number:', commentId)
          } else if (typeof response === 'string') {
            const parsed = parseInt(response, 10)
            if (!isNaN(parsed)) {
              commentId = parsed
              console.log('CommentId parsed from string:', commentId)
            }
          }
        }
        
        console.log('Final commentId:', commentId)
        console.log('Has files:', this.commentFiles.length > 0)
        
        if (commentId && this.commentFiles.length > 0) {
          console.log(`Uploading ${this.commentFiles.length} files for comment ${commentId}`)
          // Dosyaları sakla çünkü uploadCommentFiles içinde this.commentFiles temizleniyor
          const filesToUpload = [...this.commentFiles]
          this.uploadCommentFiles(this.ticket!.id, commentId, filesToUpload)
        } else {
          if (!commentId) {
            console.error('CommentId could not be extracted from response!')
            console.error('Response was:', JSON.stringify(response, null, 2))
          }
          if (this.commentFiles.length === 0) {
            console.log('No files to upload')
          }
          // Yorum başarıyla eklendi, formu temizle ve ticket'ı yenile
          this.commentText = ''
          this.commentFiles = []
          this.saving = false
          this.loadTicketDetails()
        }
      },
      error: (err) => {
        console.error('Yorum ekleme hatası:', err)
        console.error('Error details:', err.error)
        console.error('Error status:', err.status)
        
        // 500 hatası olsa bile yorum eklenmiş olabilir, ticket'ı yenile
        if (err.status === 500) {
          console.log('500 hatası alındı, ticket yenileniyor...')
          this.commentText = ''
          this.commentFiles = []
          this.saving = false
          this.loadTicketDetails()
        } else {
          alert(err.error?.detail || err.error?.message || 'Yorum eklenirken bir hata oluştu')
          this.saving = false
        }
      }
    })
  }

  uploadCommentFiles(ticketId: number, commentId: number, files?: File[]): void {
    const filesToUpload = files || this.commentFiles
    
    if (filesToUpload.length === 0) {
      console.log('No files to upload')
      this.commentText = ''
      this.commentFiles = []
      this.saving = false
      this.loadTicketDetails()
      return
    }

    console.log(`Starting file upload for comment ${commentId}, ${filesToUpload.length} files`)
    const formData = new FormData()
    filesToUpload.forEach((file, index) => {
      console.log(`Adding file ${index + 1}: ${file.name} (${file.size} bytes)`)
      formData.append('file', file)
    })

    // Content-Type header'ını ekleme, browser otomatik ekleyecek (multipart/form-data)
    const uploadHeaders = new HttpHeaders()
    const token = this.authService.getToken()
    if (token) {
      uploadHeaders.set('Authorization', `Bearer ${token}`)
    }

    const uploadUrl = `${environment.apiUrl}/tickets/${ticketId}/comments/${commentId}/upload`
    console.log('Upload URL:', uploadUrl)
    console.log('FormData files count:', this.commentFiles.length)

    this.http.post(uploadUrl, formData, { headers: uploadHeaders }).subscribe({
      next: (response) => {
        console.log('Dosya yükleme başarılı:', response)
        this.commentText = ''
        this.commentFiles = []
        this.saving = false
        this.loadTicketDetails()
      },
      error: (err) => {
        console.error('Dosya yükleme hatası:', err)
        console.error('Error status:', err.status)
        console.error('Error details:', err.error)
        console.error('Error message:', err.message)
        alert(err.error?.detail || err.error?.message || 'Dosya yüklenirken bir hata oluştu')
        this.saving = false
        // Hata olsa bile ticket'ı yenile
        this.loadTicketDetails()
      }
    })
  }

  downloadAttachment(attachment: TicketAttachment): void {
    window.open(`${environment.apiUrl}/tickets/attachments/${attachment.id}/download`, '_blank')
  }

  getAttachmentUrl(attachment: TicketAttachment): string {
    return `${environment.apiUrl}/tickets/attachments/${attachment.id}/download`
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

  getStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      'Open': 'bg-primary',
      'InProgress': 'bg-warning',
      'Resolved': 'bg-success',
      'Closed': 'bg-secondary'
    }
    return colors[status] || 'bg-secondary'
  }
}

