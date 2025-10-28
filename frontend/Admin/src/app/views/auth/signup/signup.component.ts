import { LogoBoxComponent } from '@/app/components/logo-box.component'
import { SocialBtnComponent } from '@/app/components/social-btn/social-btn.component'
import { Component } from '@angular/core'
import { RouterLink } from '@angular/router'

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [SocialBtnComponent, LogoBoxComponent, RouterLink],
  templateUrl: './signup.component.html',
  styles: ``,
})
export class SignupComponent {}
