import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { environment } from 'src/environments/environment';

type Provider = 'simulator' | 'paytr' | 'iyzico';

interface GatewaySettingsPayload {
	provider: Provider;
	paytrMerchantId?: string;
	paytrMerchantKey?: string;
	paytrMerchantSalt?: string;
	iyzicoApiKey?: string;
	iyzicoSecretKey?: string;
	iyzicoBaseUrl?: string;
}

@Component({
	selector: 'app-gateway-settings',
	templateUrl: './gateway-settings.component.html',
	standalone: true,
	imports: [CommonModule, ReactiveFormsModule]
})
export class GatewaySettingsComponent implements OnInit {
	form!: FormGroup;
	loading = false;
	saving = false;
	alertMessage = '';
	alertType: 'success' | 'danger' | '' = '';
	private readonly API_URL = environment.apiUrl;

	constructor(
		private fb: FormBuilder,
		private http: HttpClient,
		private router: Router
	) {}

	ngOnInit(): void {
		this.form = this.fb.group({
			provider: ['simulator', Validators.required],
			paytrMerchantId: [''],
			paytrMerchantKey: [''],
			paytrMerchantSalt: [''],
			iyzicoApiKey: [''],
			iyzicoSecretKey: [''],
			iyzicoBaseUrl: ['https://api.iyzipay.com']
		});
		this.load();
	}

	get provider(): Provider {
		return this.form.get('provider')?.value as Provider;
	}

	async load(): Promise<void> {
		this.loading = true;
		this.alertType = '';
		try {
			const payload = await this.http.get<GatewaySettingsPayload>(`${this.API_URL}/platform-settings/gateway`).toPromise();
			if (payload) {
				this.form.patchValue(payload);
			}
		} catch {
			this.showAlert('Ayarlar yüklenemedi.', 'danger');
		} finally {
			this.loading = false;
		}
	}

	async save(): Promise<void> {
		if (this.form.invalid) return;
		this.saving = true;
		this.alertType = '';
		const payload: GatewaySettingsPayload = this.form.value;
		try {
			await this.http.put(`${this.API_URL}/platform-settings/gateway`, payload).toPromise();
			this.showAlert('Ayarlar kaydedildi.', 'success');
		} catch {
			this.showAlert('Ayarlar kaydedilemedi.', 'danger');
		} finally {
			this.saving = false;
		}
	}

	private showAlert(msg: string, type: 'success' | 'danger'): void {
		this.alertMessage = msg;
		this.alertType = type;
		setTimeout(() => (this.alertType = ''), 4000);
	}
}


