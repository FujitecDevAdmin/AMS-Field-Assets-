using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Web.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public static class ImportAssetsExcelEndpoint
{
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);

        group.MapPost("/imports/excel", async (
                [Microsoft.AspNetCore.Mvc.FromForm] ImportAssetsExcelRequest request,
                IDispatcher dispatcher,
                CancellationToken ct) =>
            {
                var command = await ImportAssetsExcelMapper.ToCommandAsync(request, ct);
                var result = await dispatcher.SendAsync(command, ct);
                return result.ToHttpResult();
            })
            .DisableAntiforgery()
            .RequireCapability(Capabilities.Assets.FieldAssetManage)
            .WithName("ImportAssetsExcel")
            .Accepts<ImportAssetsExcelRequest>("multipart/form-data")
            .Produces<ImportAssetsExcelResponse>()
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status400BadRequest);
    }
}
