import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface SubscriptionPlan {
  id: number;
  planAdi: string;
  maxKullaniciSayisi: number;
  maxBayiSayisi: number;
  aylikUcret: number;
  aktif: boolean;
}

export interface TenantRequest {
  firmaAdi: string;
  email: string;
  telefon?: string;
  adres?: string;
  vergiNo?: string;
}

export interface TenantResponse {
  id: number;
  tenantKey: string;
  licenseKey?: string;
  adminUser?: {
    username: string;
    email: string;
    password: string;
  };
}

export interface PaymentRequest {
  tenantId: number;
  planId: number;
  tutar: number;
  paraBirimi: string;
  taksitSayisi: number;
  kartNumarasi: string;
  sonKullanmaAy: string;
  sonKullanmaYil: string;
  cvv: string;
  kartSahibi: string;
  ucDSecure: boolean;
}

export interface PaymentResponse {
  reference: string;
  status: string;
  bankName?: string;
  chargedAmount: number;
  commissionAmount: number;
  commissionRate: number;
  totalAmount: number;
  installment: number;
  currency: string;
  maskedCard: string;
  threeDSecure: boolean;
  message?: string;
  processedAt: string;
}

export interface SubscriptionRequest {
  tenantId: number;
  planId: number;
  baslangicTarihi: string;
  bitisTarihi?: string | null;
  paymentReference?: string;
  odenenTutar?: number;
  taksitSayisi?: number;
  paraBirimi?: string;
}

export interface SubscriptionResponse {
  subscription: {
    id: number;
    tenantId: number;
    planId: number;
    planAdi?: string;
    baslangicTarihi: string;
    bitisTarihi?: string | null;
    aktif: boolean;
    olusturmaTarihi: string;
    paymentReference?: string;
    chargedAmount?: number;
    commissionAmount?: number;
    paymentStatus?: string;
  };
  licenseKey?: string;
  message?: string;
}

@Injectable({
  providedIn: 'root'
})
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  getSubscriptionPlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${this.baseUrl}/subscription-plans/all`);
  }

  getVirtualPosBanks(): Observable<{ banks: string[] }> {
    return this.http.get<{ banks: string[] }>(`${this.baseUrl}/virtual-pos/banks`);
  }

  getVirtualPosInstallments(): Observable<number[]> {
    return this.http.get<number[]>(`${this.baseUrl}/virtual-pos/installments`);
  }

  getPaymentSettings(): Observable<{ iban: string; bankName?: string; accountHolder?: string; updatedAt?: string }> {
    return this.http.get<{ iban: string; bankName?: string; accountHolder?: string; updatedAt?: string }>(
      `${this.baseUrl}/platform-settings/payment`
    );
  }

  chargeVirtualPos(payload: PaymentRequest): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(`${this.baseUrl}/virtual-pos/charge`, payload);
  }

  createTenant(payload: TenantRequest): Observable<TenantResponse> {
    return this.http.post<TenantResponse>(`${this.baseUrl}/tenants`, {
      FirmaAdi: payload.firmaAdi,
      Email: payload.email,
      Telefon: payload.telefon,
      Adres: payload.adres,
      VergiNo: payload.vergiNo
    });
  }

  createSubscription(payload: SubscriptionRequest): Observable<SubscriptionResponse> {
    return this.http.post<SubscriptionResponse>(`${this.baseUrl}/subscriptions`, payload);
  }
}

