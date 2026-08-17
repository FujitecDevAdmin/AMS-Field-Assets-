namespace AMS.Modules.Assets.Features.CreateAuditorLocation;

public static class CreateAuditorLocationMapper
{
    public static CreateAuditorLocationCommand ToCommand(CreateAuditorLocationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CreateAuditorLocationCommand(request.LocationName.Trim());
    }
}
