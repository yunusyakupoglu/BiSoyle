import { Component, OnInit, OnDestroy, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { environment } from 'src/environments/environment';

interface LogEntry {
  serviceName: string;
  level: string;
  message: string;
  exception?: string;
  timestamp: string;
}

@Component({
  selector: 'app-log-viewer',
  standalone: true,
  imports: [CommonModule, FormsModule],
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
  templateUrl: './log-viewer.component.html',
  styleUrls: ['./log-viewer.component.scss']
})
export class LogViewerComponent implements OnInit, OnDestroy {
  logs: LogEntry[] = [];
  filteredLogs: LogEntry[] = [];
  connection?: HubConnection;
  isConnected = false;
  autoScroll = true;
  maxLogs = 1000;
  
  // Filters
  selectedService = 'all';
  selectedLevel = 'all';
  searchText = '';
  
  // Service names
  services: string[] = ['all', 'Gateway', 'UserService', 'TenantService', 'ProductService', 'TransactionService', 'ReceiptService', 'VoiceService'];
  levels: string[] = ['all', 'Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'];

  ngOnInit() {
    this.startConnection();
  }

  ngOnDestroy() {
    this.stopConnection();
  }

  startConnection() {
    // Gateway URL'ini direkt kullan (apiUrl'den /api/v1 kısmını kaldır)
    const gatewayUrl = environment.apiUrl.replace('/api/v1', '');
    const hubUrl = `${gatewayUrl}/hub/logs`;
    
    this.connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.connection.start()
      .then(() => {
        // console.log override edildi, burada kullanma
        this.isConnected = true;
      })
      .catch(err => {
        // console.error override edildi, burada kullanma
        this.isConnected = false;
      });

    this.connection.on('logReceived', (log: LogEntry) => {
      // Servis adı yoksa Frontend olarak ayarla
      if (!log.serviceName || log.serviceName === 'Unknown') {
        log.serviceName = 'Frontend';
      }
      // Mesaj yoksa boş string yerine "No message" göster
      if (!log.message || log.message.trim().length === 0) {
        log.message = '(No message)';
      }
      this.addLog(log);
    });

    this.connection.on('connected', (connectionId: string) => {
      // console.log override edildi, burada kullanma
      this.isConnected = true;
    });

    this.connection.onreconnecting(() => {
      this.isConnected = false;
      // console.log override edildi, burada kullanma
    });

    this.connection.onreconnected(() => {
      this.isConnected = true;
      // console.log override edildi, burada kullanma
    });

    this.connection.onclose(() => {
      this.isConnected = false;
      // console.log override edildi, burada kullanma
    });
  }

  stopConnection() {
    if (this.connection) {
      this.connection.stop()
        .then(() => {
          // console.log override edildi, burada kullanma
          this.isConnected = false;
        })
        .catch(err => {
          // console.error override edildi, burada kullanma
        });
    }
  }

  addLog(log: LogEntry) {
    // Format timestamp
    const date = new Date(log.timestamp);
    log.timestamp = date.toLocaleString('tr-TR', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      fractionalSecondDigits: 3
    });

    this.logs.unshift(log);
    
    // Limit logs
    if (this.logs.length > this.maxLogs) {
      this.logs = this.logs.slice(0, this.maxLogs);
    }
    
    this.applyFilters();
    
    // Auto scroll
    if (this.autoScroll) {
      setTimeout(() => {
        const logContainer = document.getElementById('log-container');
        if (logContainer) {
          logContainer.scrollTop = 0;
        }
      }, 10);
    }
  }

  applyFilters() {
    this.filteredLogs = this.logs.filter(log => {
      // Service filter
      if (this.selectedService !== 'all' && log.serviceName !== this.selectedService) {
        return false;
      }
      
      // Level filter
      if (this.selectedLevel !== 'all' && log.level !== this.selectedLevel) {
        return false;
      }
      
      // Search text filter
      if (this.searchText) {
        const searchLower = this.searchText.toLowerCase();
        const matchesMessage = log.message.toLowerCase().includes(searchLower);
        const matchesException = log.exception?.toLowerCase().includes(searchLower) || false;
        const matchesService = log.serviceName.toLowerCase().includes(searchLower);
        
        if (!matchesMessage && !matchesException && !matchesService) {
          return false;
        }
      }
      
      return true;
    });
  }

  onFilterChange() {
    this.applyFilters();
  }

  clearLogs() {
    if (confirm('Tüm logları temizlemek istediğinizden emin misiniz?')) {
      this.logs = [];
      this.filteredLogs = [];
    }
  }

  getLevelClass(level: string): string {
    switch (level.toLowerCase()) {
      case 'error':
      case 'critical':
        return 'text-danger';
      case 'warning':
        return 'text-warning';
      case 'information':
        return 'text-info';
      case 'debug':
        return 'text-secondary';
      case 'trace':
        return 'text-muted';
      default:
        return '';
    }
  }

  getLevelBadgeClass(level: string): string {
    switch (level.toLowerCase()) {
      case 'error':
      case 'critical':
        return 'badge bg-danger';
      case 'warning':
        return 'badge bg-warning';
      case 'information':
        return 'badge bg-info';
      case 'debug':
        return 'badge bg-secondary';
      case 'trace':
        return 'badge bg-dark';
      default:
        return 'badge bg-primary';
    }
  }

  getServiceBadgeClass(serviceName: string): string {
    const colors: { [key: string]: string } = {
      'Gateway': 'bg-primary',
      'UserService': 'bg-success',
      'TenantService': 'bg-info',
      'ProductService': 'bg-warning',
      'TransactionService': 'bg-danger',
      'ReceiptService': 'bg-secondary',
      'VoiceService': 'bg-dark'
    };
    
    return `badge ${colors[serviceName] || 'bg-primary'}`;
  }

  exportLogs() {
    const logText = this.filteredLogs.map(log => 
      `[${log.timestamp}] [${log.serviceName}] [${log.level}] ${log.message}${log.exception ? '\n' + log.exception : ''}`
    ).join('\n\n');
    
    const blob = new Blob([logText], { type: 'text/plain' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `bisoyle-logs-${new Date().toISOString().replace(/[:.]/g, '-')}.txt`;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}



