import { CUSTOM_ELEMENTS_SCHEMA, Component, inject } from '@angular/core'
import { SimplebarAngularModule } from 'simplebar-angular'
import { NavigationEnd, Router, RouterModule } from '@angular/router'
import {
  NgbCollapse,
  NgbCollapseModule,
  NgbTooltipModule,
} from '@ng-bootstrap/ng-bootstrap'
import { CommonModule } from '@angular/common'
import { findAllParent, findMenuItem } from '../../helpers/utils'
import { LogoBoxComponent } from '@/app/components/logo-box.component'
import { MENU, type MenuItem } from '@/app/common/menu-meta'
import { changesidebarsize } from '@/app/store/layout/layout-action'
import { Store } from '@ngrx/store'
import { getSidebarsize } from '@/app/store/layout/layout-selector'
import { basePath } from '@/app/common/constants'
import { AuthService } from '@/app/services/auth.service'
import { HttpClient, HttpHeaders } from '@angular/common/http'
import { environment } from '../../../environments/environment'
import { OnDestroy } from '@angular/core'

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    SimplebarAngularModule,
    RouterModule,
    NgbCollapseModule,
    CommonModule,
    NgbTooltipModule,
    LogoBoxComponent,
  ],
  templateUrl: './sidebar.component.html',
  styles: `
    .nav-icon iconify-icon {
      display: inline-block;
      width: 20px;
      height: 20px;
      min-width: 20px;
      min-height: 20px;
    }
    .nav-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
    }
  `,
  schemas: [CUSTOM_ELEMENTS_SCHEMA],
})
export class SidebarComponent implements OnDestroy {
  menuItems: MenuItem[] = []
  activeMenuItems: string[] = []
  unreadAnnouncementCount = 0
  private announcementPollInterval: any = null

  store = inject(Store)
  router = inject(Router)
  authService = inject(AuthService)
  http = inject(HttpClient)
  trimmedURL = this.router.url?.replaceAll(
    basePath !== '' ? basePath + '/' : '',
    '/'
  )

  constructor() {
    this.router.events.forEach((event) => {
      if (event instanceof NavigationEnd) {
        this.trimmedURL = this.router.url?.replaceAll(
          basePath !== '' ? basePath + '/' : '',
          '/'
        )
        // Menüyü yeniden yükle (kullanıcı bilgisi güncellenmiş olabilir)
        this.initMenu()
        this._activateMenu()
        setTimeout(() => {
          this.scrollToActive()
        }, 200)
      }
    })
  }

  ngOnInit(): void {
    // Kullanıcı bilgisi yüklenene kadar bekle
    const user = this.authService.getUser()
    if (user && user.roles) {
      this.initMenu()
      this.loadUnreadAnnouncementCount()
      this.startAnnouncementPolling()
    } else {
      // Kullanıcı bilgisi henüz yüklenmemişse, kısa bir süre bekle
      setTimeout(() => {
        this.initMenu()
        this.loadUnreadAnnouncementCount()
        this.startAnnouncementPolling()
      }, 100)
    }
  }

  ngOnDestroy(): void {
    if (this.announcementPollInterval) {
      clearInterval(this.announcementPollInterval)
    }
  }

