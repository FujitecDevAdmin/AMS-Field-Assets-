export interface AssetRegisterRow {
  readonly id: number;
  readonly assetNumber: string;
  readonly assetName: string;
  readonly serialNumber: string | null;
  readonly typeName: string;
  readonly className: string | null;
  readonly statusName: string;
  readonly make: string | null;
  readonly model: string | null;
  readonly currentLocationId: number | null;
  readonly currentEmployeeId: number | null;
  readonly departmentId: number | null;
  readonly costCenter: string | null;
  readonly qrCodeValue: string | null;
  readonly barcodeValue: string | null;
  readonly erpAssetNumber: string | null;
  readonly sapAssetNumber: string | null;
  readonly sapPlant: string | null;
  readonly lastPhysicalCheckOnUtc: string | null;
  readonly remarks: string | null;
  readonly importedDataJson: string | null;
  readonly isBulk: boolean;
  readonly quantity: number;
  readonly unitOfMeasure: string | null;
  readonly acquisitionDate: string | null;
  readonly isDeleted: boolean;
}

export interface AssetRegisterFilters {
  readonly assetTypeId?: number;
  readonly assetClassId?: number;
  readonly assetStatusId?: number;
  readonly locationId?: number;
  readonly isVerified?: boolean;
  readonly employeeId?: number;
  readonly departmentId?: number;
  readonly costCenter?: string;
  readonly sapAssetNumber?: string;
  readonly sapPlant?: string;
  readonly acquiredFrom?: string;
  readonly acquiredTo?: string;
  readonly isBulk?: boolean;
  readonly includeDeleted?: boolean;
}

export interface AssetRegisterResponse {
  readonly rows: readonly AssetRegisterRow[];
  readonly totalCount: number;
}

export interface AssetImportError {
  readonly rowNumber: number;
  readonly message: string;
}

export interface AssetImportSkippedRow {
  readonly rowNumber: number;
  readonly fields: Readonly<Record<string, string | null>>;
  readonly systemRemarks: string;
}

export interface AssetImportResponse {
  readonly totalRows: number;
  readonly importedRows: number;
  readonly reactivatedRows: number;
  readonly skippedRows: number;
  readonly createdAssetTypes: number;
  readonly skippedRowDetails: readonly AssetImportSkippedRow[];
  readonly errors: readonly AssetImportError[];
}

export interface ImportedAssetDetailsUpdateResponse {
  readonly assetId: number;
  readonly importedDataJson: string;
}

export interface AssetDetailResponse {
  readonly asset: AssetDetailCore;
}

export interface AssetDetailCore {
  readonly id: number;
  readonly assetNumber: string;
  readonly assetName: string;
  readonly typeName: string;
  readonly statusName: string;
  readonly importedDataJson: string | null;
}

export interface AssetDashboardBreakdown {
  readonly name: string;
  readonly value: number;
  readonly count: number;
}

export interface AssetDashboardTrendPoint {
  readonly period: string;
  readonly added: number;
  readonly verified: number;
}

export interface AssetDashboardRecentAsset {
  readonly id: number;
  readonly assetNumber: string;
  readonly assetName: string;
  readonly status: string;
  readonly location: string;
  readonly createdOnUtc: string;
}

export interface AssetDashboardResponse {
  readonly totalAssets: number;
  readonly verifiedAssets: number;
  readonly pendingVerification: number;
  readonly missingAssets: number;
  readonly employeeMappedAssets: number;
  readonly unmappedAssets: number;
  readonly assetsUnderRepair: number;
  readonly disposedAssets: number;
  readonly totalAssetValue: number;
  readonly generatedOnUtc: string;
  readonly assetValueByLocation: readonly AssetDashboardBreakdown[];
  readonly assetValueByDepartment: readonly AssetDashboardBreakdown[];
  readonly assetsByStatus: readonly AssetDashboardBreakdown[];
  readonly assetsByType: readonly AssetDashboardBreakdown[];
  readonly assetTrend: readonly AssetDashboardTrendPoint[];
  readonly recentAssets: readonly AssetDashboardRecentAsset[];
}
