import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnDestroy, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxChartModule } from 'devextreme-angular/ui/chart';
import { DxLoadPanelModule } from 'devextreme-angular/ui/load-panel';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';

import {
  AnalyticsTableComponent,
  type AnalyticsColumn,
} from '../../components/analytics-table.component';
import { AssetDashboardStore } from './asset-dashboard.store';

type ViewMode = 'Chart' | 'Table';
type ChartType = 'bar' | 'line' | 'spline';

@Component({
  selector: 'ams-asset-dashboard',
  imports: [
    AnalyticsTableComponent,
    DatePipe,
    RouterLink,
    DxButtonModule,
    DxChartModule,
    DxLoadPanelModule,
    DxSelectBoxModule,
  ],
  providers: [AssetDashboardStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './asset-dashboard.page.html',
  styleUrl: './asset-dashboard.page.scss',
})
export class AssetDashboardPage implements OnInit, OnDestroy {
  protected readonly store = inject(AssetDashboardStore);
  protected readonly viewModes: ViewMode[] = ['Chart', 'Table'];
  protected readonly trendChartTypes: ChartType[] = ['bar', 'line', 'spline'];
  protected trendView: ViewMode = 'Chart';
  protected locationView: ViewMode = 'Chart';
  protected trendChartType: ChartType = 'bar';
  private refreshTimer: ReturnType<typeof setInterval> | undefined;

  protected readonly trendColumns: readonly AnalyticsColumn[] = [
    { dataField: 'period', caption: 'Month' },
    { dataField: 'added', caption: 'Assets added', dataType: 'number' },
    { dataField: 'verified', caption: 'Assets verified', dataType: 'number' },
  ];
  protected readonly valueColumns: readonly AnalyticsColumn[] = [
    { dataField: 'name', caption: 'Location / department' },
    { dataField: 'count', caption: 'Asset count', dataType: 'number' },
    { dataField: 'value', caption: 'Asset value', dataType: 'number', format: '₹#,##0' },
  ];
  protected readonly breakdownColumns: readonly AnalyticsColumn[] = [
    { dataField: 'name', caption: 'Category' },
    { dataField: 'count', caption: 'Assets', dataType: 'number' },
  ];
  protected readonly recentColumns: readonly AnalyticsColumn[] = [
    { dataField: 'assetNumber', caption: 'Asset number' },
    { dataField: 'assetName', caption: 'Asset name' },
    { dataField: 'status', caption: 'Status' },
    { dataField: 'location', caption: 'Location' },
    { dataField: 'createdOnUtc', caption: 'Created', dataType: 'datetime', format: 'dd MMM yyyy' },
  ];

  ngOnInit(): void {
    void this.store.load();
    this.refreshTimer = setInterval(() => void this.store.load(true), 30_000);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  protected setTrendView(value: string): void {
    if (value === 'Chart' || value === 'Table') this.trendView = value;
  }

  protected setLocationView(value: string): void {
    if (value === 'Chart' || value === 'Table') this.locationView = value;
  }

  protected setTrendChart(value: string): void {
    if (value === 'bar' || value === 'line' || value === 'spline') this.trendChartType = value;
  }

  protected formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(value);
  }

  protected formatNumber(value: number): string {
    return new Intl.NumberFormat('en-IN').format(value);
  }

  protected readonly currencyTooltip = (point: { value?: number; argumentText?: string }) => ({
    text: `${point.argumentText ?? ''}: ${this.formatCurrency(point.value ?? 0)}`,
  });
}
