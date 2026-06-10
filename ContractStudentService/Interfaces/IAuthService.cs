using ContractStudentService.DTOs.Auth;

namespace ContractStudentService.Interfaces;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterDto dto);

    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
}