import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxCheckBoxModule } from 'devextreme-angular/ui/check-box';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxTextAreaModule } from 'devextreme-angular/ui/text-area';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';

import { MyAuditsApi, type MyAudit, type MyAuditAsset } from '../../data/my-audits.api';

@Component({
  selector: 'ams-my-audits-page',
  imports: [DxButtonModule, DxCheckBoxModule, DxDataGridModule, DxPopupModule, DxSelectBoxModule, DxTextAreaModule, DxTextBoxModule],
  templateUrl: './my-audits.page.html',
  styleUrl: './my-audits.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyAuditsPage implements OnInit {
  private readonly api = inject(MyAuditsApi);
  readonly audits = signal<MyAudit[]>([]);
  readonly selectedAuditId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedAudit = computed(() => this.audits().find((audit) => audit.id === this.selectedAuditId()) ?? null);
  readonly pendingCount = computed(() => this.selectedAudit()?.assets.filter((asset) => !asset.isVerified).length ?? 0);
  readonly selectedAsset = signal<MyAuditAsset | null>(null);
  readonly conditionOptions = ['Good', 'MinorDamage', 'Damaged', 'NotWorking', 'Missing'];
  readonly condition = signal('Good');
  readonly serialVerified = signal(false);
  readonly scannedQr = signal('');
  readonly remarks = signal('');
  readonly saving = signal(false);

  ngOnInit(): void { this.load(); }

  openVerification(asset: MyAuditAsset): void {
    if (asset.isVerified || !this.selectedAudit()?.isActive) return;
    this.selectedAsset.set(asset);
    this.condition.set('Good');
    this.serialVerified.set(false);
    this.scannedQr.set('');
    this.remarks.set('');
    this.error.set(null);
  }

  closeVerification(): void { this.selectedAsset.set(null); }

  submitVerification(): void {
    const audit = this.selectedAudit();
    const asset = this.selectedAsset();
    if (audit === null || asset === null || this.saving()) return;
    this.saving.set(true);
    this.api.verify({
      cycleId: audit.id,
      assetId: asset.id,
      clientCaptureId: crypto.randomUUID(),
      workingCondition: this.condition(),
      serialVerified: this.serialVerified(),
      scannedQrValue: this.scannedQr().trim() || null,
      remarks: this.remarks().trim() || null,
    }).subscribe({
      next: () => { this.saving.set(false); this.closeVerification(); this.load(audit.id); },
      error: (error) => { this.error.set(error?.error?.detail ?? error?.error?.title ?? 'Verification could not be saved.'); this.saving.set(false); },
    });
  }

  private load(preferredId?: number): void {
    this.loading.set(true);
    this.api.list().subscribe({
      next: (response) => {
        this.audits.set([...response.rows]);
        const selected = preferredId ?? this.selectedAuditId();
        this.selectedAuditId.set(response.rows.some((audit) => audit.id === selected) ? selected : response.rows[0]?.id ?? null);
        this.loading.set(false);
      },
      error: (error) => { this.error.set(error?.error?.detail ?? 'Assigned audits could not be loaded.'); this.loading.set(false); },
    });
  }
}
