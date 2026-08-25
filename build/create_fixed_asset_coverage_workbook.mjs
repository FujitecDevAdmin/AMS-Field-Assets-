import fs from 'node:fs/promises';
import { Workbook, SpreadsheetFile } from '@oai/artifact-tool';

const outputDir = 'C:/Users/keerthivasan/source/repos/AMS/AMS/outputs/fixed-asset-coverage';
const outputPath = `${outputDir}/AMS_Fixed_Asset_Requirements_Coverage.xlsx`;

const rows = [
  ['QR & Barcode','Unique QR value per asset','Database Ready','Assets.Asset.QrCodeValue and UX_Asset_QrCode enforce storage and uniqueness. No automatic generator found.','Assets.Asset; Assets module duplicate translation','AMS/AMS_Consolidated_Design_v2.sql:795; AMS/src/Backend/Modules/AMS.Modules.Assets/AssetsModuleExtensions.cs:111','Implement secure token generation during registration/SAP import.','High'],
  ['QR & Barcode','Unique barcode value per asset','Database Ready','BarcodeValue exists and is returned to the mobile audit list; no automatic generator found.','Assets.Asset; assigned audit response','AMS/AMS_Consolidated_Design_v2.sql:796; AMS/src/Mobile/lib/features/audits/audit_models.dart','Add barcode uniqueness validation/index if required and generation policy.','High'],
  ['QR & Barcode','Generate printable QR/barcode labels','Not Covered','No label-template, QR image, barcode image, or print endpoint/component found.','None implemented','Repository feature inventory','Build label template, batch generation, PDF/print endpoint and reprint audit.','High'],
  ['QR & Barcode','Print and affix workflow','Not Covered','No Printed/Affixed/Verified tagging workflow found.','Could use AssetEvent for audit trail','Repository feature inventory','Add application workflow and operational confirmation.','Medium'],
  ['QR & Barcode','Scan QR/barcode in mobile app','Implemented','Mobile scanner resolves asset number, QR, or barcode against an assigned audit.','ResolveAuditScan; mobile scanner','AMS/src/Mobile/lib/features/audits/audits_api.dart; AMS/src/Backend/Modules/AMS.Modules.Verification/Features/ResolveAuditScan','Retain and add general asset lookup outside active audits if required.','Low'],
  ['QR & Barcode','Retrieve full asset details instantly after scan','Partial','Audit scan returns a limited AssignedAsset model, not the complete financial, vendor, warranty, calibration, holder and service profile.','ResolveAuditScan; GetAsset exists separately','AMS/src/Mobile/lib/features/audits/audit_models.dart; AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAsset','Extend scan response or call GetAsset after authorization.','High'],

  ['Asset Creation & SAP','Asset master fields stored in AMS','Implemented','Asset register, class, status, purchase, finance, calibration and SAP reference structures exist.','Assets.Asset and detail tables','AMS/AMS_Consolidated_Design_v2.sql:780','Continue using existing normalized structure.','Low'],
  ['Asset Creation & SAP','Asset Number','Implemented','AssetNumber is mandatory and supported by register/search/detail APIs.','Assets.Asset.AssetNumber','AMS/AMS_Consolidated_Design_v2.sql:782','Map SAP asset number deterministically.','Low'],
  ['Asset Creation & SAP','Asset Description','Implemented','AssetName stores the required description.','Assets.Asset.AssetName','AMS/AMS_Consolidated_Design_v2.sql:783','Map SAP description.','Low'],
  ['Asset Creation & SAP','Asset Class','Implemented','AssetClass master and AssetClassId/SapAssetClass are available.','Assets.AssetClass; Assets.Asset','AMS/AMS_Consolidated_Design_v2.sql:690; AMS/AMS_Consolidated_Design_v2.sql:789','Define SAP-to-AMS class mapping.','Low'],
  ['Asset Creation & SAP','Serial Number','Implemented','SerialNumber is stored and surfaced in mobile audit assignments.','Assets.Asset.SerialNumber','AMS/AMS_Consolidated_Design_v2.sql:784; AMS/src/Mobile/lib/features/audits/audit_models.dart','Add duplicate exception policy for serial numbers.','Low'],
  ['Asset Creation & SAP','Plant','Implemented','SapPlant is present.','Assets.Asset.SapPlant','AMS/AMS_Consolidated_Design_v2.sql:800','Map SAP plant codes.','Low'],
  ['Asset Creation & SAP','Cost Center','Implemented','CostCenter is present and supported by transfers/dashboard joins.','Assets.Asset.CostCenter','AMS/AMS_Consolidated_Design_v2.sql:793','Validate against SAP master data if required.','Low'],
  ['Asset Creation & SAP','Business Location','Implemented','CurrentLocationId links assets to branches; SapPlant also retained.','Assets.Asset.CurrentLocationId; Organization.Branch','AMS/AMS_Consolidated_Design_v2.sql:790; AMS/AMS_Consolidated_Design_v2.sql:507','Define SAP business-location to branch mapping.','Low'],
  ['Asset Creation & SAP','Purchase Value','Implemented','PurchaseCost and finance values are available.','AssetPurchaseDetail.PurchaseCost; AssetFinance','AMS/AMS_Consolidated_Design_v2.sql:903; AMS/AMS_Consolidated_Design_v2.sql:996','Choose authoritative SAP field for reporting.','Low'],
  ['Asset Creation & SAP','Capitalization Date','Partial','AcquisitionDate, FirstAcquisitionDate and PostingDate exist; no explicitly named capitalization date.','Assets.Asset; Assets.AssetFinance','AMS/AMS_Consolidated_Design_v2.sql:794; AMS/AMS_Consolidated_Design_v2.sql:1008','Confirm which existing date is the SAP capitalization date.','Medium'],
  ['Asset Creation & SAP','Purchase Order view','Partial','PurchaseOrderNumber is stored, but no PO document/view integration is implemented.','AssetPurchaseDetail.PurchaseOrderNumber','AMS/AMS_Consolidated_Design_v2.sql:900','Add SAP deep link or document retrieval endpoint/UI.','Medium'],
  ['Asset Creation & SAP','Vendor Name','Implemented','Vendor master and VendorId are implemented with CRUD APIs.','Organization.Vendor; AssetPurchaseDetail','AMS/AMS_Consolidated_Design_v2.sql:542; AMS/AMS_Consolidated_Design_v2.sql:899','Map SAP vendor key and name.','Low'],
  ['Asset Creation & SAP','Calibration start/end dates','Implemented','Instrument detail stores both dates with date-window validation.','AssetInstrumentDetail','AMS/AMS_Consolidated_Design_v2.sql:1145','Expose in mobile full-detail response.','Low'],
  ['Asset Creation & SAP','Warranty start/end dates','Implemented','Purchase detail stores both dates with validation.','AssetPurchaseDetail','AMS/AMS_Consolidated_Design_v2.sql:904','Expose expiry alert/report UI.','Low'],
  ['Asset Creation & SAP','Automatic SAP S/4HANA inbound synchronization','Database Ready','SapSync log/watermark tables exist, but no SAP connector, scheduled pull or synchronization handler was found.','SapSync.SapSyncLog; SapSyncWatermark','AMS/AMS_Consolidated_Design_v2.sql:2716','Implement authenticated SAP API/IDoc/OData adapter, delta pull, mapping, retries and reconciliation.','Critical'],
  ['Asset Creation & SAP','Automatic synchronization to mobile','Partial','Mobile loads assigned audits from AMS API; it does not directly synchronize full SAP asset details or maintain an offline asset master.','Mobile AuditsApi','AMS/src/Mobile/lib/features/audits/audits_api.dart','Expose complete asset DTO and implement cache/offline sync if required.','High'],

  ['Employee & Site Mapping','Employee ID and name','Implemented','EmployeeCode and FullName exist; allocation references EmployeeId.','Organization.Employee; AssetAllocation','AMS/AMS_Consolidated_Design_v2.sql:562; AMS/AMS_Consolidated_Design_v2.sql:1280','Surface in mobile scan DTO.','Low'],
  ['Employee & Site Mapping','Department','Implemented','Employee.DepartmentId and asset DepartmentId exist.','Organization.Employee; Assets.Asset','AMS/AMS_Consolidated_Design_v2.sql:569; AMS/AMS_Consolidated_Design_v2.sql:792','Define whether holder or asset department is authoritative.','Low'],
  ['Employee & Site Mapping','Designation','Not Covered','No designation field or designation master was found in the Employee model.','No existing reliable field','AMS/AMS_Consolidated_Design_v2.sql:562','Requires approved SAP/HR runtime lookup or schema extension; cannot be reliably persisted as-is.','High'],
  ['Employee & Site Mapping','Branch/location','Implemented','Employee and asset both reference Branch.','Organization.Employee.BranchId; Assets.Asset.CurrentLocationId','AMS/AMS_Consolidated_Design_v2.sql:570; AMS/AMS_Consolidated_Design_v2.sql:790','Define mismatch rules.','Low'],
  ['Employee & Site Mapping','Employee cost center','Partial','Asset has CostCenter, but Employee has no CostCenter column.','Assets.Asset.CostCenter only','AMS/AMS_Consolidated_Design_v2.sql:793; AMS/AMS_Consolidated_Design_v2.sql:562','Resolve from SAP/HR at runtime or approve a schema extension.','High'],
  ['Employee & Site Mapping','Date of issue and expected return','Implemented','AllocatedOnUtc and ExpectedReturnDate are implemented.','Allocations.AssetAllocation','AMS/AMS_Consolidated_Design_v2.sql:1285','Expose in assignment screens/mobile details.','Low'],
  ['Employee & Site Mapping','Consent/acknowledgement form','Implemented','DocumentPath, status and signing data exist; allocation feature set includes signing.','AssetAcknowledgement; SignAcknowledgement','AMS/AMS_Consolidated_Design_v2.sql:1330; AMS/src/Backend/Modules/AMS.Modules.Allocations/Features/SignAcknowledgement','Build/confirm web/mobile user-facing workflow.','Medium'],
  ['Employee & Site Mapping','Digital employee signature','Implemented','SignatureImagePath and SignedOnUtc exist with backend signing feature.','AssetAcknowledgement','AMS/AMS_Consolidated_Design_v2.sql:1335','Ensure secure upload/storage and consent UX.','Medium'],
  ['Employee & Site Mapping','Reporting manager approval','Implemented','Employee manager relationship and acknowledgement approval fields/features exist.','Employee.ReportingManagerId; ApproveAcknowledgement','AMS/AMS_Consolidated_Design_v2.sql:571; AMS/src/Backend/Modules/AMS.Modules.Allocations/Features/ApproveAcknowledgement','Confirm approver resolution is reporting manager.','Medium'],
  ['Employee & Site Mapping','Customer/project site mapping','Implemented','CustomerSite and AssetSiteMapping with mapping/removal APIs exist.','Allocations.CustomerSite; AssetSiteMapping','AMS/AMS_Consolidated_Design_v2.sql:1442; AMS/src/Backend/Modules/AMS.Modules.Allocations/Features/MapAssetToSite','Complete production UI and validation.','Low'],
  ['Employee & Site Mapping','Repair request generation','Implemented','Asset-linked AssetIssue service requests, categories, attachments and workflow are supported.','ServiceDesk.ServiceRequest','AMS/AMS_Consolidated_Design_v2.sql:1818; AMS/src/Backend/Modules/AMS.Modules.ServiceDesk/Features/RaiseServiceRequest','Configure Repair category and mobile entry point.','Medium'],
  ['Employee & Site Mapping','Replacement request generation','Partial','Can be configured as AssetIssue/category with approvals, but no explicit replacement workflow/UI was found.','ServiceDesk and approval workflows','AMS/src/Backend/Modules/AMS.Modules.ServiceDesk/Features/CreateApprovalWorkflow','Configure replacement category, approval chain and asset replacement outcome.','High'],

  ['Physical Verification','Verification cycle, branches and auditor assignments','Implemented','Cycle, branch scope and auditor assignments have backend and web/mobile screens.','Verification cycle/assignment/location','AMS/src/Backend/Modules/AMS.Modules.Verification/VerificationModuleExtensions.cs; AMS/src/Web/src/app/modules/audit','Retain.','Low'],
  ['Physical Verification','Scan asset during assigned audit','Implemented','ResolveAuditScan validates scan against cycle and branch before submission.','ResolveAuditScan; mobile scanner','AMS/src/Mobile/lib/features/audits/audits_api.dart','Retain.','Low'],
  ['Physical Verification','Auto-populate asset number','Implemented','AssignedAsset contains asset number/name and is returned after scan.','AssignedAsset','AMS/src/Mobile/lib/features/audits/audit_models.dart','Retain.','Low'],
  ['Physical Verification','Auto-display employee and department','Partial','Database/request supports HolderEmployeeId, but current mobile AssignedAsset does not contain employee or department.','PhysicalVerification.HolderEmployeeId; limited mobile DTO','AMS/AMS_Consolidated_Design_v2.sql:2528; AMS/src/Mobile/lib/features/audits/audit_models.dart','Extend resolve-scan and mobile models/UI.','High'],
  ['Physical Verification','GPS current location capture','Partial','Backend accepts/stores GPS, but current mobile verify() sends no latitude/longitude and no geolocation implementation was found.','PhysicalVerification.GpsLatitude/Longitude','AMS/src/Backend/Modules/AMS.Modules.Verification/Features/SubmitVerification/SubmitVerificationRequest.cs; AMS/src/Mobile/lib/features/audits/audits_api.dart','Add permission handling, geolocation capture, accuracy and mock-location checks.','Critical'],
  ['Physical Verification','Asset photo using mobile camera','Partial','Backend stores PhotoPath, but current mobile submission has no photo capture/upload.','PhysicalVerification.PhotoPath','AMS/src/Backend/Modules/AMS.Modules.Verification/Features/SubmitVerification/SubmitVerificationRequest.cs; AMS/src/Mobile/lib/features/audits/audits_api.dart','Add camera capture, upload endpoint/storage and submit returned path.','Critical'],
  ['Physical Verification','Working condition','Implemented','Mobile sends workingCondition and database validates condition vocabulary.','PhysicalVerification.WorkingCondition','AMS/src/Mobile/lib/features/audits/audits_api.dart; AMS/AMS_Consolidated_Design_v2.sql:2541','Align vocabulary: requirement says Repair/Scrap while current values are MinorDamage/Damaged/NotWorking/Missing.','Medium'],
  ['Physical Verification','Serial number verification','Implemented','Mobile submits serialVerified and database stores it.','PhysicalVerification.SerialVerified','AMS/src/Mobile/lib/features/audits/audits_api.dart','Retain.','Low'],
  ['Physical Verification','Verifier name auto-recorded','Implemented','Backend derives VerifiedByUserId from authenticated user; reports resolve auditor name.','PhysicalVerification.VerifiedByUserId','AMS/AMS_Consolidated_Design_v2.sql:2531; AMS/src/Backend/Modules/AMS.Modules.Verification/Features/SubmitVerification','Retain.','Low'],
  ['Physical Verification','Verification date/time auto-recorded','Implemented','VerifiedOnUtc is supported and backend can use server time.','PhysicalVerification.VerifiedOnUtc','AMS/AMS_Consolidated_Design_v2.sql:2532','Prefer trusted server timestamp; retain optional client capture timestamp separately.','Low'],
  ['Physical Verification','Remarks','Implemented','Remarks are submitted from mobile and stored.','PhysicalVerification.Remarks','AMS/src/Mobile/lib/features/audits/audits_api.dart','Retain.','Low'],
  ['Physical Verification','Update current asset status during PV','Partial','StatusUpdatedToId exists, but it is absent from SubmitVerificationRequest and current mobile verify payload.','PhysicalVerification.StatusUpdatedToId','AMS/AMS_Consolidated_Design_v2.sql:2529; AMS/src/Backend/Modules/AMS.Modules.Verification/Features/SubmitVerification/SubmitVerificationRequest.cs','Add controlled condition-to-status decision and atomic asset update.','High'],
  ['Physical Verification','Close verification cycle','Implemented','CloseVerificationCycle backend feature and audit web workflows exist.','CloseVerificationCycle','AMS/src/Backend/Modules/AMS.Modules.Verification/Features/CloseVerificationCycle','Retain and enforce completion rules.','Low'],
  ['Physical Verification','Physical verification report','Implemented','SearchVerifications API and Audit Reports web grid exist with exception counts and filters.','SearchVerifications; AuditReportsPage','AMS/src/Web/src/app/modules/audit/features/audit-reports/audit-reports.page.ts','Add explicit Excel/PDF export and evidence-photo links.','Medium'],
  ['Physical Verification','Offline capture and duplicate-safe sync','Database Ready','ClientCaptureId and unique index exist, but mobile does not send it and no offline queue was found.','PhysicalVerification.ClientCaptureId','AMS/AMS_Consolidated_Design_v2.sql:2512; AMS/src/Mobile/lib/features/audits/audits_api.dart','Generate UUID on device, persist offline queue, retry idempotently.','High'],

  ['GPS Verification','GPS coordinates','Partial','Storage/API ready; mobile capture missing.','PhysicalVerification GPS fields','AMS/AMS_Consolidated_Design_v2.sql:2524','Implement device capture.','Critical'],
  ['GPS Verification','Branch location','Partial','Branch is recorded by LocationId, but Organization.Branch has no coordinates for branch geofence validation.','Branch; PhysicalVerification.LocationId','AMS/AMS_Consolidated_Design_v2.sql:507; AMS/AMS_Consolidated_Design_v2.sql:2527','Use approved existing external master or add branch coordinates after schema approval.','High'],
  ['GPS Verification','Date/time and verifier evidence','Implemented','Verification stores authenticated verifier and timestamp.','PhysicalVerification','AMS/AMS_Consolidated_Design_v2.sql:2531','Retain.','Low'],
  ['GPS Verification','False-verification controls/geofencing','Not Covered','No distance, accuracy, mock-location or geofence validation found.','CustomerSite has coordinates; branch does not','AMS/AMS_Consolidated_Design_v2.sql:1448','Implement coordinate validation, accuracy threshold and exception flagging.','High'],

  ['Asset Transfer','Employee-to-employee transfer','Implemented','TransferType Employee, approval and completion features exist.','AssetTransferRequest; Raise/Decide/CompleteTransfer','AMS/AMS_Consolidated_Design_v2.sql:1611; AMS/src/Backend/Modules/AMS.Modules.Transfers/Features','Build web/mobile UI if business users require it; current Angular route falls to NotBuilt.','Medium'],
  ['Asset Transfer','Department-to-department transfer','Implemented','TransferType Department is supported.','AssetTransferRequest','AMS/AMS_Consolidated_Design_v2.sql:1614','Complete UI.','Medium'],
  ['Asset Transfer','Branch-to-branch transfer and shipment','Implemented','Transfer plus Movement/MovementBatch dispatch and receipt flows exist.','Transfers; Movements','AMS/AMS_Consolidated_Design_v2.sql:1515; AMS/src/Backend/Modules/AMS.Modules.Movements/Features','Complete UI.','Medium'],
  ['Asset Transfer','Cost-center-to-cost-center transfer','Implemented','TransferType CostCenter is supported.','AssetTransferRequest','AMS/AMS_Consolidated_Design_v2.sql:1618','Complete UI and SAP outbound adapter.','Medium'],
  ['Asset Transfer','Transfer approvals','Implemented','Pending/Approved/Rejected lifecycle and DecideTransfer feature exist.','AssetTransferRequest','AMS/src/Backend/Modules/AMS.Modules.Transfers/Features/DecideTransfer','Confirm approval policy.','Low'],
  ['Asset Transfer','Update employee mapping after completion','Implemented','CompleteTransfer feature and allocation infrastructure are present.','CompleteTransfer; AssetAllocation','AMS/src/Backend/Modules/AMS.Modules.Transfers/Features/CompleteTransfer','End-to-end test each transfer type.','Medium'],
  ['Asset Transfer','Synchronize transfer changes with SAP','Database Ready','SapSyncStatus exists on transfer request, but no SAP outbound transport is implemented.','AssetTransferRequest.SapSyncStatus; SapSyncLog','AMS/AMS_Consolidated_Design_v2.sql:1631','Implement SAP outbound queue/worker, acknowledgement and retry.','Critical'],

  ['Exception Reports','Assets not physically verified','Partial','Dashboard computes pending verification and verification search exists; no dedicated exception export found.','GetAssetDashboard; SearchVerifications','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard','Add cycle-aware exception report/export.','High'],
  ['Exception Reports','Assets without employee mapping / unmapped','Implemented','Dashboard exposes EmployeeMappedAssets and UnmappedAssets; asset search can support listing.','GetAssetDashboard','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard/GetAssetDashboardResponse.cs','Add dedicated downloadable detail view if needed.','Medium'],
  ['Exception Reports','Missing assets','Implemented','Missing count exists and verification condition supports Missing.','Dashboard; PhysicalVerification','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard; AMS/AMS_Consolidated_Design_v2.sql:2541','Add dedicated exception detail/export.','Medium'],
  ['Exception Reports','Duplicate assets','Partial','Unique asset/QR constraints cover exact keys, but no consolidated duplicate-detection report for serial/SAP/barcode/fuzzy matches was found.','Asset indexes and import checks','AMS/AMS_Consolidated_Design_v2.sql:1206','Create duplicate rules and exception query/export.','High'],
  ['Exception Reports','Idle assets','Not Covered','No agreed idle definition or dedicated query/report found.','Possible derivation from status, allocation and activity dates','Repository feature inventory','Define threshold and implement report.','Medium'],
  ['Exception Reports','Scrap assets','Partial','Statuses/disposal model can represent this; dedicated report not found.','AssetStatus; AssetDisposal','AMS/AMS_Consolidated_Design_v2.sql:1097','Configure status and add report/export.','Medium'],
  ['Exception Reports','Assets under repair','Implemented','Dashboard returns AssetsUnderRepair; service requests support asset issues.','GetAssetDashboard; ServiceDesk','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard/GetAssetDashboardResponse.cs','Add detailed downloadable listing.','Medium'],
  ['Exception Reports','Warranty expiry','Database Ready','Warranty dates exist, but no dedicated reminder worker/report found for purchase-detail warranty. Contracts can model Warranty separately.','AssetPurchaseDetail; Contracts','AMS/AMS_Consolidated_Design_v2.sql:904; AMS/src/Backend/Modules/AMS.Modules.Contracts','Standardize source and add report/reminder.','High'],
  ['Exception Reports','AMC due','Implemented','Contracts support AMC, covered assets, reminder windows and background reminder worker.','Contracts module','AMS/src/Backend/Modules/AMS.Modules.Contracts/Reminders/ContractReminderWorker.cs','Add dedicated UI/report if required.','Low'],
  ['Exception Reports','Employee-wise asset listing','Implemented','Allocation search and MyAssets/GetMyAssets backend features exist.','Allocations search; GetMyAssets','AMS/src/Backend/Modules/AMS.Modules.Allocations/Features/SearchAllocations','Complete broad management UI/export.','Medium'],
  ['Exception Reports','Branch-wise asset listing','Implemented','Asset search/report/dashboard break down by location.','SearchAssets; dashboard','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/SearchAssets','Add export if required.','Low'],
  ['Exception Reports','Cost-center-wise asset listing','Partial','CostCenter is stored and searchable/transferable, but dedicated report UI/export was not found.','Assets.Asset.CostCenter','AMS/AMS_Consolidated_Design_v2.sql:793','Add grouped/detail report.','Medium'],

  ['Dashboard','Total, verified, pending and missing assets','Implemented','Backend response and Angular dashboard store/page are implemented.','GetAssetDashboard; AssetDashboardPage','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard/GetAssetDashboardResponse.cs; AMS/src/Web/src/app/modules/assets/features/dashboard','Retain and validate metric definitions.','Low'],
  ['Dashboard','Employee-mapped and unmapped assets','Implemented','Both KPI values are in dashboard response.','GetAssetDashboard','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard/GetAssetDashboardResponse.cs','Retain.','Low'],
  ['Dashboard','Under repair and disposed assets','Implemented','Both KPI values are in dashboard response.','GetAssetDashboard','AMS/src/Backend/Modules/AMS.Modules.Assets/Features/GetAssetDashboard/GetAssetDashboardResponse.cs','Retain.','Low'],
  ['Dashboard','Asset value by location and department','Implemented','Both breakdown collections are implemented and rendered in reports/dashboard.','GetAssetDashboard; AssetReportsPage','AMS/src/Web/src/app/modules/assets/features/reports/asset-reports.page.ts','Retain and reconcile value source with SAP.','Low'],

  ['SAP Integration','Read asset master from SAP','Not Covered','Database landing fields and sync logs exist; connector/business synchronization code not found.','SapSync schema only','AMS/AMS_Consolidated_Design_v2.sql:2710','Implement SAP adapter and scheduled/incremental sync.','Critical'],
  ['SAP Integration','Update employee assignments in SAP','Not Covered','Allocation features exist locally; no SAP outbound assignment handler found.','Allocations + SapSync schema','Repository feature inventory','Implement outbound mapping, acknowledgement and reconciliation.','Critical'],
  ['SAP Integration','Synchronize asset transfers','Database Ready','Transfer records track SapSyncStatus, but transport is absent.','AssetTransferRequest.SapSyncStatus','AMS/AMS_Consolidated_Design_v2.sql:1631','Implement queue/worker.','Critical'],
  ['SAP Integration','Update physical verification status in SAP','Not Covered','Verification works locally; no SAP outbound PV status handler found.','Verification + SapSync schema','Repository feature inventory','Define SAP target object and implement outbound sync.','Critical'],
  ['SAP Integration','Attach verification photos/documents in SAP','Not Covered','No photo upload currently in mobile and no SAP attachment integration found.','PhotoPath storage only','AMS/src/Backend/Modules/AMS.Modules.Verification/Features/SubmitVerification/SubmitVerificationRequest.cs','Implement file storage first, then SAP attachment API.','Critical'],
  ['SAP Integration','Avoid duplicate data entry','Partial','Normalized AMS model, imports, SAP keys and sync watermark/log structures support this goal, but live SAP integration is absent.','Assets; DataImport; SapSync','AMS/AMS_Consolidated_Design_v2.sql','Complete SAP integration and reconciliation.','High'],
];

