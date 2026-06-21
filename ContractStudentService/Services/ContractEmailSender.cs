using ContractStudentService.Entities;
using System.Net;
using System.Net.Mail;

namespace ContractStudentService.Services;

public sealed class ContractEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContractEmailSender> _logger;

    public ContractEmailSender(
        IConfiguration configuration,
        ILogger<ContractEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendTemplateReadyAsync(
        Student student,
        Contract contract,
        string templatePath)
    {
        await SendContractEmailAsync(
            student,
            contract,
            templatePath,
            $"Hop dong noi tru {contract.ContractCode}",
            "Nha truong da phat hanh file hop dong noi tru. Vui long dang nhap he thong DormManager de xem va ky online.");
    }

    public async Task SendSignedContractAsync(
        Student student,
        Contract contract,
        string signedPath)
    {
        await SendContractEmailAsync(
            student,
            contract,
            signedPath,
            $"Hop dong da ky {contract.ContractCode}",
            "He thong da ghi nhan chu ky online va tao file hop dong da ky. File PDF da ky duoc dinh kem trong email nay.");
    }

    private async Task SendContractEmailAsync(
        Student student,
        Contract contract,
        string attachmentPath,
        string subject,
        string body)
    {
        if (string.IsNullOrWhiteSpace(student.Email) ||
            !File.Exists(attachmentPath) ||
            !IsConfigured())
        {
            return;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(
                    _configuration["Email:FromEmail"] ?? _configuration["Email:Username"]!,
                    _configuration["Email:FromName"] ?? "DormManager Contract"),
                Subject = subject,
                Body = $"""
                    Xin chao {student.FullName},

                    {body}

                    Ma hop dong: {contract.ContractCode}
                    Cong thong tin: {_configuration["Frontend:BaseUrl"] ?? "DormManager"}

                    Tran trong.
                    """,
            };

            message.To.Add(student.Email);
            message.Attachments.Add(new Attachment(attachmentPath, "application/pdf"));

            using var client = new SmtpClient(
                _configuration["Email:SmtpHost"] ?? "smtp.gmail.com",
                int.TryParse(_configuration["Email:SmtpPort"], out var port) ? port : 587)
            {
                EnableSsl = bool.TryParse(_configuration["Email:EnableSsl"], out var enableSsl)
                    ? enableSsl
                    : true,
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"],
                    string.Concat((_configuration["Email:Password"] ?? string.Empty)
                        .Where(character => !char.IsWhiteSpace(character))))
            };

            await client.SendMailAsync(message);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Could not send contract email for contract {ContractCode} to {Email}.",
                contract.ContractCode,
                student.Email);
        }
    }

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_configuration["Email:Username"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Email:Password"]);
    }
}
