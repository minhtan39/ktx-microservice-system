using System.Net.Http.Json;
using ContractStudentService.DTOs.Integration;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class BillingGatewayClient : IBillingGatewayClient
{
    private readonly HttpClient _httpClient;

    public BillingGatewayClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Gateway");
    }

    public async Task CreateContractBillingAsync(Contract contract)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/billing/contracts",
            new ContractBillingRequestDto
            {
                ContractId = contract.Id,
                ContractCode = contract.ContractCode,
                StudentId = contract.StudentId,
                RoomId = contract.RoomId,
                DepositAmount = contract.DepositAmount,
                MonthlyFee = contract.MonthlyFee,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate
            });

        if (!response.IsSuccessStatusCode)
            throw new Exception("BillingService tao khoan thu tu hop dong that bai.");
    }
}