const wb = Workbook.create();
const summary = wb.worksheets.add('Executive Summary');
const coverage = wb.worksheets.add('Detailed Coverage');
const mapping = wb.worksheets.add('Existing DB Mapping');
const roadmap = wb.worksheets.add('Priority Roadmap');
const legend = wb.worksheets.add('Status Legend');
for (const s of [summary, coverage, mapping, roadmap, legend]) s.showGridLines = false;

const navy = '#17365D', blue = '#2F75B5', lightBlue = '#D9EAF7', green = '#C6E0B4', amber = '#FFE699', red = '#F4CCCC', gray = '#E7E6E6', white = '#FFFFFF', dark = '#1F2937';
const title = (sheet, range, value) => {
  sheet.getRange(range).merge(); sheet.getRange(range.split(':')[0]).values = [[value]];
  sheet.getRange(range).format = { fill: navy, font: { bold: true, color: white, size: 16 }, verticalAlignment: 'center' };
};
const header = (range) => { range.format = { fill: blue, font: { bold: true, color: white }, wrapText: true, verticalAlignment: 'center' }; };

title(summary, 'A1:H2', 'AMS Fixed Asset Requirements Coverage Assessment');
summary.getRange('A3:H3').merge(); summary.getRange('A3').values = [[`Repository assessment as of 2026-08-24 | Coverage reflects code present, not production deployment status`]];
summary.getRange('A3:H3').format = { fill: lightBlue, font: { italic: true, color: dark }, verticalAlignment: 'center' };
summary.getRange('A5:B9').values = [['Metric','Value'],['Total requirements',null],['Implemented',null],['Partial / Database Ready',null],['Not Covered',null]];
header(summary.getRange('A5:B5'));
summary.getRange('B6').formulas = [[`=COUNTA('Detailed Coverage'!$B$6:$B$${rows.length+5})`]];
summary.getRange('B7').formulas = [[`=COUNTIF('Detailed Coverage'!$C$6:$C$${rows.length+5},"Implemented")`]];
summary.getRange('B8').formulas = [[`=COUNTIF('Detailed Coverage'!$C$6:$C$${rows.length+5},"Partial")+COUNTIF('Detailed Coverage'!$C$6:$C$${rows.length+5},"Database Ready")`]];
summary.getRange('B9').formulas = [[`=COUNTIF('Detailed Coverage'!$C$6:$C$${rows.length+5},"Not Covered")`]];
summary.getRange('D5:E10').values = [['Status','Meaning'],['Implemented','Backend/database capability exists and is usable; UI/export may still need completion.'],['Partial','Some layers or required fields are implemented, but the end-to-end requirement is incomplete.'],['Database Ready','Schema supports the requirement, but business/API/UI integration is not implemented.'],['Not Covered','No substantive implementation was found.'],['Important','“Implemented” does not mean deployed, configured, integrated with SAP, or accepted by users.']];
header(summary.getRange('D5:E5'));
summary.getRange('A12:H12').merge(); summary.getRange('A12').values = [['Key conclusions']]; summary.getRange('A12:H12').format = { fill: navy, font: { bold: true, color: white } };
summary.getRange('A13:H18').merge(true); summary.getRange('A13:A18').values = [
  ['1. Strongest coverage: asset register, allocation data model, customer-site mapping, transfer backend, verification cycles/scanning, contracts/AMC reminders, and management dashboard.'],
  ['2. Critical missing capability: actual SAP S/4HANA inbound/outbound integration. Current SapSync tables are operational scaffolding, not a connector.'],
  ['3. Mobile verification currently scans and submits condition/serial/remarks, but does not submit GPS, photos, employee/department display, status change, or ClientCaptureId offline idempotency.'],
  ['4. QR/barcode values are stored and scanned, but automatic generation, label rendering, printing, affixing confirmation, and reprint control are not implemented.'],
  ['5. Designation and employee cost center are not present in the existing Employee table; they need runtime SAP/HR lookup or an approved schema change.'],
  ['6. Reports/dashboard exist, but several exception-specific listings and Excel/PDF exports still need implementation.'],
];
summary.getRange('A13:H18').format = { wrapText: true, fill: '#F8FAFC', font: { color: dark }, verticalAlignment: 'center' };
summary.getRange('A5:B9').format.borders = { preset: 'outside', style: 'thin', color: '#9CA3AF' };
summary.getRange('D5:E10').format.borders = { preset: 'outside', style: 'thin', color: '#9CA3AF' };
summary.getRange('A1:H18').format.font = { name: 'Aptos' };
summary.getRange('A:A').format.columnWidth = 27; summary.getRange('B:B').format.columnWidth = 15; summary.getRange('C:C').format.columnWidth = 4; summary.getRange('D:D').format.columnWidth = 20; summary.getRange('E:E').format.columnWidth = 72; summary.getRange('F:H').format.columnWidth = 12;
summary.getRange('1:2').format.rowHeight = 28; summary.getRange('13:18').format.rowHeight = 36;

