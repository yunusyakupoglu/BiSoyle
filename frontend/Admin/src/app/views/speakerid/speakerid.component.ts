import { Component, OnInit, OnDestroy, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';

interface IdentifyResult {
  ok: boolean;
  best: string | null;
  score: number;
  top: [string, number][];
}

@Component({
  selector: 'app-speakerid',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './speakerid.component.html',
  styleUrls: ['./speakerid.component.scss']
})
export class SpeakerIDComponent implements OnInit, OnDestroy {
  private apiUrl = 'http://localhost:8765';
  
  // Enroll
  enrollName: string = '';
  enrollUserId: number | null = null;
  enrollFile: File | null = null;
  enrollStatus: string = '';
  enrollStatusType: 'success' | 'danger' | 'warning' | 'info' = 'info';
  
  // Users dropdown
  allUsers: any[] = [];
  filteredUsers: any[] = [];
  userSearchText: string = '';
  showUserDropdown: boolean = false;
  
  // Identify
  identifyFile: File | null = null;
  identifyResult: string = '';
  identifyResultType: 'success' | 'danger' | 'warning' | 'info' = 'info';
  
  // Live
  isLiveActive: boolean = false;
  liveIndicator: string = 'Hazır';
  liveSpeaker: string = '—';
  liveScore: string = '';
  liveTop: [string, number][] = [];
  
  // Stats
  totalSpeakers: number = 0;
  enrolledSpeakers: string[] = [];  // List of enrolled speaker names
  
  // WebSocket
  private ws: WebSocket | null = null;
  private audioContext: AudioContext | null = null;
  private processor: ScriptProcessorNode | null = null;
  private mediaStream: MediaStream | null = null;

  // Permission
  canEnrollSpeakers: boolean = false;

  constructor(private http: HttpClient, private authService: AuthService) {}

  ngOnInit() {
    this.loadSpeakersCount();
    this.loadUsers();
    this.checkPermissions();
  }

  checkPermissions() {
    const user = this.authService.getUser();
    if (user && user.roles) {
      // Sadece Admin rolü ekleyebilir
      this.canEnrollSpeakers = user.roles.includes('Admin') || user.roles.includes('SuperAdmin');
    }
  }

  ngOnDestroy() {
    this.stopLive();
  }

  loadSpeakersCount() {
    this.http.get<{ok: boolean, speakers: string[]}>(`${this.apiUrl}/api/speakers/list`)
      .subscribe({
        next: (data) => {
          this.totalSpeakers = data.speakers?.length || 0;
          this.enrolledSpeakers = data.speakers || [];
        },
        error: (err) => console.error('Failed to load speakers:', err)
      });
  }

  loadUsers() {
    this.http.get<any[]>('http://localhost:5004/api/users')
      .subscribe({
        next: (users) => {
          this.allUsers = users;
        },
        error: (err) => {
          console.error('Failed to load users:', err);
          this.enrollStatus = '⚠️ Kullanıcılar yüklenemedi. Manuel isim girebilirsiniz.';
          this.enrollStatusType = 'warning';
        }
      });
  }

  onEnrollFileChange(event: any) {
    this.enrollFile = event.target.files[0] || null;
  }

  searchUsers(searchText: string) {
    this.userSearchText = searchText;
    
    if (searchText.length < 3) {
      this.filteredUsers = [];
      this.showUserDropdown = false;
      return;
    }

    this.filteredUsers = this.allUsers.filter(user => {
      const fullName = `${user.firstName} ${user.lastName}`.toLowerCase();
      const username = (user.username || '').toLowerCase();
      const email = (user.email || '').toLowerCase();
      const search = searchText.toLowerCase();
      
      return fullName.includes(search) || username.includes(search) || email.includes(search);
    });

    this.showUserDropdown = this.filteredUsers.length > 0;
  }

  selectUser(user: any) {
    this.enrollUserId = user.id;
    this.enrollName = `${user.firstName} ${user.lastName}`;
    this.userSearchText = '';
    this.filteredUsers = [];
    this.showUserDropdown = false;
  }

  enrollSpeaker() {
    // Yetki kontrolü
    if (!this.canEnrollSpeakers) {
      this.enrollStatus = '⚠️ Yetkiniz yok! Sadece Admin rolündeki kullanıcılar ses kaydı ekleyebilir.';
      this.enrollStatusType = 'warning';
      return;
    }

    if (!this.enrollName || !this.enrollFile) {
      this.enrollStatus = '⚠️ Kişi ve ses dosyası gerekli.';
      this.enrollStatusType = 'warning';
      return;
    }

    const formData = new FormData();
    formData.append('name', this.enrollName);
    if (this.enrollUserId) {
      formData.append('userId', this.enrollUserId.toString());
    }
    formData.append('file', this.enrollFile);

    this.enrollStatus = '⏳ Yükleniyor...';
    this.enrollStatusType = 'info';

    this.http.post<{ok: boolean, message: string}>(`${this.apiUrl}/enroll`, formData)
      .subscribe({
        next: (data) => {
          this.enrollStatus = '✅ ' + (data.message || 'Kayıt başarılı!');
          this.enrollStatusType = 'success';
          this.enrollName = '';
          this.enrollUserId = null;
          this.enrollFile = null;
          this.loadSpeakersCount();
        },
        error: (err) => {
          this.enrollStatus = '❌ Hata: ' + err.message;
          this.enrollStatusType = 'danger';
        }
      });
  }

  onIdentifyFileChange(event: any) {
    this.identifyFile = event.target.files[0] || null;
  }

  identifySpeaker() {
    if (!this.identifyFile) {
      this.identifyResult = '⚠️ Ses dosyası seçiniz.';
      this.identifyResultType = 'warning';
      return;
    }

    const formData = new FormData();
    formData.append('file', this.identifyFile);

    this.identifyResult = '⏳ Analiz ediliyor...';
    this.identifyResultType = 'info';

    this.http.post<IdentifyResult>(`${this.apiUrl}/identify`, formData)
      .subscribe({
        next: (data) => {
          if (data.best) {
            this.identifyResult = `<strong>🎯 Tahmin: ${data.best}</strong><br>Benzerlik: ${(data.score * 100).toFixed(1)}%`;
            this.identifyResultType = 'success';
          } else {
            this.identifyResult = `❓ Bilinmeyen konuşmacı (skor: ${data.score.toFixed(3)})`;
            this.identifyResultType = 'warning';
          }
          this.identifyFile = null;
        },
        error: (err) => {
          this.identifyResult = '❌ Hata: ' + err.message;
          this.identifyResultType = 'danger';
        }
      });
  }

  async startLive() {
    if (this.ws && this.ws.readyState === WebSocket.OPEN) {
      console.log('WebSocket already open');
      return;
    }

    // Eğer eski bağlantı varsa temizle
    if (this.ws) {
      this.stopLive();
      await new Promise(resolve => setTimeout(resolve, 500)); // Temizlik için bekle
    }

    this.liveIndicator = 'Başlatılıyor...';

    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      const source = this.audioContext.createMediaStreamSource(this.mediaStream);
      this.processor = this.audioContext.createScriptProcessor(2048, 1, 1);

      const wsProto = location.protocol === 'https:' ? 'wss' : 'ws';
      this.ws = new WebSocket(`${wsProto}://localhost:8766`);

      this.ws.onopen = () => {
        console.log('WebSocket connected');
        this.liveIndicator = '🎤 Dinleniyor';
        this.isLiveActive = true;
        
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
          this.ws.send(JSON.stringify({ type: 'config', sr: this.audioContext?.sampleRate }));
        }

        if (this.processor && this.audioContext) {
          this.processor.onaudioprocess = (e) => {
            // WebSocket durumunu kontrol et
            if (!this.ws || this.ws.readyState !== WebSocket.OPEN) {
              console.warn('WebSocket not open, skipping audio data');
              return;
            }
            
            try {
              const input = e.inputBuffer.getChannelData(0);
              const buf = new Float32Array(input.length);
              buf.set(input);
              this.ws.send(buf.buffer);
            } catch (err) {
              console.error('Error sending audio data:', err);
              this.stopLive(); // Hata durumunda bağlantıyı kapat
            }
          };
          source.connect(this.processor);
          this.processor.connect(this.audioContext.destination);
        }
      };

      this.ws.onmessage = (evt) => {
        try {
          const msg = JSON.parse(evt.data);
          if (msg.type === 'result') {
            this.liveSpeaker = msg.best ? msg.best + ' konuşuyor' : 'Bilinmeyen';
            this.liveScore = msg.best ? `Güven: ${(msg.score * 100).toFixed(1)}%` : '';
            this.liveTop = msg.top || [];
          } else if (msg.type === 'error') {
            this.liveSpeaker = msg.message.includes('no_enrollments') ? 
              '⚠️ Henüz kayıtlı kişi yok' : '❌ Hata: ' + msg.message;
            this.liveScore = '';
          }
        } catch (e) {
          console.error('WS message error:', e);
        }
      };

      this.ws.onclose = () => {
        console.log('WebSocket closed');
        this.liveIndicator = 'Hazır';
        this.isLiveActive = false;
        this.cleanupAudioResources();
      };

      this.ws.onerror = (error) => {
        console.error('WebSocket error:', error);
        this.liveIndicator = '❌ Bağlantı hatası';
        this.isLiveActive = false;
        this.stopLive();
      };

    } catch (e) {
      console.error('Mikrofon hatası:', e);
      this.liveIndicator = '❌ Mikrofon hatası';
      this.isLiveActive = false;
      this.cleanupAudioResources();
    }
  }

  private cleanupAudioResources() {
    if (this.processor) {
      this.processor.disconnect();
      this.processor.onaudioprocess = null;
      this.processor = null;
    }
    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
      this.mediaStream = null;
    }
  }

  stopLive() {
    console.log('Stopping live recognition...');
    
    // WebSocket'i kapat
    if (this.ws) {
      if (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING) {
        this.ws.close();
      }
      this.ws = null;
    }

    // Audio kaynaklarını temizle
    this.cleanupAudioResources();

    this.isLiveActive = false;
    this.liveIndicator = 'Hazır';
    this.liveSpeaker = '—';
    this.liveScore = '';
    this.liveTop = [];
  }
}

