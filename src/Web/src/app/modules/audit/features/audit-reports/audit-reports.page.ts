import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxLoadPanelModule } from 'devextreme-angular/ui/load-panel';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { firstValueFrom } from 'rxjs';

import { AssetsApi, type AssetBranch } from '../../../assets/data/assets.api';
import { AuditsApi, type VerificationReportRow } from '../../data/audits.api';

@Component({
  selector: 'ams-audit-reports-page',
  imports: [DxButtonModule, DxDataGridModule, DxLoadPanelModule, DxSelectBoxModule, DxTextBoxModule],
  templateUrl: './audit-reports.page.html',
  styleUrl: './audit-reports-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditReportsPage implements OnInit {
  private readonly auditsApi = inject(AuditsApi);
  private readonly assetsApi = inject(AssetsApi);

  readonly rows = signal<readonly VerificationReportRow[]>([]);
  readonly total = signal(0);
  readonly exceptions = signal(0);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly branches = signal<AssetBranch[]>([]);
  readonly branchId = signal<number | null>(null);
  readonly location = signal<string | null>(null);
  readonly search = signal('');
  readonly locations = computed(() => [...new Set(
    this.rows().map(row => row.locationName).filter((value): value is string => !!value),
  )].sort((a, b) => a.localeCompare(b)));

  ngOnInit(): void {
    void this.load();
    void this.loadBranches();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const response = await firstValueFrom(this.auditsApi.verificationReport({
        branchId: this.branchId() ?? undefined,
        location: this.location() ?? undefined,
        search: this.search(),
      }));
      this.rows.set(response.rows);
      this.total.set(response.totalCount);
      this.exceptions.set(response.exceptionCount);
    } catch {
      this.error.set('Audit verification records could not be loaded. Check API access and restart the API.');
    } finally {
      this.loading.set(false);
    }
  }

  clearFilters(): void {
    this.branchId.set(null);
    this.location.set(null);
    this.search.set('');
    void this.load();
  }

  private async loadBranches(): Promise<void> {
    try {
      const response = await firstValueFrom(this.assetsApi.listBranches());
      this.branches.set([...response.rows]);
    } catch {
      this.branches.set([]);
    }
  }
}
