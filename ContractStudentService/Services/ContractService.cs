using ContractStudentService.DTOs.Contract;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;
using Microsoft.AspNetCore.Http;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ContractStudentService.Services;

public class ContractService : IContractService
{
    private readonly IContractRepository _contractRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IRoomGatewayClient _roomGatewayClient;
    private readonly IWebHostEnvironment _environment;
    private readonly ContractEmailSender _emailSender;
    private readonly ContractPdfGenerator _pdfGenerator;

    public ContractService(
        IContractRepository contractRepository,
        IStudentRepository studentRepository,
        IRoomGatewayClient roomGatewayClient,
        IWebHostEnvironment environment,
        ContractEmailSender emailSender,
        ContractPdfGenerator pdfGenerator)
    {
        _contractRepository = contractRepository;
        _studentRepository = studentRepository;
        _roomGatewayClient = roomGatewayClient;
        _environment = environment;
        _emailSender = emailSender;
        _pdfGenerator = pdfGenerator;
    }

    public async Task<IEnumerable<Contract>> GetAllAsync()
    {
        return await _contractRepository.GetAllAsync();
    }

    public async Task<Contract?> GetByIdAsync(long id)
    {
        return await _contractRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Contract>> GetByStudentIdAsync(long studentId)
    {
        return await _contractRepository.GetByStudentIdAsync(studentId);
    }

    public async Task<Contract> CreateAsync(CreateContractDto dto)
    {
        var student = await _studentRepository.GetByIdAsync(dto.StudentId);

        if (student == null)
            throw new Exception("Student không tồn tại.");

        var contract = new Contract
        {
            ContractCode = string.IsNullOrWhiteSpace(dto.ContractCode)
                ? $"HD-{DateTime.Now:yyyyMMddHHmmss}"
                : dto.ContractCode,
            StudentId = dto.StudentId,
            RoomId = dto.RoomId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DepositAmount = dto.DepositAmount,
            MonthlyFee = dto.MonthlyFee,
            Terms = string.IsNullOrWhiteSpace(dto.Terms)
                ? "Sinh viên đóng tiền đúng hạn, tuân thủ nội quy ký túc xá và bàn giao phòng khi kết thúc hợp đồng."
                : dto.Terms,
            Status = "PendingSignature"
        };

        var createdContract = await _contractRepository.CreateAsync(contract);

        try
        {
            var occupiedRoom = await _roomGatewayClient.OccupyRoomAsync(
                createdContract.RoomId,
                createdContract.StudentId,
                createdContract.Id,
                createdContract.ContractCode);

            if (occupiedRoom.RoomId > 0 &&
                occupiedRoom.RoomId != createdContract.RoomId)
            {
                createdContract.RoomId = occupiedRoom.RoomId;
                createdContract = await _contractRepository.UpdateAsync(createdContract)
                    ?? createdContract;
            }
        }
        catch
        {
            await _contractRepository.DeleteAsync(createdContract.Id);
            throw;
        }

        student.Status = "PendingSignature";
        student.ResidenceHistory = string.IsNullOrWhiteSpace(student.ResidenceHistory)
            ? $"Phòng {createdContract.RoomId}, {createdContract.StartDate:dd/MM/yyyy} - {createdContract.EndDate:dd/MM/yyyy}"
            : $"{student.ResidenceHistory}; Phòng {createdContract.RoomId}, {createdContract.StartDate:dd/MM/yyyy} - {createdContract.EndDate:dd/MM/yyyy}";

        await _studentRepository.UpdateAsync(student);

        return createdContract;
    }

    public async Task<ContractRoomSyncResultDto> SyncRoomOccupancyAsync()
    {
        var contracts = (await _contractRepository.GetAllAsync())
            .Where(contract => IsActiveOrPendingSignature(contract.Status))
            .ToList();
        var result = new ContractRoomSyncResultDto
        {
            Total = contracts.Count
        };

        foreach (var contract in contracts)
        {
            try
            {
                var occupiedRoom = await _roomGatewayClient.OccupyRoomAsync(
                    contract.RoomId,
                    contract.StudentId,
                    contract.Id,
                    contract.ContractCode,
                    allowMaintenance: true);

                if (occupiedRoom.RoomId > 0 &&
                    occupiedRoom.RoomId != contract.RoomId)
                {
                    contract.RoomId = occupiedRoom.RoomId;
                    await _contractRepository.UpdateAsync(contract);
                }

                result.Synced++;
            }
            catch (Exception exception)
            {
                result.Failed++;
                result.Errors.Add($"{contract.ContractCode}: {exception.Message}");
            }
        }

        return result;
    }

    public async Task<Contract?> UpdateAsync(long id, UpdateContractDto dto)
    {
        var existingContract = await _contractRepository.GetByIdAsync(id);

        if (existingContract == null)
            return null;

        existingContract.ContractCode = dto.ContractCode;
        existingContract.StudentId = dto.StudentId;
        existingContract.RoomId = dto.RoomId;
        existingContract.StartDate = dto.StartDate;
        existingContract.EndDate = dto.EndDate;
        existingContract.DepositAmount = dto.DepositAmount;
        existingContract.MonthlyFee = dto.MonthlyFee;
        existingContract.Terms = dto.Terms;
        existingContract.Status = dto.Status;

        return await _contractRepository.UpdateAsync(existingContract);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        return await _contractRepository.DeleteAsync(id);
    }

    public async Task<Contract?> CancelAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!IsActiveOrPendingSignature(contract.Status))
            throw new InvalidOperationException("Chỉ được hủy hợp đồng đang hiệu lực hoặc đang chờ ký.");

