import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import type { DxDataGridTypes } from 'devextreme-angular/ui/data-grid';
import { DxLoadPanelModule } from 'devextreme-angular/ui/load-panel';
import { DxPopupModule } from 'devextreme-angular/ui/popup';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { exportDataGrid } from 'devextreme/excel_exporter';
import { Workbook } from 'exceljs';
import { Buffer } from 'buffer';
import { saveAs } from 'file-saver';

import { AuthStore } from '../../../../core/auth/auth.store';
import { FieldAssetsStore } from './field-assets.store';
import type {
  AssetImportResponse,
  AssetImportSkippedRow,
  AssetRegisterRow,
} from '../../data/models/asset-register.models';

interface ImportedField {
  readonly name: string;
  readonly value: string;
  readonly isEmpty: boolean;
}

interface DetailGroup {
  readonly title: string;
  readonly fields: readonly ImportedField[];
}

interface DetailCategory {
  readonly title: string;
  readonly groups: readonly DetailGroup[];
}

interface ImportedChooserColumn {
  readonly caption: string;
  readonly dataField: string;
  readonly calculateCellValue: (asset: AssetRegisterRow) => string;
}

interface ColumnChoice {
  readonly dataField: string;
  readonly caption: string;
  readonly visible: boolean;
}

const defaultRegisterColumns = [
  ['assetNumber', 'Asset No'], ['assetName', 'Asset Name'], ['typeName', 'TechnicalGroup'],
  ['className', 'Asset Class'], ['statusName', 'Status'],
  ['serialNumber', 'ManufactureSerialNumber'], ['costCenter', 'Cost Centre'],
  ['quantity', 'Capitalized Quantity'], ['acquisitionDate', 'First Acquisition Date'],
] as const;

const promotedTemplateHeaders = new Set<string>(defaultRegisterColumns.map(([, caption]) => caption));
const defaultImportedHeaders = new Set<string>(['Location']);

interface EditableImportedField {
  readonly name: string;
  readonly value: string;
  readonly isReadOnly: boolean;
}

interface EditableDetailGroup {
  readonly title: string;
  readonly fields: readonly EditableImportedField[];
}

interface EditableDetailCategory {
  readonly title: string;
  readonly groups: readonly EditableDetailGroup[];
}

const importedColumnHeaders = [
  'Sl.no.', 'Branch', 'Asset No', 'Asset Name', 'ManufactureSerialNumber', 'TechnicalGroup',
  'Asset Class', 'Location', 'OpportunityName', 'PhysicalCondition', 'Capitalized Quantity',
  'Disposal Qty', 'Gross Qty', 'Orignal Value', 'Migrated Book Value', 'Additional Value',
  'Gross Value', 'Disposal Gross Value', 'Current Gross Value', 'Deprecitaion Method',
  'Depreciation Percentage', 'Additions During the year', 'Acc. Dep. as of beginning of Year',
  'Depreciation Charged for the year', 'Acc. Dep. as of End of Year', 'Net Book Value',
  'Asset Class Code', 'Asset Category', 'Asset Description', 'Narration', 'Status', 'AUCNo',
  'VoucherNo', 'Year of Purchase', 'Posting Date', 'First Acquisition Date', 'Disposal Date',
  'Asset Useful Life', 'Cost Centre', 'AP VoucherNo', 'Invoice No', 'Invoice Date', 'Vendor Name',
  'Purchase Order No', 'GRN Number', 'Gross Value COA', 'Gross Value COA Description',
  'Accumulated Depreciation COA', 'Accumulated Depreciation COA Description', 'Depreciation COA',
  'Depreciation COA Description', 'WarrantyPeriodsInMonth', 'InsurancePolicyNumber',
  'InsurancePolicyType', 'InsurancePolicyStartDate', 'InsurancePolicyEndDate', 'EmployeeUniqueID',
  'EmployeeName', 'EmpEMailAddress', 'ContractNo', 'CalibrationStartDate', 'CalibrationEndDate',
  'WarrantyPeriodStartDate', 'WarrantyPeriodEndDate', 'Make', 'Model',
] as const;

