import { CommonModule } from '@angular/common'
import { Component, Input } from '@angular/core'

@Component({
  selector: 'app-logo-box',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [class]="className" class="logo-container">
      <a href="/" class="logo-link">
        <img 
          src="assets/images/debakiim-logo.png" 
          [attr.width]="logoHeight || 120" 
          [attr.height]="logoHeight || 120"
          alt="De' Bakiim Logo"
          class="sidebar-logo"
        />
      </a>
    </div>
  `,
  styles: [`
    .logo-container {
      display: flex;
      justify-content: center;
      align-items: center;
      padding: 0.5rem 0;
      margin-bottom: -40px;
      margin-top: -30px;
      position: relative;
      z-index: 10;
    }
    
    .logo-link {
      display: block;
    }
    
    .sidebar-logo {
      display: block;
      object-fit: contain;
      transition: all 0.3s ease;
    }
    
    /* Sidebar küçüldüğünde logo da küçülsün */
    :host-context(.sidebar-size-sm) .logo-container {
      margin-bottom: -20px;
    }
    
    :host-context(.sidebar-size-sm) .sidebar-logo {
      width: 80px !important;
      height: 80px !important;
    }
    
    :host-context(.sidebar-size-collapsed) .logo-container {
      margin-bottom: -10px;
    }
    
    :host-context(.sidebar-size-collapsed) .sidebar-logo {
      width: 50px !important;
      height: 50px !important;
    }
  `]
})
export class LogoBoxComponent {
  @Input() className: string = ''
  @Input() height: string = ''
  @Input() logoHeight: string = ''
}
