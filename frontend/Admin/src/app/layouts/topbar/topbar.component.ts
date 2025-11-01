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
import { Router } from '@angular/router'
import { activityStreamData, appsData, notificationsData } from './data'
import { AuthService } from '@/app/services/auth.service'
import { TransactionRefreshService } from '@/app/services/transaction-refresh.service'

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [
    NgbDropdownModule,
    SimplebarAngularModule,
    NgbOffcanvasModule,
    CommonModule,
    NgbTooltipModule,
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

  notificationList = notificationsData
  appsList = appsData
  activityList = activityStreamData

  // Voice Recognition
  isListening = false
  currentSpeaker: string | null = null
  ws: WebSocket | null = null
  mediaStream: MediaStream | null = null
  audioContext: AudioContext | null = null
  processor: ScriptProcessorNode | null = null
  recognition: any = null
  recognizedText: string = ''
  isReceiptMode = false  // "fiş başlat" denince true olur
  receiptItems: any[] = []  // Fiş itemları
  lastPrintTime = 0  // Debounce için son yazdırma zamanı
  
  // Dynamic notifications
  voiceNotifications: any[] = []
  private maxNotifications = 5  // Max 5 bildirim göster

  constructor(@Inject(DOCUMENT) private document: any) {}
  @Output() settingsButtonClicked = new EventEmitter()
  @Output() mobileMenuButtonClicked = new EventEmitter()

  ngOnInit(): void {
    this.element = document.documentElement
  }

  ngOnDestroy(): void {
    this.stopListening()
  }

  toggleListening(): void {
    if (this.isListening) {
      this.stopListening()
    } else {
      this.startListening()
    }
  }

  startListening(): void {
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
        // Hata olsa bile tekrar başlat
        if (this.isListening && this.recognition && event.error !== 'no-speech') {
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
        this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)()
        const source = this.audioContext.createMediaStreamSource(this.mediaStream)
        this.processor = this.audioContext.createScriptProcessor(2048, 1, 1)

        // Sample rate gönder
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
          this.ws.send(JSON.stringify({ type: 'config', sr: this.audioContext.sampleRate }))
        }

        // Ses işleme
        this.processor.onaudioprocess = (e) => {
          // WebSocket durumunu kontrol et
          if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
            return; // WebSocket kapalıysa veri gönderme
          }
          
          try {
            const input = e.inputBuffer.getChannelData(0)
            const buf = new Float32Array(input.length)
            buf.set(input)
            this.ws.send(buf.buffer)
          } catch (err) {
            console.error('Audio data send error:', err)
            this.stopListening()
          }
        }

        source.connect(this.processor)
        this.processor.connect(this.audioContext.destination)
        this.isListening = true
      } catch (err) {
        console.error('Mikrofon erişimi hatası:', err)
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
    if (this.processor) {
      this.processor.disconnect()
      this.processor = null
    }
    if (this.audioContext) {
      this.audioContext.close()
      this.audioContext = null
    }
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop())
      this.mediaStream = null
    }
    if (this.ws) {
      this.ws.close()
      this.ws = null
    }
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
        this.isReceiptMode = true
        this.receiptItems = []
        console.log('✓ Fiş modu başlatıldı')
        
        // Bildirim ekle
        if (this.currentSpeaker) {
          this.addVoiceNotification(this.currentSpeaker, 'başlattı')
        }
        
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

      // Fiş modu aktifse ürün ekleme
      if (this.isReceiptMode) {
        if (cmd.trim().length > 5) {
          console.log('Parsing product from text:', cmd)
          this.parseProductFromVoice(cmd)
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

  parseProductFromVoice(text: string): void {
    // Örnek: "3 adet patatesli poğaça" veya "yarım kilo cevizli baklava" veya "3 çikolatalı kruvasan"
    const patterns = [
      // ÖNCELIK 1: "yarım kilo" gibi özel durumlar
      { regex: /(yarım|yarim)\s*(kilo|kg)\s+(.+)/i, extract: (m: any) => ({ quantity: 0.5, unit: 'kg', product: m[3].trim() }) },
      // ÖNCELIK 2: "3 adet" gibi açık belirtilen durumlar
      { regex: /(\d+)\s*(adet|adette|tane|tanes?)\s+(.+)/i, extract: (m: any) => ({ quantity: parseInt(m[1]), unit: 'adet', product: m[3].trim() }) },
      // ÖNCELIK 3: "3 kilo" veya "2 kg" - KİLO önce kontrol edilmeli
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
        this.receiptItems.push(item)
        console.log(`✓ Ürün eklendi: ${item.quantity} ${item.unit} - ${item.product}`)
        return // Eşleşti, devam etme
      }
    }
    
    console.log('✗ Hiçbir pattern eşleşmedi:', text)
  }

  async printReceipt(items: any[]): Promise<void> {
    try {
      console.log('printReceipt çağrıldı, items:', items)
      
      // Notification: İşlem başlatıldı
      this.addVoiceNotification('Sistem', 'Fiş işleniyor...')
      
      // HIZLANDIRMA: Gateway üzerinden ürün fiyatlarını paralel çek
      this.addVoiceNotification('Sistem', 'Ürün bilgileri alınıyor...')
      const urunler = await fetch('http://localhost:5000/api/v1/products', {
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
      const receiptData = { 
        Items: enrichedItems,
        TenantId: user?.tenantId || 1,  // Default to 1 if not set
        UserId: user?.id || 1
      }

      // Notification: Fiş oluşturuluyor
      this.addVoiceNotification('Sistem', 'Fiş oluşturuluyor...')

      // Backend API'ye istek at - Gateway üzerinden
      const printResponse = await fetch('http://localhost:5000/api/v1/receipt/print', {
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
