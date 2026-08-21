import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { Location } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxLoadPanelModule } from 'devextreme-angular/ui/load-panel';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';

import { FieldAssetViewStore } from './field-asset-view.store';
import type { AssetRegisterRow } from '../../data/models/asset-register.models';

interface ViewField {
  readonly name: string;
  value: string;
  isEmpty: boolean;
  readonly isReadOnly: boolean;
}

interface ViewGroup {
  readonly title: string;
  readonly fields: ViewField[];
}

interface ViewCategory {
  readonly title: string;
  readonly groups: ViewGroup[];
  isEditing: boolean;
}

const categoryConfig = [
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
  { title: '7. Contract', groups: [{ title: 'Contract Details', fields: ['ContractNo'] }] },
  { title: '8. Calibration', groups: [{ title: 'Calibration Details', fields: ['CalibrationStartDate', 'CalibrationEndDate'] }] },
] as const;

@Component({
  selector: 'ams-field-asset-view-page',
  imports: [DxButtonModule, DxLoadPanelModule, DxTextBoxModule],
  providers: [FieldAssetViewStore],
  templateUrl: './field-asset-view.page.html',
  styleUrl: './field-asset-view.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldAssetViewPage implements OnInit {
  readonly store = inject(FieldAssetViewStore);
  readonly categories = signal<ViewCategory[]>([]);
  readonly expandedFields = signal<ReadonlySet<string>>(new Set());
  readonly categoryColumns = computed(() => {
    const columns: [ViewCategory[], ViewCategory[]] = [[], []];
    const categories = this.categories();
    const contract = categories.find(category => category.title === '7. Contract');
    const calibration = categories.find(category => category.title === '8. Calibration');
    const mainCategories = categories.filter(category => category !== contract && category !== calibration);
    mainCategories.forEach((category, index) => {
      if (index % 2 === 0) columns[0].push(category);
      else columns[1].push(category);
    });
    if (calibration) columns[1].push(calibration);
    if (contract) columns[1].push(contract);
    return columns;
  });
  private readonly route = inject(ActivatedRoute);
  private readonly location = inject(Location);

  async ngOnInit(): Promise<void> {
    const assetId = Number(this.route.snapshot.paramMap.get('assetId'));
    if (!Number.isInteger(assetId) || assetId <= 0) return;
    const navigationAsset = this.readNavigationAsset(window.history.state);
    if (navigationAsset?.id === assetId) {
      this.store.preload(navigationAsset);
      this.rebuildCategories();
    }
    await this.store.load(assetId);
    this.rebuildCategories();
  }

  goBack(): void {
    this.location.back();
  }

  isLongValue(field: ViewField): boolean {
    return this.valueWords(field.value).length > 5;
  }

  isExpanded(category: ViewCategory, field: ViewField): boolean {
    return this.expandedFields().has(this.fieldKey(category, field));
  }

  visibleValue(category: ViewCategory, field: ViewField): string {
    const words = this.valueWords(field.value);
    return words.length <= 5 || this.isExpanded(category, field)
      ? field.value
      : `${words.slice(0, 5).join(' ')}…`;
  }

  toggleValue(category: ViewCategory, field: ViewField): void {
    const key = this.fieldKey(category, field);
    this.expandedFields.update(current => {
      const next = new Set(current);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  }

  edit(category: ViewCategory): void {
    this.categories.update(categories => categories.map(item =>
      item.title === category.title ? { ...item, isEditing: true } : item));
  }

  cancel(category: ViewCategory): void {
    this.rebuildCategories(category.title);
  }

  updateField(category: ViewCategory, name: string, value: string): void {
    for (const group of category.groups) {
      const field = group.fields.find(item => item.name === name);
      if (field) {
        field.value = value;
        field.isEmpty = value.trim().length === 0;
      }
    }
  }

  async update(category: ViewCategory): Promise<void> {
    const fields = Object.fromEntries(category.groups.flatMap(group => group.fields)
      .filter(field => !field.isReadOnly)
      .map(field => [field.name, field.value.trim() || null]));
    const result = await this.store.update(fields);
    if (result) this.rebuildCategories();
  }

  private rebuildCategories(editingTitle?: string): void {
    const asset = this.store.asset();
    const values = this.readValues(asset?.importedDataJson ?? null);
    if (asset) {
      values.set('asset no', { name: 'Asset No', value: asset.assetNumber });
      values.set('asset name', { name: 'Asset Name', value: asset.assetName });
      if (!values.has('asset category')) {
        values.set('asset category', { name: 'Asset Category', value: asset.typeName });
      }
      if (!values.has('status')) {
        values.set('status', { name: 'Status', value: asset.statusName });
      }
    }
    const categories: ViewCategory[] = categoryConfig.map(category => ({
      title: category.title,
      isEditing: category.title === editingTitle,
      groups: category.groups.map(group => ({
        title: group.title,
        fields: group.fields.map(name => {
          const match = values.get(name.toLocaleLowerCase());
          return this.toField(match?.name ?? name, match?.value ?? '—');
        }),
      })),
    }));
    this.categories.set(categories);
  }

  private readValues(json: string | null): Map<string, { name: string; value: string }> {
    if (!json) return new Map();
    try {
      const parsed: unknown = JSON.parse(json);
      if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) return new Map();
      return new Map(Object.entries(parsed).map(([name, value]) => [
        name.trim().toLocaleLowerCase(),
        { name, value: value === null || value === undefined || value === '' ? '—' : String(value) },
      ]));
    } catch {
      return new Map();
    }
  }

  private toField(name: string, value: string): ViewField {
    const normalized = name.replaceAll(' ', '').toLocaleUpperCase();
    return {
      name,
      value,
      isEmpty: value === '—',
      isReadOnly: normalized === 'ASSETNO' || normalized === 'ASSETNUMBER'
        || normalized === 'ASSETNAME' || normalized.includes('ERP') || normalized.includes('HOST'),
    };
  }

  private readNavigationAsset(state: unknown): AssetRegisterRow | null {
    if (!state || typeof state !== 'object' || !('asset' in state)) return null;
    const asset: unknown = state.asset;
    if (!asset || typeof asset !== 'object') return null;
    if (!('id' in asset) || typeof asset.id !== 'number') return null;
    if (!('assetNumber' in asset) || typeof asset.assetNumber !== 'string') return null;
    if (!('assetName' in asset) || typeof asset.assetName !== 'string') return null;
    if (!('typeName' in asset) || typeof asset.typeName !== 'string') return null;
    if (!('statusName' in asset) || typeof asset.statusName !== 'string') return null;
    return asset as AssetRegisterRow;
  }

  private fieldKey(category: ViewCategory, field: ViewField): string {
    return `${category.title}\u0000${field.name}`;
  }

  private valueWords(value: string): string[] {
    return value.trim().split(/\s+/).filter(Boolean);
  }

}
