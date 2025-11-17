import { Component, OnDestroy, WritableSignal, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { take } from 'rxjs';
import { ApiService, PaymentResponse, SubscriptionPlan, TenantResponse } from './api.service';

interface StepState {
  id: number;
  title: string;
  description: string;
}

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class AppComponent implements OnDestroy {
  private readonly fb = inject(FormBuilder);

  readonly steps: StepState[] = [
    { id: 1, title: 'Plan Seçimi', description: 'İhtiyacınıza uygun abonelik planını belirleyin.' },
    { id: 2, title: 'Firma Bilgileri', description: "De'Bakiim hesabınızı oluşturmak için bilgilerinizi girin." },
    { id: 3, title: 'Ödeme', description: 'Güvenli sanal POS ile ödemenizi gerçekleştirin.' },
    { id: 4, title: 'Kurulum Hazır', description: 'Hesap bilgileriniz ve lisans anahtarınız hazır.' }
  ];

  readonly currentStep: WritableSignal<number> = signal(1);
  readonly plans = signal<SubscriptionPlan[]>([]);
  readonly plansLoading = signal<boolean>(false);
  readonly plansError = signal<string | null>(null);
  readonly selectedPlan = signal<SubscriptionPlan | null>(null);
  readonly paymentResponse = signal<PaymentResponse | null>(null);
  readonly subscriptionResponse = signal<any | null>(null);
  readonly bankList = signal<string[]>([]);
  readonly paymentSettings = signal<{ iban: string; bankName?: string; accountHolder?: string } | null>(null);
  readonly installments = signal<number[]>([1]);
  readonly createTenantError = signal<string | null>(null);
  readonly paymentError = signal<string | null>(null);
  readonly subscriptionError = signal<string | null>(null);
  readonly createTenantLoading = signal<boolean>(false);
  readonly paymentLoading = signal<boolean>(false);
  readonly licensesAvailable = signal<boolean>(false);
  readonly cardPreview = signal({
    number: '•••• •••• •••• ••••',
    holder: 'KART SAHİBİ',
    expiry: 'AA/YY',
    cvv: '•••',
    brand: 'unknown' as CardBrand
  });
  readonly cardSide = signal<'front' | 'back'>('front');

  private subscriptionCleanup?: () => void;

  createdTenant: TenantResponse | null = null;

  readonly companyForm = this.fb.group({
    firmaAdi: ['', [Validators.required, Validators.minLength(3)]],
    email: ['', [Validators.required, Validators.email]],
    telefon: [''],
    adres: [''],
    vergiNo: ['']
  });

  readonly paymentForm = this.fb.group({
    cardHolder: ['', Validators.required],
    cardNumber: ['', [Validators.required, Validators.pattern(/^[0-9 ]{15,23}$/)]],
    expiryMonth: ['', [Validators.required, Validators.pattern(/^(0[1-9]|1[0-2])$/)]],
    expiryYear: ['', [Validators.required, Validators.pattern(/^[0-9]{2,4}$/)]],
    cvv: ['', [Validators.required, Validators.pattern(/^[0-9]{3,4}$/)]],
    installment: [1, Validators.required]
  });

  readonly currentYear = new Date().getFullYear();

  constructor(private readonly api: ApiService) {
    this.loadPlans();
    this.loadInstallments();
    this.loadBankList();
    this.loadPaymentSettings();
    const subscription = this.paymentForm.valueChanges.subscribe(() => this.updateCardPreview());
    this.subscriptionCleanup = () => subscription.unsubscribe();
    this.updateCardPreview();
  }

  ngOnDestroy(): void {
    this.subscriptionCleanup?.();
  }

  loadPlans(): void {
    this.plansLoading.set(true);
    this.plansError.set(null);

    this.api.getSubscriptionPlans()
      .pipe(take(1))
      .subscribe({
        next: (plans) => {
          const activePlans = plans.filter(plan => plan.aktif);
          this.plans.set(activePlans);
          this.plansLoading.set(false);
          this.licensesAvailable.set(activePlans.length > 0);
        },
        error: () => {
          this.plansLoading.set(false);
          this.plansError.set('Abonelik planları yüklenirken bir sorun oluştu. Lütfen daha sonra tekrar deneyin.');
        }
      });
  }

  private loadInstallments(): void {
    this.api.getVirtualPosInstallments()
      .pipe(take(1))
      .subscribe({
        next: (data) => {
          if (Array.isArray(data) && data.length) {
            this.installments.set(data);
            this.paymentForm.patchValue({ installment: data[0] });
          }
        },
        error: () => {
          // varsayılan 1 taksit ile devam edilir
        }
      });
  }

  private loadBankList(): void {
    this.api.getVirtualPosBanks()
      .pipe(take(1))
      .subscribe({
        next: ({ banks }) => this.bankList.set(banks ?? []),
        error: () => this.bankList.set([])
      });
  }

  private loadPaymentSettings(): void {
    this.api.getPaymentSettings()
      .pipe(take(1))
      .subscribe({
        next: (data) => {
          if (data?.iban) {
            this.paymentSettings.set({
              iban: data.iban,
              bankName: data.bankName ?? undefined,
              accountHolder: data.accountHolder ?? undefined
            });
          } else {
            this.paymentSettings.set(null);
          }
        },
        error: () => this.paymentSettings.set(null)
      });
  }

  selectPlan(plan: SubscriptionPlan): void {
    this.selectedPlan.set(plan);
    this.currentStep.set(2);
  }

  goBack(step: number): void {
    if (step < 1) {
      step = 1;
    }
    if (step === 1) {
      this.resetFlow(true);
    } else if (step === 2) {
      const installments = this.installments();
      this.paymentForm.reset({
        cardHolder: '',
        cardNumber: '',
        expiryMonth: '',
        expiryYear: '',
        cvv: '',
        installment: installments.length ? installments[0] : 1
      });
      this.paymentResponse.set(null);
      this.subscriptionResponse.set(null);
      this.paymentError.set(null);
      this.subscriptionError.set(null);
    }
    this.currentStep.set(step);
  }

  submitCompanyForm(): void {
    if (this.companyForm.invalid || !this.selectedPlan()) {
      this.companyForm.markAllAsTouched();
      return;
    }

    this.createTenantLoading.set(true);
    this.createTenantError.set(null);

    const payload = this.companyForm.getRawValue() as {
      firmaAdi: string;
      email: string;
      telefon?: string | null;
      adres?: string | null;
      vergiNo?: string | null;
    };

    this.api.createTenant({
      firmaAdi: payload.firmaAdi,
      email: payload.email,
      telefon: payload.telefon ?? undefined,
      adres: payload.adres ?? undefined,
      vergiNo: payload.vergiNo ?? undefined
    })
      .pipe(take(1))
      .subscribe({
        next: (tenant) => {
          this.createTenantLoading.set(false);
          this.createdTenant = tenant;
          this.currentStep.set(3);
        },
        error: (err) => {
          this.createTenantLoading.set(false);
          const message = err?.error?.detail ?? err?.error?.message ?? 'Firma kaydedilirken bir hata oluştu.';
          this.createTenantError.set(message);
        }
      });
  }

  submitPaymentForm(): void {
    if (this.paymentForm.invalid || !this.createdTenant || !this.selectedPlan()) {
      this.paymentForm.markAllAsTouched();
      return;
    }

    const plan = this.selectedPlan()!;
    if (plan.aylikUcret <= 0) {
      this.paymentResponse.set(null);
      this.createSubscription(null);
      return;
    }

    const value = this.paymentForm.value;
    const sanitizedCardNumber = (value.cardNumber ?? '').replace(/\s+/g, '');

    this.paymentLoading.set(true);
    this.paymentError.set(null);

    this.api.chargeVirtualPos({
      tenantId: this.createdTenant.id,
      planId: plan.id,
      tutar: plan.aylikUcret,
      paraBirimi: 'TRY',
      taksitSayisi: Number(value.installment ?? 1),
      kartNumarasi: sanitizedCardNumber,
      sonKullanmaAy: value.expiryMonth ?? '',
      sonKullanmaYil: value.expiryYear ?? '',
      cvv: value.cvv ?? '',
      kartSahibi: value.cardHolder ?? '',
      ucDSecure: true
    })
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          this.paymentLoading.set(false);
          this.paymentResponse.set(response);
          this.createSubscription(response.reference);
        },
        error: (err) => {
          this.paymentLoading.set(false);
          this.paymentError.set(err?.error?.message ?? 'Ödeme sırasında bir hata oluştu. Lütfen bilgilerinizi kontrol edip tekrar deneyin.');
        }
      });
  }

  private createSubscription(paymentReference: string | null): void {
    if (!this.createdTenant || !this.selectedPlan()) {
      return;
    }

    const plan = this.selectedPlan()!;
    const paymentSummary = this.paymentResponse();

    const payload: any = {
      tenantId: this.createdTenant.id,
      planId: plan.id,
      baslangicTarihi: new Date().toISOString(),
      bitisTarihi: null
    };

    if (paymentReference && paymentSummary) {
      payload.paymentReference = paymentReference;
      payload.odenenTutar = paymentSummary.chargedAmount;
      payload.taksitSayisi = paymentSummary.installment;
      payload.paraBirimi = paymentSummary.currency;
    }

    this.subscriptionError.set(null);

    this.api.createSubscription(payload)
      .pipe(take(1))
      .subscribe({
        next: (response) => {
          this.subscriptionResponse.set(response);
          this.currentStep.set(4);
        },
        error: (err) => {
          this.subscriptionError.set(err?.error?.detail ?? err?.error?.message ?? 'Abonelik oluşturulurken bir hata oluştu. Lütfen destek ekibiyle iletişime geçin.');
        }
      });
  }

  resetFlow(resetPlan = false): void {
    if (resetPlan) {
      this.selectedPlan.set(null);
    }
    this.currentStep.set(1);
    this.companyForm.reset();
    const installments = this.installments();
    this.paymentForm.reset({
      cardHolder: '',
      cardNumber: '',
      expiryMonth: '',
      expiryYear: '',
      cvv: '',
      installment: installments.length ? installments[0] : 1
    });
    this.createdTenant = null;
    this.paymentResponse.set(null);
    this.subscriptionResponse.set(null);
    this.createTenantError.set(null);
    this.paymentError.set(null);
    this.subscriptionError.set(null);
    this.cardSide.set('front');
    this.updateCardPreview();
  }

  formatPrice(value: number): string {
    return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY', maximumFractionDigits: 2 }).format(value);
  }

  maskedEmail(email?: string): string {
    if (!email) {
      return '';
    }
    const [local, domain] = email.split('@');
    if (!domain) {
      return email;
    }
    const maskedLocal = local.length <= 2 ? local : `${local[0]}***${local.slice(-1)}`;
    return `${maskedLocal}@${domain}`;
  }

  get finalLicenseKey(): string | undefined {
    return this.subscriptionResponse()?.licenseKey;
  }

  get adminCredentials() {
    return this.createdTenant?.adminUser;
  }

  onCvvFocus(focused: boolean): void {
    this.cardSide.set(focused ? 'back' : 'front');
  }

  private updateCardPreview(): void {
    const cardNumberRaw = this.paymentForm.get('cardNumber')?.value ?? '';
    const sanitizedNumber = cardNumberRaw.replace(/\D/g, '').slice(0, 19);
    const formattedNumber = this.formatCardNumber(sanitizedNumber);

    const holderRaw = (this.paymentForm.get('cardHolder')?.value ?? '').toString().toUpperCase();
    const holder = holderRaw.trim().length ? holderRaw : 'KART SAHİBİ';

    const month = this.paymentForm.get('expiryMonth')?.value ?? '';
    const year = this.paymentForm.get('expiryYear')?.value ?? '';
    const expiry = month && year ? `${month.padStart(2, '0')}/${year.slice(-2).padStart(2, '0')}` : 'AA/YY';

    const cvvRaw = this.paymentForm.get('cvv')?.value ?? '';
    const cvv = cvvRaw ? cvvRaw.replace(/\D/g, '').slice(0, 4).padEnd(3, '•') : '•••';

    const brand = this.detectCardBrand(sanitizedNumber);

    this.cardPreview.set({
      number: formattedNumber || '•••• •••• •••• ••••',
      holder,
      expiry,
      cvv,
      brand
    });
  }

  private formatCardNumber(value: string): string {
    if (!value) {
      return '';
    }
    const groups: string[] = [];
    for (let i = 0; i < value.length; i += 4) {
      groups.push(value.slice(i, i + 4));
    }
    return groups.join(' ').trim();
  }

  private detectCardBrand(value: string): CardBrand {
    if (value.startsWith('4')) {
      return 'visa';
    }
    if (/^(5[1-5]|2(2[2-9]|[3-6][0-9]|7[01]))/.test(value)) {
      return 'mastercard';
    }
    if (/^(9792|65|36|38)/.test(value)) {
      return 'troy';
    }
    if (/^3[47]/.test(value)) {
      return 'amex';
    }
    if (!value) {
      return 'unknown';
    }
    return 'unknown';
  }
}

type CardBrand = 'visa' | 'mastercard' | 'troy' | 'amex' | 'unknown';
