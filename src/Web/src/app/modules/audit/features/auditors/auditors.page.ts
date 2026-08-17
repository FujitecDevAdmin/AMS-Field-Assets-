import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
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

import { AuditorLocationsApi, type AuditorLocation } from '../../data/auditor-locations.api';
import { AuditorsApi, type CreatedAuditor } from '../../data/auditors.api';

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
  private readonly locationsApi = inject(AuditorLocationsApi);
  private readonly auditorsApi = inject(AuditorsApi);

  readonly auditors = signal<readonly AuditorRow[]>([]);
  readonly isAddPanelVisible = signal(false);
  readonly isLocationPanelVisible = signal(false);
  readonly locations = signal<AuditorLocation[]>([]);
  readonly selectedLocationIds = signal<number[]>([]);
  readonly newLocationName = signal('');
  readonly isSavingLocation = signal(false);
  readonly locationError = signal<string | null>(null);
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
    this.loadLocationsAndAuditors();
  }

  showAddAuditor(): void {
    this.resetAuditorForm();
    this.isAddPanelVisible.set(true);
    this.loadLocations();
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
    const branchIds = this.hasAllBranches() ? [] : [...this.selectedLocationIds()];
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

  showAddLocation(): void {
    this.newLocationName.set('');
    this.locationError.set(null);
    this.isLocationPanelVisible.set(true);
    this.loadLocations();
  }

  closeAddLocation(): void {
    this.isLocationPanelVisible.set(false);
  }

  saveLocation(): void {
    const locationName = this.newLocationName().trim();
    if (locationName.length === 0 || this.isSavingLocation()) {
      return;
    }

    this.isSavingLocation.set(true);
    this.locationError.set(null);
    this.locationsApi.create(locationName).subscribe({
      next: (location) => {
        this.locations.update((rows) =>
          [...rows, location].sort((left, right) =>
            left.locationName.localeCompare(right.locationName),
          ),
        );
        this.selectedLocationIds.update((ids) => [...ids, location.locationId]);
        this.isSavingLocation.set(false);
        this.closeAddLocation();
      },
      error: () => {
        this.locationError.set(
          'The location could not be saved. Check that it does not already exist.',
        );
        this.isSavingLocation.set(false);
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

  private loadLocations(): void {
    this.locationsApi.list().subscribe({
      next: (response) => this.locations.set([...response.rows]),
      error: () => this.locationError.set('Assigned locations could not be loaded.'),
    });
  }

  private loadLocationsAndAuditors(): void {
    this.locationsApi.list().subscribe({
      next: (response) => {
        this.locations.set([...response.rows]);
        this.loadAuditors();
      },
      error: () => {
        this.locationError.set('Assigned locations could not be loaded.');
        this.loadAuditors();
      },
    });
  }

  private loadAuditors(): void {
    this.auditorsApi.list().subscribe({
      next: (response) =>
        this.auditors.set(
          response.rows.map((auditor) => ({
            id: auditor.id,
            username: auditor.username,
            displayName: auditor.displayName,
            email: auditor.email,
            employeeId: auditor.employeeId,
            branchScope: auditor.hasAllBranches
              ? 'All branches'
              : this.locations()
                  .filter((location) => auditor.branchIds.includes(location.locationId))
                  .map((location) => location.locationName)
                  .join(', ') || auditor.branchIds.join(', '),
            isActive: auditor.isActive,
            isLocked: auditor.isLocked,
            mfaEnabled: auditor.mfaEnabled,
            lastLoginOnUtc: auditor.lastLoginOnUtc,
            completedVerifications: 0,
          })),
        ),
      error: (error) =>
        this.auditorFormError.set(
          this.readApiError(error, 'Auditor accounts could not be loaded.'),
        ),
    });
  }

  private completeAuditorCreation(created: CreatedAuditor, branchIds: readonly number[]): void {
    const branchScope = this.hasAllBranches()
      ? 'All branches'
      : this.locations()
          .filter((location) => branchIds.includes(location.locationId))
          .map((location) => location.locationName)
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
    this.selectedLocationIds.set([]);
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