        await _roomGatewayClient.ReleaseRoomAsync(
            contract.RoomId,
            contract.StudentId,
            contract.ContractCode);

        contract.Status = "Cancelled";

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        if (student != null)
        {
            student.Status = "Pending";
            student.ResidenceHistory = string.IsNullOrWhiteSpace(student.ResidenceHistory)
                ? $"Đã hủy hợp đồng {contract.ContractCode}"
                : $"{student.ResidenceHistory}; Đã hủy hợp đồng {contract.ContractCode}";

            await _studentRepository.UpdateAsync(student);
        }

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> ExpireAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!contract.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được kết thúc hợp đồng đang hiệu lực.");

        await _roomGatewayClient.ReleaseRoomAsync(
            contract.RoomId,
            contract.StudentId,
            contract.ContractCode);

        contract.Status = "Expired";

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        if (student != null)
        {
            student.Status = "CheckedOut";
            student.ResidenceHistory = string.IsNullOrWhiteSpace(student.ResidenceHistory)
                ? $"Đã kết thúc hợp đồng {contract.ContractCode}"
                : $"{student.ResidenceHistory}; Đã kết thúc hợp đồng {contract.ContractCode}";

            await _studentRepository.UpdateAsync(student);
        }

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> RenewAsync(long id, RenewContractDto dto)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!contract.Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ được gia hạn hợp đồng đang hiệu lực.");

        if (dto.NewEndDate.Date <= contract.EndDate.Date)
            throw new InvalidOperationException("Ngày kết thúc mới phải sau ngày kết thúc hiện tại.");

        contract.EndDate = dto.NewEndDate.Date;
        contract.RenewalCount++;
        contract.LastRenewedAt = DateTime.UtcNow;
        contract.RenewalNote = string.IsNullOrWhiteSpace(dto.Note)
            ? $"Gia hạn lần {contract.RenewalCount}"
            : dto.Note.Trim();
        contract.Terms = string.IsNullOrWhiteSpace(contract.Terms)
            ? $"Hợp đồng được gia hạn đến ngày {contract.EndDate:dd/MM/yyyy}."
            : $"{contract.Terms} Gia hạn lần {contract.RenewalCount} đến ngày {contract.EndDate:dd/MM/yyyy}.";

