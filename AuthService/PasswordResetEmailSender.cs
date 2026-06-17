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
        var username = _configuration["Email:Username"]?.Trim()
            ?? throw new InvalidOperationException("Email username is not configured.");
        var configuredPassword = _configuration["Email:Password"]
            ?? throw new InvalidOperationException("Email password is not configured.");
        var password = string.Concat(configuredPassword.Where(character => !char.IsWhiteSpace(character)));
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
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation("Password reset email sent to student account {Recipient}.", recipientEmail);
    }

    public async Task SendStudentInvitationAsync(
        string recipientEmail,
        string recipientName,
        string activationUrl,
        string studentCode)
    {
        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var username = _configuration["Email:Username"]?.Trim()
            ?? throw new InvalidOperationException("Email username is not configured.");
        var configuredPassword = _configuration["Email:Password"]
            ?? throw new InvalidOperationException("Email password is not configured.");
        var password = string.Concat(configuredPassword.Where(character => !char.IsWhiteSpace(character)));
        var fromEmail = _configuration["Email:FromEmail"] ?? username;
        var fromName = _configuration["Email:FromName"] ?? "DormManager";

        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeUrl = WebUtility.HtmlEncode(activationUrl);
        var safeStudentCode = WebUtility.HtmlEncode(studentCode);

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = "Kích hoạt tài khoản ký túc xá DormManager",
            IsBodyHtml = true,
            Body = $"""
                <div style="font-family:Arial,sans-serif;line-height:1.6;color:#111827">
                  <h2 style="color:#0f7f51">Kích hoạt tài khoản ký túc xá</h2>
                  <p>Xin chào {safeName},</p>
                  <p>Nhà trường đã tạo hồ sơ nội trú cho bạn trên DormManager.</p>
                  <p><b>Tên đăng nhập:</b> {safeStudentCode}</p>
                  <p>Hãy bấm nút bên dưới để đặt mật khẩu riêng và đăng nhập cổng sinh viên.</p>
                  <p>
                    <a href="{safeUrl}" style="display:inline-block;padding:12px 18px;background:#169b63;color:#fff;text-decoration:none;border-radius:6px;font-weight:700">
                      Kích hoạt tài khoản
                    </a>
                  </p>
                  <p>Liên kết có hiệu lực trong thời gian ngắn và chỉ sử dụng được một lần.</p>
                </div>
                """
        };

        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:EnableSsl", true),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation("Student invitation email sent to {Recipient}.", recipientEmail);
    }

    public async Task SendAccountAccessLinkAsync(
        string recipientEmail,
        string recipientName,
        string accessUrl,
        string username,
        bool isActivation)
    {
        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var smtpUsername = _configuration["Email:Username"]?.Trim()
            ?? throw new InvalidOperationException("Email username is not configured.");
        var configuredPassword = _configuration["Email:Password"]
            ?? throw new InvalidOperationException("Email password is not configured.");
        var password = string.Concat(configuredPassword.Where(character => !char.IsWhiteSpace(character)));
        var fromEmail = _configuration["Email:FromEmail"] ?? smtpUsername;
        var fromName = _configuration["Email:FromName"] ?? "DormManager";

        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeUrl = WebUtility.HtmlEncode(accessUrl);
        var safeUsername = WebUtility.HtmlEncode(username);
        var title = isActivation
            ? "Kich hoat tai khoan DormManager"
            : "Dat lai mat khau DormManager";
        var action = isActivation
            ? "Kich hoat tai khoan"
            : "Dat lai mat khau";

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = title,
            IsBodyHtml = true,
            Body = $"""
                <div style="font-family:Arial,sans-serif;line-height:1.6;color:#111827">
                  <h2 style="color:#0f7f51">{title}</h2>
                  <p>Xin chao {safeName},</p>
                  <p>Quan tri vien DormManager da gui lien ket bao mat cho tai khoan cua ban.</p>
                  <p><b>Ten dang nhap:</b> {safeUsername}</p>
                  <p>
                    <a href="{safeUrl}" style="display:inline-block;padding:12px 18px;background:#169b63;color:#fff;text-decoration:none;border-radius:6px;font-weight:700">
                      {action}
                    </a>
                  </p>
                  <p>Lien ket co hieu luc trong thoi gian ngan va chi su dung duoc mot lan.</p>
                  <p>Neu ban khong yeu cau thao tac nay, hay bao lai quan tri vien ky tuc xa.</p>
                </div>
                """
        };

        message.To.Add(new MailAddress(recipientEmail, recipientName));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:EnableSsl", true),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(smtpUsername, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation("Account access link sent to {Recipient}.", recipientEmail);
    }
}