title(coverage, 'A1:H2', 'Detailed Requirements Coverage');
coverage.getRange('A3:H3').merge(); coverage.getRange('A3').values = [['Status is based on repository evidence across SQL schema, .NET backend, Angular web app, and Flutter mobile app.']];
coverage.getRange('A3:H3').format = { fill: lightBlue, font: { italic: true, color: dark } };
const cols = ['Area','Requirement','Coverage Status','Assessment / What Exists','Existing Table / Module','Repository Evidence','Gap / Recommended Action','Priority'];
coverage.getRange('A5:H5').values = [cols]; header(coverage.getRange('A5:H5'));
coverage.getRange(`A6:H${rows.length+5}`).values = rows;
coverage.getRange(`A5:H${rows.length+5}`).format.font = { name: 'Aptos', size: 10 };
coverage.getRange(`A6:H${rows.length+5}`).format.wrapText = true;
coverage.getRange(`A6:H${rows.length+5}`).format.verticalAlignment = 'top';
coverage.getRange(`C6:C${rows.length+5}`).conditionalFormats.add('containsText',{text:'Implemented',format:{fill:green,font:{bold:true,color:'#274E13'}}});
coverage.getRange(`C6:C${rows.length+5}`).conditionalFormats.add('containsText',{text:'Partial',format:{fill:amber,font:{bold:true,color:'#7F6000'}}});
coverage.getRange(`C6:C${rows.length+5}`).conditionalFormats.add('containsText',{text:'Database Ready',format:{fill:lightBlue,font:{bold:true,color:'#1F4E78'}}});
coverage.getRange(`C6:C${rows.length+5}`).conditionalFormats.add('containsText',{text:'Not Covered',format:{fill:red,font:{bold:true,color:'#990000'}}});
coverage.getRange(`H6:H${rows.length+5}`).conditionalFormats.add('containsText',{text:'Critical',format:{fill:'#E06666',font:{bold:true,color:white}}});
coverage.getRange(`H6:H${rows.length+5}`).conditionalFormats.add('containsText',{text:'High',format:{fill:'#F4CCCC',font:{bold:true,color:'#990000'}}});
coverage.getRange(`H6:H${rows.length+5}`).conditionalFormats.add('containsText',{text:'Medium',format:{fill:amber,font:{color:'#7F6000'}}});
coverage.getRange(`A5:H${rows.length+5}`).format.borders = { insideHorizontal: { style:'thin', color:'#D9E2F3' }, bottom: { style:'thin', color:'#9CA3AF' } };
coverage.getRange('A:A').format.columnWidth=23; coverage.getRange('B:B').format.columnWidth=34; coverage.getRange('C:C').format.columnWidth=18; coverage.getRange('D:D').format.columnWidth=62; coverage.getRange('E:E').format.columnWidth=39; coverage.getRange('F:F').format.columnWidth=62; coverage.getRange('G:G').format.columnWidth=58; coverage.getRange('H:H').format.columnWidth=12;
coverage.getRange('5:5').format.rowHeight=34; coverage.freezePanes.freezeRows(5); coverage.freezePanes.freezeColumns(2);
const table = coverage.tables.add(`A5:H${rows.length+5}`, true, 'CoverageMatrix'); table.style='TableStyleMedium2';

