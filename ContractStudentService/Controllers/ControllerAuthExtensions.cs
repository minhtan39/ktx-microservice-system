using System.Security.Claims;

namespace ContractStudentService.Controllers;

internal static class ControllerAuthExtensions
{
    public static bool IsOperationsUser(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Staff");

    public static long? CurrentStudentId(this ClaimsPrincipal user)
    {
        var rawStudentId = user.FindFirstValue("studentId");

        return long.TryParse(rawStudentId, out var studentId)
            ? studentId
            : null;
    }
}
