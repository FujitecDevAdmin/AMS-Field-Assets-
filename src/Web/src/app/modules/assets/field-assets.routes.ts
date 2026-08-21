import type { Routes } from '@angular/router';

import { FieldAssetsPage } from './features/field-assets/field-assets.page';
import { FieldAssetViewPage } from './features/field-asset-view/field-asset-view.page';

export const FIELD_ASSETS_ROUTES: Routes = [
  { path: '', component: FieldAssetsPage },
  { path: ':assetId', component: FieldAssetViewPage },
];
