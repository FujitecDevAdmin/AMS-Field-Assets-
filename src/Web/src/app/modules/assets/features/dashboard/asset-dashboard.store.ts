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
    return data ? this.percentage(data.verifiedAssets, data.totalAssets) : 0;
  });
  readonly mappingRate = computed(() => {
    const data = this.data();
    return data ? this.percentage(data.employeeMappedAssets, data.totalAssets) : 0;
  });

  private percentage(value: number, total: number): number {
    if (value <= 0 || total <= 0) return 0;

    // Preserve small, valid non-zero percentages instead of rounding them to 0%.
    return Math.max(0.1, Math.round((value / total) * 1000) / 10);
  }

  async load(silent = false): Promise<void> {
    if (this.loading()) return;
    this.loading.set(true);
    if (!silent) this.error.set(null);
    try {
      const [dashboard, verifiedAssets] = await Promise.all([
        firstValueFrom(this.api.dashboard()),
        firstValueFrom(this.api.search('', 0, 1, { isVerified: true })),
      ]);
      const verifiedCount = Math.min(dashboard.totalAssets, verifiedAssets.totalCount);
      this.data.set({
        ...dashboard,
        verifiedAssets: verifiedCount,
        pendingVerification: Math.max(0, dashboard.totalAssets - verifiedCount),
      });
      this.error.set(null);
    } catch {
      this.error.set('Dashboard analytics could not be loaded. Check the API and your access.');
    } finally {
      this.loading.set(false);
    }
  }
}