const detailCategoryConfig = [
  { title: '1. Asset Details', groups: [
    { title: 'Asset Identification', fields: ['Sl.no.', 'Asset No', 'ManufactureSerialNumber', 'ERP/Voucher No', 'AUCNo'] },
    { title: 'Asset Classification', fields: ['Asset Category', 'Asset Class', 'Asset Class Code', 'TechnicalGroup', 'Status', 'PhysicalCondition'] },
    { title: 'Description', fields: ['Asset Name', 'Asset Description', 'Narration', 'Make', 'Model', 'OpportunityName'] },
  ] },
  { title: '2. Location & Assignment', groups: [
    { title: 'Location', fields: ['Branch', 'Location', 'Cost Centre'] },
    { title: 'Employee / Custodian', fields: ['EmployeeUniqueID', 'EmployeeName', 'EmpEMailAddress'] },
  ] },
  { title: '3. Financial Details', groups: [
    { title: 'Values', fields: ['Orignal Value', 'Migrated Book Value', 'Additional Value', 'Gross Value', 'Disposal Gross Value', 'Current Gross Value', 'Net Book Value'] },
    { title: 'Quantity', fields: ['Capitalized Quantity', 'Disposal Qty', 'Gross Qty'] },
    { title: 'Depreciation', fields: ['Deprecitaion Method', 'Depreciation Percentage', 'Additions During the year', 'Acc. Dep. as of beginning of Year', 'Depreciation Charged for the year', 'Acc. Dep. as of End of Year', 'Asset Useful Life', 'Year of Purchase', 'Disposal Date'] },
  ] },
  { title: '4. Procurement', groups: [
    { title: 'Vendor & Purchase', fields: ['Vendor Name', 'Purchase Order No', 'GRN Number', 'Invoice No', 'Invoice Date'] },
    { title: 'Acquisition', fields: ['Posting Date', 'First Acquisition Date'] },
  ] },
  { title: '5. Accounting', groups: [
    { title: 'References', fields: ['VoucherNo', 'AP VoucherNo', 'AUCNo'] },
    { title: 'Chart of Accounts', fields: ['Gross Value COA', 'Gross Value COA Description', 'Accumulated Depreciation COA', 'Accumulated Depreciation COA Description', 'Depreciation COA', 'Depreciation COA Description'] },
  ] },
  { title: '6. Warranty & Insurance', groups: [
    { title: 'Warranty', fields: ['WarrantyPeriodsInMonth', 'WarrantyPeriodStartDate', 'WarrantyPeriodEndDate'] },
    { title: 'Insurance', fields: ['InsurancePolicyNumber', 'InsurancePolicyType', 'InsurancePolicyStartDate', 'InsurancePolicyEndDate'] },
  ] },
  { title: '7. Contract', groups: [
    { title: 'Contract Details', fields: ['ContractNo'] },
  ] },
  { title: '8. Calibration', groups: [
    { title: 'Calibration Details', fields: ['CalibrationStartDate', 'CalibrationEndDate'] },
  ] },
] as const;

