import { Route } from '@angular/router'
import { SigninComponent } from './signin/signin.component'
import { Signin2Component } from './signin2/signin2.component'
import { SignupComponent } from './signup/signup.component'
import { Signup2Component } from './signup2/signup2.component'
import { ResetPassComponent } from './reset-pass/reset-pass.component'
import { ResetPass2Component } from './reset-pass2/reset-pass2.component'
import { LockScreenComponent } from './lock-screen/lock-screen.component'
import { LockScreen2Component } from './lock-screen2/lock-screen2.component'
import { LoginComponent } from './login/login.component'
import { RegisterComponent } from './register/register.component'
import { LicenseKeyComponent } from './license-key/license-key.component'

export const AUTH_ROUTES: Route[] = [
  {
    path: 'login',
    component: LoginComponent,
    data: { title: 'Login' },
  },
  {
    path: 'sign-in',
    component: LoginComponent,
    data: { title: 'Sign In' },
  },
  {
    path: 'register',
    component: RegisterComponent,
    data: { title: 'Register' },
  },
  {
    path: 'sign-up',
    component: RegisterComponent,
    data: { title: 'Sign Up' },
  },
  {
    path: 'sign-in-2',
    component: Signin2Component,
    data: { title: 'Sign In 2' },
  },
  {
    path: 'sign-up',
    component: SignupComponent,
    data: { title: 'Sign Up' },
  },
  {
    path: 'sign-up-2',
    component: Signup2Component,
    data: { title: 'Sign Up 2' },
  },
  {
    path: 'reset-pass',
    component: ResetPassComponent,
    data: { title: 'Reset Password' },
  },
  {
    path: 'reset-pass-2',
    component: ResetPass2Component,
    data: { title: 'Reset Password 2' },
  },
  {
    path: 'lock-screen',
    component: LockScreenComponent,
    data: { title: 'Lock Screen' },
  },
  {
    path: 'lock-screen-2',
    component: LockScreen2Component,
    data: { title: 'Lock Screen 2' },
  },
      {
        path: 'license-key',
        component: LicenseKeyComponent,
        data: { title: 'License Key' },
      },
]
