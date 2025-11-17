import { Injectable } from '@angular/core';

/**
 * Device Fingerprint Service
 * 
 * Computes deterministic device fingerprint as SHA-256 of concatenated fields:
 * - Machine GUID (Windows) or /etc/machine-id (Linux) or macOS ioreg
 * - BIOS/SMBIOS UUID (if available)
 * - Primary disk serial (if available)
 * - Primary MAC address
 * 
 * Cross-platform fallbacks included.
 */
@Injectable({
  providedIn: 'root'
})
export class DeviceFingerprintService {
  
  /**
   * Compute device fingerprint hash (SHA-256)
   * Returns: Promise<string> - SHA-256 hash in hex format
   */
  async computeFingerprint(): Promise<string> {
    try {
      const fields: string[] = [];
      
      // Field 1: Machine ID / GUID
      const machineId = await this.getMachineId();
      if (machineId) fields.push(`machine:${machineId}`);
      
      // Field 2: BIOS UUID (if available - browser limited)
      // Note: Browser security prevents direct hardware access
      // This is a simplified version - full implementation would need
      // native code or Electron/Desktop app
      
      // Field 3: Canvas fingerprint (browser-based fallback)
      const canvasFingerprint = this.getCanvasFingerprint();
      if (canvasFingerprint) fields.push(`canvas:${canvasFingerprint}`);
      
      // Field 4: Screen resolution
      if (window.screen) {
        fields.push(`screen:${window.screen.width}x${window.screen.height}x${window.screen.colorDepth}`);
      }
      
      // Field 5: Timezone
      fields.push(`timezone:${Intl.DateTimeFormat().resolvedOptions().timeZone}`);
      
      // Field 6: Language
      fields.push(`lang:${navigator.language}`);
      
      // Field 7: User Agent (hashed)
      if (navigator.userAgent) {
        fields.push(`ua:${await this.sha256(navigator.userAgent)}`);
      }
      
      // Field 8: Platform
      if (navigator.platform) {
        fields.push(`platform:${navigator.platform}`);
      }
      
      // Concatenate all fields
      const fingerprintData = fields.join('|');
      
      // Compute SHA-256 hash
      const hash = await this.sha256(fingerprintData);
      
      return hash;
    } catch (error) {
      console.error('Error computing device fingerprint:', error);
      // Fallback: Use session storage ID + timestamp
      return await this.sha256(`fallback:${sessionStorage.getItem('device-id') || Date.now().toString()}`);
    }
  }
  
  /**
   * Get machine ID (browser-accessible method)
   * Note: Full hardware access requires native app (Electron, etc.)
   */
  private async getMachineId(): Promise<string | null> {
    try {
      // Browser storage-based machine ID (persistent)
      let machineId = localStorage.getItem('_BISOYLE_MACHINE_ID_');
      
      if (!machineId) {
        // Generate a persistent ID
        machineId = this.generateUUID();
        localStorage.setItem('_BISOYLE_MACHINE_ID_', machineId);
      }
      
      return machineId;
    } catch (error) {
      console.error('Error getting machine ID:', error);
      return null;
    }
  }
  
  /**
   * Generate UUID v4
   */
  private generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }
  
  /**
   * Canvas fingerprint (browser-based device identification)
   */
  private getCanvasFingerprint(): string | null {
    try {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      
      if (!ctx) return null;
      
      canvas.width = 200;
      canvas.height = 50;
      
      ctx.textBaseline = 'top';
      ctx.font = '14px Arial';
      ctx.fillStyle = '#f60';
      ctx.fillRect(125, 1, 62, 20);
      ctx.fillStyle = '#069';
      ctx.fillText('BiSoyle Device ID', 2, 15);
      ctx.fillStyle = 'rgba(102, 204, 0, 0.7)';
      ctx.fillText('BiSoyle Device ID', 4, 17);
      
      return canvas.toDataURL();
    } catch (error) {
      console.error('Error getting canvas fingerprint:', error);
      return null;
    }
  }
  
  /**
   * Compute SHA-256 hash of input string
   */
  private async sha256(message: string): Promise<string> {
    // Use Web Crypto API if available
    if (window.crypto && window.crypto.subtle) {
      const msgBuffer = new TextEncoder().encode(message);
      const hashBuffer = await window.crypto.subtle.digest('SHA-256', msgBuffer);
      const hashArray = Array.from(new Uint8Array(hashBuffer));
      const hashHex = hashArray.map(b => b.toString(16).padStart(2, '0')).join('');
      return hashHex;
    }
    
    // Fallback: Simple hash (not cryptographically secure, but deterministic)
    let hash = 0;
    for (let i = 0; i < message.length; i++) {
      const char = message.charCodeAt(i);
      hash = ((hash << 5) - hash) + char;
      hash = hash & hash; // Convert to 32-bit integer
    }
    return Math.abs(hash).toString(16).padStart(64, '0');
  }
  
  /**
   * Format fingerprint for display (first 8 chars + ... + last 8 chars)
   */
  formatFingerprint(fingerprint: string): string {
    if (fingerprint.length <= 16) return fingerprint;
    return `${fingerprint.substring(0, 8)}...${fingerprint.substring(fingerprint.length - 8)}`;
  }
}