@Component({
  selector: 'ams-field-assets-page',
  imports: [
    DxButtonModule,
    DxDataGridModule,
    DxLoadPanelModule,
    DxPopupModule,
    DxSelectBoxModule,
    DxTextBoxModule,
  ],
  providers: [FieldAssetsStore],
  templateUrl: './field-assets.page.html',
  styleUrl: './field-assets.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldAssetsPage implements OnInit {
  readonly store = inject(FieldAssetsStore);
  private readonly auth = inject(AuthStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  selectedAsset: AssetRegisterRow | null = null;
  importedFields: ImportedField[] = [];
  detailCategories: DetailCategory[] = [];
  isDetailVisible = false;
  isEditVisible = false;
  isGeneratingSkippedReport = false;
  skippedReportError: string | null = null;
  skippedReportResult: AssetImportResponse | null = null;
  isFilterVisible = false;
  isColumnChooserVisible = false;
  selectedBranchId: number | null = null;
  editableFields: EditableImportedField[] = [];
  editCategories: EditableDetailCategory[] = [];
  editCategoryColumns: readonly (readonly EditableDetailCategory[])[] = [[], []];
  readonly importedChooserColumns: readonly ImportedChooserColumn[] = importedColumnHeaders.map(
    (caption, index): ImportedChooserColumn => ({
      caption,
      dataField: `imported_${index + 1}`,
      calculateCellValue: (asset: AssetRegisterRow) => this.readImportedValue(asset, caption),
    }),
  ).filter(column => !promotedTemplateHeaders.has(column.caption));
  readonly columnVisibility = signal<Record<string, boolean>>(this.loadColumnPreferences());
  readonly columnChoices = signal<readonly ColumnChoice[]>([]);

  ngOnInit(): void {
    this.route.queryParamMap.subscribe(params => {
      const verification = params.get('verification');
      const search = params.get('search')?.trim() ?? '';

      this.store.filters.set({
        ...this.store.filters(),
        isVerified: verification === 'verified' ? true : undefined,
      });
      this.store.search.set(search);
      this.store.pageIndex.set(0);
      void this.store.load();
    });
    void this.store.loadBranches();
  }

  showFilters(): void {
    this.selectedBranchId = this.store.filters().locationId ?? null;
    this.isFilterVisible = true;
    void this.store.loadBranches();
  }

  showColumnChooser(): void {
    const visibility = this.columnVisibility();
    this.columnChoices.set([
      ...defaultRegisterColumns.map(([dataField, caption]) => ({
        dataField,
        caption,
        visible: visibility[dataField] ?? true,
      })),
      ...this.importedChooserColumns.map(column => ({
        dataField: column.dataField,
        caption: column.caption,
        visible: visibility[column.dataField] ?? defaultImportedHeaders.has(column.caption),
      })),
    ]);
    this.isColumnChooserVisible = true;
  }

  setColumnChoice(dataField: string, visible: boolean): void {
    this.columnChoices.update(choices => choices.map(choice =>
      choice.dataField === dataField ? { ...choice, visible } : choice));
  }

  applyColumnChoices(): void {
    const visibility = Object.fromEntries(
      this.columnChoices().map(choice => [choice.dataField, choice.visible]),
    );
    this.columnVisibility.set(visibility);
    this.saveColumnPreferences(visibility);
    this.isColumnChooserVisible = false;
  }

  cancelColumnChooser(): void {
    this.isColumnChooserVisible = false;
  }

  isColumnVisible(dataField: string, defaultVisible: boolean): boolean {
    return this.columnVisibility()[dataField] ?? defaultVisible;
  }

  isImportedColumnVisible(column: ImportedChooserColumn): boolean {
    return this.isColumnVisible(column.dataField, defaultImportedHeaders.has(column.caption));
  }

  async applyBranchFilter(): Promise<void> {
    const { locationId: _, ...existingFilters } = this.store.filters();
    await this.store.applyFilters(this.selectedBranchId === null
      ? existingFilters
      : { ...existingFilters, locationId: this.selectedBranchId });
    this.isFilterVisible = false;
  }

  async clearBranchFilter(): Promise<void> {
    this.selectedBranchId = null;
    const { locationId: _, ...existingFilters } = this.store.filters();
    await this.store.applyFilters(existingFilters);
    this.isFilterVisible = false;
  }

  onSearch(value: string): void {
    void this.store.applySearch(value);
  }

  private columnPreferenceKey(): string {
    const userId = this.auth.session()?.userId ?? 'anonymous';
    return `ams.field-assets.columns.user.${userId}`;
  }

  private loadColumnPreferences(): Record<string, boolean> {
    try {
      const stored = localStorage.getItem(this.columnPreferenceKey());
      if (stored === null) return {};
      const parsed = JSON.parse(stored) as unknown;
      return parsed !== null && typeof parsed === 'object'
        ? parsed as Record<string, boolean>
        : {};
    } catch {
      return {};
    }
  }

  private saveColumnPreferences(preferences: Record<string, boolean>): void {
    try {
      localStorage.setItem(this.columnPreferenceKey(), JSON.stringify(preferences));
    } catch {
      // Storage can be unavailable in private browsing; defaults remain usable.
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.store.selectFile(input.files?.item(0) ?? null);
  }

  async importSelected(): Promise<void> {
    const sourceFile = this.store.selectedFile();
    this.skippedReportResult = null;
    this.skippedReportError = null;
    const result = await this.store.importSelected();
    if (result) {
      window.setTimeout(() => this.store.clearImportResult(), 25_000);
    }
    if (!result || result.skippedRows === 0) return;

    let skippedRowDetails = result.skippedRowDetails ?? [];
    if (skippedRowDetails.length === 0 && sourceFile && result.errors.length > 0) {
      skippedRowDetails = await this.extractSkippedRowsFromSource(sourceFile, result);
    }
    if (skippedRowDetails.length > 0) {
      this.skippedReportResult = { ...result, skippedRowDetails };
      await this.downloadSkippedRows(this.skippedReportResult);
    } else {
      this.skippedReportError = 'Skipped rows were reported, but their row details were not returned. Restart the backend API and import again.';
    }
  }

  onPageSizeChanged(value: number): void {
    void this.store.changePageSize(value);
  }

  async onExporting(event: DxDataGridTypes.ExportingEvent): Promise<void> {
    event.cancel = true;
    const workbook = new Workbook();
    const worksheet = workbook.addWorksheet('Assets');
    await exportDataGrid({
      component: event.component,
      worksheet,
      autoFilterEnabled: true,
      keepColumnWidths: true,
    });
    worksheet.views = [{ state: 'frozen', ySplit: 1 }];
    const buffer = await workbook.xlsx.writeBuffer();
    const date = new Date().toISOString().slice(0, 10);
    saveAs(
      new Blob([new Uint8Array(buffer)], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
      }),
      `AMS_Field_Assets_${date}.xlsx`,
    );
  }

  async downloadSkippedRows(result: AssetImportResponse): Promise<void> {
    if (this.isGeneratingSkippedReport || !result.skippedRowDetails?.length) return;
    this.isGeneratingSkippedReport = true;
    this.skippedReportError = null;
    try {
      const extraHeaders = result.skippedRowDetails
      .flatMap(row => Object.keys(row.fields))
      .filter(header => !importedColumnHeaders.some(standard =>
        standard.toLocaleLowerCase() === header.trim().toLocaleLowerCase()))
      .filter((header, index, values) => values.findIndex(value =>
        value.toLocaleLowerCase() === header.toLocaleLowerCase()) === index);
      const headers = [...importedColumnHeaders, ...extraHeaders, 'System Remarks'];
      const workbook = new Workbook();
      const worksheet = workbook.addWorksheet('Skipped Rows');
      worksheet.addRow(headers);
      for (const skipped of result.skippedRowDetails) {
        const fieldLookup = new Map(Object.entries(skipped.fields).map(([name, value]) =>
          [name.trim().toLocaleLowerCase(), value]));
        worksheet.addRow(headers.map(header => header === 'System Remarks'
          ? skipped.systemRemarks
          : (fieldLookup.get(header.toLocaleLowerCase()) ?? '')));
      }
      const headerRow = worksheet.getRow(1);
      headerRow.height = 34;
      headerRow.eachCell(cell => {
        cell.font = { bold: true, color: { argb: 'FFFFFFFF' } };
        cell.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFA60010' } };
        cell.alignment = { vertical: 'middle', horizontal: 'left', wrapText: true };
      });
      worksheet.columns.forEach((column, index) => {
        column.width = headers[index] === 'System Remarks' ? 48 : 22;
      });
      worksheet.views = [{ state: 'frozen', ySplit: 1 }];
      worksheet.autoFilter = { from: 'A1', to: headerRow.getCell(headers.length).address };
      const buffer = await workbook.xlsx.writeBuffer();
      const timestamp = new Date().toISOString().replaceAll(':', '-').slice(0, 19);
      saveAs(
        new Blob([new Uint8Array(buffer)], {
          type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
        }),
        `AMS_Skipped_Asset_Rows_${timestamp}.xlsx`,
      );
    } catch {
      this.skippedReportError = 'The skipped-row report could not be generated. Please try the download button again.';
    } finally {
      this.isGeneratingSkippedReport = false;
    }
  }

  private async extractSkippedRowsFromSource(
    sourceFile: File,
    result: AssetImportResponse,
  ): Promise<readonly AssetImportSkippedRow[]> {
    try {
      const workbook = new Workbook();
      const sourceBytes = Buffer.from(await sourceFile.arrayBuffer());
      await workbook.xlsx.load(sourceBytes as never);
      const worksheet = workbook.worksheets[0];
      if (!worksheet) return [];
      const headerRow = worksheet.getRow(1);
      const headers = Array.from(
        { length: worksheet.actualColumnCount },
        (_, index) => String(headerRow.getCell(index + 1).value ?? '').trim(),
      );
      return result.errors.flatMap(error => {
        const sourceRow = worksheet.getRow(error.rowNumber);
        if (!sourceRow.hasValues) return [];
        const fields: Record<string, string | null> = {};
        headers.forEach((header, index) => {
          if (!header) return;
          const value = sourceRow.getCell(index + 1).value;
          fields[header] = value === null || value === undefined ? null : String(value);
        });
        return [{
          rowNumber: error.rowNumber,
          fields,
          systemRemarks: error.message,
        }];
      });
    } catch {
      return [];
    }
  }

  viewAsset(asset: AssetRegisterRow): void {
    void this.router.navigate(['/field-assets', asset.id], { state: { asset } });
  }

  editAsset(asset: AssetRegisterRow): void {
    this.selectedAsset = asset;
    this.editableFields = this.readImportedFields(asset).map(field => ({
      name: field.name,
      value: field.isEmpty ? '' : field.value,
      isReadOnly: this.isProtectedEditField(field.name),
    }));
    this.editCategories = this.buildEditableCategories(this.editableFields);
    this.editCategoryColumns = this.splitEditCategories(this.editCategories);
    this.isEditVisible = true;
  }

  updateEditableField(name: string, value: string): void {
    this.editableFields = this.editableFields.map(field =>
      field.name === name ? { ...field, value } : field,
    );
    this.editCategories = this.buildEditableCategories(this.editableFields);
    this.editCategoryColumns = this.splitEditCategories(this.editCategories);
  }

  async saveEditedAsset(): Promise<void> {
    if (!this.selectedAsset) return;
    const fields = Object.fromEntries(this.editableFields.map(field => [field.name, field.value || null]));
    const result = await this.store.updateImportedDetails(this.selectedAsset.id, fields);
    if (result) {
      this.selectedAsset = { ...this.selectedAsset, importedDataJson: result.importedDataJson };
      this.importedFields = this.readImportedFields(this.selectedAsset);
      this.detailCategories = this.buildDetailCategories(this.importedFields);
      this.isEditVisible = false;
    }
  }

  private isProtectedEditField(name: string): boolean {
    const normalized = name.replaceAll(' ', '').toLocaleUpperCase();
    return normalized === 'ASSETNO' || normalized === 'ASSETNUMBER' || normalized === 'ASSETNAME'
      || normalized.includes('ERP') || normalized.includes('HOST');
  }

  private buildEditableCategories(fields: readonly EditableImportedField[]): EditableDetailCategory[] {
    const fieldLookup = new Map(fields.map(field => [field.name.trim().toLocaleLowerCase(), field]));
    const categories: EditableDetailCategory[] = detailCategoryConfig.map(category => ({
      title: category.title,
      groups: category.groups.map(group => ({
        title: group.title,
        fields: group.fields.flatMap(name => {
          const field = fieldLookup.get(name.toLocaleLowerCase());
          if (!field) return [];
          return [field];
        }),
      })).filter(group => group.fields.length > 0),
    })).filter(category => category.groups.length > 0);
    return categories;
  }

  private splitEditCategories(
    categories: readonly EditableDetailCategory[],
  ): readonly (readonly EditableDetailCategory[])[] {
    const columns: [EditableDetailCategory[], EditableDetailCategory[]] = [[], []];
    const columnWeights: [number, number] = [0, 0];

    for (const category of categories) {
      const fieldRows = category.groups.reduce(
        (total, group) => total + Math.max(1, Math.ceil(group.fields.length / 2)),
        0,
      );
      const weight = 1 + category.groups.length * 0.45 + fieldRows;
      const targetColumn: 0 | 1 = columnWeights[0] <= columnWeights[1] ? 0 : 1;
      columns[targetColumn].push(category);
      columnWeights[targetColumn] += weight;
    }

    return columns;
  }

  private buildDetailCategories(fields: readonly ImportedField[]): DetailCategory[] {
    const fieldLookup = new Map(fields.map(field => [field.name.trim().toLocaleLowerCase(), field]));
    const categories: DetailCategory[] = detailCategoryConfig.map(category => ({
      title: category.title,
      groups: category.groups.map(group => ({
        title: group.title,
        fields: group.fields.flatMap(name => {
          const field = fieldLookup.get(name.toLocaleLowerCase());
          if (field) {
            return [field];
          }
          return [];
        }),
      })),
    }));
    return categories;
  }

  private readImportedFields(asset: AssetRegisterRow): ImportedField[] {
    if (!asset.importedDataJson) {
      return [{ name: 'Import detail', value: 'Upload an asset workbook again to attach its original row to this asset.', isEmpty: false }];
    }

    try {
      const values = JSON.parse(asset.importedDataJson) as Record<string, unknown>;
      return Object.entries(values).map(([name, value]) => ({
        name,
        isEmpty: value === null || value === undefined || value === '',
        value: value === null || value === undefined || value === '' ? '—' : String(value),
      }));
    } catch {
      return [{ name: 'Import detail', value: 'The stored import detail could not be read.', isEmpty: false }];
    }
  }

  private readImportedValue(asset: AssetRegisterRow, header: string): string {
    if (!asset.importedDataJson) return '';
    try {
      const values = JSON.parse(asset.importedDataJson) as Record<string, unknown>;
      const matchingKey = Object.keys(values).find(key => key.trim().toLocaleLowerCase() === header.toLocaleLowerCase());
      const value = matchingKey ? values[matchingKey] : null;
      return value === null || value === undefined ? '' : String(value);
    } catch {
      return '';
    }
  }

}
