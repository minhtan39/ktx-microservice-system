public sealed class PaymentQrBuilder
{
    private readonly IConfiguration _configuration;

    public PaymentQrBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BankCode => _configuration["Payment:BankCode"]?.Trim() ?? string.Empty;
    public string AccountNumber => _configuration["Payment:AccountNumber"]?.Trim() ?? string.Empty;
    public string AccountName => _configuration["Payment:AccountName"]?.Trim() ?? string.Empty;
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BankCode) &&
        !string.IsNullOrWhiteSpace(AccountNumber);

    public string Build(decimal amount, string paymentCode)
    {
        if (!IsConfigured)
            return string.Empty;

        var template = _configuration["Payment:QrTemplate"]?.Trim() ?? "compact2";
        var account = Uri.EscapeDataString(AccountNumber);
        var bank = Uri.EscapeDataString(BankCode);
        var info = Uri.EscapeDataString(paymentCode);
        var name = Uri.EscapeDataString(AccountName);
        var roundedAmount = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);

        return $"https://img.vietqr.io/image/{bank}-{account}-{template}.png" +
               $"?amount={roundedAmount:0}&addInfo={info}&accountName={name}";
    }
}