title(mapping, 'A1:F2', 'Existing Database Mapping');
mapping.getRange('A4:F4').values = [['Requirement Area','Existing table/entity','Key fields','Current support','Important limitation','Primary schema evidence']]; header(mapping.getRange('A4:F4'));
const mapRows = [
 ['QR / Barcode','Assets.Asset','QrCodeValue, BarcodeValue','Unique QR storage; scan identifiers','No generation/printing workflow','AMS/AMS_Consolidated_Design_v2.sql:780'],
 ['SAP asset identity','Assets.Asset','ErpAssetNumber, SapAssetNumber, SapAssetClass, SapPlant, LastSapSyncOnUtc','Landing fields available','No SAP connector','AMS/AMS_Consolidated_Design_v2.sql:797'],
 ['Purchase / Warranty','Assets.AssetPurchaseDetail','VendorId, PurchaseOrderNumber, PurchaseCost, WarrantyStartDate, WarrantyEndDate','Data storage available','PO document view and warranty report incomplete','AMS/AMS_Consolidated_Design_v2.sql:897'],
 ['Capitalization / Value','Assets.AssetFinance','OriginalValue, GrossValue, FirstAcquisitionDate, PostingDate','Finance values available','Capitalization-date interpretation must be confirmed','AMS/AMS_Consolidated_Design_v2.sql:996'],
 ['Calibration','Assets.AssetInstrumentDetail','CalibrationStartDate, CalibrationEndDate, frequency, agency','Implemented','Mobile full-detail display pending','AMS/AMS_Consolidated_Design_v2.sql:1145'],
 ['Employee','Organization.Employee','EmployeeCode, FullName, DepartmentId, BranchId, ReportingManagerId','Core directory implemented','Designation and employee cost center absent','AMS/AMS_Consolidated_Design_v2.sql:562'],
 ['Employee allocation','Allocations.AssetAllocation','AssetId, EmployeeId, AllocatedOnUtc, ExpectedReturnDate','Implemented','UI coverage incomplete','AMS/AMS_Consolidated_Design_v2.sql:1280'],
 ['Acknowledgement','Allocations.AssetAcknowledgement','DocumentPath, SignatureImagePath, SignedOnUtc, ManagerApprovedOnUtc','Implemented','Secure document UX must be confirmed','AMS/AMS_Consolidated_Design_v2.sql:1330'],
 ['Installation site','Allocations.CustomerSite / AssetSiteMapping','Customer/site coordinates, mapped/removed dates, commissioned date','Implemented','Branch geofence coordinates absent','AMS/AMS_Consolidated_Design_v2.sql:1442'],
 ['Transfer','Transfers.AssetTransferRequest','Four transfer types, approval, movement, SapSyncStatus','Backend implemented','SAP transport and web UI incomplete','AMS/AMS_Consolidated_Design_v2.sql:1608'],
 ['Physical movement','Movements.MovementBatch / AssetMovement','Dispatch, courier, receipt, from/to location','Implemented','User-facing UI incomplete','AMS/AMS_Consolidated_Design_v2.sql:1515'],
 ['Physical verification','Verification.PhysicalVerification','QR, condition, serial, GPS, photo, holder, verifier, timestamp','Backend/storage implemented','Mobile omits GPS/photo/holder/status/client capture ID','AMS/AMS_Consolidated_Design_v2.sql:2508'],
 ['Repair / Replacement','ServiceDesk.ServiceRequest + approvals','AssetId, AssetIssue, categories, workflow, attachments','Repair foundation implemented','Replacement outcome needs configuration','AMS/AMS_Consolidated_Design_v2.sql:1818'],
 ['AMC / Warranty contracts','Contracts.Contract / ContractAsset','ContractType, dates, vendor, asset mapping, reminders','Implemented','Dedicated report UI may be needed','AMS/AMS_Consolidated_Design_v2.sql:2332'],
 ['SAP operations','SapSync.SapSyncLog / SapSyncWatermark','Direction, type, outcome, counters, delta watermark','Operational scaffolding','No business connector/worker','AMS/AMS_Consolidated_Design_v2.sql:2716'],
];
mapping.getRange(`A5:F${mapRows.length+4}`).values=mapRows; mapping.getRange(`A4:F${mapRows.length+4}`).format.wrapText=true; mapping.getRange(`A5:F${mapRows.length+4}`).format.verticalAlignment='top'; mapping.tables.add(`A4:F${mapRows.length+4}`,true,'DatabaseMapping').style='TableStyleMedium2';
mapping.getRange('A:A').format.columnWidth=24; mapping.getRange('B:B').format.columnWidth=40; mapping.getRange('C:C').format.columnWidth=48; mapping.getRange('D:D').format.columnWidth=32; mapping.getRange('E:E').format.columnWidth=48; mapping.getRange('F:F').format.columnWidth=55; mapping.freezePanes.freezeRows(4);

