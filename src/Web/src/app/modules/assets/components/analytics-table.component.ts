import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import type { DxDataGridTypes } from 'devextreme-angular/ui/data-grid';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { Workbook } from 'exceljs';
import { saveAs } from 'file-saver';

export interface AnalyticsColumn {
  readonly dataField: string;
  readonly caption: string;
  readonly dataType?: 'string' | 'number' | 'date' | 'datetime' | 'boolean';
  readonly format?: string;
}

@Component({
  selector: 'ams-analytics-table',
  imports: [DxDataGridModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dx-data-grid
      class="analytics-table"
      [dataSource]="$any(rows)"
      [showBorders]="true"
      [rowAlternationEnabled]="true"
      [columnAutoWidth]="true"
      [hoverStateEnabled]="true"
      (onExporting)="exportExcel($event)"
    >
      <dxo-paging [pageSize]="pageSize" />
      <dxo-pager
        [visible]="rows.length > pageSize"
        [showPageSizeSelector]="true"
        [allowedPageSizes]="[5, 10, 20, 50]"
        [showInfo]="true"
      />
      <dxo-search-panel [visible]="searchable" [width]="220" />
      <dxo-export [enabled]="true" [formats]="['xlsx']" />
      <dxo-toolbar>
        <dxi-item location="before" template="tableTitle" />
        <dxi-item name="searchPanel" location="after" />
        <dxi-item name="exportButton" location="after" />
      </dxo-toolbar>
      <div *dxTemplate="let _ of 'tableTitle'" class="table-title">
        <strong>{{ title }}</strong>
        <span>Excel export available</span>
      </div>
      @for (column of columns; track column.dataField) {
        <dxi-column
          [dataField]="column.dataField"
          [caption]="column.caption"
          [dataType]="column.dataType ?? 'string'"
          [format]="column.format"
        />
      }
    </dx-data-grid>
  `,
  styles: `
    :host {
      display: block;
      min-width: 0;
    }
    .table-title {
      display: flex;
      flex-direction: column;
      line-height: 1.2;
    }
    .table-title strong {
      font-size: 0.78rem;
    }
    .table-title span {
      color: var(--ams-text-muted);
      font-size: 0.62rem;
    }
  `,
})
export class AnalyticsTableComponent {
  @Input({ required: true }) rows: readonly unknown[] = [];
  @Input({ required: true }) columns: readonly AnalyticsColumn[] = [];
  @Input() title = 'Analytics';
  @Input() fileName = 'AMS_Analytics';
  @Input() pageSize = 10;
  @Input() searchable = false;

  protected async exportExcel(event: DxDataGridTypes.ExportingEvent): Promise<void> {
    event.cancel = true;
    const workbook = new Workbook();
    const worksheet = workbook.addWorksheet(this.title.slice(0, 31));
    await exportDataGrid({ component: event.component, worksheet, autoFilterEnabled: true });
    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(
      new Blob([new Uint8Array(buffer)], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      }),
      `${this.fileName}.xlsx`,
    );
  }
}
