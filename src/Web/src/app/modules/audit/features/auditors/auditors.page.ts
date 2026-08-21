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
import { DxCheckBoxModule } from 'devextreme-angular/ui/check-box';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import type { DxDataGridTypes } from 'devextreme-angular/ui/data-grid';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxTagBoxModule } from 'devextreme-angular/ui/tag-box';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';
import { forkJoin } from 'rxjs';

import { AuditorBranchesApi, type AuditorBranch } from '../../data/auditor-branches.api';
import {
  AuditorsApi,
  type AuditorVerificationActivityResponse,
  type CreatedAuditor,
} from '../../data/auditors.api';

interface AuditorRow {
  readonly id: number;
  readonly username: string;
  readonly displayName: string;
  readonly email: string | null;
  readonly employeeId: number | null;
  readonly branchScope: string;
  readonly isActive: boolean;
  readonly isLocked: boolean;
  readonly mfaEnabled: boolean;
  readonly lastLoginOnUtc: string | null;
  readonly completedVerifications: number;
}

@Component({
  selector: 'ams-auditors-page',
  imports: [
    DatePipe,
    DxButtonModule,
    DxCheckBoxModule,
    DxDataGridModule,
    DxPopupModule,
    DxSelectBoxModule,
    DxTagBoxModule,
    DxTextBoxModule,
  ],
  templateUrl: './auditors.page.html',
  styleUrl: './auditors.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditorsPage implements OnInit {
  private readonly branchesApi = inject(AuditorBranchesApi);
  private readonly auditorsApi = inject(AuditorsApi);
  private readonly route = inject(ActivatedRoute);

  readonly auditors = signal<readonly AuditorRow[]>([]);
  readonly isAddPanelVisible = signal(false);
  readonly isBranchPanelVisible = signal(false);
  readonly branches = signal<AuditorBranch[]>([]);
  readonly selectedBranchIds = signal<number[]>([]);
  readonly newBranchCode = signal('');
  readonly newBranchName = signal('');
  readonly newBranchTimeZone = signal('India Standard Time');
  readonly isSavingBranch = signal(false);
  readonly branchError = signal<string | null>(null);
  readonly displayName = signal('');
  readonly username = signal('');
  readonly email = signal('');
  readonly employeeId = signal('');
  readonly temporaryPassword = signal('');
  readonly hasAllBranches = signal(false);
  readonly requireMfa = signal(true);
  readonly isCreatingAuditor = signal(false);
  readonly auditorFormError = signal<string | null>(null);
  readonly search = signal('');
  readonly statusOptions = ['All accounts', 'Active', 'Inactive', 'Locked'];
  readonly selectedStatus = signal(this.statusOptions[0]);
  readonly selectedAuditor = signal<AuditorRow | null>(null);
  readonly auditorActivity = signal<AuditorVerificationActivityResponse | null>(null);
  readonly isViewPanelVisible = signal(false);
  readonly isLoadingActivity = signal(false);
  readonly activityError = signal<string | null>(null);
  readonly activeCount = computed(
    () => this.auditors().filter((auditor) => auditor.isActive).length,
  );
  readonly lockedCount = computed(
    () => this.auditors().filter((auditor) => auditor.isLocked).length,
  );
  readonly mfaCount = computed(
    () => this.auditors().filter((auditor) => auditor.mfaEnabled).length,
  );
  readonly verificationCount = computed(() =>
    this.auditors().reduce((total, auditor) => total + auditor.completedVerifications, 0),
  );
  readonly filteredAuditors = computed(() => {
    const query = this.search().trim().toLocaleLowerCase();
    const status = this.selectedStatus();
    return this.auditors().filter(auditor => {
      const matchesQuery = query.length === 0 || [
        auditor.displayName,
        auditor.username,
        auditor.email ?? '',
        auditor.branchScope,
        String(auditor.employeeId ?? ''),
      ].some(value => value.toLocaleLowerCase().includes(query));
      const matchesStatus = status === 'All accounts'
        || (status === 'Active' && auditor.isActive && !auditor.isLocked)
        || (status === 'Inactive' && !auditor.isActive)
        || (status === 'Locked' && auditor.isLocked);
      return matchesQuery && matchesStatus;
    });
  });
  readonly canCreateAuditor = computed(() => {
    const username = this.username().trim();
    const email = this.email().trim();
    return (
      this.displayName().trim().length > 0 &&
      /^[A-Za-z0-9._@-]+$/.test(username) &&
      this.temporaryPassword().length >= 12 &&
      (email.length === 0 || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) &&
      !this.auditors().some(
        (auditor) => auditor.username.toLocaleLowerCase() === username.toLocaleLowerCase(),
      ) &&
      !this.isCreatingAuditor()
    );
  });

  ngOnInit(): void {
    this.search.set(this.route.snapshot.queryParamMap.get('search')?.trim() ?? '');
    this.loadBranchesAndAuditors();
  }

  showAddAuditor(): void {
    this.resetAuditorForm();
    this.isAddPanelVisible.set(true);
    this.loadBranches();
  }

  viewAuditor(auditor: AuditorRow): void {
    this.selectedAuditor.set(auditor);
    this.auditorActivity.set(null);
    this.activityError.set(null);
    this.isViewPanelVisible.set(true);
    this.isLoadingActivity.set(true);
    this.auditorsApi.verificationActivity(auditor.id).subscribe({
      next: (response) => {
        this.auditorActivity.set(response);
        this.isLoadingActivity.set(false);
      },
      error: (error) => {
        this.activityError.set(
          this.readApiError(error, 'Auditor verification activity could not be loaded.'),
        );
        this.isLoadingActivity.set(false);
      },
    });
  }

  closeAuditorView(): void {
    this.isViewPanelVisible.set(false);
  }

  closeAddAuditor(): void {
    this.isAddPanelVisible.set(false);
  }

  createAuditor(): void {
    if (
      this.auditors().some(
        (auditor) =>
          auditor.username.toLocaleLowerCase() === this.username().trim().toLocaleLowerCase(),
      )
    ) {
      this.auditorFormError.set('That username is already in use. Choose a different username.');
      return;
    }

    if (!this.canCreateAuditor()) {
      return;
    }

    const employeeIdText = this.employeeId().trim();
    const employeeId = employeeIdText.length > 0 ? Number(employeeIdText) : null;
    if (employeeId !== null && (!Number.isInteger(employeeId) || employeeId <= 0)) {
      this.auditorFormError.set('Employee ID must be a positive number.');
      return;
    }

    this.isCreatingAuditor.set(true);
    this.auditorFormError.set(null);
    const branchIds = this.hasAllBranches() ? [] : [...this.selectedBranchIds()];
    this.auditorsApi
      .create({
        username: this.username().trim(),
        displayName: this.displayName().trim(),
        password: this.temporaryPassword(),
        email: this.email().trim() || null,
        employeeId,
        hasAllBranches: this.hasAllBranches(),
        branchIds,
        primaryBranchId: branchIds[0] ?? null,
        requireMfa: this.requireMfa(),
      })
      .subscribe({
        next: (created) => this.completeAuditorCreation(created, branchIds),
        error: (error) => {
          this.auditorFormError.set(
            this.readApiError(error, 'The auditor account could not be created.'),
          );
          this.isCreatingAuditor.set(false);
        },
      });
  }

  showAddBranch(): void {
    this.newBranchCode.set('');
    this.newBranchName.set('');
    this.newBranchTimeZone.set('India Standard Time');
    this.branchError.set(null);
    this.isBranchPanelVisible.set(true);
    this.loadBranches();
  }

  closeAddBranch(): void {
    this.isBranchPanelVisible.set(false);
  }

  saveBranch(): void {
    const branchCode = this.newBranchCode().trim().toUpperCase();
    const branchName = this.newBranchName().trim();
    const timeZoneId = this.newBranchTimeZone().trim();
    if (branchCode.length === 0 || branchName.length === 0 || timeZoneId.length === 0 || this.isSavingBranch()) {
      return;
    }

    this.isSavingBranch.set(true);
    this.branchError.set(null);
    this.branchesApi.create(branchCode, branchName, timeZoneId).subscribe({
      next: (branch) => {
        this.branches.update((rows) =>
          [...rows, branch].sort((left, right) =>
            left.branchName.localeCompare(right.branchName),
          ),
        );
        this.selectedBranchIds.update((ids) => [...ids, branch.id]);
        this.isSavingBranch.set(false);
        this.closeAddBranch();
      },
      error: () => {
        this.branchError.set(
          'The branch could not be saved. Check that it does not already exist.',
        );
        this.isSavingBranch.set(false);
      },
    });
  }

  async exportTable(event: DxDataGridTypes.ExportingEvent, fileName: string): Promise<void> {
    event.cancel = true;
    const workbook = new Workbook();
    const worksheet = workbook.addWorksheet('Report');
    await exportDataGrid({ component: event.component, worksheet, autoFilterEnabled: true });
    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(
      new Blob([new Uint8Array(buffer)], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      }),
      `${fileName}.xlsx`,
    );
  }

  private loadBranches(): void {
    this.branchesApi.list().subscribe({
      next: (response) => this.branches.set([...response.rows]),
      error: () => this.branchError.set('Assigned branches could not be loaded.'),
    });
  }

  private loadBranchesAndAuditors(): void {
    this.branchesApi.list().subscribe({
      next: (response) => {
        this.branches.set([...response.rows]);
        this.loadAuditors();
      },
      error: () => {
        this.branchError.set('Assigned branches could not be loaded.');
        this.loadAuditors();
      },
    });
  }

  private loadAuditors(): void {
    forkJoin({
      accounts: this.auditorsApi.list(),
      counts: this.auditorsApi.verificationCounts(),
    }).subscribe({
      next: ({ accounts, counts }) => {
        const verificationCounts = new Map(
          counts.rows.map((row) => [row.auditorUserId, row.verifiedAssetCount]),
        );
        this.auditors.set(
          accounts.rows.map((auditor) => ({
            id: auditor.id,
            username: auditor.username,
            displayName: auditor.displayName,
            email: auditor.email,
            employeeId: auditor.employeeId,
            branchScope: auditor.hasAllBranches
              ? 'All branches'
              : this.branches()
                  .filter((branch) => auditor.branchIds.includes(branch.id))
                  .map((branch) => branch.branchName)
                  .join(', ') || auditor.branchIds.join(', '),
            isActive: auditor.isActive,
            isLocked: auditor.isLocked,
            mfaEnabled: auditor.mfaEnabled,
            lastLoginOnUtc: auditor.lastLoginOnUtc,
            completedVerifications: verificationCounts.get(auditor.id) ?? 0,
          })),
        );
      },
      error: (error) =>
        this.auditorFormError.set(
          this.readApiError(error, 'Auditor accounts could not be loaded.'),
        ),
    });
  }

  private completeAuditorCreation(created: CreatedAuditor, branchIds: readonly number[]): void {
    const branchScope = this.hasAllBranches()
      ? 'All branches'
      : this.branches()
          .filter((branch) => branchIds.includes(branch.id))
          .map((branch) => branch.branchName)
          .join(', ') || 'No branches';
    this.auditors.update((rows) => [
      ...rows,
      {
        id: created.id,
        username: created.username,
        displayName: created.displayName,
        email: this.email().trim() || null,
        employeeId: this.employeeId().trim() ? Number(this.employeeId()) : null,
        branchScope,
        isActive: true,
        isLocked: false,
        mfaEnabled: false,
        lastLoginOnUtc: null,
        completedVerifications: 0,
      },
    ]);
    this.isCreatingAuditor.set(false);
    this.closeAddAuditor();
  }

  private resetAuditorForm(): void {
    this.displayName.set('');
    this.username.set('');
    this.email.set('');
    this.employeeId.set('');
    this.temporaryPassword.set('');
    this.hasAllBranches.set(false);
    this.selectedBranchIds.set([]);
    this.auditorFormError.set(null);
  }

  private readApiError(error: unknown, fallback: string): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const body = (error as { error?: { detail?: string; title?: string } }).error;
      return body?.detail ?? body?.title ?? fallback;
    }
    return fallback;
  }
}