        return await _contractRepository.UpdateAsync(contract);
    }

    public async Task<Contract?> SignAsync(
        long id,
        SignContractDto dto,
        string ipAddress)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (!IsActiveOrPendingSignature(contract.Status))
            throw new InvalidOperationException("Chỉ được ký hợp đồng đang hiệu lực hoặc đang chờ ký.");

        if (contract.SignedAt.HasValue)
            throw new InvalidOperationException("Hợp đồng này đã được ký online.");

        if (!dto.AcceptedTerms)
            throw new InvalidOperationException("Sinh viên phải xác nhận đã đọc và đồng ý điều khoản hợp đồng.");

        if (string.IsNullOrWhiteSpace(contract.TemplateFilePath))
            throw new InvalidOperationException("Hợp đồng chưa có file PDF mẫu. Admin/nhân viên cần tải mẫu hợp đồng chuẩn lên trước khi sinh viên ký.");

        if (string.IsNullOrWhiteSpace(dto.SignatureImageDataUrl))
            throw new InvalidOperationException("Vui lòng ký tay trên khung chữ ký trước khi xác nhận hợp đồng.");

        if (string.IsNullOrWhiteSpace(dto.FullName) ||
            string.IsNullOrWhiteSpace(dto.StudentCode))
        {
            throw new InvalidOperationException("Vui lòng nhập đầy đủ họ tên và MSSV để ký hợp đồng.");
        }

        var student = await _studentRepository.GetByIdAsync(contract.StudentId);
        var submittedStudentCode = dto.StudentCode.Trim();

        if (student == null ||
            !string.Equals(
                student.StudentCode.Trim(),
                submittedStudentCode,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("MSSV ký hợp đồng không khớp với hồ sơ sinh viên.");
        }

        var signedAt = DateTime.UtcNow;

        contract.SignatureFullName = dto.FullName.Trim();
        contract.SignatureStudentCode = submittedStudentCode.ToUpperInvariant();
        contract.SignatureIpAddress = ipAddress;
        contract.SignedAt = signedAt;
        contract.Status = "Active";
        contract.SignedFilePath = CreateSignedPdf(
            contract,
            student,
            dto.SignatureImageDataUrl,
            signedAt);
        contract.SignedFileName = $"{Path.GetFileNameWithoutExtension(contract.ContractCode)}-signed.pdf";
        contract.SignedFileCreatedAt = signedAt;
        contract.SignatureHash = BuildSignatureHash(contract, signedAt);

        var updatedContract = await _contractRepository.UpdateAsync(contract);

        student.Status = "Active";
        await _studentRepository.UpdateAsync(student);

        if (updatedContract != null &&
            !string.IsNullOrWhiteSpace(updatedContract.SignedFilePath))
        {
            await _emailSender.SendSignedContractAsync(
                student,
                updatedContract,
                ToAbsoluteContractPath(updatedContract.SignedFilePath));
        }

        return updatedContract;
    }

    public async Task<Contract?> UploadTemplateAsync(long id, IFormFile file)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (contract.SignedAt.HasValue)
            throw new InvalidOperationException("Hợp đồng đã được sinh viên ký. Không thể thay file mẫu sau khi ký.");

        if (file.Length == 0)
            throw new InvalidOperationException("File hợp đồng không được để trống.");

        if (file.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("File hợp đồng không được vượt quá 10MB.");

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ chấp nhận file PDF hợp đồng.");

        var templateDirectory = EnsureContractDirectory("templates");
        var safeContractCode = SafeFilePart(contract.ContractCode);
        var storedFileName = $"{contract.Id}-{safeContractCode}-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var fullPath = Path.Combine(templateDirectory, storedFileName);

        await using (var stream = File.Create(fullPath))
        {
            await file.CopyToAsync(stream);
        }

        contract.TemplateFileName = string.IsNullOrWhiteSpace(file.FileName)
            ? storedFileName
            : Path.GetFileName(file.FileName);
        contract.TemplateFilePath = ToRelativeContractPath(fullPath);
        contract.TemplateUploadedAt = DateTime.UtcNow;
        contract.SignedFileName = string.Empty;
        contract.SignedFilePath = string.Empty;
        contract.SignedFileCreatedAt = null;

        var updatedContract = await _contractRepository.UpdateAsync(contract);
        var student = await _studentRepository.GetByIdAsync(contract.StudentId);

        if (updatedContract != null && student != null)
        {
            await _emailSender.SendTemplateReadyAsync(
                student,
                updatedContract,
                fullPath);
        }

        return updatedContract;
    }

    public async Task<Contract?> GenerateTemplateAsync(
        long id,
        GenerateContractTemplateDto dto)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null)
            return null;

        if (contract.SignedAt.HasValue)
            throw new InvalidOperationException("Hợp đồng đã được sinh viên ký. Không thể phát hành lại file mẫu.");

        ValidateTemplateIssuer(dto);

        var student = await _studentRepository.GetByIdAsync(contract.StudentId)
            ?? throw new InvalidOperationException("Không tìm thấy hồ sơ sinh viên của hợp đồng.");
        var room = await _roomGatewayClient.GetRoomByIdAsync(contract.RoomId);
        var pdfBytes = _pdfGenerator.Generate(contract, student, room, dto);
        var templateDirectory = EnsureContractDirectory("templates");
        var safeContractCode = SafeFilePart(contract.ContractCode);
        var storedFileName = $"{contract.Id}-{safeContractCode}-standard-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var fullPath = Path.Combine(templateDirectory, storedFileName);

        await File.WriteAllBytesAsync(fullPath, pdfBytes);

        contract.TemplateFileName = $"{safeContractCode}-hop-dong-noi-tru.pdf";
        contract.TemplateFilePath = ToRelativeContractPath(fullPath);
        contract.TemplateUploadedAt = DateTime.UtcNow;
        contract.SignedFileName = string.Empty;
        contract.SignedFilePath = string.Empty;
        contract.SignedFileCreatedAt = null;

        var updatedContract = await _contractRepository.UpdateAsync(contract);

        if (updatedContract != null)
        {
            await _emailSender.SendTemplateReadyAsync(
                student,
                updatedContract,
                fullPath);
        }

        return updatedContract;
    }

    public async Task<ContractFileResultDto?> GetTemplateFileAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null || string.IsNullOrWhiteSpace(contract.TemplateFilePath))
            return null;

        return OpenContractFile(
            contract.TemplateFilePath,
            string.IsNullOrWhiteSpace(contract.TemplateFileName)
                ? $"{contract.ContractCode}-template.pdf"
                : contract.TemplateFileName);
    }

    public async Task<ContractFileResultDto?> GetSignedFileAsync(long id)
    {
        var contract = await _contractRepository.GetByIdAsync(id);

        if (contract == null || string.IsNullOrWhiteSpace(contract.SignedFilePath))
            return null;

        return OpenContractFile(
            contract.SignedFilePath,
            string.IsNullOrWhiteSpace(contract.SignedFileName)
                ? $"{contract.ContractCode}-signed.pdf"
                : contract.SignedFileName);
    }

    private static bool IsActiveOrPendingSignature(string status)
    {
        return status.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
            status.Equals("PendingSignature", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildSignatureHash(
        Contract contract,
        DateTime signedAt)
    {
        var raw = string.Join(
            "|",
            contract.Id,
            contract.ContractCode,
            contract.StudentId,
            contract.SignatureStudentCode,
            signedAt.ToString("O"),
            contract.EndDate.ToString("O"),
            contract.SignedFilePath);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(bytes);
    }

    private string CreateSignedPdf(
        Contract contract,
        Student student,
        string signatureImageDataUrl,
        DateTime signedAt)
    {
        var templatePath = ToAbsoluteContractPath(contract.TemplateFilePath);

        if (!File.Exists(templatePath))
            throw new InvalidOperationException("Không tìm thấy file PDF mẫu của hợp đồng.");

        var signatureBytes = DecodeSignatureImage(signatureImageDataUrl);
        var signedDirectory = EnsureContractDirectory("signed");
        var safeContractCode = SafeFilePart(contract.ContractCode);
        var outputFileName = $"{contract.Id}-{safeContractCode}-signed-{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var outputPath = Path.Combine(signedDirectory, outputFileName);

        using var document = PdfReader.Open(templatePath, PdfDocumentOpenMode.Modify);

        if (document.PageCount == 0)
            throw new InvalidOperationException("File PDF mẫu không có trang hợp lệ.");

        var page = document.Pages[document.PageCount - 1];
        using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);

        const double boxWidth = 245;
        const double boxHeight = 112;
        var x = Math.Max(36, page.Width.Point - boxWidth - 42);
        var y = Math.Max(36, page.Height.Point - boxHeight - 42);

        graphics.DrawRectangle(
            new XSolidBrush(XColor.FromArgb(248, 253, 250)),
            x,
            y,
            boxWidth,
            boxHeight);
        graphics.DrawRectangle(
            new XPen(XColor.FromArgb(34, 125, 74), 1),
            x,
            y,
            boxWidth,
            boxHeight);

        var titleFont = new XFont(
            ContractFontResolver.FamilyName,
            9,
            XFontStyle.Bold);
        var textFont = new XFont(
            ContractFontResolver.FamilyName,
            8,
            XFontStyle.Regular);
        var mutedBrush = new XSolidBrush(XColor.FromArgb(68, 80, 92));
        var strongBrush = new XSolidBrush(XColor.FromArgb(13, 93, 58));

        graphics.DrawString(
            "CHỮ KÝ SINH VIÊN TRỰC TUYẾN",
            titleFont,
            strongBrush,
            new XRect(x + 10, y + 8, boxWidth - 20, 14),
            XStringFormats.TopLeft);

        using (var signatureImage = XImage.FromStream(() => new MemoryStream(signatureBytes)))
        {
            const double imageHeight = 42;
            var ratio = signatureImage.PixelHeight == 0
                ? 3d
                : (double)signatureImage.PixelWidth / signatureImage.PixelHeight;
            var imageWidth = Math.Min(160d, imageHeight * ratio);
            graphics.DrawImage(signatureImage, x + 10, y + 28, imageWidth, imageHeight);
        }

        graphics.DrawString(
            $"Người ký: {student.FullName}",
            textFont,
            mutedBrush,
            new XRect(x + 10, y + 74, boxWidth - 20, 11),
            XStringFormats.TopLeft);
        graphics.DrawString(
            $"MSSV: {student.StudentCode}",
            textFont,
            mutedBrush,
            new XRect(x + 10, y + 87, boxWidth - 20, 11),
            XStringFormats.TopLeft);
        graphics.DrawString(
            $"Thời gian: {signedAt:dd/MM/yyyy HH:mm} UTC",
            textFont,
            mutedBrush,
            new XRect(x + 10, y + 100, boxWidth - 20, 11),
            XStringFormats.TopLeft);

        document.Save(outputPath);

        return ToRelativeContractPath(outputPath);
    }

    private ContractFileResultDto? OpenContractFile(
        string relativePath,
        string downloadFileName)
    {
        var fullPath = ToAbsoluteContractPath(relativePath);

        if (!File.Exists(fullPath))
            return null;

        return new ContractFileResultDto(
            File.OpenRead(fullPath),
            downloadFileName,
            "application/pdf");
    }

    private static byte[] DecodeSignatureImage(string dataUrl)
    {
        var value = dataUrl.Trim();
        const string pngPrefix = "data:image/png;base64,";
        const string jpegPrefix = "data:image/jpeg;base64,";

        if (value.StartsWith(pngPrefix, StringComparison.OrdinalIgnoreCase))
            value = value[pngPrefix.Length..];
        else if (value.StartsWith(jpegPrefix, StringComparison.OrdinalIgnoreCase))
            value = value[jpegPrefix.Length..];
        else
            throw new InvalidOperationException("Chữ ký phải là ảnh PNG hoặc JPEG hợp lệ.");

        try
        {
            var bytes = Convert.FromBase64String(value);

            if (bytes.Length > 1024 * 1024)
                throw new InvalidOperationException("Ảnh chữ ký không được vượt quá 1MB.");

            return bytes;
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Dữ liệu ảnh chữ ký không hợp lệ.");
        }
    }

    private string EnsureContractDirectory(string child)
    {
        var directory = Path.Combine(
            _environment.ContentRootPath,
            "Data",
            "contract-files",
            child);

        Directory.CreateDirectory(directory);

        return directory;
    }

    private string ToRelativeContractPath(string fullPath)
    {
        var root = Path.Combine(_environment.ContentRootPath, "Data", "contract-files");
        Directory.CreateDirectory(root);

        return Path.GetRelativePath(root, fullPath);
    }

    private string ToAbsoluteContractPath(string relativePath)
    {
        var root = Path.Combine(_environment.ContentRootPath, "Data", "contract-files");

        return Path.GetFullPath(Path.Combine(root, relativePath));
    }

    private static string SafeFilePart(string value)
    {
        var cleaned = Regex.Replace(value, "[^a-zA-Z0-9_-]+", "-").Trim('-');

        return string.IsNullOrWhiteSpace(cleaned)
            ? "contract"
            : cleaned;
    }

    private static void ValidateTemplateIssuer(GenerateContractTemplateDto dto)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.ParentOrganization))
            missingFields.Add("cơ quan chủ quản");
        if (string.IsNullOrWhiteSpace(dto.DormitoryName))
            missingFields.Add("tên đơn vị quản lý ký túc xá");
        if (string.IsNullOrWhiteSpace(dto.RepresentativeName))
            missingFields.Add("họ tên người đại diện Bên A");
        if (string.IsNullOrWhiteSpace(dto.RepresentativeTitle))
            missingFields.Add("chức vụ người đại diện");
        if (string.IsNullOrWhiteSpace(dto.Address))
            missingFields.Add("địa chỉ Bên A");
        if (string.IsNullOrWhiteSpace(dto.PlaceOfSigning))
            missingFields.Add("địa danh ký hợp đồng");

        if (missingFields.Count > 0)
        {
            throw new InvalidOperationException(
                $"Vui lòng nhập đầy đủ: {string.Join(", ", missingFields)}.");
        }
    }
}
