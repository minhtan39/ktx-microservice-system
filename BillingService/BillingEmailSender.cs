using System.Net;
using System.Net.Mail;
using System.Text;

public sealed class BillingEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BillingEmailSender> _logger;

    public BillingEmailSender(
        IConfiguration configuration,
        ILogger<BillingEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["Email:Username"]) &&
        !string.IsNullOrWhiteSpace(_configuration["Email:Password"]);

    public async Task SendInvoiceAsync(MonthlyInvoice invoice)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Billing email is not configured.");

        var host = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var username = _configuration["Email:Username"]!.Trim();
        var configuredPassword = _configuration["Email:Password"]!;
        var password = string.Concat(configuredPassword.Where(character => !char.IsWhiteSpace(character)));
        var fromEmail = _configuration["Email:FromEmail"] ?? username;
        var fromName = _configuration["Email:FromName"] ?? "DormManager";
        var html = BuildInvoiceHtml(invoice);

        using var message = new MailMessage
        {
            From = new MailAddress(fromEmail, fromName),
            Subject = $"Phiếu thanh toán {invoice.BillingPeriod} - {invoice.InvoiceCode}",
            IsBodyHtml = true,
            Body = html
        };

        message.To.Add(new MailAddress(invoice.StudentEmail, invoice.StudentName));

        var attachmentBytes = Encoding.UTF8.GetBytes(html);
        var attachmentStream = new MemoryStream(attachmentBytes);
        message.Attachments.Add(new Attachment(
            attachmentStream,
            $"Phieu-thanh-toan-{invoice.InvoiceCode}.html",
            "text/html"));

        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = _configuration.GetValue("Email:EnableSsl", true),
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(username, password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = 20000
        };

        await smtp.SendMailAsync(message);
        _logger.LogInformation(
            "Monthly invoice {InvoiceCode} emailed to student {StudentCode}.",
            invoice.InvoiceCode,
            invoice.StudentCode);
    }

    private static string BuildInvoiceHtml(MonthlyInvoice invoice)
    {
        static string Money(decimal value) => $"{value:N0} đ";

        var qrBlock = string.IsNullOrWhiteSpace(invoice.QrCodeUrl)
            ? "<p><strong>QR chưa được cấu hình.</strong> Vui lòng liên hệ quản lý ký túc xá.</p>"
            : $"""
               <div style="text-align:center;margin:24px 0">
                 <img src="{WebUtility.HtmlEncode(invoice.QrCodeUrl)}" width="280" alt="VietQR" />
                 <p>Nội dung chuyển khoản: <strong>{WebUtility.HtmlEncode(invoice.PaymentCode)}</strong></p>
               </div>
               """;

        return $"""
            <!doctype html>
            <html lang="vi">
            <head><meta charset="utf-8"><title>{WebUtility.HtmlEncode(invoice.InvoiceCode)}</title></head>
            <body style="font-family:Arial,sans-serif;color:#17201b;line-height:1.55;max-width:760px;margin:24px auto">
              <h1 style="color:#0f7f51">PHIẾU THANH TOÁN HÀNG THÁNG</h1>
              <p><strong>Mã phiếu:</strong> {WebUtility.HtmlEncode(invoice.InvoiceCode)}</p>
              <p><strong>Sinh viên:</strong> {WebUtility.HtmlEncode(invoice.StudentName)} ({WebUtility.HtmlEncode(invoice.StudentCode)})</p>
              <p><strong>Phòng:</strong> {WebUtility.HtmlEncode(invoice.RoomName)} &nbsp; <strong>Kỳ:</strong> {WebUtility.HtmlEncode(invoice.BillingPeriod)}</p>
              <table style="width:100%;border-collapse:collapse;margin-top:20px">
                <thead><tr style="background:#e9f7ef"><th style="padding:10px;border:1px solid #ccd8d0;text-align:left">Khoản thu</th><th style="padding:10px;border:1px solid #ccd8d0;text-align:right">Thành tiền</th></tr></thead>
                <tbody>
                  <tr><td style="padding:10px;border:1px solid #ccd8d0">Tiền phòng</td><td style="padding:10px;border:1px solid #ccd8d0;text-align:right">{Money(invoice.RoomFee)}</td></tr>
                  <tr><td style="padding:10px;border:1px solid #ccd8d0">Điện: {invoice.ElectricityUsage} số x {Money(invoice.ElectricityRate)}</td><td style="padding:10px;border:1px solid #ccd8d0;text-align:right">{Money(invoice.ElectricityAmount)}</td></tr>
                  <tr><td style="padding:10px;border:1px solid #ccd8d0">Nước: {invoice.WaterUsage} số x {Money(invoice.WaterRate)}</td><td style="padding:10px;border:1px solid #ccd8d0;text-align:right">{Money(invoice.WaterAmount)}</td></tr>
                  <tr style="font-size:18px;font-weight:bold"><td style="padding:12px;border:1px solid #ccd8d0">Tổng cộng</td><td style="padding:12px;border:1px solid #ccd8d0;text-align:right;color:#0f7f51">{Money(invoice.TotalAmount)}</td></tr>
                </tbody>
              </table>
              <p><strong>Hạn thanh toán:</strong> {invoice.DueDate:dd/MM/yyyy}</p>
              {qrBlock}
              <p style="color:#66736b">Hệ thống tự động ghi nhận khi ngân hàng gửi thông báo giao dịch đúng nội dung và đủ số tiền.</p>
            </body>
            </html>
            """;
    }
}
