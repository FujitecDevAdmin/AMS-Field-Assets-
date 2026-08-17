using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Features.ListAuditorLocations;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.CreateAuditorLocation;

public sealed class CreateAuditorLocationHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<CreateAuditorLocationCommand, CreateAuditorLocationResponse>
{
    public async Task<Result<CreateAuditorLocationResponse>> HandleAsync(
        CreateAuditorLocationCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var registryType = await db.AssetTypes
            .SingleOrDefaultAsync(type => type.TypeName == ListAuditorLocationsHandler.RegistryTypeName, ct);

        if (registryType is null)
        {
            registryType = new AssetType
            {
                TypeName = ListAuditorLocationsHandler.RegistryTypeName,
                IsAllocatable = false,
                IsPhysical = false,
                IsBulkDefault = false,
                TracksHardware = false,
                TracksSoftware = false,
                TracksVehicle = false,
                TracksCalibration = false,
                IsActive = false,
                CreatedOnUtc = clock.UtcNow,
                CreatedBy = currentUser.Username,
            };
            db.AssetTypes.Add(registryType);
            await db.SaveChangesAsync(ct);
        }

        var duplicate = await db.CustomFieldDefinitions.AnyAsync(
            field => field.AssetTypeId == registryType.Id
                && field.IsActive
                && field.DisplayLabel == request.LocationName,
            ct);
        if (duplicate)
        {
            return Error.Conflict("AuditorLocation.NameTaken", "That assigned location already exists.");
        }

        var nextLocationId = (await db.CustomFieldDefinitions
            .Where(field => field.AssetTypeId == registryType.Id)
            .MaxAsync(field => (int?)field.DisplayOrder, ct) ?? 0) + 1;

        var definition = new CustomFieldDefinition
        {
            AssetTypeId = registryType.Id,
            FieldName = $"AuditorLocation_{nextLocationId}",
            DisplayLabel = request.LocationName,
            FieldType = "Text",
            IsRequired = false,
            DisplayOrder = nextLocationId,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };
        db.CustomFieldDefinitions.Add(definition);
        await db.SaveChangesAsync(ct);

        return new CreateAuditorLocationResponse(
            definition.Id,
            nextLocationId,
            definition.DisplayLabel);
    }
}
