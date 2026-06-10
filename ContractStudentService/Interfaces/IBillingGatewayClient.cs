using ContractStudentService.Entities;

namespace ContractStudentService.Interfaces;

public interface IBillingGatewayClient
{
    Task CreateContractBillingAsync(Contract contract);
}
