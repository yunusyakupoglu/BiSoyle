/// <reference types="@angular/localize" />

import { bootstrapApplication } from '@angular/platform-browser'
import { appConfig } from './app/app.config'
import { AppComponent } from './app/app.component'
import 'iconify-icon'

// Global error handler for browser extension errors (ignore harmless extension errors)
window.addEventListener('error', (event: ErrorEvent) => {
  // Ignore Chrome extension errors
  if (event.message && (
    event.message.includes('Receiving end does not exist') ||
    event.message.includes('Could not establish connection') ||
    event.message.includes('content-all.js') ||
    event.message.includes('Extension context invalidated')
  )) {
    event.preventDefault()
    return
  }
})

// Handle unhandled promise rejections from extensions
window.addEventListener('unhandledrejection', (event: PromiseRejectionEvent) => {
  if (event.reason && typeof event.reason === 'object' && 'message' in event.reason) {
    const message = String(event.reason.message || '')
    if (
      message.includes('Receiving end does not exist') ||
      message.includes('Could not establish connection') ||
      message.includes('Extension context invalidated')
    ) {
      event.preventDefault()
      return
    }
  }
})

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err))
