import {
  ApplicationConfig,
  importProvidersFrom,
  isDevMode,
  provideExperimentalZonelessChangeDetection,
  provideZoneChangeDetection,
} from '@angular/core'
import {
  provideRouter,
  withInMemoryScrolling,
  type InMemoryScrollingFeature,
  type InMemoryScrollingOptions,
} from '@angular/router'

import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi,
} from '@angular/common/http'
import { provideStore } from '@ngrx/store'
import { provideStoreDevtools } from '@ngrx/store-devtools'
import { routes } from './app.routes'
import { rootReducer } from './store'
import { localStorageSyncReducer } from './store/layout/layout-reducers'
import { DecimalPipe } from '@angular/common'
import { provideEffects } from '@ngrx/effects'
import { CalendarEffects } from './store/calendar/calendar.effects'
// import { FakeBackendProvider } from './helpers/fake-backend' // REMOVED: Using real API now
import { AuthenticationEffects } from './store/authentication/authentication.effects'
import { provideToastr } from 'ngx-toastr'
import { BrowserAnimationsModule } from '@angular/platform-browser/animations'
import { BrowserModule } from '@angular/platform-browser'
import { AuthInterceptor } from './services/http.interceptor'

// scroll
const scrollConfig: InMemoryScrollingOptions = {
  scrollPositionRestoration: 'top',
  anchorScrolling: 'enabled',
}

const inMemoryScrollingFeatures: InMemoryScrollingFeature =
  withInMemoryScrolling(scrollConfig)

export const appConfig: ApplicationConfig = {
  providers: [
    // FakeBackendProvider, // REMOVED: Using real API now
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, inMemoryScrollingFeatures),
    DecimalPipe,
    provideStore(rootReducer, { metaReducers: [localStorageSyncReducer] }),
    provideStoreDevtools({ maxAge: 25, logOnly: !isDevMode() }),
    provideEffects(AuthenticationEffects, CalendarEffects),
    provideHttpClient(withFetch(), withInterceptorsFromDi()),
    {
      provide: HTTP_INTERCEPTORS,
      useClass: AuthInterceptor,
      multi: true
    },
    importProvidersFrom(BrowserAnimationsModule, BrowserModule),
    provideToastr({}),
  ],
}
