import { Component, OnInit, OnDestroy, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../services/auth.service';
import { environment } from '../../../environments/environment';

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
  private apiUrl = environment.voiceServiceUrl;
  
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
  identifyTop: [string, number][] = [];
  isIdentifying: boolean = false;
  
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
  private mediaStream: MediaStream | null = null;
  private liveMediaRecorder: MediaRecorder | null = null;
  private audioChunks: Blob[] = [];

  // Permission
  canEnrollSpeakers: boolean = false;

  // Audio Recording
  isRecording: boolean = false;
  recordCount: number = 0;
  recordedChunks: Blob[] = [];
  currentRecordingTime: number = 0;
  private recordingMediaRecorder: MediaRecorder | null = null;
  private recordingStream: MediaStream | null = null;
  private recordingTimer: any = null;
  private recordingTimeout: any = null;

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
    this.stopRecording();
    this.cleanupRecordingResources();
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
    // Manuel dosya seçildiğinde kayıtları temizle
    if (this.enrollFile) {
      this.recordedChunks = [];
      this.recordCount = 0;
    }
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

  async startRecording() {
    if (this.recordCount >= 8) {
      this.enrollStatus = '🎉 Maksimum 8 kayıt yapabilirsiniz!';
      this.enrollStatusType = 'success';
      return;
    }

    if (!this.enrollName) {
      this.enrollStatus = '⚠️ Önce kişi seçin veya isim girin.';
      this.enrollStatusType = 'warning';
      return;
    }

    try {
      this.recordingStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      
      // MediaRecorder için uygun mimeType bul
      let mimeType = 'audio/webm';
      if (MediaRecorder.isTypeSupported('audio/webm;codecs=opus')) {
        mimeType = 'audio/webm;codecs=opus';
      } else if (MediaRecorder.isTypeSupported('audio/ogg;codecs=opus')) {
        mimeType = 'audio/ogg;codecs=opus';
      } else if (MediaRecorder.isTypeSupported('audio/mp4')) {
        mimeType = 'audio/mp4';
      }

      this.recordingMediaRecorder = new MediaRecorder(this.recordingStream, { mimeType });
      const chunks: Blob[] = [];

      this.recordingMediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0) {
          chunks.push(event.data);
        }
      };

      this.recordingMediaRecorder.onstop = () => {
        if (chunks.length > 0) {
          const blob = new Blob(chunks, { type: mimeType });
          this.recordedChunks.push(blob);
          this.recordCount++;
          
          // MP3 formatına dönüştür (blob'u File olarak kaydet)
          const fileName = `recording_${this.recordCount}_${Date.now()}.${mimeType.includes('webm') ? 'webm' : mimeType.includes('ogg') ? 'ogg' : 'mp4'}`;
          const file = new File([blob], fileName, { type: mimeType });
          
          // 8 kayda ulaşıldıysa bildir
          if (this.recordCount >= 8) {
            this.enrollStatus = '🎉 Hazır! 8 kayıt tamamlandı.';
            this.enrollStatusType = 'success';
          }
        }
        this.cleanupRecordingResources();
      };

      this.recordingMediaRecorder.start();
      this.isRecording = true;
      this.currentRecordingTime = 0;

      // 10 saniye geri sayım
      this.recordingTimer = setInterval(() => {
        this.currentRecordingTime++;
        if (this.currentRecordingTime >= 10) {
          this.stopRecording();
        }
      }, 1000);

      // Güvenlik için 11 saniye sonra otomatik durdur
      this.recordingTimeout = setTimeout(() => {
        if (this.isRecording) {
          this.stopRecording();
        }
      }, 11000);

    } catch (error: any) {
      console.error('Mikrofon hatası:', error);
      this.enrollStatus = '❌ Mikrofon erişimi reddedildi. Lütfen izin verin.';
      this.enrollStatusType = 'danger';
      this.cleanupRecordingResources();
    }
  }

  stopRecording() {
    if (this.recordingMediaRecorder && this.isRecording) {
      this.recordingMediaRecorder.stop();
      this.isRecording = false;
      this.currentRecordingTime = 0;
      
      if (this.recordingTimer) {
        clearInterval(this.recordingTimer);
        this.recordingTimer = null;
      }
      if (this.recordingTimeout) {
        clearTimeout(this.recordingTimeout);
        this.recordingTimeout = null;
      }
    }
  }

  resetRecordings() {
    this.stopRecording();
    this.recordedChunks = [];
    this.recordCount = 0;
    this.currentRecordingTime = 0;
    this.enrollStatus = '';
  }

  removeRecording(index: number) {
    if (index >= 0 && index < this.recordedChunks.length) {
      this.recordedChunks.splice(index, 1);
      this.recordCount--;
    }
  }

  private cleanupRecordingResources() {
    if (this.recordingStream) {
      this.recordingStream.getTracks().forEach(track => track.stop());
      this.recordingStream = null;
    }
    this.recordingMediaRecorder = null;
    this.isRecording = false;
  }

  private async combineRecordings(): Promise<File> {
    if (this.recordedChunks.length === 0) {
      throw new Error('Kayıt bulunamadı');
    }

    // AudioContext kullanarak kayıtları doğru şekilde birleştir
    const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    const audioBuffers: AudioBuffer[] = [];

    // Her blob'u AudioBuffer'a dönüştür
    for (const chunk of this.recordedChunks) {
      try {
        const arrayBuffer = await chunk.arrayBuffer();
        const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);
        audioBuffers.push(audioBuffer);
      } catch (error) {
        console.error('Error decoding audio chunk:', error);
        throw new Error('Kayıtlar decode edilemedi');
      }
    }

    // Tüm audio buffer'ları birleştir
    const totalLength = audioBuffers.reduce((sum, buffer) => sum + buffer.length, 0);
    const sampleRate = audioBuffers[0].sampleRate;
    const numberOfChannels = audioBuffers[0].numberOfChannels;
    
    const combinedBuffer = audioContext.createBuffer(numberOfChannels, totalLength, sampleRate);
    
    for (let channel = 0; channel < numberOfChannels; channel++) {
      let offset = 0;
      for (const buffer of audioBuffers) {
        combinedBuffer.getChannelData(channel).set(buffer.getChannelData(channel), offset);
        offset += buffer.length;
      }
    }

    // AudioBuffer'ı WAV formatına dönüştür
    const wavBlob = this.audioBufferToWav(combinedBuffer);
    const fileName = `combined_recording_${Date.now()}.wav`;
    return new File([wavBlob], fileName, { type: 'audio/wav' });
  }

  private audioBufferToWav(buffer: AudioBuffer): Blob {
    const length = buffer.length;
    const numberOfChannels = buffer.numberOfChannels;
    const sampleRate = buffer.sampleRate;
    const bytesPerSample = 2;
    const blockAlign = numberOfChannels * bytesPerSample;
    const byteRate = sampleRate * blockAlign;
    const dataSize = length * blockAlign;
    const bufferSize = 44 + dataSize;
    
    const arrayBuffer = new ArrayBuffer(bufferSize);
    const view = new DataView(arrayBuffer);
    
    // WAV header
    const writeString = (offset: number, string: string) => {
      for (let i = 0; i < string.length; i++) {
        view.setUint8(offset + i, string.charCodeAt(i));
      }
    };
    
    writeString(0, 'RIFF');
    view.setUint32(4, bufferSize - 8, true);
    writeString(8, 'WAVE');
    writeString(12, 'fmt ');
    view.setUint32(16, 16, true); // fmt chunk size
    view.setUint16(20, 1, true); // audio format (PCM)
    view.setUint16(22, numberOfChannels, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, byteRate, true);
    view.setUint16(32, blockAlign, true);
    view.setUint16(34, 16, true); // bits per sample
    writeString(36, 'data');
    view.setUint32(40, dataSize, true);
    
    // Audio data
    let offset = 44;
    for (let i = 0; i < length; i++) {
      for (let channel = 0; channel < numberOfChannels; channel++) {
        const sample = Math.max(-1, Math.min(1, buffer.getChannelData(channel)[i]));
        view.setInt16(offset, sample < 0 ? sample * 0x8000 : sample * 0x7FFF, true);
        offset += 2;
      }
    }
    
    return new Blob([arrayBuffer], { type: 'audio/wav' });
  }

  enrollSpeaker(): void {
    // Yetki kontrolü
    if (!this.canEnrollSpeakers) {
      this.enrollStatus = '⚠️ Yetkiniz yok! Sadece Admin rolündeki kullanıcılar ses kaydı ekleyebilir.';
      this.enrollStatusType = 'warning';
      return;
    }

    if (!this.enrollName) {
      this.enrollStatus = '⚠️ Kişi adı gerekli.';
      this.enrollStatusType = 'warning';
      return;
    }

    // Kayıt kontrolü
    if (!this.enrollFile && this.recordedChunks.length < 5) {
      this.enrollStatus = '⚠️ En az 5 kayıt yapmalısınız (şu an: ' + this.recordedChunks.length + ').';
      this.enrollStatusType = 'warning';
      return;
    }

    try {
      const formData = new FormData();
      formData.append('name', this.enrollName);
      if (this.enrollUserId) {
        formData.append('userId', this.enrollUserId.toString());
      }

      // Her kaydı ayrı ayrı dosya olarak gönder (daha güvenilir)
      if (this.recordedChunks.length > 0) {
        // Her kaydı ayrı dosya olarak ekle
        // Backend hem 'file' hem de 'files[]' destekliyor, 'file' kullanıyoruz
        for (let i = 0; i < this.recordedChunks.length; i++) {
          const chunk = this.recordedChunks[i];
          // Her chunk'ı File olarak oluştur
          const fileName = `recording_${i + 1}.webm`;
          const file = new File([chunk], fileName, { type: chunk.type || 'audio/webm' });
          // 'file' olarak gönder, backend birden fazla 'file' field'ını destekliyor
          formData.append('file', file);
        }
        this.enrollStatus = `⏳ ${this.recordedChunks.length} kayıt yükleniyor (her biri ayrı analiz edilecek)...`;
      } else if (this.enrollFile) {
        // Manuel dosya varsa onu gönder
        formData.append('file', this.enrollFile);
        this.enrollStatus = '⏳ Yükleniyor...';
      } else {
        this.enrollStatus = '⚠️ Ses dosyası veya kayıt gerekli.';
        this.enrollStatusType = 'warning';
        return;
      }

      this.enrollStatusType = 'info';

      this.http.post<{ok: boolean, message: string}>(`${this.apiUrl}/enroll`, formData)
        .subscribe({
          next: (data) => {
            this.enrollStatus = '✅ ' + (data.message || 'Kayıt başarılı!');
            this.enrollStatusType = 'success';
            this.enrollName = '';
            this.enrollUserId = null;
            this.enrollFile = null;
            this.resetRecordings();
            this.loadSpeakersCount();
          },
          error: (err) => {
            console.error('Enroll error:', err);
            this.enrollStatus = '❌ Hata: ' + (err.error?.message || err.message || 'Bilinmeyen hata');
            this.enrollStatusType = 'danger';
          }
        });
    } catch (error: any) {
      console.error('Enroll exception:', error);
      this.enrollStatus = '❌ Hata: ' + (error?.message || 'Beklenmeyen hata oluştu');
      this.enrollStatusType = 'danger';
    }
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

    // Kayıtlı konuşmacı kontrolü
    if (this.enrolledSpeakers.length === 0) {
      this.identifyResult = '⚠️ Sistemde kayıtlı konuşmacı bulunmuyor. Önce kişi kaydı yapın.';
      this.identifyResultType = 'warning';
      return;
    }

    const formData = new FormData();
    formData.append('file', this.identifyFile);

    this.identifyResult = '⏳ Ses dosyası analiz ediliyor...<br><small>Sistemdeki ' + this.enrolledSpeakers.length + ' kayıtlı konuşmacı ile karşılaştırılıyor...</small>';
    this.identifyResultType = 'info';
    this.isIdentifying = true;
    this.identifyTop = [];

    this.http.post<IdentifyResult>(`${this.apiUrl}/identify`, formData)
      .subscribe({
        next: (data) => {
          this.isIdentifying = false;
          
          // Top listesini kaydet (her zaman göster)
          if (data.top && data.top.length > 0) {
            this.identifyTop = data.top;
            
            if (data.best) {
              this.identifyResult = `<strong>🎯 Tanımlanan Konuşmacı: ${data.best}</strong><br>Benzerlik Skoru: ${(data.score * 100).toFixed(1)}%`;
              this.identifyResultType = 'success';
            } else if (data.top.length > 0 && data.top[0][1] > 0) {
              // En yüksek skorlu konuşmacıyı göster (eşik altında olsa bile)
              const topMatch = data.top[0];
              this.identifyResult = `⚠️ En Yüksek Benzerlik: <strong>${topMatch[0]}</strong><br>Benzerlik Skoru: ${(topMatch[1] * 100).toFixed(1)}% (Eşik değeri: ${(0.5 * 100).toFixed(0)}%)<br><small>Eşik değerin altında olduğu için kesin tanımlama yapılamadı.</small>`;
              this.identifyResultType = 'warning';
            } else {
              this.identifyResult = `❓ Bilinmeyen konuşmacı<br>Hiçbir kayıtlı konuşmacı ile eşleşme bulunamadı.`;
              this.identifyResultType = 'warning';
            }
          } else {
            this.identifyResult = `❓ Sonuç bulunamadı<br>Sistemde kayıtlı konuşmacı yok veya dosya işlenemedi.`;
            this.identifyResultType = 'danger';
            this.identifyTop = [];
          }
          
          this.identifyFile = null;
        },
        error: (err) => {
          this.isIdentifying = false;
          console.error('Identify error:', err);
          
          let errorMessage = 'Bilinmeyen hata';
          if (err.error?.message) {
            errorMessage = err.error.message;
          } else if (err.message) {
            errorMessage = err.message;
          } else if (err.status === 0) {
            errorMessage = 'Backend servisine bağlanılamıyor. Voice service çalışıyor mu?';
          } else if (err.status === 500) {
            errorMessage = 'Sunucu hatası: ' + (err.error?.message || 'Backend loglarını kontrol edin');
          }
          
          this.identifyResult = `❌ Hata: ${errorMessage}<br><small>HTTP Status: ${err.status || 'N/A'}</small>`;
          this.identifyResultType = 'danger';
          this.identifyTop = [];
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
      
      // MediaRecorder için uygun mimeType bul
      let mimeType = 'audio/webm';
      if (MediaRecorder.isTypeSupported('audio/webm;codecs=opus')) {
        mimeType = 'audio/webm;codecs=opus';
      } else if (MediaRecorder.isTypeSupported('audio/ogg;codecs=opus')) {
        mimeType = 'audio/ogg;codecs=opus';
      } else if (MediaRecorder.isTypeSupported('audio/mp4')) {
        mimeType = 'audio/mp4';
      }

      this.liveMediaRecorder = new MediaRecorder(this.mediaStream, { mimeType });
      this.audioChunks = [];

      // Audio data geldiğinde WebSocket'e gönder
      this.liveMediaRecorder.ondataavailable = (event) => {
        if (event.data.size > 0 && this.ws && this.ws.readyState === WebSocket.OPEN) {
          try {
            // Blob'u ArrayBuffer'a dönüştür ve gönder
            event.data.arrayBuffer()
              .then(buffer => {
                if (this.ws && this.ws.readyState === WebSocket.OPEN) {
                  try {
                    this.ws.send(buffer);
                  } catch (sendErr) {
                    console.error('Error sending buffer to WebSocket:', sendErr);
                  }
                }
              })
              .catch(err => {
                // Promise rejection'ı sessizce yakala (browser extension hatalarını önlemek için)
                if (err?.message && !err.message.includes('message channel closed')) {
                  console.error('Error converting blob to buffer:', err);
                }
              });
          } catch (err) {
            // Genel hataları yakala
            if (err instanceof Error && !err.message.includes('message channel closed')) {
              console.error('Error sending audio data:', err);
            }
          }
        }
      };

      const wsProto = location.protocol === 'https:' ? 'wss' : 'ws';
      this.ws = new WebSocket(`${wsProto}://localhost:8766`);

      this.ws.onopen = () => {
        console.log('WebSocket connected');
        this.liveIndicator = '🎤 Dinleniyor';
        this.isLiveActive = true;
        
        // Audio context'i sample rate için kullan
        this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
        
        if (this.ws && this.ws.readyState === WebSocket.OPEN) {
          this.ws.send(JSON.stringify({ type: 'config', sr: this.audioContext.sampleRate }));
        }

        // MediaRecorder'ı başlat (her 500ms'de bir chunk gönder)
        if (this.liveMediaRecorder && this.liveMediaRecorder.state === 'inactive') {
          this.liveMediaRecorder.start(500); // Her 500ms'de bir data event
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
    if (this.liveMediaRecorder && this.liveMediaRecorder.state !== 'inactive') {
      try {
        this.liveMediaRecorder.stop();
      } catch (e) {
        console.error('Error stopping MediaRecorder:', e);
      }
      this.liveMediaRecorder = null;
    }
    if (this.audioContext) {
      try {
        this.audioContext.close();
      } catch (e) {
        console.error('Error closing AudioContext:', e);
      }
      this.audioContext = null;
    }
    if (this.mediaStream) {
      this.mediaStream.getTracks().forEach(track => track.stop());
      this.mediaStream = null;
    }
    this.audioChunks = [];
  }

  stopLive() {
    console.log('Stopping live recognition...');
    
    // Önce aktif durumu kapat (startAudioStreaming'in durması için)
    this.isLiveActive = false;
    
    // WebSocket'i kapat
    if (this.ws) {
      if (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING) {
        this.ws.close();
      }
      this.ws = null;
    }

    // Audio kaynaklarını temizle
    this.cleanupAudioResources();

    this.liveIndicator = 'Hazır';
    this.liveSpeaker = '—';
    this.liveScore = '';
    this.liveTop = [];
  }
}

