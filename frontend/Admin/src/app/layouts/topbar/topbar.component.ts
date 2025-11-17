import { changetheme } from '@/app/store/layout/layout-action'
import { CommonModule, DOCUMENT } from '@angular/common'
import {
  CUSTOM_ELEMENTS_SCHEMA,
  Component,
  EventEmitter,
  Inject,
  Output,
  inject,
  type TemplateRef,
  OnInit,
  OnDestroy,
  ChangeDetectorRef,
} from '@angular/core'
import {
  NgbDropdownModule,
  NgbOffcanvas,
  NgbOffcanvasModule,
  NgbTooltipModule,
} from '@ng-bootstrap/ng-bootstrap'
import { Store } from '@ngrx/store'
import { SimplebarAngularModule } from 'simplebar-angular'
import { getLayoutColor } from '../../store/layout/layout-selector'
import { logout } from '@/app/store/authentication/authentication.actions'
import { Router, RouterModule } from '@angular/router'
import { activityStreamData, appsData, notificationsData } from './data'
import { AuthService } from '@/app/services/auth.service'
import { TransactionRefreshService } from '@/app/services/transaction-refresh.service'
import { environment } from '@/environments/environment'
import { HttpClient, HttpHeaders } from '@angular/common/http'

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [
    NgbDropdownModule,
    SimplebarAngularModule,
    NgbOffcanvasModule,
    CommonModule,
    NgbTooltipModule,
    RouterModule,
  ],
  templateUrl: './topbar.component.html',
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class TopbarComponent implements OnInit, OnDestroy {
  element: any

  router = inject(Router)
  store = inject(Store)
  offcanvasService = inject(NgbOffcanvas)
  authService = inject(AuthService)
  transactionRefreshService = inject(TransactionRefreshService)
  http = inject(HttpClient)
  cdr = inject(ChangeDetectorRef)

  notificationList = notificationsData
  appsList = appsData
  activityList = activityStreamData

  // Voice Recognition
  isListening = false
  currentSpeaker: string | null = null
  ws: WebSocket | null = null
  mediaStream: MediaStream | null = null
  audioContext: AudioContext | null = null
  analyser: AnalyserNode | null = null
  animationFrameId: number | null = null
  recognition: any = null
  recognizedText: string = ''
  isReceiptMode = false  // "fiş başlat" denince true olur
  receiptItems: any[] = []  // Fiş itemları
  lastPrintTime = 0  // Debounce için son yazdırma zamanı
  
  // Dynamic notifications
  voiceNotifications: any[] = []
  private maxNotifications = 5  // Max 5 bildirim göster
  
  // Task notifications
  taskNotifications: any[] = []
  unreadCount = 0
  
  private notificationPollInterval: any = null
  isAdmin = false
  isSuperAdmin = false
  
  // DEBUG: Test için geçici olarak true yap
  showNotifications = true
  
  // Product cache for validation
  private productCache: any[] = []
  private productCacheTime = 0
  private readonly PRODUCT_CACHE_TTL = 60000 // 1 dakika
  
  // User cache for speaker-to-user mapping
  private userCache: any[] = []
  private userCacheTime = 0
  private readonly USER_CACHE_TTL = 300000 // 5 dakika
  
  // Microphone permission
  microphonePermission: PermissionState | null = null

  constructor(@Inject(DOCUMENT) private document: any) {}
  @Output() settingsButtonClicked = new EventEmitter()
  @Output() mobileMenuButtonClicked = new EventEmitter()

  ngOnInit(): void {
    this.element = document.documentElement
    this.checkMicrophonePermission()
    // Kullanıcı bilgisi yüklenene kadar bekle - daha uzun süre
    setTimeout(() => {
      this.checkAdminRole()
    }, 200)
    
    // Birkaç kez kontrol et (kullanıcı bilgisi geç yüklenebilir)
    setTimeout(() => {
      this.checkAdminRole()
      if (this.isAdmin) {
        this.loadNotifications()
        this.startNotificationPolling()
      }
    }, 500)
    
    setTimeout(() => {
      this.checkAdminRole()
    }, 1000)
  }
  
  checkAdminRole(): void {
    const user = this.authService.getUser()
    console.log('Topbar checkAdminRole: user =', user)
    if (!user) {
      console.log('Topbar: Kullanıcı bilgisi henüz yüklenmemiş')
      this.isAdmin = false
      this.isSuperAdmin = false
      this.cdr.detectChanges()
      return
    }
    const roles = user?.roles || []
    const wasAdmin = this.isAdmin
    const wasSuperAdmin = this.isSuperAdmin
    this.isAdmin = roles.includes('Admin') || roles.includes('SuperAdmin')
    this.isSuperAdmin = roles.includes('SuperAdmin')
    console.log('Topbar: isAdmin =', this.isAdmin, 'isSuperAdmin =', this.isSuperAdmin, 'roles =', roles, 'wasAdmin =', wasAdmin)
    
    if (this.isAdmin && !wasAdmin) {
      // İlk kez admin olarak tespit edildi, bildirimleri yükle
      this.loadNotifications()
      this.startNotificationPolling()
    }
    
    
    this.cdr.detectChanges()
  }
  
  ngOnDestroy(): void {
    if (this.notificationPollInterval) {
      clearInterval(this.notificationPollInterval)
    }
    this.stopListening()
  }
  
  get headers() {
    const token = this.authService.getToken()
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    })
  }
  
  loadNotifications(): void {
    if (!this.isAdmin) return
    
    const user = this.authService.getUser()
    if (!user || !user.id) return
    
    const params = new URLSearchParams()
    params.append('userId', user.id.toString())
    if (user.tenantId) {
      params.append('tenantId', user.tenantId.toString())
    }
    
    this.http.get<any[]>(`${environment.apiUrl}/notifications?${params.toString()}`, { headers: this.headers }).subscribe({
      next: (notifications) => {
        this.taskNotifications = notifications || []
        this.updateUnreadCount()
      },
      error: (err) => {
        // Sadece 500 hatası değilse log'la
        if (err.status !== 500) {
          console.error('Bildirimler yüklenemedi:', err)
        }
        // Hata durumunda mevcut bildirimleri koru
      }
    })
  }
  
  loadUnreadCount(): void {
    if (!this.isAdmin) return
    
    const user = this.authService.getUser()
    if (!user || !user.id) return
    
    const params = new URLSearchParams()
    params.append('userId', user.id.toString())
    if (user.tenantId) {
      params.append('tenantId', user.tenantId.toString())
    }
    
    this.http.get<{count: number}>(`${environment.apiUrl}/notifications/unread-count?${params.toString()}`, { headers: this.headers }).subscribe({
      next: (data) => {
        this.unreadCount = data.count || 0
      },
      error: (err) => {
        // Sadece 500 hatası değilse log'la, 500 hatası çok fazla olabilir
        if (err.status !== 500) {
          console.error('Okunmamış bildirim sayısı alınamadı:', err)
        }
        // Hata durumunda unreadCount'u 0 yapma, mevcut değeri koru
      }
    })
  }
  
  updateUnreadCount(): void {
    this.unreadCount = this.taskNotifications.filter(n => !n.isRead).length
  }
  
  startNotificationPolling(): void {
    // Her 30 saniyede bir bildirimleri kontrol et (10 saniyeden 30 saniyeye çıkarıldı)
    this.notificationPollInterval = setInterval(() => {
      this.loadUnreadCount()
      // Sadece okunmamış bildirim sayısını güncelle, tüm bildirimleri her seferinde yükleme
    }, 30000)
  }
  
  markAsRead(notification: any): void {
    if (notification.isRead) return
    
    this.http.patch(`${environment.apiUrl}/notifications/${notification.id}/read`, {}, { headers: this.headers }).subscribe({
      next: () => {
        notification.isRead = true
        this.updateUnreadCount()
      },
      error: (err) => {
        console.error('Bildirim okundu işaretlenemedi:', err)
      }
    })
  }
  
  markAllAsRead(): void {
    const user = this.authService.getUser()
    if (!user || !user.id) return
    
    const body: any = { userId: user.id }
    if (user.tenantId) {
      body.tenantId = user.tenantId
    }
    
    this.http.patch(`${environment.apiUrl}/notifications/read-all`, body, { headers: this.headers }).subscribe({
      next: () => {
        this.taskNotifications.forEach(n => n.isRead = true)
        this.updateUnreadCount()
      },
      error: (err) => {
        console.error('Tüm bildirimler okundu işaretlenemedi:', err)
      }
    })
  }
  
  deleteNotification(notification: any, event: Event): void {
    event.stopPropagation()
    
    this.http.delete(`${environment.apiUrl}/notifications/${notification.id}`, { headers: this.headers }).subscribe({
      next: () => {
        this.taskNotifications = this.taskNotifications.filter(n => n.id !== notification.id)
        this.updateUnreadCount()
      },
      error: (err) => {
        console.error('Bildirim silinemedi:', err)
      }
    })
  }
  
  navigateToTask(notification: any): void {
    // Ticket notification'ları için ticket sayfasına yönlendir
    if (notification.relatedTicketId) {
      const user = this.authService.getUser()
      const roles = user?.roles || []
      const isSuperAdmin = roles.includes('SuperAdmin')
      
      if (isSuperAdmin) {
        this.router.navigate(['/ticket-management'])
      } else {
        this.router.navigate(['/destek-ticket'], { queryParams: { ticketId: notification.relatedTicketId } })
      }
      this.markAsRead(notification)
    } else if (notification.relatedTaskId) {
      this.router.navigate(['/task-management'])
      this.markAsRead(notification)
    }
  }
  
  trackByNotificationId(index: number, notification: any): any {
    return notification?.id || index
  }
  
  async checkMicrophonePermission(): Promise<void> {
    try {
      if (navigator.permissions) {
        const result = await navigator.permissions.query({ name: 'microphone' as PermissionName })
        this.microphonePermission = result.state
        result.onchange = () => {
          this.microphonePermission = result.state
        }
      }
    } catch (err) {
      console.log('Permission API not supported, using fallback')
      // Fallback: try to get permission by requesting media
      try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
        stream.getTracks().forEach(track => track.stop())
        this.microphonePermission = 'granted'
      } catch (e) {
        this.microphonePermission = 'denied'
      }
    }
  }
  
  async requestMicrophonePermission(): Promise<void> {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      stream.getTracks().forEach(track => track.stop())
      this.microphonePermission = 'granted'
      this.addVoiceNotification('Sistem', 'Mikrofon izni verildi')
      // İzin verildikten sonra dinlemeyi başlat
      if (!this.isListening) {
        this.startListening()
      }
    } catch (err) {
      this.microphonePermission = 'denied'
      this.addVoiceNotification('Sistem', 'Mikrofon izni reddedildi')
      console.error('Microphone permission denied:', err)
    }
  }


  toggleListening(): void {
    if (this.isListening) {
      this.stopListening()
    } else {
      this.startListening()
    }
  }

  startListening(): void {
    // Mikrofon izni kontrolü
    if (this.microphonePermission === 'denied') {
      this.addVoiceNotification('Sistem', 'Mikrofon izni reddedilmiş. Lütfen İzinler sayfasından izin verin.')
      return
    }

    // WebSocket ile konuşmacı tanıma - Voice Service (Python)
    const wsProto = location.protocol === 'https:' ? 'wss' : 'ws'
    this.ws = new WebSocket(`${wsProto}://localhost:8766`)

    // Web Speech API ile konuşma tanıma
    const SpeechRecognition = (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition
    if (SpeechRecognition) {
      this.recognition = new SpeechRecognition()
      this.recognition.lang = 'tr-TR'
      this.recognition.continuous = true
      this.recognition.interimResults = false

      this.recognition.onresult = (event: any) => {
        // Son kesin sonucu al (interim olmayan)
        for (let i = event.results.length - 1; i >= 0; i--) {
          if (event.results[i].isFinal) {
            const text = event.results[i][0].transcript.toLowerCase()
            this.recognizedText = text
            console.log('Speech recognition result:', text)
            this.processVoiceCommand(text)
            break
          }
        }
      }
      
      this.recognition.onend = () => {
        // Hızlandırma: Anında tekrar başlat
        if (this.isListening && this.recognition) {
          try {
            this.recognition.start()
          } catch (e: any) {
            // Ignore - zaten çalışıyor
          }
        }
      }

      this.recognition.onerror = (event: any) => {
        console.error('Speech recognition error:', event.error)
        
        // Network ve not-allowed hatası için bildirim göster
        if (event.error === 'network' || event.error === 'not-allowed') {
          this.addVoiceNotification('Sistem', 'Mikrofon erişimi reddedildi! Lütfen izinleri kontrol edin.')
          this.microphonePermission = 'denied'
          this.stopListening()
        }
        
        // Hata olsa bile tekrar başlat
        if (this.isListening && this.recognition && event.error !== 'no-speech' && event.error !== 'not-allowed' && event.error !== 'network') {
          setTimeout(() => {
            if (this.isListening && this.recognition) {
              try {
                this.recognition.start()
              } catch (e) {}
            }
          }, 100)
        }
      }

      this.recognition.start()
    }

    this.ws.onopen = async () => {
      try {
        this.mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true })
        // İzin verildiğinde durumu güncelle
        this.microphonePermission = 'granted'
        this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)()
        const source = this.audioContext.createMediaStreamSource(this.mediaStream)
        
        // Modern AnalyserNode kullan (ScriptProcessorNode yerine)
        this.analyser = this.audioContext.createAnalyser()
        this.analyser.fftSize = 2048
        this.analyser.smoothingTimeConstant = 0.8
        source.connect(this.analyser)

        // Sample rate gönder
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
          this.ws.send(JSON.stringify({ type: 'config', sr: this.audioContext.sampleRate }))
        }

        // Ses işleme - requestAnimationFrame ile modern yaklaşım
        const bufferLength = this.analyser.frequencyBinCount
        const dataArray = new Float32Array(bufferLength)
        
        const processAudio = () => {
          if (!this.isListening || !this.analyser || !this.audioContext) {
            return
          }
          
          // WebSocket durumunu kontrol et
          if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
            this.animationFrameId = requestAnimationFrame(processAudio)
            return
          }
          
          try {
            // Ses verilerini al
            this.analyser.getFloatTimeDomainData(dataArray)
            // WebSocket'e gönder
            this.ws.send(dataArray.buffer)
          } catch (err) {
            console.error('Audio data send error:', err)
            this.stopListening()
            return
          }
          
          // Bir sonraki frame için devam et
          this.animationFrameId = requestAnimationFrame(processAudio)
        }
        
        // Ses işlemeyi başlat
        this.animationFrameId = requestAnimationFrame(processAudio)
        this.isListening = true
      } catch (err: any) {
        console.error('Mikrofon erişimi hatası:', err)
        this.microphonePermission = 'denied'
        this.addVoiceNotification('Sistem', 'Mikrofon erişimi reddedildi! Lütfen izinleri kontrol edin.')
        this.stopListening()
      }
    }

    this.ws.onmessage = (evt) => {
      try {
        const msg = JSON.parse(evt.data)
        if (msg.type === 'result' && msg.best) {
          this.currentSpeaker = msg.best
        } else if (msg.type === 'error') {
          this.currentSpeaker = null
          console.error('WebSocket hatası:', msg.message)
        }
      } catch (e) {
        console.error('Mesaj işleme hatası:', e)
      }
    }

    this.ws.onclose = () => {
      console.log('WebSocket kapandı')
      this.isListening = false
    }

    this.ws.onerror = (err) => {
      console.error('WebSocket hatası:', err)
      this.stopListening()
    }
  }

  stopListening(): void {
    console.log('Dinleme durduruluyor...')
    
    // Animation frame'i iptal et
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId)
      this.animationFrameId = null
    }
    
    // Analyser'ı temizle
    if (this.analyser) {
      this.analyser.disconnect()
      this.analyser = null
    }
    
    // Audio context'i kapat
    if (this.audioContext) {
      this.audioContext.close()
      this.audioContext = null
    }
    
    // Media stream'i durdur
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop())
      this.mediaStream = null
    }
    
    // WebSocket'i kapat
    if (this.ws) {
      this.ws.close()
      this.ws = null
    }
    
    // Speech recognition'ı durdur
    if (this.recognition) {
      try {
        this.recognition.stop()
      } catch (e: any) {
        // Ignore - zaten durduruldu
      }
      this.recognition = null
    }
    
    this.isListening = false
    this.currentSpeaker = null
    console.log('Dinleme durduruldu.')
  }

  processVoiceCommand(text: string): void {
    console.log('Voice command received:', text)
    console.log('Current receipt mode:', this.isReceiptMode)
    console.log('Current receipt items:', this.receiptItems.length)
    
    // Komutları ayrıştır - virgül ve "ve" ile birleşik cümleleri parse et
    const commands = this.splitCommands(text)
    
    for (const cmd of commands) {
      // Fiş başlat komutları
      if (cmd.includes('başlat') || cmd.includes('baslat') || cmd.includes('fiş başlat') || cmd.includes('fis baslat')) {
        // ÖNEMLİ: Sadece tanınan kullanıcılar fiş başlatabilir
        if (!this.currentSpeaker) {
          console.log('✗ Konuşan kişi tanınmadı, fiş başlatılamaz')
          this.addVoiceNotification('Sistem', '⚠️ Konuşan kişi tanınmadı! Lütfen ses kaydınızı ekleyin.')
          continue
        }
        
        this.isReceiptMode = true
        this.receiptItems = []
        console.log('✓ Fiş modu başlatıldı')
        
        // Bildirim ekle
        this.addVoiceNotification(this.currentSpeaker, 'başlattı')
        
        continue
      }

      // Fiş yazdır komutları
      if (cmd.includes('yazdır') || cmd.includes('yazdir') || cmd.includes('fiş yazdır') || cmd.includes('fis yazdir')) {
        const now = Date.now()
        
        // Debounce: 2 saniye içinde tekrar yazdırma isteği varsa görmezden gel
        if (now - this.lastPrintTime < 2000) {
          console.log('⏭ Yazdırma isteği atlandı (çok yakın)')
          continue
        }
        
        console.log('Yazdır komutu alındı, receipt items:', this.receiptItems)
        
        // ÖNEMLİ: Sadece tanınan kullanıcılar fiş kesebilir
        if (!this.currentSpeaker) {
          console.log('✗ Konuşan kişi tanınmadı, fiş kesilemez')
          this.addVoiceNotification('Sistem', '⚠️ Konuşan kişi tanınmadı! Lütfen ses kaydınızı ekleyin.')
          continue
        }
        
        if (this.isReceiptMode && this.receiptItems.length > 0) {
          this.lastPrintTime = now
          
          // CRITICAL: Items'ı kopyala - çünkü printReceipt asenkron çalışacak
          const itemsToPrint = [...this.receiptItems]
          
          console.log('✓ Fiş yazdırılıyor...', itemsToPrint)
          
          // ASENKRON: Promise döndür - items'ı parametre olarak gönder
          this.printReceipt(itemsToPrint).catch(err => {
            console.error('✗ Yazdırma hatası:', err)
          })
          
          // Hemen reset et - yeni komutlar için hazır ol
          this.isReceiptMode = false
          this.receiptItems = []
          console.log('✓ Fiş kaydedildi, yeni komutlar hazır')
          
          // Bildirim ekle
          if (this.currentSpeaker) {
            this.addVoiceNotification(this.currentSpeaker, 'yazdırdı')
          }
        } else if (!this.isReceiptMode) {
          console.log('✗ Önce "başlat" komutu verilmeli')
        } else {
          console.log(`✗ Fişte ürün bulunmuyor (${this.receiptItems.length} ürün)`)
        }
        continue
      }

      // Fiş modu aktifse ürün ekleme - Sadece tanınan kullanıcılar ürün ekleyebilir
      if (this.isReceiptMode) {
        // ÖNEMLİ: Sadece tanınan kullanıcılar ürün ekleyebilir
        if (!this.currentSpeaker) {
          console.log('✗ Konuşan kişi tanınmadı, ürün eklenemez')
          this.addVoiceNotification('Sistem', '⚠️ Konuşan kişi tanınmadı! Lütfen ses kaydınızı ekleyin.')
          continue
        }
        
        if (cmd.trim().length > 5) {
          console.log('Parsing product from text:', cmd)
          this.parseProductFromVoice(cmd).catch(err => {
            console.error('Product parsing error:', err)
          })
        }
      }
    }
  }

  splitCommands(text: string): string[] {
    // 1. Önce "başlat", "yazdır" gibi ana komutları bul
    const commands: string[] = []
    let currentText = text
    
    // "başlat" komutunu bul ve çıkar
    const baslatIndex = currentText.search(/başlat|baslat|fiş başlat|fis baslat/i)
    if (baslatIndex >= 0) {
      commands.push('başlat')
      const offset = currentText.substring(baslatIndex).match(/başlat|baslat/)?.[0]?.length || 6
      currentText = currentText.substring(baslatIndex + offset).trim()
    }
    
    // "yazdır" komutunu bul ve çıkar
    const yazdirIndex = currentText.search(/yazdır|yazdir|fiş yazdır|fis yazdir/i)
    if (yazdirIndex >= 0) {
      const beforeYazdir = currentText.substring(0, yazdirIndex).trim()
      if (beforeYazdir) {
        commands.push(beforeYazdir)
      }
      commands.push('yazdır')
    } else if (currentText.trim()) {
      // Yazdır komutu yoksa, geri kalanı ürün olarak ekle
      commands.push(currentText.trim())
    }
    
    // Virgül ve "ve" ile ayrılmış olanları da işle
    const expanded: string[] = []
    for (const cmd of commands) {
      if (cmd.includes(',') || cmd.includes(' ve ')) {
        const parts = cmd.split(/,/).map(p => p.trim()).filter(p => p.length > 0)
        for (const part of parts) {
          if (part.includes(' ve ')) {
            expanded.push(...part.split(' ve ').map(p => p.trim()).filter(p => p.length > 0))
          } else if (part.length > 0) {
            expanded.push(part)
          }
        }
      } else if (cmd.length > 0) {
        expanded.push(cmd)
      }
    }
    
    return expanded
  }

  async parseProductFromVoice(text: string): Promise<void> {
    // Örnek: "3 adet patatesli poğaça" veya "yarım kilo cevizli baklava" veya "3 çikolatalı kruvasan"
    
    // Yazılı sayı eşleştirme fonksiyonu
    const wordToNumber = (word: string): number => {
      const map: { [key: string]: number } = {
        'bir': 1, 'iki': 2, 'üç': 3, 'dört': 4, 'beş': 5,
        'altı': 6, 'yedi': 7, 'sekiz': 8, 'dokuz': 9, 'on': 10,
        'onbir': 11, 'oniki': 12, 'onüç': 13, 'ondört': 14, 'onbeş': 15,
        'onaltı': 16, 'onyedi': 17, 'onsekiz': 18, 'ondokuz': 19, 'yirmi': 20
      }
      return map[word.toLowerCase()] || 0
    }
    
    const patterns = [
      // ÖNCELIK 1: "bir adet" gibi yazılı sayı + birim
      { regex: /(bir|iki|üç|dört|beş|altı|yedi|sekiz|dokuz|on|onbir|oniki|onüç|ondört|onbeş|onaltı|onyedi|onsekiz|ondokuz|yirmi)\s*(adet|adette|tane|tanes?)\s+(.+)/i, extract: (m: any) => ({ quantity: wordToNumber(m[1]), unit: 'adet', product: m[3].trim() }) },
      // ÖNCELIK 2: "yarım kilo" gibi özel durumlar
      { regex: /(yarım|yarim)\s*(kilo|kg)\s+(.+)/i, extract: (m: any) => ({ quantity: 0.5, unit: 'kg', product: m[3].trim() }) },
      // ÖNCELIK 3: "3 adet" gibi açık belirtilen durumlar
      { regex: /(\d+)\s*(adet|adette|tane|tanes?)\s+(.+)/i, extract: (m: any) => ({ quantity: parseInt(m[1]), unit: 'adet', product: m[3].trim() }) },
      // ÖNCELIK 4: "3 kilo" veya "2 kg" - KİLO önce kontrol edilmeli
      { regex: /(\d+\.?\d*)\s*(kilo|kg)\s+(.+)/i, extract: (m: any) => ({ quantity: parseFloat(m[1]), unit: 'kg', product: m[3].trim() }) },
      // FALLBACK: Sadece sayı var ama birim yok - otomatik "adet"
      { regex: /(\d+)\s+(.+)/i, extract: (m: any) => ({ quantity: parseInt(m[1]), unit: 'adet', product: m[2].trim() }) },
    ]

    console.log('Parsing text:', text)
    for (const pattern of patterns) {
      const match = text.match(pattern.regex)
      console.log('Pattern match attempt:', pattern.regex, match)
      if (match) {
        const item = pattern.extract(match)
        
        // Ürünün veritabanında olup olmadığını kontrol et
        const isValidProduct = await this.validateProduct(item.product)
        
        if (isValidProduct) {
          this.receiptItems.push(item)
          console.log(`✓ Ürün eklendi: ${item.quantity} ${item.unit} - ${item.product}`)
          this.addVoiceNotification(this.currentSpeaker || 'Sistem', `Ürün eklendi: ${item.product}`)
          return // Eşleşti, devam etme
        } else {
          console.log(`✗ Ürün bulunamadı: ${item.product}`)
          this.addVoiceNotification('Sistem', `Ürün bulunamadı: ${item.product}`)
          return // Geçersiz ürün
        }
      }
    }
    
    console.log('✗ Hiçbir pattern eşleşmedi:', text)
  }
  
  private async validateProduct(productName: string): Promise<boolean> {
    try {
      // Cache kontrolü
      const now = Date.now()
      if (this.productCache.length === 0 || (now - this.productCacheTime) > this.PRODUCT_CACHE_TTL) {
        console.log('Loading products from API...')
        const response = await fetch(`${environment.apiUrl}/products`, {
          headers: { 'Authorization': `Bearer ${this.authService.getToken()}` }
        })
        this.productCache = await response.json()
        this.productCacheTime = now
      }
      
      // Ürünü ara - sadece DB'deki tam isim varsa kabul et
      const searchName = productName.toLowerCase().trim()
      const found = this.productCache.find((p: any) => {
        const dbName = (p.urunAdi || p.UrunAdi || '').toLowerCase().trim()
        // Sadece tam eşleşme veya DB ismi aranan kelimeleri içeriyorsa geçerli
        return dbName === searchName || dbName.includes(searchName)
      })
      
      return !!found
    } catch (error) {
      console.error('Product validation error:', error)
      return false
    }
  }

  /**
   * Speaker ismini kullanıcı ID'sine çevir
   * Örnek: "Erdem Yalçınkaya" -> firstName: "Erdem", lastName: "Yalçınkaya" olan kullanıcının ID'si
   */
  private async getUserIdFromSpeakerName(speakerName: string): Promise<number | null> {
    try {
      // Cache kontrolü
      const now = Date.now()
      if (this.userCache.length === 0 || (now - this.userCacheTime) > this.USER_CACHE_TTL) {
        console.log('Loading users from API...')
        const response = await fetch(`${environment.apiUrl}/users`, {
          headers: { 'Authorization': `Bearer ${this.authService.getToken()}` }
        })
        if (!response.ok) {
          console.error('Failed to load users:', response.status)
          return null
        }
        this.userCache = await response.json()
        this.userCacheTime = now
      }
      
      // Speaker ismini parçala (örn: "Erdem Yalçınkaya" -> ["Erdem", "Yalçınkaya"])
      const nameParts = speakerName.trim().split(/\s+/)
      if (nameParts.length < 2) {
        // Tek kelime ise, firstName veya lastName ile eşleştirmeyi dene
        const searchName = speakerName.toLowerCase().trim()
        const found = this.userCache.find((u: any) => {
          const firstName = (u.firstName || '').toLowerCase().trim()
          const lastName = (u.lastName || '').toLowerCase().trim()
          return firstName === searchName || lastName === searchName
        })
        return found ? found.id : null
      }
      
      // İlk kelime firstName, geri kalanı lastName olarak kabul et
      const firstName = nameParts[0].trim()
      const lastName = nameParts.slice(1).join(' ').trim()
      
      // Kullanıcıyı bul - firstName ve lastName eşleşmesi
      const found = this.userCache.find((u: any) => {
        const uFirstName = (u.firstName || '').trim()
        const uLastName = (u.lastName || '').trim()
        // Tam eşleşme veya kısmi eşleşme (büyük/küçük harf duyarsız)
        return uFirstName.toLowerCase() === firstName.toLowerCase() && 
               uLastName.toLowerCase() === lastName.toLowerCase()
      })
      
      return found ? found.id : null
    } catch (error) {
      console.error('Speaker to user ID conversion error:', error)
      return null
    }
  }

  async printReceipt(items: any[]): Promise<void> {
    try {
      console.log('printReceipt çağrıldı, items:', items)
      
      // Notification: İşlem başlatıldı
      this.addVoiceNotification('Sistem', 'Fiş işleniyor...')
      
      // HIZLANDIRMA: Gateway üzerinden ürün fiyatlarını paralel çek
      this.addVoiceNotification('Sistem', 'Ürün bilgileri alınıyor...')
      const urunler = await fetch(`${environment.apiUrl}/products`, {
        headers: { 'Authorization': `Bearer ${this.authService.getToken()}` }
      }).then(r => r.json())
      
      // Receipt items'a fiyat ekle - parametreden gelen items kullan
      const enrichedItems = items.map((item: any) => {
        const matchedUrun = urunler.find((u: any) => 
          (u.urunAdi || u.UrunAdi || '').toLowerCase().includes(item.product?.toLowerCase() || '') ||
          (item.product?.toLowerCase() || '').includes((u.urunAdi || u.UrunAdi || '').toLowerCase())
        )
        return {
          Quantity: item.quantity,      // Backend büyük harf bekliyor
          Unit: item.unit,               // Backend büyük harf bekliyor
          Product: item.product,         // Backend büyük harf bekliyor
          Price: matchedUrun ? (matchedUrun.birimFiyat || matchedUrun.BirimFiyat || 0) : 0,  // Backend büyük harf bekliyor
          UrunId: matchedUrun ? (matchedUrun.id || matchedUrun.Id || 0) : 0
        }
      })

      console.log('Enriched items:', enrichedItems)
      
      // Get user info for TenantId and UserId
      const user = this.authService.getUser()
      
      // ÖNEMLİ: Sadece veritabanında tanımlı kullanıcılar fiş kesebilir
      if (!this.currentSpeaker) {
        throw new Error('Konuşan kişi tanınmadı! Fiş kesilemez.')
      }
      
      // Speaker'ın kullanıcı ID'sini bul
      const speakerUserId = await this.getUserIdFromSpeakerName(this.currentSpeaker)
      if (!speakerUserId) {
        throw new Error(`"${this.currentSpeaker}" veritabanında bulunamadı! Lütfen ses kaydınızı ekleyin.`)
      }
      
      console.log(`✓ Speaker "${this.currentSpeaker}" için kullanıcı ID: ${speakerUserId}`)
      this.addVoiceNotification('Sistem', `Fiş "${this.currentSpeaker}" adına kesiliyor`)
      
      const receiptData = { 
        Items: enrichedItems,
        TenantId: user?.tenantId || 1,  // Default to 1 if not set
        UserId: speakerUserId
      }

      // Notification: Fiş oluşturuluyor
      this.addVoiceNotification('Sistem', 'Fiş oluşturuluyor...')

      // Backend API'ye istek at - Gateway üzerinden
      const printResponse = await fetch(`${environment.apiUrl}/receipt/print`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.authService.getToken()}`
        },
        body: JSON.stringify(receiptData)
      })

      if (!printResponse.ok) {
        const errorText = await printResponse.text()
        console.error('Backend error:', printResponse.status, errorText)
        this.addVoiceNotification('Sistem', 'Fiş oluşturulamadı! Hata oluştu.')
        throw new Error(`Backend error: ${printResponse.status}`)
      }
      
      const data = await printResponse.json()
      
      if (data.ok) {
        const islemKodu = data.islem_kodu || 'Bilinmiyor'
        const toplamTutar = data.toplam_tutar || 0
        console.log(`✓ Fiş oluşturuldu: ${islemKodu} - Toplam: ${toplamTutar} TL`)
        
        // Notification: Başarılı
        this.addVoiceNotification('Sistem', `Fiş oluşturuldu: ${islemKodu} (${toplamTutar} TL)`)
        
        // Notification: PDF kaydedildi
        this.addVoiceNotification('Sistem', 'Fiş PDF olarak masaüstüne kaydedildi')
        
        // İşlemler listesini yenile
        this.transactionRefreshService.triggerRefresh()
        this.addVoiceNotification('Sistem', 'İşlemler listesi yenilendi')
      }
    } catch (err) {
      console.error('✗ Yazdırma hatası:', err)
      this.addVoiceNotification('Sistem', 'Fiş yazdırma hatası oluştu!')
    }
  }

  settingMenu() {
    this.settingsButtonClicked.emit()
  }

  /**
   * Toggle the menu bar when having mobile screen
   */
  toggleMobileMenu() {
    // document.getElementById('topnav-hamburger-icon')?.classList.toggle('open');
    this.mobileMenuButtonClicked.emit()
  }

  // Change Theme
  changeTheme() {
    const color = document.documentElement.getAttribute('data-bs-theme')
    console.log(color)
    if (color == 'light') {
      this.store.dispatch(changetheme({ color: 'dark' }))
    } else {
      this.store.dispatch(changetheme({ color: 'light' }))
    }
    this.store.select(getLayoutColor).subscribe((color) => {
      document.documentElement.setAttribute('data-bs-theme', color)
    })
  }

  open(content: TemplateRef<any>) {
    this.offcanvasService.open(content, {
      position: 'end',
      panelClass: 'border-0 width-auto',
    })
  }

  logout() {
    // NgRx logout action
    this.store.dispatch(logout())
    
    // AuthService logout - token'ları temizle ve login'e yönlendir
    this.authService.logout()
    
    // Router ile login sayfasına yönlendir
    this.router.navigate(['/auth/sign-in'])
  }

  getFileExtensionIcon(file: any) {
    const dotIndex = file.lastIndexOf('.')
    const extension = file.slice(dotIndex + 1)
    if (extension == 'docs') {
      return 'bxs-file-doc'
    } else if (extension == 'zip') {
      return 'bxs-file-archive'
    } else {
      return 'bxl-figma '
    }
  }

  getActivityIcon(type: string) {
    if (type == 'task') {
      return 'iconamoon:folder-check-duotone'
    } else if (type == 'design') {
      return 'iconamoon:check-circle-1-duotone'
    } else {
      return 'iconamoon:certificate-badge-duotone'
    }
  }
  
  addVoiceNotification(speaker: string, action: string) {
    const notification = {
      from: speaker,
      content: `${action}`,
      icon: '',
      timestamp: new Date(),
      type: 'voice'
    }
    
    // Add to beginning
    this.voiceNotifications.unshift(notification)
    
    // Keep only max notifications
    if (this.voiceNotifications.length > this.maxNotifications) {
      this.voiceNotifications = this.voiceNotifications.slice(0, this.maxNotifications)
    }
  }
}
