import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LogService {
  private originalConsole: {
    log: typeof console.log;
    error: typeof console.error;
    warn: typeof console.warn;
    info: typeof console.info;
    debug: typeof console.debug;
  };

  private logQueue: any[] = [];
  private isSending = false;
  private readonly maxQueueSize = 100;

  constructor(private http: HttpClient) {
    this.originalConsole = {
      log: console.log.bind(console),
      error: console.error.bind(console),
      warn: console.warn.bind(console),
      info: console.info.bind(console),
      debug: console.debug.bind(console)
    };

    this.overrideConsole();
  }

  private overrideConsole(): void {
    // Console.log override - sadece log sistemine gönder, console'a yazma
    console.log = (...args: any[]) => {
      const message = this.formatMessage(args);
      // Debug: Eğer mesaj boşsa, args'ı kontrol et
      if (!message || message.trim().length === 0) {
        // Orijinal console'u kullan (override edilmiş olabilir)
        if (this.originalConsole && this.originalConsole.log) {
          this.originalConsole.log('LogService: Empty message from args:', args);
        }
      }
      this.sendLog('Information', 'Frontend', message);
    };

    // Console.error override - sadece log sistemine gönder, console'a yazma
    console.error = (...args: any[]) => {
      this.sendLog('Error', 'Frontend', this.formatMessage(args), this.formatError(args));
    };

    // Console.warn override - sadece log sistemine gönder, console'a yazma
    console.warn = (...args: any[]) => {
      this.sendLog('Warning', 'Frontend', this.formatMessage(args));
    };

    // Console.info override - sadece log sistemine gönder, console'a yazma
    console.info = (...args: any[]) => {
      this.sendLog('Information', 'Frontend', this.formatMessage(args));
    };

    // Console.debug override - sadece log sistemine gönder, console'a yazma
    console.debug = (...args: any[]) => {
      this.sendLog('Debug', 'Frontend', this.formatMessage(args));
    };
  }

  private formatMessage(args: any[]): string {
    if (!args || args.length === 0) {
      return '(No arguments)';
    }
    
    const messages = args.map(arg => {
      if (arg === null) {
        return 'null';
      }
      if (arg === undefined) {
        return 'undefined';
      }
      if (typeof arg === 'string') {
        return arg;
      }
      if (typeof arg === 'number' || typeof arg === 'boolean') {
        return String(arg);
      }
      if (typeof arg === 'object') {
        try {
          // Circular reference kontrolü
          const seen = new WeakSet();
          return JSON.stringify(arg, (key, value) => {
            if (typeof value === 'object' && value !== null) {
              if (seen.has(value)) {
                return '[Circular]';
              }
              seen.add(value);
            }
            return value;
          }, 2);
        } catch (e) {
          // JSON.stringify başarısız olursa, toString() dene
          try {
            return String(arg);
          } catch {
            return '[Object]';
          }
        }
      }
      return String(arg);
    });
    
    const result = messages.join(' ').trim();
    return result || '(Empty message)';
  }

  private formatError(args: any[]): string | undefined {
    const error = args.find(arg => arg instanceof Error);
    if (error instanceof Error) {
      return `${error.name}: ${error.message}\n${error.stack}`;
    }
    return undefined;
  }

  private sendLog(level: string, serviceName: string, message: string, exception?: string): void {
    // Mesajı temizle
    const cleanMessage = message ? message.trim() : '';
    
    // Final mesaj: cleanMessage varsa onu kullan, yoksa exception, yoksa default
    const finalMessage = cleanMessage || exception || '(No message)';

    const logEntry = {
      serviceName: serviceName || 'Frontend',
      level: level || 'Information',
      message: finalMessage,
      exception: exception || null,
      timestamp: new Date().toISOString()
    };

    // Queue'ya ekle
    this.logQueue.push(logEntry);

    // Queue çok büyükse eski logları temizle
    if (this.logQueue.length > this.maxQueueSize) {
      this.logQueue.shift();
    }

    // Batch olarak gönder (performans için)
    if (!this.isSending) {
      this.flushLogs();
    }
  }

  private flushLogs(): void {
    if (this.logQueue.length === 0 || this.isSending) {
      return;
    }

    this.isSending = true;
    const logsToSend = [...this.logQueue];
    this.logQueue = [];

    // Her log'u ayrı ayrı gönder (SignalR için)
    logsToSend.forEach(log => {
      try {
        const gatewayUrl = environment.apiUrl.replace('/api/v1', '');
        
        this.http.post(`${gatewayUrl}/api/v1/logs`, log, {
          headers: { 'Content-Type': 'application/json' }
        }).subscribe({
          next: () => {
            // Başarılı - log gönderildi
          },
          error: (err) => {
            // Hata durumunda sessizce devam et (console'a yazma)
            // Log'u tekrar queue'ya ekleme (sonsuz döngü olmasın)
          }
        });
      } catch (err) {
        // Hata durumunda sessizce devam et
      }
    });

    // Bir sonraki batch için hazır ol
    setTimeout(() => {
      this.isSending = false;
      if (this.logQueue.length > 0) {
        this.flushLogs();
      }
    }, 100);
  }

  // Manuel log gönderme metodu
  log(level: string, message: string, exception?: string): void {
    this.sendLog(level, 'Frontend', message, exception);
  }
}



