import { Component, OnInit, OnDestroy, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

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
  private apiUrl = 'http://localhost:8000';
  
  // Enroll
  enrollName: string = '';
  enrollFile: File | null = null;
  enrollStatus: string = '';
  enrollStatusType: 'success' | 'danger' | 'warning' | 'info' = 'info';
  
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
  
  // WebSocket
  private ws: WebSocket | null = null;
  private audioContext: AudioContext | null = null;
  private processor: ScriptProcessorNode | null = null;
  private mediaStream: MediaStream | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit() {
    this.loadSpeakersCount();
  }

  ngOnDestroy() {
    this.stopLive();
  }

  loadSpeakersCount() {
    this.http.get<{ok: boolean, speakers: string[]}>(`${this.apiUrl}/api/speakers/list`)
      .subscribe({
        next: (data) => this.totalSpeakers = data.speakers?.length || 0,
        error: (err) => console.error('Failed to load speakers:', err)
      });
  }

  onEnrollFileChange(event: any) {
    this.enrollFile = event.target.files[0] || null;
  }

  enrollSpeaker() {
    if (!this.enrollName || !this.enrollFile) {
      this.enrollStatus = '⚠️ İsim ve ses dosyası gerekli.';
      this.enrollStatusType = 'warning';
      return;
    }

    const formData = new FormData();
    formData.append('name', this.enrollName);
    formData.append('file', this.enrollFile);

    this.enrollStatus = '⏳ Yükleniyor...';
    this.enrollStatusType = 'info';

    this.http.post<{ok: boolean, message: string}>(`${this.apiUrl}/enroll`, formData)
      .subscribe({
        next: (data) => {
          this.enrollStatus = '✅ ' + (data.message || 'Kayıt başarılı!');
          this.enrollStatusType = 'success';
          this.enrollName = '';
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
    if (this.ws) return;

    this.liveIndicator = 'Başlatılıyor...';

    try {
      this.mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      this.audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
      const source = this.audioContext.createMediaStreamSource(this.mediaStream);
      this.processor = this.audioContext.createScriptProcessor(2048, 1, 1);

      const wsProto = location.protocol === 'https:' ? 'wss' : 'ws';
      this.ws = new WebSocket(`${wsProto}://localhost:8000/ws/identify`);

      this.ws.onopen = () => {
        this.liveIndicator = '🎤 Dinleniyor';
        this.isLiveActive = true;
        this.ws?.send(JSON.stringify({ type: 'config', sr: this.audioContext?.sampleRate }));

        if (this.processor && this.audioContext) {
          this.processor.onaudioprocess = (e) => {
            const input = e.inputBuffer.getChannelData(0);
            const buf = new Float32Array(input.length);
            buf.set(input);
            this.ws?.send(buf.buffer);
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
        this.liveIndicator = 'Hazır';
        this.isLiveActive = false;
      };

    } catch (e) {
      this.liveIndicator = '❌ Mikrofon hatası';
      this.isLiveActive = false;
    }
  }

  stopLive() {
    if (this.ws) {
      this.ws.close();
      this.ws = null;
    }
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
      this.mediaStream.getTracks().forEach(t => t.stop());
      this.mediaStream = null;
    }

    this.isLiveActive = false;
    this.liveIndicator = 'Hazır';
    this.liveSpeaker = '—';
    this.liveScore = '';
    this.liveTop = [];
  }
}

