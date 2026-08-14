using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public sealed class ImportAssetsExcelHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<ImportAssetsExcelCommand, ImportAssetsExcelResponse>
{
    private static readonly string[] TemplateHeaders =
    [
        "Sl.no.", "Branch", "Asset No", "Asset Name", "ManufactureSerialNumber", "TechnicalGroup",
        "Asset Class", "Location", "OpportunityName", "PhysicalCondition", "Capitalized Quantity",
        "Disposal Qty", "Gross Qty", "Orignal Value", "Migrated Book Value", "Additional Value",
        "Gross Value", "Disposal Gross Value", "Current Gross Value", "Deprecitaion Method",
        "Depreciation Percentage", "Additions During the year", "Acc. Dep. as of beginning of Year",
        "Depreciation Charged for the year", "Acc. Dep. as of End of Year", "Net Book Value",
        "Asset Class Code", "Asset Category", "Asset Description", "Narration", "Status", "AUCNo",
        "VoucherNo", "Year of Purchase", "Posting Date", "First Acquisition Date", "Disposal Date",
        "Asset Useful Life", "Cost Centre", "AP VoucherNo", "Invoice No", "Invoice Date", "Vendor Name",
        "Purchase Order No", "GRN Number", "Gross Value COA", "Gross Value COA Description",
        "Accumulated Depreciation COA", "Accumulated Depreciation COA Description", "Depreciation COA",
        "Depreciation COA Description", "WarrantyPeriodsInMonth", "InsurancePolicyNumber",
        "InsurancePolicyType", "InsurancePolicyStartDate", "InsurancePolicyEndDate", "EmployeeUniqueID",
        "EmployeeName", "EmpEMailAddress", "ContractNo", "CalibrationStartDate", "CalibrationEndDate",
        "WarrantyPeriodStartDate", "WarrantyPeriodEndDate", "Auditor Name", "Auditor Company Name",
        "Verified", "Auditor Remarks", "Make", "Model",
    ];

    private static readonly string[] PurchaseHeaders =
    [
        "Purchase Order No", "Invoice No", "Invoice Date", "WarrantyPeriodStartDate", "WarrantyPeriodEndDate",
    ];

    public async Task<Result<ImportAssetsExcelResponse>> HandleAsync(
        ImportAssetsExcelCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<IReadOnlyDictionary<string, string?>> rows;
        try
        {
            using var stream = new MemoryStream(request.Content, writable: false);
            rows = AssetExcelWorkbook.Read(stream);
        }
        catch (InvalidDataException exception)
        {
            return Error.Validation("AssetImport.InvalidWorkbook", exception.Message);
        }

        if (rows.Count == 0)
        {
            return Error.Validation("AssetImport.EmptyWorkbook", "The workbook contains no asset rows.");
        }

        var now = clock.UtcNow;
        var username = currentUser.Username;
        var types = await db.AssetTypes.ToDictionaryAsync(type => type.TypeName, StringComparer.OrdinalIgnoreCase, ct);
        var classes = await db.AssetClasses.ToListAsync(ct);
        var statuses = await db.AssetStatuses.ToListAsync(ct);
        var stockStatus = statuses.FirstOrDefault(status => status.StatusName == "In Stock")
            ?? statuses.FirstOrDefault(status => status.IsActive);
        if (stockStatus is null)
        {
            return Error.Validation("AssetImport.NoActiveStatus", "Create an active asset status before importing.");
        }

        var createdTypes = 0;
        foreach (var group in rows.GroupBy(row => Text(row, "TechnicalGroup"), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(group.Key) || types.ContainsKey(group.Key))
            {
                continue;
            }

            var isBulk = group.Any(row => Decimal(row, "Capitalized Quantity") > 1m);
            var type = new AssetType
            {
                TypeName = Limit(group.Key, 100)!,
                IsAllocatable = !isBulk,
                IsPhysical = true,
                IsBulkDefault = isBulk,
                IsActive = true,
                CreatedOnUtc = now,
                CreatedBy = username,
            };
            db.AssetTypes.Add(type);
            types[type.TypeName] = type;
            createdTypes++;
        }

        var accounts = await db.ChartOfAccounts.ToDictionaryAsync(account => account.CoaCode, StringComparer.OrdinalIgnoreCase, ct);
        AddAccounts(rows, accounts, now, username);
        await db.SaveChangesAsync(ct);

        var existingAssets = await db.Assets.ToDictionaryAsync(asset => asset.AssetNumber, StringComparer.OrdinalIgnoreCase, ct);
        var errors = new List<ImportAssetsExcelResponse.RowError>();
        var skippedRowDetails = new List<ImportAssetsExcelResponse.SkippedRow>();
        var imported = new List<(Asset Asset, IReadOnlyDictionary<string, string?> Row)>();
        var processed = new List<(Asset Asset, IReadOnlyDictionary<string, string?> Row)>();
        var reactivated = 0;
        var skipped = 0;

        for (var index = 0; index < rows.Count; index++)
        {
            var row = rows[index];
            var rowNumber = index + 2;
            var assetNumber = Limit(Text(row, "Asset No"), 40);
            var assetName = Limit(Text(row, "Asset Name"), 200);
            var typeName = Text(row, "TechnicalGroup");
            var missingFields = new List<string>(3);
            if (string.IsNullOrWhiteSpace(assetNumber))
            {
                missingFields.Add("Asset No");
            }

            if (string.IsNullOrWhiteSpace(assetName))
            {
                missingFields.Add("Asset Name");
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                missingFields.Add("TechnicalGroup");
            }

            if (missingFields.Count > 0)
            {
                var reason = missingFields.Count == 1
                    ? $"{missingFields[0]} is required."
                    : $"{string.Join(", ", missingFields)} are required.";
                errors.Add(new(rowNumber, reason));
                skippedRowDetails.Add(new(rowNumber, row, reason));
                skipped++;
                continue;
            }

            if (!types.TryGetValue(typeName!, out var type))
            {
                var reason = $"TechnicalGroup '{typeName}' could not be resolved.";
                errors.Add(new(rowNumber, reason));
                skippedRowDetails.Add(new(rowNumber, row, reason));
                skipped++;
                continue;
            }

            var validAssetNumber = assetNumber!;
            var validAssetName = assetName!;

            var importedData = TemplateHeaders
                .Concat(row.Keys.Where(header => !TemplateHeaders.Contains(header, StringComparer.OrdinalIgnoreCase)))
                .ToDictionary(header => header, header => Text(row, header), StringComparer.OrdinalIgnoreCase);
            var importedJson = JsonSerializer.Serialize(importedData);
            if (existingAssets.TryGetValue(validAssetNumber, out var existingAsset))
            {
                existingAsset.ImportedDataJson = importedJson;
                existingAsset.ModifiedOnUtc = now;
                existingAsset.ModifiedBy = username;

                if (existingAsset.IsDeleted)
                {
                    existingAsset.IsDeleted = false;
                    existingAsset.AssetName = validAssetName;
                    existingAsset.SerialNumber = Limit(Text(row, "ManufactureSerialNumber"), 100);
                    existingAsset.AssetTypeId = type.Id;
                    existingAsset.Make = Limit(Text(row, "Make"), 100);
                    existingAsset.Model = Limit(Text(row, "Model"), 100);
                    existingAsset.CostCenter = Limit(Text(row, "Cost Centre"), 40);
                    existingAsset.AcquisitionDate = Date(row, "First Acquisition Date");
                    existingAsset.Remarks = Limit(JoinRemarks(row), 1000);
                    processed.Add((existingAsset, row));
                    reactivated++;
                    continue;
                }

                var reason = $"Asset number '{validAssetNumber}' already exists; its imported detail was refreshed.";
                errors.Add(new(rowNumber, reason));
                skippedRowDetails.Add(new(rowNumber, row, reason));
                skipped++;
                continue;
            }

            var quantity = Decimal(row, "Capitalized Quantity") ?? Decimal(row, "Gross Qty") ?? 1m;
            if (quantity <= 0)
            {
                const string reason = "Capitalized Quantity must be greater than zero.";
                errors.Add(new(rowNumber, reason));
                skippedRowDetails.Add(new(rowNumber, row, reason));
                skipped++;
                continue;
            }

            var isBulk = quantity > 1m;
            var classCode = Text(row, "Asset Class Code");
            var className = Text(row, "Asset Class");
            var assetClass = classes.FirstOrDefault(item =>
                string.Equals(item.ClassCode, classCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.ClassName, className, StringComparison.OrdinalIgnoreCase));
            var remarks = JoinRemarks(row);
            var asset = new Asset
            {
                AssetNumber = validAssetNumber,
                AssetName = validAssetName,
                SerialNumber = Limit(Text(row, "ManufactureSerialNumber"), 100),
                AssetTypeId = type.Id,
                AssetClassId = assetClass?.Id,
                Make = Limit(Text(row, "Make"), 100),
                Model = Limit(Text(row, "Model"), 100),
                AssetStatusId = stockStatus.Id,
                CostCenter = Limit(Text(row, "Cost Centre"), 40),
                AcquisitionDate = Date(row, "First Acquisition Date"),
                SapAssetNumber = Limit(Text(row, "Asset No"), 50),
                SapAssetClass = Limit(classCode, 50),
                Remarks = Limit(remarks, 1000),
                ImportedDataJson = importedJson,
                IsBulk = isBulk,
                Quantity = isBulk ? quantity : 1m,
                UnitOfMeasure = isBulk ? "Nos" : null,
                IsDeleted = false,
                ConcurrencyStamp = Guid.NewGuid(),
                CreatedOnUtc = now,
                CreatedBy = username,
            };
            db.Assets.Add(asset);
            existingAssets[validAssetNumber] = asset;
            imported.Add((asset, row));
            processed.Add((asset, row));
        }

        await db.SaveChangesAsync(ct);

        await AddCustomFieldsAsync(processed, now, username, ct);

        foreach (var (asset, row) in imported)
        {
            AddFinance(asset, row, accounts, now, username);
            AddDepreciation(asset, row, now);
            AddPurchase(asset, row, now, username);
            AddInstrument(asset, row, now, username);
        }

        await db.SaveChangesAsync(ct);

        return new ImportAssetsExcelResponse(
            rows.Count,
            imported.Count,
            reactivated,
            skipped,
            createdTypes,
            skippedRowDetails,
            errors.Take(100).ToArray());
    }

    private async Task AddCustomFieldsAsync(
        List<(Asset Asset, IReadOnlyDictionary<string, string?> Row)> processed,
        DateTime now,
        string username,
        CancellationToken ct)
    {
        var extraHeaders = processed
            .SelectMany(item => item.Row.Keys)
            .Where(header => !TemplateHeaders.Contains(header, StringComparer.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (extraHeaders.Length == 0 || processed.Count == 0)
        {
            return;
        }

        var typeIds = processed.Select(item => item.Asset.AssetTypeId).Distinct().ToArray();
        var definitions = await db.CustomFieldDefinitions
            .Where(definition => typeIds.Contains(definition.AssetTypeId))
            .ToListAsync(ct);
        var definitionLookup = definitions.ToDictionary(
            definition => $"{definition.AssetTypeId}:{definition.FieldName}",
            StringComparer.OrdinalIgnoreCase);

        foreach (var typeId in typeIds)
        {
            var displayOrder = definitions
                .Where(definition => definition.AssetTypeId == typeId)
                .Select(definition => definition.DisplayOrder)
                .DefaultIfEmpty(0)
                .Max();
            foreach (var header in extraHeaders)
            {
                var fieldName = CustomFieldName(header);
                var key = $"{typeId}:{fieldName}";
                if (definitionLookup.ContainsKey(key))
                {
                    continue;
                }

                var definition = new CustomFieldDefinition
                {
                    AssetTypeId = typeId,
                    FieldName = fieldName,
                    DisplayLabel = Limit(header, 150)!,
                    FieldType = "Text",
                    IsRequired = false,
                    DisplayOrder = ++displayOrder,
                    IsActive = true,
                    CreatedOnUtc = now,
                    CreatedBy = username,
                };
                db.CustomFieldDefinitions.Add(definition);
                definitions.Add(definition);
                definitionLookup[key] = definition;
            }
        }

        await db.SaveChangesAsync(ct);

        var assetIds = processed.Select(item => item.Asset.Id).ToArray();
        var existingValues = await db.AssetCustomValues
            .Where(value => assetIds.Contains(value.AssetId))
            .ToDictionaryAsync(value => $"{value.AssetId}:{value.CustomFieldDefinitionId}", ct);
        foreach (var (asset, row) in processed)
        {
            foreach (var header in extraHeaders)
            {
                var definition = definitionLookup[$"{asset.AssetTypeId}:{CustomFieldName(header)}"];
                var key = $"{asset.Id}:{definition.Id}";
                var value = Limit(Text(row, header), 1000);
                if (existingValues.TryGetValue(key, out var existingValue))
                {
                    existingValue.Value = value;
                    existingValue.UpdatedOnUtc = now;
                    existingValue.UpdatedBy = username;
                    continue;
                }

                var customValue = new AssetCustomValue
                {
                    AssetId = asset.Id,
                    CustomFieldDefinitionId = definition.Id,
                    Value = value,
                    UpdatedOnUtc = now,
                    UpdatedBy = username,
                };
                db.AssetCustomValues.Add(customValue);
                existingValues[key] = customValue;
            }
        }
    }

    private static string CustomFieldName(string header)
    {
        var normalized = string.Concat(header.Trim().Select(character =>
            char.IsLetterOrDigit(character) ? character : '_'));
        if (normalized.Length <= 80)
        {
            return normalized;
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(header)))[..8];
        return $"{normalized[..71]}_{hash}";
    }

    private void AddAccounts(
        IEnumerable<IReadOnlyDictionary<string, string?>> rows,
        Dictionary<string, ChartOfAccount> accounts,
        DateTime now,
        string username)
    {
        var pairs = new[]
        {
            ("Gross Value COA", "Gross Value COA Description"),
            ("Accumulated Depreciation COA", "Accumulated Depreciation COA Description"),
            ("Depreciation COA", "Depreciation COA Description"),
        };
        foreach (var row in rows)
        {
            foreach (var (codeHeader, descriptionHeader) in pairs)
            {
                var code = Limit(Text(row, codeHeader), 30);
                if (string.IsNullOrWhiteSpace(code) || accounts.ContainsKey(code))
                {
                    continue;
                }

                var account = new ChartOfAccount
                {
                    CoaCode = code,
                    Description = Limit(Text(row, descriptionHeader), 200),
                    IsActive = true,
                    CreatedOnUtc = now,
                    CreatedBy = username,
                };
                db.ChartOfAccounts.Add(account);
                accounts[code] = account;
            }
        }
    }

    private void AddFinance(
        Asset asset,
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, ChartOfAccount> accounts,
        DateTime now,
        string username)
    {
        db.AssetFinances.Add(new AssetFinance
        {
            AssetId = asset.Id,
            OriginalValue = Decimal(row, "Orignal Value"),
            MigratedBookValue = Decimal(row, "Migrated Book Value"),
            AdditionalValue = Decimal(row, "Additional Value"),
            GrossValue = Decimal(row, "Current Gross Value") ?? Decimal(row, "Gross Value"),
            DisposalGrossValue = Decimal(row, "Disposal Gross Value"),
            AccumulatedDepreciation = Decimal(row, "Acc. Dep. as of End of Year"),
            NetBookValue = Decimal(row, "Net Book Value"),
            DepreciationMethod = DepreciationMethod(Text(row, "Deprecitaion Method")),
            DepreciationPercent = Decimal(row, "Depreciation Percentage"),
            UsefulLifeMonths = Integer(row, "Asset Useful Life"),
            CapitalisedQuantity = Decimal(row, "Capitalized Quantity"),
            FirstAcquisitionDate = Date(row, "First Acquisition Date"),
            PostingDate = Date(row, "Posting Date"),
            SapPostingStatus = Limit(Text(row, "Status"), 20),
            AucReference = Limit(Text(row, "AUCNo"), 50),
            OpportunityName = Limit(Text(row, "OpportunityName"), 200),
            VoucherNo = Limit(Text(row, "VoucherNo"), 60),
            ApVoucherNo = Limit(Text(row, "AP VoucherNo"), 60),
            GrossValueCoaId = AccountId(row, "Gross Value COA", accounts),
            AccumulatedDepreciationCoaId = AccountId(row, "Accumulated Depreciation COA", accounts),
            DepreciationChargeCoaId = AccountId(row, "Depreciation COA", accounts),
            LastSyncedOnUtc = now,
            CreatedOnUtc = now,
            CreatedBy = username,
        });
    }

    private void AddDepreciation(Asset asset, IReadOnlyDictionary<string, string?> row, DateTime now)
    {
        var year = Integer(row, "Year of Purchase") ?? Date(row, "Posting Date")?.Year ?? now.Year;
        if (year is < 1900 or > 9999)
        {
            return;
        }

        db.AssetDepreciationEntries.Add(new AssetDepreciationEntry
        {
            AssetId = asset.Id,
            FinancialYear = checked((short)year),
            OpeningAccumulated = Decimal(row, "Acc. Dep. as of beginning of Year") ?? 0m,
            Additions = Decimal(row, "Additions During the year") ?? 0m,
            ChargedForPeriod = Decimal(row, "Depreciation Charged for the year") ?? 0m,
            ClosingAccumulated = Decimal(row, "Acc. Dep. as of End of Year") ?? 0m,
            NetBookValueAtClose = Decimal(row, "Net Book Value") ?? 0m,
            SourceSystem = "Import",
            SyncedOnUtc = now,
        });
    }

    private void AddPurchase(Asset asset, IReadOnlyDictionary<string, string?> row, DateTime now, string username)
    {
        if (PurchaseHeaders
            .All(header => string.IsNullOrWhiteSpace(Text(row, header))))
        {
            return;
        }

        var warrantyStart = Date(row, "WarrantyPeriodStartDate");
        var warrantyEnd = ValidEndDate(warrantyStart, Date(row, "WarrantyPeriodEndDate"));

        db.AssetPurchaseDetails.Add(new AssetPurchaseDetail
        {
            AssetId = asset.Id,
            PurchaseOrderNumber = Limit(Text(row, "Purchase Order No"), 50),
            InvoiceNumber = Limit(Text(row, "Invoice No"), 50),
            PurchaseDate = Date(row, "Invoice Date"),
            PurchaseCost = Decimal(row, "Orignal Value"),
            WarrantyStartDate = warrantyStart,
            WarrantyEndDate = warrantyEnd,
            CreatedOnUtc = now,
            CreatedBy = username,
        });
    }

    private void AddInstrument(Asset asset, IReadOnlyDictionary<string, string?> row, DateTime now, string username)
    {
        var start = Date(row, "CalibrationStartDate");
        var end = ValidEndDate(start, Date(row, "CalibrationEndDate"));
        if (start is null && end is null)
        {
            return;
        }

        db.AssetInstrumentDetails.Add(new AssetInstrumentDetail
        {
            AssetId = asset.Id,
            CalibrationStartDate = start,
            CalibrationEndDate = end,
            CreatedOnUtc = now,
            CreatedBy = username,
        });
    }

    private static int? AccountId(
        IReadOnlyDictionary<string, string?> row,
        string header,
        IReadOnlyDictionary<string, ChartOfAccount> accounts)
    {
        var code = Text(row, header);
        return code is not null && accounts.TryGetValue(code, out var account) ? account.Id : null;
    }

    private static string? Text(IReadOnlyDictionary<string, string?> row, string header) =>
        row.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

    private static decimal? Decimal(IReadOnlyDictionary<string, string?> row, string header) =>
        decimal.TryParse(Text(row, header), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static int? Integer(IReadOnlyDictionary<string, string?> row, string header) =>
        int.TryParse(Text(row, header), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static DateOnly? Date(IReadOnlyDictionary<string, string?> row, string header)
    {
        var text = Text(row, header);
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial))
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        }

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var value)
            ? DateOnly.FromDateTime(value)
            : null;
    }

    private static DateOnly? ValidEndDate(DateOnly? start, DateOnly? end) =>
        start.HasValue && end.HasValue && end.Value < start.Value ? null : end;

    private static string? DepreciationMethod(string? source) => source?.Trim().ToUpperInvariant() switch
    {
        "STRAIGHT LINE METHOD" => "StraightLine",
        "WRITTEN DOWN VALUE" => "WrittenDownValue",
        "NONE" => "None",
        _ => null,
    };

    private static string JoinRemarks(IReadOnlyDictionary<string, string?> row) => string.Join(
        " | ",
        new[]
        {
            Text(row, "PhysicalCondition"), Text(row, "Asset Description"), Text(row, "Narration"),
            Text(row, "Auditor Remarks"),
        }.Where(value => !string.IsNullOrWhiteSpace(value)));

    private static string? Limit(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, length)];
}
