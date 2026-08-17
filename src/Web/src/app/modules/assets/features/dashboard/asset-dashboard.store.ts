import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AssetsApi } from '../../data/assets.api';
import type { AssetDashboardResponse } from '../../data/models/asset-register.models';

@Injectable()
export class AssetDashboardStore {
  private readonly api = inject(AssetsApi);

  readonly data = signal<AssetDashboardResponse | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly lastUpdated = computed(() => this.data()?.generatedOnUtc ?? null);
  readonly verificationRate = computed(() => {
    const data = this.data();
    return data && data.totalAssets > 0
      ? Math.round((data.verifiedAssets / data.totalAssets) * 100)
      : 0;
  });
  readonly mappingRate = computed(() => {
    const data = this.data();
    return data && data.totalAssets > 0
      ? Math.round((data.employeeMappedAssets / data.totalAssets) * 100)
      : 0;
  });

  async load(silent = false): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    if (!silent) this.error.set(null);
    try {
      this.data.set(await firstValueFrom(this.api.dashboard()));
      this.error.set(null);
    } catch {
      this.error.set('Dashboard analytics could not be loaded. Check the API and your access.');
    } finally {
      this.loading.set(false);
    }
  }
}
