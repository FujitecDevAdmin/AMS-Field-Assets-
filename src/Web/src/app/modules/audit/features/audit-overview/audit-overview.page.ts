import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxDateBoxModule } from 'devextreme-angular/ui/date-box';
import { DxLoadPanelModule } from 'devextreme-angular/ui/load-panel';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { DxTagBoxModule } from 'devextreme-angular/ui/tag-box';

import { AuditorBranchesApi, type AuditorBranch } from '../../data/auditor-branches.api';
import { type AuditorAccount, AuditorsApi } from '../../data/auditors.api';
import { type AuditAsset, type AuditCycle, AuditsApi } from '../../data/audits.api';

type AuditStatus = 'Upcoming' | 'Active' | 'Completed' | 'Closed';

interface AuditRow extends AuditCycle {
  readonly status: AuditStatus;
  readonly pendingCount: number;
  readonly progressPercent: number;
}

function todayIso(): string {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, '0');
  const day = String(now.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function dateValueToIso(value: unknown): string {
  if (typeof value === 'string') {
    return value.slice(0, 10);
  }
  if (value instanceof Date) {
    const year = value.getFullYear();
    const month = String(value.getMonth() + 1).padStart(2, '0');
    const day = String(value.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
  return '';
}

@Component({
  selector: 'ams-audit-overview-page',
  imports: [
    DxButtonModule,
    DxDataGridModule,
    DxDateBoxModule,
    DxLoadPanelModule,
    DxPopupModule,
    DxSelectBoxModule,
    DxTextBoxModule,
    DxTagBoxModule,
    DatePipe,
  ],
  templateUrl: './audit-overview.page.html',
  styleUrl: './audit-overview.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditOverviewPage implements OnInit {
  private readonly auditsApi = inject(AuditsApi);
  private readonly branchesApi = inject(AuditorBranchesApi);
  private readonly auditorsApi = inject(AuditorsApi);
  private readonly route = inject(ActivatedRoute);

  readonly audits = signal<readonly AuditRow[]>([]);
  readonly loading = signal(false);
  readonly pageError = signal<string | null>(null);
  readonly search = signal('');
  readonly statusOptions: string[] = ['All statuses', 'Upcoming', 'Active', 'Completed', 'Closed'];
  readonly selectedStatus = signal('All statuses');
  readonly isAddPanelVisible = signal(false);
  readonly auditName = signal('');
  readonly startDate = signal(todayIso());
  readonly endDate = signal('');
  readonly isCreating = signal(false);
  readonly formError = signal<string | null>(null);
  readonly branches = signal<AuditorBranch[]>([]);
  readonly auditors = signal<AuditorAccount[]>([]);
  readonly branchId = signal<number | null>(null);
  readonly selectedAuditorIds = signal<number[]>([]);
  readonly selectedLocationIds = signal<number[]>([]);
  readonly totalAssetCount = signal<number | null>(null);
  readonly isCountingAssets = signal(false);
  readonly assetCountError = signal<string | null>(null);
  readonly selectedAudit = signal<AuditRow | null>(null);
  readonly isDetailsVisible = signal(false);
  readonly showingAssets = signal(false);
  readonly auditAssets = signal<readonly AuditAsset[]>([]);
  readonly assetsLoading = signal(false);
  readonly detailsError = signal<string | null>(null);
  readonly isAddAuditorVisible = signal(false);
  readonly additionalAuditorIds = signal<number[]>([]);
  readonly isAddingAuditors = signal(false);
  readonly addAuditorError = signal<string | null>(null);
  private assetCountRequestVersion = 0;
  readonly eligibleAuditors = computed(() =>
    this.auditors().filter((auditor) => auditor.isActive && !auditor.isLocked),
  );
  readonly selectedBranchName = computed(() => {
    const audit = this.selectedAudit();
    return this.branches().find((branch) => branch.id === audit?.branchId)?.branchName
      ?? (audit === null ? '' : `Branch ${audit.branchId}`);
  });
  readonly selectedAuditorNames = computed(() => {
    const ids = new Set(this.selectedAudit()?.auditorUserIds ?? []);
    return this.auditors().filter((auditor) => ids.has(auditor.id)).map((auditor) => auditor.displayName);
  });
  readonly availableAdditionalAuditors = computed(() => {
    const assigned = new Set(this.selectedAudit()?.auditorUserIds ?? []);
    return this.eligibleAuditors().filter(auditor => !assigned.has(auditor.id));
  });
  readonly canAddAuditors = computed(() =>
    this.selectedAudit()?.isActive === true
      && this.additionalAuditorIds().length > 0
      && !this.isAddingAuditors(),
  );

  readonly filteredAudits = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const status = this.selectedStatus();
    return this.audits().filter(
      (audit) =>
        (query.length === 0 || audit.cycleName.toLocaleLowerCase().includes(query)) &&
        (status === 'All statuses' || audit.status === status),
    );
  });
  readonly activeCount = computed(
    () => this.audits().filter((audit) => audit.status === 'Active').length,
  );
  readonly completedCount = computed(
    () =>
      this.audits().filter((audit) => audit.status === 'Completed' || audit.status === 'Closed')
        .length,
  );
  readonly verifiedCount = computed(() =>
    this.audits().reduce((total, audit) => total + audit.verifiedCount, 0),
  );
  readonly exceptionCount = computed(() =>
    this.audits().reduce((total, audit) => total + audit.exceptionCount, 0),
  );
  readonly canCreate = computed(
    () =>
      this.auditName().trim().length > 0 &&
      this.startDate().length > 0 &&
      this.branchId() !== null &&
      this.selectedAuditorIds().length > 0 &&
      (this.endDate().length === 0 || this.endDate() >= this.startDate()) &&
      !this.isCreating(),
  );

  ngOnInit(): void {
    this.search.set(this.route.snapshot.queryParamMap.get('search')?.trim() ?? '');
    this.loadAudits();
  }

  showAddAudit(): void {
    this.auditName.set('');
    this.startDate.set(todayIso());
    this.endDate.set('');
    this.formError.set(null);
    this.branchId.set(null);
    this.selectedAuditorIds.set([]);
    this.selectedLocationIds.set([]);
    this.totalAssetCount.set(null);
    this.isCountingAssets.set(false);
    this.assetCountError.set(null);
    this.isAddPanelVisible.set(true);
    this.loadFormLookups();
  }

  showAuditDetails(audit: AuditRow): void {
    this.selectedAudit.set(audit);
    this.showingAssets.set(false);
    this.auditAssets.set([]);
    this.detailsError.set(null);
    this.isDetailsVisible.set(true);
    this.loadFormLookups();
  }

  closeAuditDetails(): void {
    this.isDetailsVisible.set(false);
    this.showingAssets.set(false);
  }

  showAddAuditors(): void {
    if (this.selectedAudit()?.isActive !== true) return;
    this.additionalAuditorIds.set([]);
    this.addAuditorError.set(null);
    this.isAddAuditorVisible.set(true);
  }

  closeAddAuditors(): void {
    if (this.isAddingAuditors()) return;
    this.isAddAuditorVisible.set(false);
  }

  addAuditors(): void {
    const audit = this.selectedAudit();
    if (audit === null || !this.canAddAuditors()) return;
    this.isAddingAuditors.set(true);
    this.addAuditorError.set(null);
    this.auditsApi.addAuditors(audit.id, this.additionalAuditorIds()).subscribe({
      next: response => {
        const updated = { ...audit, auditorUserIds: response.auditorUserIds };
        this.selectedAudit.set(updated);
        this.audits.update(rows => rows.map(row => row.id === audit.id ? updated : row));
        this.isAddingAuditors.set(false);
        this.isAddAuditorVisible.set(false);
      },
      error: error => {
        this.addAuditorError.set(this.readApiError(error, 'The auditors could not be added.'));
        this.isAddingAuditors.set(false);
      },
    });
  }

  closeAuditAssets(): void {
    this.showingAssets.set(false);
  }

  viewAuditAssets(): void {
    const audit = this.selectedAudit();
    if (audit === null || this.assetsLoading()) return;
    this.showingAssets.set(true);
    this.loadAuditAssets(audit.id);
  }

  refreshAuditAssets(): void {
    const audit = this.selectedAudit();
    if (audit === null || this.assetsLoading()) return;
    this.loadAuditAssets(audit.id);
  }

  private loadAuditAssets(auditId: number): void {
    this.assetsLoading.set(true);
    this.detailsError.set(null);
    this.auditsApi.listAssets(auditId).subscribe({
      next: (response) => {
        this.auditAssets.set([...response.rows]);
        this.assetsLoading.set(false);
      },
      error: (error) => {
        this.detailsError.set(this.readApiError(error, 'The assets for this audit could not be loaded.'));
        this.assetsLoading.set(false);
      },
    });
  }

  closeAddAudit(): void {
    this.assetCountRequestVersion++;
    this.isAddPanelVisible.set(false);
  }

  setStartDate(value: unknown): void {
    this.startDate.set(dateValueToIso(value));
  }

  setEndDate(value: unknown): void {
    this.endDate.set(dateValueToIso(value));
  }

  setAuditBranch(value: number | null): void {
    this.branchId.set(value);
    this.setAuditLocations(value === null ? [] : [value]);
  }

  private setAuditLocations(values: readonly number[]): void {
    const locationIds = [...(values ?? [])];
    this.selectedLocationIds.set(locationIds);
    const requestVersion = ++this.assetCountRequestVersion;
    this.assetCountError.set(null);

    if (locationIds.length === 0) {
      this.totalAssetCount.set(null);
      this.isCountingAssets.set(false);
      return;
    }

    this.isCountingAssets.set(true);
    this.auditsApi.calculateAssetCount(locationIds).subscribe({
      next: (response) => {
        if (requestVersion !== this.assetCountRequestVersion) {
          return;
        }
        this.totalAssetCount.set(response.totalAssetCount);
        this.isCountingAssets.set(false);
      },
      error: (error) => {
        if (requestVersion !== this.assetCountRequestVersion) {
          return;
        }
        this.totalAssetCount.set(null);
        this.assetCountError.set(this.readApiError(error, 'The asset count could not be calculated.'));
        this.isCountingAssets.set(false);
      },
    });
  }

  createAudit(): void {
    if (!this.canCreate()) {
      return;
    }

    this.isCreating.set(true);
    this.formError.set(null);
    this.auditsApi
      .create({
        cycleName: this.auditName().trim(),
        branchId: this.branchId() ?? 0,
        startDate: this.startDate(),
        endDate: this.endDate() || null,
        auditorUserIds: this.selectedAuditorIds(),
        locationBranchIds: this.selectedLocationIds(),
      })
      .subscribe({
        next: () => {
          this.isCreating.set(false);
          this.closeAddAudit();
          this.loadAudits();
        },
        error: (error) => {
          this.formError.set(this.readApiError(error, 'The audit could not be created.'));
          this.isCreating.set(false);
        },
      });
  }

  private loadAudits(): void {
    this.loading.set(true);
    this.pageError.set(null);
    this.auditsApi.list().subscribe({
      next: (response) => {
        this.audits.set(
          response.rows.map((audit) => ({
            ...audit,
            status: this.statusFor(audit),
            pendingCount: Math.max(audit.totalAssetCount - audit.verifiedCount, 0),
            progressPercent:
              audit.totalAssetCount === 0
                ? 0
                : Math.min(Math.round((audit.verifiedCount / audit.totalAssetCount) * 100), 100),
          })),
        );
        this.loading.set(false);
      },
      error: (error) => {
        this.pageError.set(this.readApiError(error, 'Audits could not be loaded.'));
        this.loading.set(false);
      },
    });
  }

  private loadFormLookups(): void {
    this.branchesApi.listForAudit().subscribe({
      next: (response) => this.branches.set([...response.rows]),
      error: () => this.formError.set('Branch Master could not be loaded.'),
    });
    this.auditorsApi.list().subscribe({
      next: (response) => this.auditors.set([...response.rows]),
      error: () => this.formError.set('Auditor Master could not be loaded.'),
    });
  }

  private statusFor(audit: AuditCycle): AuditStatus {
    if (audit.closedOnUtc !== null) {
      return 'Closed';
    }
    if (audit.isActive) {
      return 'Active';
    }
    if (audit.startDate > todayIso()) {
      return 'Upcoming';
    }
    return 'Completed';
  }

  private readApiError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const body = (error as { error?: { detail?: string; title?: string } }).error;
      return body?.detail ?? body?.title ?? fallback;
    }
    return fallback;
  }
}
