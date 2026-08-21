import { inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AssetsApi } from '../../data/assets.api';
import type { AssetDetailCore } from '../../data/models/asset-register.models';
import type { AssetRegisterRow } from '../../data/models/asset-register.models';

@Injectable()
export class FieldAssetViewStore {
  private readonly api = inject(AssetsApi);
  readonly asset = signal<AssetDetailCore | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  preload(row: AssetRegisterRow): void {
    this.asset.set({
      id: row.id,
      assetNumber: row.assetNumber,
      assetName: row.assetName,
      typeName: row.typeName,
      statusName: row.statusName,
      importedDataJson: row.importedDataJson,
    });
  }

  async load(assetId: number): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const existing = this.asset();
      const response = await firstValueFrom(this.api.get(assetId));
      this.asset.set({
        ...response.asset,
        importedDataJson: response.asset.importedDataJson ?? existing?.importedDataJson ?? null,
      });
    } catch {
      this.error.set('The asset could not be loaded. It may not exist or you may not have access.');
    } finally {
      this.loading.set(false);
    }
  }

  async update(fields: Readonly<Record<string, string | null>>): Promise<string | null> {
    const asset = this.asset();
    if (!asset || this.saving()) return null;
    this.saving.set(true);
    this.error.set(null);
    try {
      const result = await firstValueFrom(this.api.updateImportedDetails(asset.id, fields));
      this.asset.set({ ...asset, importedDataJson: result.importedDataJson });
      return result.importedDataJson;
    } catch {
      this.error.set('The asset section could not be updated. Check your access and try again.');
      return null;
    } finally {
      this.saving.set(false);
    }
  }
}
