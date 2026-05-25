using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace SistemaEvaluacionAcademica.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    protected Guid UserId   => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    protected string UserRole => User.FindFirstValue(ClaimTypes.Role)!;
}
