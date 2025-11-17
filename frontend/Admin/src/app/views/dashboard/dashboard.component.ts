import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { AuthService } from 'src/app/services/auth.service';
import { NgApexchartsModule } from 'ng-apexcharts';
import { environment } from 'src/environments/environment';
import {
  ApexNonAxisChartSeries,
  ApexResponsive,
  ApexChart,
  ApexTheme,
  ApexTitleSubtitle,
  ChartComponent,
  ApexAxisChartSeries,
  ApexXAxis,
  ApexYAxis,
  ApexDataLabels,
  ApexGrid,
  ApexStroke,
  ApexLegend,
  ApexFill,
  ApexTooltip,
  ApexPlotOptions
} from 'ng-apexcharts';

export type ChartOptions = {
  series?: ApexAxisChartSeries | ApexNonAxisChartSeries;
  chart?: ApexChart;
  responsive?: ApexResponsive[];
  labels?: any;
  theme?: ApexTheme;
  title?: ApexTitleSubtitle;
  xaxis?: ApexXAxis;
  yaxis?: ApexYAxis | ApexYAxis[];
  dataLabels?: ApexDataLabels;
  grid?: ApexGrid;
  stroke?: ApexStroke;
  legend?: ApexLegend;
  fill?: ApexFill;
  tooltip?: ApexTooltip;
  colors?: string[];
  plotOptions?: ApexPlotOptions;
};

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, NgApexchartsModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit, OnDestroy {
  loading = true;
  error: string | null = null;
  isSuperAdmin = false;
  isAdmin = false;
  isUser = false;
  
  // Statistics
  todaySales = 0;
  todayExpenses = 0;
  todayProfit = 0;
  totalProducts = 0;
  totalCategories = 0;
  
  // Charts - Use any to avoid strict type checking issues with ApexCharts
  dailySalesChartOptions: any = {};
  profitLossChartOptions: any = {};
  categoriesChartOptions: any = {};
  paymentTrendChartOptions: any = {};
  // SuperAdmin payment summary
  paymentSummary: any = null;
  // Çalışan bazlı satış istatistikleri
  salesByEmployee: any[] = [];
  employeeNames: Map<number, string> = new Map();
  currentTime: Date = new Date();
  
  private refreshInterval: any;
  private readonly API_URL = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const roles = this.authService.getUser()?.roles || [];
    this.isSuperAdmin = roles.includes('SuperAdmin');
    this.isAdmin = roles.includes('Admin');
    this.isUser = roles.includes('User') && !this.isAdmin && !this.isSuperAdmin;
    this.loadDashboardData();
    // Her 10 saniyede bir veriyi yenile (canlı güncelleme)
    this.refreshInterval = setInterval(() => {
      this.loadDashboardData();
    }, 10000);
  }

  ngOnDestroy(): void {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  loadDashboardData(): void {
    this.loading = true;
    this.error = null;
    this.currentTime = new Date(); // Zamanı güncelle

    // Mock data modu - De'Bakiim firması için örnek veriler
    const useMockData = (window as any).USE_MOCK_DASHBOARD === true || 
                       localStorage.getItem('useMockDashboard') === 'true';

    if (useMockData) {
      // De'Bakiim firması için mock data
      setTimeout(() => {
        this.todaySales = 45230.50;
        this.todayExpenses = 18250.75;
        this.todayProfit = 26979.75;
        this.totalProducts = 127;
        this.totalCategories = 18;

        // Son 7 günlük mock data
        const mockDailySales = [
          { dateLabel: '8 Kas', sales: 38500, expenses: 15200, profit: 23300 },
          { dateLabel: '9 Kas', sales: 41200, expenses: 16800, profit: 24400 },
          { dateLabel: '10 Kas', sales: 38900, expenses: 17500, profit: 21400 },
          { dateLabel: '11 Kas', sales: 42100, expenses: 18200, profit: 23900 },
          { dateLabel: '12 Kas', sales: 44500, expenses: 19000, profit: 25500 },
          { dateLabel: '13 Kas', sales: 43800, expenses: 18500, profit: 25300 },
          { dateLabel: '14 Kas', sales: 45230, expenses: 18250, profit: 26979 }
        ];

        this.updateCharts(mockDailySales);
        this.loading = false;
      }, 500); // Kısa bir gecikme ile loading animasyonu göster
      return;
    }

    const token = this.authService.getToken();
    if (!token) {
      this.error = 'Oturum bulunamadı. Lütfen tekrar giriş yapın.';
      this.loading = false;
      return;
    }

    // Dashboard ana istatistikleri
    this.http.get<any>(`${this.API_URL}/dashboard/stats`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    }).subscribe({
      next: (data) => {
        this.todaySales = data.todaySales || 0;
        this.todayExpenses = data.todayExpenses || 0;
        this.todayProfit = data.todayProfit || 0;
        this.totalProducts = data.totalProducts || 0;
        this.totalCategories = data.totalCategories || 0;

        this.updateCharts(data.dailySales || []);
        this.loading = false;
      },
      error: (err) => {
        console.error('Dashboard data error:', err);
        // API hatası durumunda mock data göster
        this.loadMockData();
      }
    });

    // Admin ve SuperAdmin için çalışan bazlı satış istatistikleri
    if (this.isAdmin || this.isSuperAdmin) {
      const user = this.authService.getUser();
      const tenantId = user?.tenantId;
      const today = new Date();
      const todayStart = new Date(today.getFullYear(), today.getMonth(), today.getDate());
      const todayEnd = new Date(todayStart);
      todayEnd.setDate(todayEnd.getDate() + 1);
      
      let salesUrl = `${this.API_URL}/transactions/sales-by-employee?baslangic=${todayStart.toISOString()}&bitis=${todayEnd.toISOString()}`;
      if (tenantId && !this.isSuperAdmin) {
        salesUrl += `&tenantId=${tenantId}`;
      }
      
      this.http.get<any[]>(salesUrl, {
        headers: { 'Authorization': `Bearer ${token}` }
      }).subscribe({
        next: (salesData) => {
          this.salesByEmployee = salesData || [];
          // Kullanıcı isimlerini çek
          this.loadEmployeeNames(salesData.map(s => s.userId));
        },
        error: (err) => {
          console.warn('Sales by employee error:', err);
          this.salesByEmployee = [];
        }
      });
    }

    // SuperAdmin için ödeme özet ve trend
    if (this.isSuperAdmin) {
      // Summary
      this.http.get<any>(`${this.API_URL}/admin/payments/summary`, {
        headers: { 'Authorization': `Bearer ${token}` }
      }).subscribe({
        next: (summary) => {
          this.paymentSummary = summary;
        },
        error: (err) => {
          console.warn('Payment summary error:', err);
          this.paymentSummary = null;
        }
      });
      // Trend
      this.http.get<any[]>(`${this.API_URL}/admin/payments/trend?months=6`, {
        headers: { 'Authorization': `Bearer ${token}` }
      }).subscribe({
        next: (points) => {
          const labels = points.map(p => p.month);
          const totals = points.map(p => p.total);
          const counts = points.map(p => p.count);
          this.paymentTrendChartOptions = {
            series: [
              { name: 'Toplam Tahsilat (₺)', type: 'column', data: totals },
              { name: 'Ödeme Adedi', type: 'line', data: counts }
            ],
            chart: { height: 320, type: 'line', stacked: false, toolbar: { show: false } },
            xaxis: { categories: labels },
            yaxis: [
              { title: { text: '₺' } },
              { opposite: true, title: { text: 'Adet' } }
            ],
            stroke: { width: [0, 3] },
            dataLabels: { enabled: false },
            colors: ['#c1593f', '#5d7186'],
            legend: { position: 'top', horizontalAlign: 'right' }
          };
        },
        error: (err) => {
          console.warn('Payment trend error:', err);
          this.paymentTrendChartOptions = {};
        }
      });
    }
  }

  private loadMockData(): void {
    this.todaySales = 45230.50;
    this.todayExpenses = 18250.75;
    this.todayProfit = 26979.75;
    this.totalProducts = 127;
    this.totalCategories = 18;

    const mockDailySales = [
      { dateLabel: '8 Kas', sales: 38500, expenses: 15200, profit: 23300 },
      { dateLabel: '9 Kas', sales: 41200, expenses: 16800, profit: 24400 },
      { dateLabel: '10 Kas', sales: 38900, expenses: 17500, profit: 21400 },
      { dateLabel: '11 Kas', sales: 42100, expenses: 18200, profit: 23900 },
      { dateLabel: '12 Kas', sales: 44500, expenses: 19000, profit: 25500 },
      { dateLabel: '13 Kas', sales: 43800, expenses: 18500, profit: 25300 },
      { dateLabel: '14 Kas', sales: 45230, expenses: 18250, profit: 26979 }
    ];

    this.updateCharts(mockDailySales);
    this.loading = false;
  }

  updateCharts(dailySales: any[]): void {
    const dates = dailySales.map(d => d.dateLabel);
    const sales = dailySales.map(d => d.sales);
    const expenses = dailySales.map(d => d.expenses);
    const profits = dailySales.map(d => d.profit);

    // Günlük Satış Grafiği
    this.dailySalesChartOptions = {
      series: [
        {
          name: 'Satış',
          data: sales
        }
      ],
      chart: {
        type: 'area',
        height: 350,
        toolbar: {
          show: false
        },
        zoom: {
          enabled: false
        }
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        curve: 'smooth',
        width: 3
      },
      fill: {
        type: 'gradient',
        gradient: {
          shadeIntensity: 1,
          opacityFrom: 0.7,
          opacityTo: 0.3,
          stops: [0, 90, 100]
        }
      },
      xaxis: {
        categories: dates
      },
      yaxis: {
        labels: {
          formatter: (val: number) => {
            return new Intl.NumberFormat('tr-TR', {
              style: 'currency',
              currency: 'TRY',
              minimumFractionDigits: 0
            }).format(val);
          }
        }
      },
      tooltip: {
        y: {
          formatter: (val: number) => {
            return new Intl.NumberFormat('tr-TR', {
              style: 'currency',
              currency: 'TRY'
            }).format(val);
          }
        }
      },
      colors: ['#c1593f'],
      theme: {
        mode: 'light'
      }
    };

    // Kar-Zarar Grafiği
    this.profitLossChartOptions = {
      series: [
        {
          name: 'Gelir',
          data: sales
        },
        {
          name: 'Gider',
          data: expenses
        },
        {
          name: 'Kar',
          data: profits
        }
      ],
      chart: {
        type: 'bar',
        height: 350,
        toolbar: {
          show: false
        }
      },
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '55%',
          borderRadius: 4
        }
      },
      dataLabels: {
        enabled: false
      },
      stroke: {
        show: true,
        width: 2,
        colors: ['transparent']
      },
      xaxis: {
        categories: dates
      },
      yaxis: {
        labels: {
          formatter: (val: number) => {
            return new Intl.NumberFormat('tr-TR', {
              style: 'currency',
              currency: 'TRY',
              minimumFractionDigits: 0
            }).format(val);
          }
        }
      },
      fill: {
        opacity: 1
      },
      tooltip: {
        y: {
          formatter: (val: number) => {
            return new Intl.NumberFormat('tr-TR', {
              style: 'currency',
              currency: 'TRY'
            }).format(val);
          }
        }
      },
      legend: {
        position: 'top',
        horizontalAlign: 'right'
      },
      colors: ['#c1593f', '#5d7186', '#28a745']
    };

    // Kategoriler Pie Chart
    this.categoriesChartOptions = {
      series: [this.totalCategories],
      chart: {
        type: 'radialBar',
        height: 300
      },
      plotOptions: {
        radialBar: {
          hollow: {
            size: '70%'
          },
          dataLabels: {
            name: {
              show: true,
              fontSize: '16px',
              fontWeight: 600
            },
            value: {
              show: true,
              fontSize: '24px',
              fontWeight: 700,
              formatter: (val: number) => {
                return val.toString();
              }
            }
          }
        }
      },
      labels: ['Toplam Kategori'],
      colors: ['#c1593f']
    };
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY'
    }).format(value);
  }

  loadEmployeeNames(userIds: number[]): void {
    if (!userIds || userIds.length === 0) return;
    
    const token = this.authService.getToken();
    if (!token) return;
    
    // Her kullanıcı için isim çek
    userIds.forEach(userId => {
      if (this.employeeNames.has(userId)) return; // Zaten yüklü
      
      this.http.get<any>(`${this.API_URL}/users/${userId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
      }).subscribe({
        next: (user) => {
          const fullName = `${user.firstName || ''} ${user.lastName || ''}`.trim() || user.username || `Kullanıcı ${userId}`;
          this.employeeNames.set(userId, fullName);
        },
        error: () => {
          this.employeeNames.set(userId, `Kullanıcı ${userId}`);
        }
      });
    });
  }

  getEmployeeName(userId: number): string {
    return this.employeeNames.get(userId) || `Kullanıcı ${userId}`;
  }

  refreshData(): void {
    this.loadDashboardData();
  }
}




