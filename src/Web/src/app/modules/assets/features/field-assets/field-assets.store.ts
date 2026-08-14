import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AssetsApi } from '../../data/assets.api';
import type {
  AssetImportResponse,
  AssetRegisterFilters,
  AssetRegisterRow,
  ImportedAssetDetailsUpdateResponse,
} from '../../data/models/asset-register.models';

@Injectable()
export class FieldAssetsStore {
  private readonly api = inject(AssetsApi);
  readonly pageSizes: number[] = [10, 20, 30, 40, 50, 60, 70, 80, 90, 100];
  readonly pageSize = signal(10);

  readonly rows = signal<readonly AssetRegisterRow[]>([]);
  readonly totalCount = signal(0);
  readonly pageIndex = signal(0);
  readonly search = signal('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedFile = signal<File | null>(null);
  readonly importResult = signal<AssetImportResponse | null>(null);
  readonly importing = signal(false);
  readonly savingDetails = signal(false);
  readonly filters = signal<AssetRegisterFilters>({});
  readonly pageLabel = computed(() => {
    if (this.totalCount() === 0) return '0 assets';
    const first = this.pageIndex() * this.pageSize() + 1;
    const last = Math.min(first + this.rows().length - 1, this.totalCount());
    return `${first}-${last} of ${this.totalCount()} assets`;
  });
  readonly canGoBack = computed(() => this.pageIndex() > 0);
  readonly canGoForward = computed(
    () => (this.pageIndex() + 1) * this.pageSize() < this.totalCount(),
  );

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const result = await firstValueFrom(
        this.api.search(
          this.search(),
          this.pageIndex() * this.pageSize(),
          this.pageSize(),
          this.filters(),
        ),
      );
      this.rows.set(result.rows);
      this.totalCount.set(result.totalCount);
    } catch {
      this.error.set('The asset register could not be loaded. Check the API and your access.');
    } finally {
      this.loading.set(false);
    }
  }

  async applySearch(value: string): Promise<void> {
    this.search.set(value);
    this.pageIndex.set(0);
    await this.load();
  }

  async previousPage(): Promise<void> {
    if (!this.canGoBack()) return;
    this.pageIndex.update((value) => value - 1);
    await this.load();
  }

  async nextPage(): Promise<void> {
    if (!this.canGoForward()) return;
    this.pageIndex.update((value) => value + 1);
    await this.load();
  }

  async changePageSize(value: number): Promise<void> {
    if (!this.pageSizes.includes(value)) return;
    this.pageSize.set(value);
    this.pageIndex.set(0);
    await this.load();
  }

  async applyFilters(filters: AssetRegisterFilters): Promise<void> {
    this.filters.set(filters);
    this.pageIndex.set(0);
    await this.load();
  }

  async clearFilters(): Promise<void> {
    await this.applyFilters({});
  }

  selectFile(file: File | null): void {
    this.selectedFile.set(file);
    this.importResult.set(null);
  }

  clearImportResult(): void {
    this.importResult.set(null);
  }

  async importSelected(): Promise<AssetImportResponse | null> {
    const file = this.selectedFile();
    if (!file || this.importing()) return null;
    this.importing.set(true);
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.api.importExcel(file));
      this.importResult.set(result);
      this.selectedFile.set(null);
      this.pageIndex.set(0);
      await this.load();
      return result;
    } catch {
      this.error.set('The workbook could not be imported. Check that Asset No, Asset Name and TechnicalGroup contain values.');
      return null;
    } finally {
      this.importing.set(false);
    }
  }

  async updateImportedDetails(
    assetId: number,
    fields: Readonly<Record<string, string | null>>,
  ): Promise<ImportedAssetDetailsUpdateResponse | null> {
    if (this.savingDetails()) return null;
    this.savingDetails.set(true);
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.api.updateImportedDetails(assetId, fields));
      this.rows.update(rows => rows.map(row => row.id === assetId
        ? { ...row, importedDataJson: result.importedDataJson }
        : row));
      await this.load();
      return result;
    } catch {
      this.error.set('The asset details could not be updated. Check your access and try again.');
      return null;
    } finally {
      this.savingDetails.set(false);
    }
  }
}
