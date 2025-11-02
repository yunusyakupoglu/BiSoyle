import { Component, OnInit, OnDestroy, CUSTOM_ELEMENTS_SCHEMA, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-izinler',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './izinler.component.html',
  styleUrls: ['./izinler.component.scss']
})
export class IzinlerComponent implements OnInit, OnDestroy {
  microphonePermission: PermissionState | null = null;
  isCheckingPermission = false;
  errorMessage: string = '';

  ngOnInit(): void {
    this.checkMicrophonePermission();
  }

  ngOnDestroy(): void {}

  async checkMicrophonePermission(): Promise<void> {
    this.isCheckingPermission = true;
    this.errorMessage = '';
    
    try {
      if (navigator.permissions) {
        const result = await navigator.permissions.query({ name: 'microphone' as PermissionName });
        this.microphonePermission = result.state;
        
        // Listen for permission changes
        result.onchange = () => {
          this.microphonePermission = result.state;
          this.isCheckingPermission = false;
        };
      } else {
        // Fallback: try to get permission by requesting media
        try {
          const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
          stream.getTracks().forEach(track => track.stop());
          this.microphonePermission = 'granted';
        } catch (e) {
          this.microphonePermission = 'denied';
        }
      }
    } catch (err) {
      console.log('Permission API not supported, using fallback');
      try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        stream.getTracks().forEach(track => track.stop());
        this.microphonePermission = 'granted';
      } catch (e) {
        this.microphonePermission = 'denied';
        this.errorMessage = 'Mikrofon izni alınamadı. Lütfen tarayıcı ayarlarından izin verin.';
      }
    } finally {
      this.isCheckingPermission = false;
    }
  }

  async requestMicrophonePermission(): Promise<void> {
    this.isCheckingPermission = true;
    this.errorMessage = '';
    
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      stream.getTracks().forEach(track => track.stop());
      this.microphonePermission = 'granted';
      
      // Refresh permission status
      await this.checkMicrophonePermission();
    } catch (err: any) {
      this.microphonePermission = 'denied';
      this.errorMessage = err.message || 'Mikrofon izni reddedildi. Lütfen tarayıcı ayarlarından izin verin.';
      console.error('Microphone permission denied:', err);
    } finally {
      this.isCheckingPermission = false;
    }
  }

  getPermissionStatusText(): string {
    switch (this.microphonePermission) {
      case 'granted':
        return 'İzin Verildi';
      case 'denied':
        return 'İzin Reddedildi';
      case 'prompt':
        return 'İzin Bekleniyor';
      default:
        return 'Bilinmiyor';
    }
  }

  getPermissionStatusClass(): string {
    switch (this.microphonePermission) {
      case 'granted':
        return 'success';
      case 'denied':
        return 'danger';
      case 'prompt':
        return 'warning';
      default:
        return 'secondary';
    }
  }

  getPermissionDescription(): string {
    switch (this.microphonePermission) {
      case 'granted':
        return 'Mikrofon kullanımı için izniniz bulunmaktadır. Sesli komut özellikleri aktif.';
      case 'denied':
        return 'Mikrofon kullanımı için izniniz reddedilmiştir. Sesli komutları kullanmak için izin vermeniz gerekmektedir.';
      case 'prompt':
        return 'Mikrofon iznini vererek sesli komut özelliklerini kullanabilirsiniz.';
      default:
        return 'Mikrofon izin durumu kontrol ediliyor...';
    }
  }

  isDisabled(): boolean {
    return this.isCheckingPermission || this.microphonePermission === 'granted';
  }

  shouldShowRequestButton(): boolean {
    return this.microphonePermission === 'prompt' || this.microphonePermission === 'denied' || this.microphonePermission === null;
  }
}
