using System.Globalization;
using ContractStudentService.DTOs.Contract;
using ContractStudentService.DTOs.Integration;
using ContractStudentService.Entities;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace ContractStudentService.Services;

public sealed class ContractPdfGenerator
{
    private static readonly XFont BodyFont = Font(13);
    private static readonly XFont BodyBoldFont = Font(13, XFontStyle.Bold);
    private static readonly XFont BodyItalicFont = Font(12, XFontStyle.Italic);
    private static readonly XFont HeaderFont = Font(12, XFontStyle.Bold);
    private static readonly XFont HeaderSmallFont = Font(11, XFontStyle.Bold);
    private static readonly XFont TitleFont = Font(16, XFontStyle.Bold);
    private static readonly XFont SubtitleFont = Font(14, XFontStyle.Bold);

    public byte[] Generate(
        Contract contract,
        Student student,
        AvailableRoomDto? room,
        GenerateContractTemplateDto issuer)
    {
        var issueDate = (issuer.IssueDate ?? DateTime.Today).Date;
        var document = new PdfDocument();
        document.Info.Title = $"Hợp đồng ở nội trú {contract.ContractCode}";
        document.Info.Author = issuer.DormitoryName.Trim();
        document.Info.Subject = "Hợp đồng bố trí chỗ ở nội trú cho sinh viên";
        document.Info.Creator = "DormManager - ContractStudentService";

        using var writer = new ContractDocumentWriter(document, contract.ContractCode);
        writer.DrawOfficialHeader(issuer, contract.ContractCode, issueDate);
        writer.DrawCentered("HỢP ĐỒNG Ở NỘI TRÚ KÝ TÚC XÁ", TitleFont, 16, 3);
        writer.DrawCentered("Về việc bố trí chỗ ở nội trú cho sinh viên", SubtitleFont, 0, 14);

        writer.DrawParagraph(
            "Căn cứ Bộ luật Dân sự số 91/2015/QH13 ngày 24 tháng 11 năm 2015;",
            BodyItalicFont);
        writer.DrawParagraph(
            "Căn cứ nội quy, quy chế quản lý và sử dụng ký túc xá của đơn vị quản lý;",
            BodyItalicFont);
        writer.DrawParagraph(
            $"Căn cứ nhu cầu ở nội trú của sinh viên và khả năng bố trí phòng ở của {issuer.DormitoryName.Trim()};",
            BodyItalicFont);
        writer.DrawParagraph(
            $"Hôm nay, ngày {issueDate:dd} tháng {issueDate:MM} năm {issueDate:yyyy}, tại {issuer.PlaceOfSigning.Trim()}, chúng tôi gồm:",
            BodyFont,
            after: 10);

        writer.DrawPartyTitle("BÊN BỐ TRÍ CHỖ Ở (BÊN A)");
        writer.DrawInfoLine("Đơn vị", issuer.DormitoryName.Trim());
        writer.DrawInfoLine("Cơ quan chủ quản", issuer.ParentOrganization.Trim());
        writer.DrawInfoLine("Đại diện", issuer.RepresentativeName.Trim());
        writer.DrawInfoLine("Chức vụ", issuer.RepresentativeTitle.Trim());
        writer.DrawInfoLine("Địa chỉ", issuer.Address.Trim());
        writer.DrawInfoLine("Điện thoại", ValueOrDash(issuer.Phone));
        writer.DrawInfoLine("Email", ValueOrDash(issuer.Email));
        writer.DrawInfoLine(
            "Tài khoản thu",
            BuildBankAccount(issuer.BankAccount, issuer.BankName));

        writer.DrawPartyTitle("SINH VIÊN Ở NỘI TRÚ (BÊN B)");
        writer.DrawInfoLine("Họ và tên", student.FullName);
        writer.DrawInfoLine("Mã sinh viên", student.StudentCode);
        writer.DrawInfoLine("Số CCCD", ValueOrDash(student.CCCD));
        writer.DrawInfoLine("Trường", ValueOrDash(student.SchoolName));
        writer.DrawInfoLine("Khoa / Lớp", JoinNonEmpty(" / ", student.FacultyName, student.ClassName));
        writer.DrawInfoLine("Điện thoại", ValueOrDash(student.Phone));
        writer.DrawInfoLine("Email", ValueOrDash(student.Email));

        writer.DrawParagraph(
            "Hai bên tự nguyện thỏa thuận ký kết hợp đồng ở nội trú ký túc xá với các điều khoản sau:",
            BodyFont,
            before: 8,
            after: 10);

        writer.DrawArticle("Điều 1. Đối tượng, vị trí và thời hạn ở nội trú");
        writer.DrawNumberedItem("1.",
            $"Bên A bố trí cho Bên B chỗ ở tại phòng {RoomLabel(contract, room)}, thuộc {BuildingLabel(room)}; loại phòng: {ValueOrDash(room?.RoomType)}; sức chứa: {CapacityLabel(room)}.");
        writer.DrawNumberedItem("2.",
            $"Thời hạn ở nội trú từ ngày {contract.StartDate:dd/MM/yyyy} đến hết ngày {contract.EndDate:dd/MM/yyyy}. Việc gia hạn chỉ có giá trị khi được Bên A phê duyệt và cập nhật trên hệ thống.");
        writer.DrawNumberedItem("3.",
            "Chỗ ở được sử dụng đúng mục đích sinh hoạt và học tập của sinh viên; Bên B không được tự ý chuyển nhượng, cho mượn, cho thuê lại hoặc đổi phòng khi chưa được Bên A chấp thuận.");

        writer.DrawArticle("Điều 2. Tiền ở, tiền đặt cọc và phương thức thanh toán");
        writer.DrawNumberedItem("1.",
            $"Tiền ở nội trú: {FormatMoney(contract.MonthlyFee)}/tháng (Bằng chữ: {MoneyInWords(contract.MonthlyFee)}).");
        writer.DrawNumberedItem("2.",
            $"Tiền đặt cọc: {FormatMoney(contract.DepositAmount)} (Bằng chữ: {MoneyInWords(contract.DepositAmount)}). Khoản đặt cọc được đối trừ hoặc hoàn trả sau khi thanh lý hợp đồng, trừ các khoản Bên B còn phải thanh toán hoặc bồi thường.");
        writer.DrawNumberedItem("3.",
            "Tiền điện, nước và các dịch vụ phát sinh được tính theo chỉ số thực tế, đơn giá do đơn vị có thẩm quyền công bố và phương án phân bổ cho người đang ở trong phòng tại kỳ thanh toán.");
        writer.DrawNumberedItem("4.",
            "Bên B thanh toán theo phiếu thu/hóa đơn được phát hành trên DormManager, bằng chuyển khoản, ví KTX hoặc phương thức hợp lệ khác. Nội dung chuyển khoản phải đúng mã thanh toán trên phiếu.");
        writer.DrawNumberedItem("5.",
            "Trường hợp chậm thanh toán, Bên A thông báo cho Bên B và xử lý theo nội quy ký túc xá, quy định tài chính của đơn vị và pháp luật có liên quan.");

        writer.DrawArticle("Điều 3. Quyền và nghĩa vụ của Bên A");
        writer.DrawNumberedItem("1.",
            "Bàn giao chỗ ở, trang thiết bị kèm theo và duy trì điều kiện vận hành chung của ký túc xá theo quy định.");
        writer.DrawNumberedItem("2.",
            "Thông báo đầy đủ các khoản thu, lịch thanh toán, nội quy, lịch bảo trì và những thay đổi ảnh hưởng trực tiếp đến Bên B.");
        writer.DrawNumberedItem("3.",
            "Tiếp nhận yêu cầu sửa chữa, bảo trì; tổ chức kiểm tra phòng khi cần thiết theo nội quy, bảo đảm tôn trọng quyền riêng tư và thông báo trước, trừ tình huống khẩn cấp.");
        writer.DrawNumberedItem("4.",
            "Có quyền yêu cầu Bên B khắc phục vi phạm, bồi thường thiệt hại, tạm ngừng dịch vụ hoặc chấm dứt hợp đồng khi có căn cứ theo hợp đồng, nội quy và pháp luật.");
        writer.DrawNumberedItem("5.",
            "Bảo vệ dữ liệu cá nhân và hồ sơ hợp đồng của Bên B; chỉ sử dụng cho mục đích quản lý nội trú, thanh toán, an ninh, an toàn và nghĩa vụ pháp lý.");

        writer.DrawArticle("Điều 4. Quyền và nghĩa vụ của Bên B");
        writer.DrawNumberedItem("1.",
            "Sử dụng chỗ ở và tiện ích chung đúng mục đích; được yêu cầu Bên A cung cấp thông tin, tiếp nhận phản ánh và xử lý sự cố theo quy trình.");
        writer.DrawNumberedItem("2.",
            "Thanh toán đầy đủ, đúng hạn; bảo quản tài sản; giữ gìn vệ sinh, trật tự, an ninh, phòng cháy chữa cháy và tuân thủ nội quy ký túc xá.");
        writer.DrawNumberedItem("3.",
            "Không tàng trữ vật cấm, chất cháy nổ; không tự ý sửa chữa, đấu nối điện nước, thay đổi kết cấu hoặc đưa người ngoài vào lưu trú trái quy định.");
        writer.DrawNumberedItem("4.",
            "Thông báo kịp thời sự cố, mất an toàn, hư hỏng; chịu trách nhiệm bồi thường thiệt hại do lỗi của mình hoặc của người do mình đưa vào ký túc xá.");
        writer.DrawNumberedItem("5.",
            "Bàn giao phòng, trang thiết bị và hoàn tất nghĩa vụ tài chính khi hết hạn hoặc chấm dứt hợp đồng.");

        writer.DrawArticle("Điều 5. Sửa chữa, bảo trì và xử lý thiệt hại");
        writer.DrawNumberedItem("1.",
            "Yêu cầu sửa chữa được tạo và theo dõi trên DormManager. Bên B có trách nhiệm mô tả trung thực, phối hợp kiểm tra và xác nhận kết quả xử lý.");
        writer.DrawNumberedItem("2.",
            "Chi phí hao mòn tự nhiên, hư hỏng thuộc trách nhiệm vận hành do Bên A xử lý. Thiệt hại do lỗi của Bên B được lập biên bản, xác định chi phí hợp lý và thông báo trước khi thu.");
        writer.DrawNumberedItem("3.",
            "Trong tình huống có nguy cơ gây mất an toàn, Bên A được áp dụng biện pháp khẩn cấp cần thiết và thông báo cho Bên B ngay sau khi xử lý.");

        writer.DrawArticle("Điều 6. Tạm ngừng, chấm dứt và thanh lý hợp đồng");
        writer.DrawNumberedItem("1.",
            "Hợp đồng chấm dứt khi hết thời hạn mà không được gia hạn, hai bên thỏa thuận chấm dứt, Bên B không còn đủ điều kiện ở nội trú hoặc có vi phạm thuộc trường hợp phải chấm dứt theo nội quy và pháp luật.");
        writer.DrawNumberedItem("2.",
            "Bên yêu cầu chấm dứt trước hạn phải thông báo theo quy trình của ký túc xá, trừ trường hợp khẩn cấp hoặc vi phạm nghiêm trọng.");
        writer.DrawNumberedItem("3.",
            "Khi thanh lý, hai bên đối chiếu công nợ, tình trạng phòng và tài sản. Bên A hoàn trả phần đặt cọc còn lại sau khi trừ các nghĩa vụ hợp lệ của Bên B.");

        writer.DrawArticle("Điều 7. Giao kết và xác nhận điện tử");
        writer.DrawNumberedItem("1.",
            "Bên B xác nhận đã đọc toàn bộ hợp đồng, kiểm tra thông tin, đồng ý điều khoản và ký trực tiếp trên DormManager bằng tài khoản được cấp.");
        writer.DrawNumberedItem("2.",
            "Hệ thống lưu thời điểm ký, tài khoản, mã sinh viên, địa chỉ IP, ảnh chữ ký và mã băm xác thực. Bản PDF đã ký được lưu trong hồ sơ hợp đồng và gửi đến email của Bên B khi dịch vụ email được cấu hình.");
        writer.DrawNumberedItem("3.",
            "Hai bên thống nhất dữ liệu điện tử nêu trên là căn cứ chứng minh việc giao kết trong phạm vi pháp luật cho phép. Việc sử dụng chữ ký số chuyên dùng hoặc chữ ký điện tử được chứng thực sẽ thực hiện khi đơn vị triển khai hạ tầng tương ứng.");

        writer.DrawArticle("Điều 8. Điều khoản riêng và giải quyết tranh chấp");
        writer.DrawNumberedItem("1.",
            string.IsNullOrWhiteSpace(contract.Terms)
                ? "Bên B tuân thủ nội quy ký túc xá và các thông báo hợp lệ được công bố trong thời gian hợp đồng có hiệu lực."
                : contract.Terms.Trim());
        writer.DrawNumberedItem("2.",
            "Mọi thay đổi, bổ sung hợp đồng phải được hai bên xác nhận bằng văn bản hoặc dữ liệu điện tử có thể truy cập, lưu trữ và đối chiếu.");
        writer.DrawNumberedItem("3.",
            "Tranh chấp được ưu tiên giải quyết bằng thương lượng. Trường hợp không giải quyết được, mỗi bên có quyền yêu cầu cơ quan có thẩm quyền giải quyết theo pháp luật.");
        writer.DrawNumberedItem("4.",
            "Hợp đồng có hiệu lực từ thời điểm Bên B ký và Bên A ghi nhận trên hệ thống, trừ khi hai bên có thỏa thuận khác. Hợp đồng được lập thành 02 bản điện tử có giá trị như nhau, mỗi bên lưu 01 bản.");

        writer.DrawSignatureSection(issuer, student, issueDate);
        writer.DrawPageNumbers();

        using var stream = new MemoryStream();
        document.Save(stream, false);
        return stream.ToArray();
    }

