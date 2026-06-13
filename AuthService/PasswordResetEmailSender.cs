using System.Net;
using System.Net.Mail;

public sealed class PasswordResetEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PasswordResetEmailSender> _logger;

    public PasswordResetEmailSender(
        IConfiguration configuration,
        ILogger<PasswordResetEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Email:Username"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:Password"]);

    public async Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        string resetUrl)
    {
        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var username = _configuration["Email:Username"]
            ?? throw new InvalidOperationException("Email username is not configured.");
        var password = _configuration["Email:Password"]
            ?? throw new InvalidOperationException("Email password is not configured.");
        var fromEmail = _configuration["Email:FromEmail"] ?? username;
        var fromName = _configuration["Email:FromName"] ?? "DormManager";

        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeUrl = WebUtility.HtmlEncode(resetUrl);

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = "Xác nhận đặt lại mật khẩu DormManager",
            IsBodyHtml = true,
            Body = $"""
                <div style="font-family:Arial,sans-serif;line-height:1.6;color:#111827">
                  <h2 style="color:#0f7f51">Đặt lại mật khẩu DormManager</h2>
                  <p>Xin chào {safeName},</p>
                  <p>Hệ thống nhận được yêu cầu đặt lại mật khẩu cho tài khoản sinh viên của bạn.</p>
                  <p>
                    <a href="{safeUrl}" style="display:inline-block;padding:12px 18px;background:#169b63;color:#fff;text-decoration:none;border-radius:6px;font-weight:700">
                      Đặt lại mật khẩu
                    </a>
                  </p>
                  <p>Liên kết có hiệu lực trong 30 phút và chỉ sử dụng được một lần.</p>
                  <p>Nếu bạn không gửi yêu cầu này, hãy bỏ qua email.</p>
                </div>
                """
        };

        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:EnableSsl", true),
            Credentials = new NetworkCredential(username, password)
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation("Password reset email sent to student account {Recipient}.", recipientEmail);
    }
}