  get headers() {
    const token = this.authService.getToken()
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    })
  }

  loadUnreadAnnouncementCount(): void {
    const user = this.authService.getUser()
    if (!user || !user.id) return

    const roles = user?.roles || []
    const isAdmin = roles.includes('Admin') || roles.includes('SuperAdmin')
    if (!isAdmin) return // Sadece Admin ve SuperAdmin için

    const tenantId = user?.tenantId
    const params = new URLSearchParams()
    params.append('userId', user.id.toString())
    if (tenantId) {
      params.append('tenantId', tenantId.toString())
    }

    this.http.get<{ count: number }>(`${environment.apiUrl}/announcements/unread-count?${params.toString()}`, { headers: this.headers }).subscribe({
      next: (data) => {
        this.unreadAnnouncementCount = data?.count || 0
        this.updateMenuBadge()
      },
      error: (err) => {
        // Sadece 500 hatası değilse log'la
        if (err.status !== 500) {
          console.error('Okunmamış duyuru sayısı yüklenemedi:', err)
        }
        // Hata durumunda mevcut count'u koru
      }
    })
  }

  updateMenuBadge(): void {
    const duyurularItem = this.menuItems.find(item => item.key === 'duyurular')
    if (duyurularItem) {
      if (this.unreadAnnouncementCount > 0) {
        duyurularItem.badge = {
          variant: 'danger',
          text: this.unreadAnnouncementCount.toString()
        }
      } else {
        duyurularItem.badge = undefined
      }
    }
  }

  startAnnouncementPolling(): void {
    // Her 60 saniyede bir kontrol et (30 saniyeden 60 saniyeye çıkarıldı)
    this.announcementPollInterval = setInterval(() => {
      this.loadUnreadAnnouncementCount()
    }, 60000)
  }

  initMenu(): void {
    const user = this.authService.getUser()
    const userRoles = (user?.roles || []).map((r: string) => r?.toLowerCase() || '')
    
    // Debug: Tüm menü öğelerini ve kullanıcı rollerini logla
    console.log('=== MENU DEBUG ===')
    console.log('All menu items:', MENU.map(item => ({ key: item.key, label: item.label, roles: item.roles })))
    console.log('User roles (lowercase):', userRoles)
    console.log('User object:', user)
    
    // SuperAdmin kontrolü: SADECE rol ile belirlenir (case-insensitive)
    const isSuperAdmin = userRoles.includes('superadmin')
    
    // Rollere göre menüyü filtrele (case-insensitive)
    this.menuItems = MENU.filter((item: MenuItem) => {
      // Eğer item'da roles tanımlanmamışsa, herkese göster
      if (!item.roles || item.roles.length === 0) {
        return true
      }
      
      // Item rollerini lowercase'e çevir
      const itemRoles = item.roles.map(r => r?.toLowerCase() || '')
      
      // SuperAdmin için özel kontrol
      if (itemRoles.includes('superadmin')) {
        const result = isSuperAdmin
        if (item.key === 'email-superadmin' || item.key === 'email') {
          console.log(`SuperAdmin check for ${item.key}:`, { isSuperAdmin, result, itemRoles, userRoles })
        }
        return result
      }
      
      // Kullanıcının rollerinden herhangi biri item'ın rollerinde varsa göster
      const hasAccess = itemRoles.some((role: string) => userRoles.includes(role))
      
      // Debug: Her item için kontrol sonucunu logla
      if (item.key === 'giderler' || item.key === 'email' || item.key === 'email-superadmin' || item.key === 'ticket-management' || item.key === 'ticket-management-superadmin') {
        console.log(`${item.key} item check:`, {
          itemRoles,
          userRoles,
          hasAccess,
          isSuperAdmin,
          item: { key: item.key, label: item.label, roles: item.roles }
        })
      }
      
      return hasAccess
    })
    
    // Debug: Filtrelenmiş menü öğelerini console'a yazdır
    console.log('Filtered menu items:', this.menuItems.map(item => ({ key: item.key, label: item.label, roles: item.roles })))
    console.log('Giderler in filtered menu?', this.menuItems.some(item => item.key === 'giderler'))
    console.log('Email in filtered menu?', this.menuItems.some(item => item.key === 'email'))
    console.log('Email-superadmin in filtered menu?', this.menuItems.some(item => item.key === 'email-superadmin'))
    console.log('Email items count:', this.menuItems.filter(item => item.key === 'email' || item.key === 'email-superadmin').length)
    console.log('All email items:', this.menuItems.filter(item => item.key === 'email' || item.key === 'email-superadmin'))
    console.log('Ticket-management in filtered menu?', this.menuItems.some(item => item.key === 'ticket-management' || item.key === 'ticket-management-superadmin'))
    console.log('Ticket-management items:', this.menuItems.filter(item => item.key === 'ticket-management' || item.key === 'ticket-management-superadmin'))
    console.log('=== END MENU DEBUG ===')
    
    // Badge'leri güncelle
    this.updateMenuBadge()
  }

  ngAfterViewInit() {
    setTimeout(() => {
      this._activateMenu()
    })
    setTimeout(() => {
      this.scrollToActive()
    }, 200)
  }

  scrollToActive(): void {
    const activatedItem = document.querySelector('.nav-item li a.active')
    if (activatedItem) {
      const simplebarContent = document.querySelector(
        '.main-nav .simplebar-content-wrapper'
      )
      if (simplebarContent) {
        const activatedItemRect = activatedItem.getBoundingClientRect()
        const simplebarContentRect = simplebarContent.getBoundingClientRect()
        const activatedItemOffsetTop =
          activatedItemRect.top + simplebarContent.scrollTop
        const centerOffset =
          activatedItemOffsetTop -
          simplebarContentRect.top -
          simplebarContent.clientHeight / 2 +
          activatedItemRect.height / 2
        this.scrollTo(simplebarContent, centerOffset, 600)
      }
    }
  }

  easeInOutQuad(t: number, b: number, c: number, d: number): number {
    t /= d / 2
    if (t < 1) return (c / 2) * t * t + b
    t--
    return (-c / 2) * (t * (t - 2) - 1) + b
  }

  scrollTo(element: Element, to: number, duration: number): void {
    const start = element.scrollTop
    const change = to - start
    const increment = 20
    let currentTime = 0

    const animateScroll = () => {
      currentTime += increment
      const val = this.easeInOutQuad(currentTime, start, change, duration)
      element.scrollTop = val
      if (currentTime < duration) {
        setTimeout(animateScroll, increment)
      }
    }
    animateScroll()
  }

  _activateMenu(): void {
    const div = document.querySelector('.navbar-nav')

    let matchingMenuItem = null

    if (div) {
      let items: any = div.getElementsByClassName('nav-link-ref')
      for (let i = 0; i < items.length; ++i) {
        if (
          this.trimmedURL === items[i].pathname ||
          (this.trimmedURL.startsWith('/invoice/') &&
            items[i].pathname === '/invoice/RB6985') ||
          (this.trimmedURL.startsWith('/ecommerce/product/') &&
            items[i].pathname === '/ecommerce/product/1')
        ) {
          matchingMenuItem = items[i]
          break
        }
      }

      if (matchingMenuItem) {
        const mid = matchingMenuItem.getAttribute('aria-controls')
        const activeMt = findMenuItem(this.menuItems, mid)

        if (activeMt) {
          const matchingObjs = [
            activeMt['key'],
            ...findAllParent(this.menuItems, activeMt),
          ]

          this.activeMenuItems = matchingObjs
          this.menuItems.forEach((menu: MenuItem) => {
            menu.collapsed = !matchingObjs.includes(menu.key!)
          })
        }
      }
    }
  }

  /**
   * Returns true or false if given menu item has child or not
   * @param item menuItem
   */
  hasSubmenu(menu: MenuItem): boolean {
    return menu.subMenu ? true : false
  }

  /**
   * toggles open menu
   * @param menuItem clicked menuitem
   * @param collapse collpase instance
   */
  toggleMenuItem(menuItem: MenuItem, collapse: NgbCollapse): void {
    collapse.toggle()
    let openMenuItems: string[]
    if (!menuItem.collapsed) {
      openMenuItems = [
        menuItem['key'],
        ...findAllParent(this.menuItems, menuItem),
      ]
      this.menuItems.forEach((menu: MenuItem) => {
        if (!openMenuItems.includes(menu.key!)) {
          menu.collapsed = true
        }
      })
    }
  }

  changeSidebarSize() {
    let size = document.documentElement.getAttribute('data-menu-size')
    if (size == 'sm-hover') {
      size = 'sm-hover-active'
    } else {
      size = 'sm-hover'
    }
    this.store.dispatch(changesidebarsize({ size }))
    this.store.select(getSidebarsize).subscribe((size: string) => {
      document.documentElement.setAttribute('data-menu-size', size)
    })
  }
}