    private static XFont Font(double size, XFontStyle style = XFontStyle.Regular)
    {
        return new XFont(ContractFontResolver.FamilyName, size, style);
    }

    private static string RoomLabel(Contract contract, AvailableRoomDto? room)
    {
        return room == null
            ? $"#{contract.RoomId}"
            : string.IsNullOrWhiteSpace(room.RoomType)
                ? $"#{contract.RoomId}"
                : $"#{contract.RoomId}";
    }

    private static string BuildingLabel(AvailableRoomDto? room)
    {
        return string.IsNullOrWhiteSpace(room?.BuildingName)
            ? "khu ký túc xá"
            : $"Tòa {room.BuildingName}";
    }

    private static string CapacityLabel(AvailableRoomDto? room)
    {
        return room == null || room.Capacity <= 0
            ? "theo hồ sơ phòng"
            : $"{room.Capacity} người";
    }

    private static string BuildBankAccount(string account, string bank)
    {
        if (string.IsNullOrWhiteSpace(account) && string.IsNullOrWhiteSpace(bank))
            return "Theo thông tin thanh toán trên phiếu thu/hóa đơn";

        return JoinNonEmpty(" - ", account.Trim(), bank.Trim());
    }

    private static string JoinNonEmpty(string separator, params string?[] values)
    {
        var result = values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        return result.Length == 0 ? "-" : string.Join(separator, result);
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string FormatMoney(decimal value)
    {
        return $"{Math.Round(value, 0):N0} đồng";
    }

    private static string MoneyInWords(decimal value)
    {
        var number = (long)Math.Round(Math.Abs(value), 0);

        if (number == 0)
            return "Không đồng";

        string[] groupNames = ["", " nghìn", " triệu", " tỷ", " nghìn tỷ", " triệu tỷ"];
        var groups = new List<int>();

        while (number > 0)
        {
            groups.Add((int)(number % 1000));
            number /= 1000;
        }

        var parts = new List<string>();

        for (var index = groups.Count - 1; index >= 0; index--)
        {
            if (groups[index] == 0)
                continue;

            var hasHigherGroup = index < groups.Count - 1;
            parts.Add(ReadThreeDigits(groups[index], hasHigherGroup) + groupNames[index]);
        }

        var words = string.Join(" ", parts).Trim() + " đồng";
        return char.ToUpper(words[0], CultureInfo.GetCultureInfo("vi-VN")) + words[1..];
    }

    private static string ReadThreeDigits(int number, bool forceHundreds)
    {
        string[] digits = ["không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"];
        var hundreds = number / 100;
        var tens = number / 10 % 10;
        var units = number % 10;
        var parts = new List<string>();

        if (hundreds > 0 || forceHundreds)
        {
            parts.Add($"{digits[hundreds]} trăm");
        }

        if (tens > 1)
        {
            parts.Add($"{digits[tens]} mươi");
            parts.Add(units switch
            {
                1 => "mốt",
                4 => "tư",
                5 => "lăm",
                > 0 => digits[units],
                _ => string.Empty
            });
        }
        else if (tens == 1)
        {
            parts.Add("mười");
            parts.Add(units == 5 ? "lăm" : units > 0 ? digits[units] : string.Empty);
        }
        else if (units > 0)
        {
            if (hundreds > 0 || forceHundreds)
                parts.Add("lẻ");
            parts.Add(digits[units]);
        }

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private sealed class ContractDocumentWriter : IDisposable
    {
        private const double PointsPerMillimeter = 72d / 25.4d;
        private const double TopMargin = 20 * PointsPerMillimeter;
        private const double BottomMargin = 20 * PointsPerMillimeter;
        private const double LeftMargin = 30 * PointsPerMillimeter;
        private const double RightMargin = 15 * PointsPerMillimeter;
        private const double BodyLineHeight = 18;

        private readonly PdfDocument _document;
        private readonly string _contractCode;
        private PdfPage _page = null!;
        private XGraphics _graphics = null!;
        private bool _graphicsDisposed;
        private double _y;

        public ContractDocumentWriter(PdfDocument document, string contractCode)
        {
            _document = document;
            _contractCode = contractCode;
            NewPage();
        }

        private double ContentWidth => _page.Width.Point - LeftMargin - RightMargin;
        private double ContentBottom => _page.Height.Point - BottomMargin;

        public void DrawOfficialHeader(
            GenerateContractTemplateDto issuer,
            string contractCode,
            DateTime issueDate)
        {
            var columnGap = 18d;
            var availableWidth = ContentWidth - columnGap;
            var leftColumnWidth = availableWidth * .44d;
            var rightColumnWidth = availableWidth - leftColumnWidth;
            var leftX = LeftMargin;
            var rightX = LeftMargin + leftColumnWidth + columnGap;

            var leftBottom = DrawBlock(
                issuer.ParentOrganization.Trim().ToUpperInvariant(),
                HeaderSmallFont,
                leftX,
                _y,
                leftColumnWidth,
                XStringFormats.TopCenter);
            leftBottom = DrawBlock(
                issuer.DormitoryName.Trim().ToUpperInvariant(),
                HeaderFont,
                leftX,
                leftBottom + 2,
                leftColumnWidth,
                XStringFormats.TopCenter);

            var rightBottom = DrawBlock(
                "CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM",
                HeaderFont,
                rightX,
                _y,
                rightColumnWidth,
                XStringFormats.TopCenter);
            rightBottom = DrawBlock(
                "Độc lập - Tự do - Hạnh phúc",
                HeaderFont,
                rightX,
                rightBottom + 2,
                rightColumnWidth,
                XStringFormats.TopCenter);

            var underlineY = Math.Max(leftBottom, rightBottom) + 2;
            _graphics.DrawLine(XPens.Black, leftX + leftColumnWidth * .3, underlineY, leftX + leftColumnWidth * .7, underlineY);
            _graphics.DrawLine(XPens.Black, rightX + rightColumnWidth * .22, underlineY, rightX + rightColumnWidth * .78, underlineY);

            var rowY = underlineY + 11;
            DrawBlock(
                $"Số: {contractCode}/HĐ-KTX",
                BodyFont,
                leftX,
                rowY,
                leftColumnWidth,
                XStringFormats.TopCenter);
            var placeAndDate = $"{issuer.PlaceOfSigning.Trim()}, ngày {issueDate:dd} tháng {issueDate:MM} năm {issueDate:yyyy}";
            var dateBottom = DrawBlock(
                placeAndDate,
                BodyItalicFont,
                rightX,
                rowY,
                rightColumnWidth,
                XStringFormats.TopCenter);

            _y = Math.Max(rowY + BodyLineHeight, dateBottom) + 10;
        }

        public void DrawCentered(
            string text,
            XFont font,
            double before = 0,
            double after = 0)
        {
            _y += before;
            var lines = Wrap(text, font, ContentWidth);
            EnsureSpace(lines.Count * LineHeight(font) + after);

            foreach (var line in lines)
            {
                _graphics.DrawString(
                    line,
                    font,
                    XBrushes.Black,
                    new XRect(LeftMargin, _y, ContentWidth, LineHeight(font)),
                    XStringFormats.TopCenter);
                _y += LineHeight(font);
            }

            _y += after;
        }

        public void DrawParagraph(
            string text,
            XFont font,
            double before = 3,
            double after = 4)
        {
            _y += before;
            DrawWrappedText(text, font, 0, 34, after);
        }

        public void DrawPartyTitle(string text)
        {
            EnsureSpace(30);
            _y += 7;
            _graphics.DrawString(
                text,
                BodyBoldFont,
                XBrushes.Black,
                new XRect(LeftMargin, _y, ContentWidth, BodyLineHeight),
                XStringFormats.TopLeft);
            _y += BodyLineHeight + 3;
        }

        public void DrawInfoLine(string label, string value)
        {
            DrawWrappedText($"{label}: {value}", BodyFont, 18, 0, 2);
        }

        public void DrawArticle(string title)
        {
            EnsureSpace(46);
            _y += 8;
            var lines = Wrap(title, BodyBoldFont, ContentWidth);

            foreach (var line in lines)
            {
                _graphics.DrawString(
                    line,
                    BodyBoldFont,
                    XBrushes.Black,
                    new XRect(LeftMargin, _y, ContentWidth, BodyLineHeight),
                    XStringFormats.TopLeft);
                _y += BodyLineHeight;
            }

            _y += 2;
        }

        public void DrawNumberedItem(string marker, string text)
        {
            const double markerWidth = 24;
            var lines = Wrap(text, BodyFont, ContentWidth - markerWidth);
            EnsureSpace(lines.Count * BodyLineHeight + 3);

            _graphics.DrawString(
                marker,
                BodyFont,
                XBrushes.Black,
                new XRect(LeftMargin, _y, markerWidth, BodyLineHeight),
                XStringFormats.TopLeft);

            foreach (var line in lines)
            {
                _graphics.DrawString(
                    line,
                    BodyFont,
                    XBrushes.Black,
                    new XRect(LeftMargin + markerWidth, _y, ContentWidth - markerWidth, BodyLineHeight),
                    XStringFormats.TopLeft);
                _y += BodyLineHeight;
            }

            _y += 3;
        }

        public void DrawSignatureSection(
            GenerateContractTemplateDto issuer,
            Student student,
            DateTime issueDate)
        {
            EnsureSpace(210);
            _y = Math.Max(_y + 14, ContentBottom - 190);

            var halfWidth = ContentWidth / 2d;
            var rightX = LeftMargin + halfWidth;
            var dateText = $"{issuer.PlaceOfSigning.Trim()}, ngày {issueDate:dd} tháng {issueDate:MM} năm {issueDate:yyyy}";

            _graphics.DrawString(
                dateText,
                BodyItalicFont,
                XBrushes.Black,
                new XRect(rightX, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);
            _y += 24;

            _graphics.DrawString(
                "ĐẠI DIỆN BÊN A",
                BodyBoldFont,
                XBrushes.Black,
                new XRect(LeftMargin, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);
            _graphics.DrawString(
                "BÊN B",
                BodyBoldFont,
                XBrushes.Black,
                new XRect(rightX, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);
            _y += 19;

            _graphics.DrawString(
                $"({issuer.RepresentativeTitle.Trim()})",
                BodyItalicFont,
                XBrushes.Black,
                new XRect(LeftMargin, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);
            _graphics.DrawString(
                "(Ký trực tuyến trên DormManager)",
                BodyItalicFont,
                XBrushes.Black,
                new XRect(rightX, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);

            _y += 105;
            _graphics.DrawString(
                issuer.RepresentativeName.Trim(),
                BodyBoldFont,
                XBrushes.Black,
                new XRect(LeftMargin, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);
            _graphics.DrawString(
                student.FullName,
                BodyBoldFont,
                XBrushes.Black,
                new XRect(rightX, _y, halfWidth, BodyLineHeight),
                XStringFormats.TopCenter);
        }

        public void DrawPageNumbers()
        {
            if (!_graphicsDisposed)
            {
                _graphics.Dispose();
                _graphicsDisposed = true;
            }

            for (var index = 0; index < _document.PageCount; index++)
            {
                var page = _document.Pages[index];
                using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
                graphics.DrawString(
                    $"Trang {index + 1}/{_document.PageCount} - {_contractCode}",
                    Font(10, XFontStyle.Italic),
                    XBrushes.Black,
                    new XRect(
                        LeftMargin,
                        page.Height.Point - 12 * PointsPerMillimeter,
                        page.Width.Point - LeftMargin - RightMargin,
                        12),
                    XStringFormats.TopCenter);
            }
        }

        private void DrawWrappedText(
            string text,
            XFont font,
            double leftIndent,
            double firstLineIndent,
            double after)
        {
            var width = ContentWidth - leftIndent;
            var lines = Wrap(text, font, width - firstLineIndent);
            EnsureSpace(lines.Count * LineHeight(font) + after);

            for (var index = 0; index < lines.Count; index++)
            {
                var indent = index == 0 ? firstLineIndent : 0;
                _graphics.DrawString(
                    lines[index],
                    font,
                    XBrushes.Black,
                    new XRect(
                        LeftMargin + leftIndent + indent,
                        _y,
                        width - indent,
                        LineHeight(font)),
                    XStringFormats.TopLeft);
                _y += LineHeight(font);
            }

            _y += after;
        }

        private double DrawBlock(
            string text,
            XFont font,
            double x,
            double y,
            double width,
            XStringFormat format)
        {
            var lines = Wrap(text, font, width);

            foreach (var line in lines)
            {
                _graphics.DrawString(
                    line,
                    font,
                    XBrushes.Black,
                    new XRect(x, y, width, LineHeight(font)),
                    format);
                y += LineHeight(font);
            }

            return y;
        }

        private List<string> Wrap(string text, XFont font, double width)
        {
            var result = new List<string>();

            foreach (var paragraph in text.Replace("\r", string.Empty).Split('\n'))
            {
                var words = paragraph
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var line = string.Empty;

                foreach (var word in words)
                {
                    var candidate = string.IsNullOrWhiteSpace(line)
                        ? word
                        : $"{line} {word}";

                    if (_graphics.MeasureString(candidate, font).Width <= width)
                    {
                        line = candidate;
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(line))
                        result.Add(line);
                    line = word;
                }

                if (!string.IsNullOrWhiteSpace(line))
                    result.Add(line);
            }

            return result.Count == 0 ? [string.Empty] : result;
        }

        private void EnsureSpace(double requiredHeight)
        {
            if (_y + requiredHeight <= ContentBottom)
                return;

            NewPage();
        }

        private void NewPage()
        {
            if (_graphics != null && !_graphicsDisposed)
                _graphics.Dispose();

            _page = _document.AddPage();
            _page.Size = PageSize.A4;
            _page.Orientation = PageOrientation.Portrait;
            _graphics = XGraphics.FromPdfPage(_page);
            _graphicsDisposed = false;
            _y = TopMargin;
        }

        private static double LineHeight(XFont font)
        {
            return Math.Max(BodyLineHeight, font.Size * 1.25);
        }

        public void Dispose()
        {
            if (!_graphicsDisposed)
            {
                _graphics.Dispose();
                _graphicsDisposed = true;
            }
        }
    }
}