title(roadmap, 'A1:F2', 'Priority Roadmap');
roadmap.getRange('A4:F4').values=[['Sequence','Work package','Why it is required','Depends on','Expected outcome','Priority']]; header(roadmap.getRange('A4:F4'));
const roadRows = [
 [1,'SAP S/4HANA integration foundation','All SAP read/update/transfer/PV requirements depend on a real connector.','SAP API choice, credentials, field mapping','Inbound delta sync, outbound queue, retry and reconciliation','Critical'],
 [2,'Mobile GPS and photo evidence','Core audit-evidence requirement is not sent by the current mobile client.','Device permissions, secure file storage','Coordinates, accuracy, photo upload and evidence links','Critical'],
 [3,'QR/barcode generation and label printing','Values can be stored/scanned, but tags cannot be operationally produced.','Label format and printers','Secure IDs, batch PDF/print, reprint audit and affix confirmation','High'],
 [4,'Complete scan detail and holder context','Current mobile DTO omits employee, department and detailed asset fields.','API authorization and DTO design','One-scan full asset profile','High'],
 [5,'PV status update and offline idempotency','StatusUpdatedToId and ClientCaptureId are not wired through mobile.','Status policy and offline queue','Atomic status update and duplicate-safe offline sync','High'],
 [6,'Employee master gaps','Designation and employee cost center are absent.','Decision: runtime HR/SAP lookup vs schema change','Complete employee mapping','High'],
 [7,'Exception reports and exports','Several KPIs exist but not detailed downloadable reports.','Metric definitions','Cycle-aware Excel/PDF exception packs','Medium'],
 [8,'Transfer/allocation/service UI completion','Backend features are extensive but Angular routes are largely placeholders.','UX scope and role permissions','Operational end-to-end business workflows','Medium'],
 [9,'End-to-end tests and UAT','Implementation status is code-based and must be validated against deployed dependencies.','All prior work packages','Auditable acceptance evidence','High'],
];
roadmap.getRange(`A5:F${roadRows.length+4}`).values=roadRows; roadmap.getRange(`A4:F${roadRows.length+4}`).format.wrapText=true; roadmap.getRange(`A5:F${roadRows.length+4}`).format.verticalAlignment='top'; roadmap.tables.add(`A4:F${roadRows.length+4}`,true,'RoadmapTable').style='TableStyleMedium2';
roadmap.getRange('A:A').format.columnWidth=10; roadmap.getRange('B:B').format.columnWidth=34; roadmap.getRange('C:C').format.columnWidth=55; roadmap.getRange('D:D').format.columnWidth=40; roadmap.getRange('E:E').format.columnWidth=52; roadmap.getRange('F:F').format.columnWidth=12;

