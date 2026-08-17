import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit } from '@angular/core';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxChartModule } from 'devextreme-angular/ui/chart';
import { DxLoadPanelModule } from 'devextreme-angular/ui/load-panel';
import { DxPieChartModule } from 'devextreme-angular/ui/pie-chart';

import {
  AnalyticsTableComponent,
  type AnalyticsColumn,
} from '../../components/analytics-table.component';
import { AssetDashboardStore } from '../dashboard/asset-dashboard.store';

@Component({
  selector: 'ams-asset-reports',
  imports: [
    AnalyticsTableComponent,
    DatePipe,
    DxButtonModule,
    DxChartModule,
    DxLoadPanelModule,
    DxPieChartModule,
  ],
  providers: [AssetDashboardStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './asset-reports.page.html',
  styleUrl: './asset-reports.page.scss',
})
export class AssetReportsPage implements OnInit {
  protected readonly store = inject(AssetDashboardStore);
  protected readonly coverageRows = computed(() => {
    const data = this.store.data();
    return data
      ? [
          { name: 'Verified', count: data.verifiedAssets },
          { name: 'Pending verification', count: data.pendingVerification },
        ]
      : [];
  });

  protected readonly trendColumns: readonly AnalyticsColumn[] = [
    { dataField: 'period', caption: 'Month' },
    { dataField: 'added', caption: 'Assets added', dataType: 'number' },
    { dataField: 'verified', caption: 'Verified', dataType: 'number' },
  ];
  protected readonly breakdownColumns: readonly AnalyticsColumn[] = [
    { dataField: 'name', caption: 'Category' },
    { dataField: 'count', caption: 'Asset count', dataType: 'number' },
  ];
  protected readonly valueColumns: readonly AnalyticsColumn[] = [
    { dataField: 'name', caption: 'Category' },
    { dataField: 'count', caption: 'Asset count', dataType: 'number' },
    { dataField: 'value', caption: 'Asset value', dataType: 'number', format: '₹#,##0' },
  ];

  ngOnInit(): void {
    void this.store.load();
  }

  protected formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(value);
  }

  protected readonly currencyTooltip = (point: { value?: number; argumentText?: string }) => ({
    text: `${point.argumentText ?? ''}: ${this.formatCurrency(point.value ?? 0)}`,
  });
}