title(legend, 'A1:D2', 'Assessment Method and Status Legend');
legend.getRange('A4:D4').values=[['Status','Definition','Interpretation rule','Color']]; header(legend.getRange('A4:D4'));
legend.getRange('A5:D8').values=[
 ['Implemented','Substantive code and database support exists.','May still require configuration, UI completion, deployment or UAT.','Green'],
 ['Partial','Only part of the end-to-end requirement exists.','At least one important layer/field/workflow is missing.','Amber'],
 ['Database Ready','Database fields/tables support the requirement.','No working business integration/API/UI was found.','Blue'],
 ['Not Covered','No substantive implementation was found.','Requires new implementation or an approved external source.','Red'],
];
legend.getRange('A10:D10').merge(); legend.getRange('A10').values=[['Scope notes']]; legend.getRange('A10:D10').format={fill:navy,font:{bold:true,color:white}};
legend.getRange('A11:D15').merge(true); legend.getRange('A11:A15').values=[
 ['Assessment includes the consolidated SQL design, EF/backend feature slices, Angular application and Flutter mobile application.'],
 ['Repository evidence is a file/area pointer for engineering follow-up; line numbers are included where stable and useful.'],
 ['“Database Ready” must not be presented as a delivered user capability.'],
 ['Production deployment, SAP endpoint availability, secrets, infrastructure, printer compatibility and user acceptance were not verified.'],
 ['The assessment intentionally separates local AMS capability from SAP integration capability.'],
];
legend.getRange('A11:D15').format={wrapText:true,fill:'#F8FAFC'};
legend.getRange('A:A').format.columnWidth=20; legend.getRange('B:B').format.columnWidth=50; legend.getRange('C:C').format.columnWidth=60; legend.getRange('D:D').format.columnWidth=14;
legend.getRange('A5').format.fill=green; legend.getRange('A6').format.fill=amber; legend.getRange('A7').format.fill=lightBlue; legend.getRange('A8').format.fill=red;

for (const s of [mapping, roadmap, legend]) s.getUsedRange().format.font={name:'Aptos',size:10};

await fs.mkdir(outputDir,{recursive:true});
for (const [sheetName, file] of [['Executive Summary','summary.png'],['Detailed Coverage','coverage.png'],['Existing DB Mapping','mapping.png'],['Priority Roadmap','roadmap.png'],['Status Legend','legend.png']]) {
  const preview=await wb.render({sheetName,autoCrop:'all',scale:1,format:'png'});
  await fs.writeFile(`${outputDir}/${file}`,new Uint8Array(await preview.arrayBuffer()));
}
const check=await wb.inspect({kind:'table',range:'Executive Summary!A1:H18',include:'values,formulas',tableMaxRows:20,tableMaxCols:10});
console.log(check.ndjson);
const errors=await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',options:{useRegex:true,maxResults:100},summary:'formula error scan'});
console.log(errors.ndjson);
const out=await SpreadsheetFile.exportXlsx(wb); await out.save(outputPath);
console.log(outputPath);
