using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class IncidentAiAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, string> CategoryLabels = new Dictionary<string, string>
    {
        ["Electric"] = "Điện",
        ["Water"] = "Nước",
        ["Furniture"] = "Nội thất",
        ["Internet"] = "Internet",
        ["Safety"] = "An toàn",
        ["Sanitation"] = "Vệ sinh",
        ["Other"] = "Khác"
    };

    private static readonly IReadOnlyDictionary<string, string> PriorityLabels = new Dictionary<string, string>
    {
        ["low"] = "Thấp",
        ["normal"] = "Bình thường",
        ["high"] = "Cao",
        ["urgent"] = "Khẩn cấp"
    };

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<IncidentAiAnalyzer> _logger;

    public IncidentAiAnalyzer(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<IncidentAiAnalyzer> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IncidentAnalysisResult> AnalyzeAsync(
        AnalyzeIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var fallback = AnalyzeWithRules(request, "Nội bộ");
        var apiKey = FirstNonBlank(
            _configuration["AI:ApiKey"],
            _configuration["OpenAI:ApiKey"],
            _configuration["OPENAI_API_KEY"]);

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.Equals(_configuration["AI:Enabled"], "false", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        try
        {
            var endpoint = FirstNonBlank(
                _configuration["AI:Endpoint"],
                "https://api.openai.com/v1/chat/completions");
            var model = FirstNonBlank(_configuration["AI:Model"], "gpt-4o-mini");
            var response = await AnalyzeWithOpenAiCompatibleEndpointAsync(
                endpoint,
                model,
                apiKey,
                request,
                cancellationToken);

            return response ?? fallback;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI incident analysis failed, using local fallback.");
            return fallback;
        }
    }

    private async Task<IncidentAnalysisResult?> AnalyzeWithOpenAiCompatibleEndpointAsync(
        string endpoint,
        string model,
        string apiKey,
        AnalyzeIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var userPayload = new
        {
            request.Description,
            request.RoomName,
            request.Building,
            request.Category,
            request.Priority,
            categoryOptions = CategoryLabels.Keys,
            priorityOptions = PriorityLabels.Keys
        };

        var body = new
        {
            model,
            temperature = 0.2,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = """
                    Bạn là trợ lý vận hành ký túc xá. Hãy phân tích mô tả sự cố sửa chữa của sinh viên.
                    Chỉ trả JSON hợp lệ với các field:
                    category, priority, summary, suggestedAction, expectedHandlingTime, safetyNote, keywords.
                    category phải thuộc Electric, Water, Furniture, Internet, Safety, Sanitation, Other.
                    priority phải thuộc low, normal, high, urgent.
                    Viết tiếng Việt ngắn gọn, thực tế cho nhân viên kỹ thuật.
                    """
                },
                new
                {
                    role = "user",
                    content = JsonSerializer.Serialize(userPayload, JsonOptions)
                }
            }
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "AI incident analysis endpoint returned {StatusCode}.",
                response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        var content = document.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            return null;

        var parsed = JsonSerializer.Deserialize<AiIncidentResponse>(content, JsonOptions);
        return parsed == null
            ? null
            : NormalizeResult(parsed, "OpenAI");
    }

    private static IncidentAnalysisResult AnalyzeWithRules(AnalyzeIncidentRequest request, string source)
    {
        var text = $"{request.Description} {request.Category}".Trim().ToLowerInvariant();
        var keywords = new List<string>();
        var category = MatchCategory(text, keywords);
        var priority = MatchPriority(text, category, request.Priority, keywords);
        var summary = BuildSummary(request.Description, category);

        return new IncidentAnalysisResult(
            category,
            CategoryLabels[category],
            priority,
            PriorityLabels[priority],
            summary,
            SuggestedActionFor(category, priority),
            ExpectedHandlingTimeFor(priority),
            SafetyNoteFor(category, priority),
            source,
            keywords.Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList());
    }

    private static string MatchCategory(string text, List<string> keywords)
    {
        if (ContainsAny(text, keywords, "chập", "ổ điện", "o dien", "mất điện", "mat dien", "đèn", "den", "quạt", "quat", "điều hòa", "dieu hoa", "aptomat", "cầu dao"))
            return "Electric";
        if (ContainsAny(text, keywords, "mất nước", "mat nuoc", "rò nước", "ro nuoc", "vòi", "voi", "lavabo", "bồn cầu", "bon cau", "toilet", "ống nước", "ong nuoc", "ngập", "ngap"))
            return "Water";
        if (ContainsAny(text, keywords, "giường", "giuong", "tủ", "tu", "bàn", "ban", "ghế", "ghe", "cửa", "cua", "khóa", "khoa", "kính", "kinh"))
            return "Furniture";
        if (ContainsAny(text, keywords, "wifi", "internet", "mạng", "mang", "lan", "router"))
            return "Internet";
        if (ContainsAny(text, keywords, "cháy", "chay", "khói", "khoi", "mùi khét", "mui khet", "rò điện", "ro dien", "nguy hiểm", "nguy hiem"))
            return "Safety";
        if (ContainsAny(text, keywords, "vệ sinh", "ve sinh", "rác", "rac", "mùi hôi", "mui hoi", "cống", "cong", "côn trùng", "con trung"))
            return "Sanitation";

        return "Other";
    }

    private static string MatchPriority(
        string text,
        string category,
        string? requestedPriority,
        List<string> keywords)
    {
        if (ContainsAny(text, keywords, "cháy", "chay", "khói", "khoi", "chập điện", "chap dien", "rò điện", "ro dien", "ngập", "ngap", "vỡ ống", "vo ong"))
            return "urgent";
        if (ContainsAny(text, keywords, "mất nước", "mat nuoc", "mất điện", "mat dien", "không khóa được", "khong khoa duoc", "hỏng khóa", "hong khoa", "rò rỉ", "ro ri"))
            return "high";
        if (category is "Electric" or "Water" or "Safety")
            return "high";

        var normalized = NormalizePriority(requestedPriority);
        return string.IsNullOrWhiteSpace(normalized) ? "normal" : normalized;
    }

    private static string BuildSummary(string description, string category)
    {
        var clean = RegexCollapseWhitespace(description);
        var prefix = CategoryLabels.TryGetValue(category, out var label)
            ? $"Sự cố {label.ToLowerInvariant()}"
            : "Sự cố cần kiểm tra";

        if (string.IsNullOrWhiteSpace(clean))
            return prefix;

        return clean.Length <= 90
            ? $"{prefix}: {clean}"
            : $"{prefix}: {clean[..90].Trim()}...";
    }

    private static string SuggestedActionFor(string category, string priority) =>
        category switch
        {
            "Electric" => priority == "urgent"
                ? "Ngắt nguồn khu vực nếu có nguy cơ chập cháy, kiểm tra ổ cắm, aptomat và dây dẫn trước khi cấp điện lại."
                : "Kiểm tra ổ cắm, công tắc, đèn/quạt và aptomat phòng; thay linh kiện hỏng nếu cần.",
            "Water" => "Kiểm tra van cấp nước, vòi, lavabo/toilet và đường ống; khóa van cục bộ nếu có rò rỉ.",
            "Furniture" => "Kiểm tra hiện trạng vật dụng, siết lại ốc vít hoặc thay khóa/bản lề/bộ phận hỏng.",
            "Internet" => "Kiểm tra modem/router, dây mạng, cổng LAN và tín hiệu Wi-Fi trong phòng.",
            "Safety" => "Ưu tiên kiểm tra an toàn tại chỗ, khoanh vùng nguy cơ và báo admin nếu có rủi ro cho sinh viên.",
            "Sanitation" => "Kiểm tra nguồn mùi/tắc nghẽn, vệ sinh khu vực và xử lý cống/rác theo quy trình.",
            _ => "Tiếp nhận, kiểm tra trực tiếp tại phòng và cập nhật nguyên nhân sau khi khảo sát."
        };

    private static string ExpectedHandlingTimeFor(string priority) =>
        priority switch
        {
            "urgent" => "Trong 15-30 phút",
            "high" => "Trong ngày",
            "low" => "1-3 ngày",
            _ => "Trong 24-48 giờ"
        };

    private static string SafetyNoteFor(string category, string priority)
    {
        if (priority == "urgent")
            return "Cần ưu tiên xử lý và hạn chế sinh viên tiếp xúc khu vực sự cố cho đến khi kiểm tra xong.";

        return category switch
        {
            "Electric" => "Nhắc sinh viên không tự tháo ổ cắm hoặc thiết bị điện.",
            "Water" => "Nhắc sinh viên tránh để nước lan ra ổ điện hoặc khu vực dễ trơn trượt.",
            "Safety" => "Theo dõi sát vì có yếu tố an toàn.",
            _ => "Không có cảnh báo an toàn đặc biệt."
        };
    }

    private static IncidentAnalysisResult NormalizeResult(AiIncidentResponse response, string source)
    {
        var category = CategoryLabels.ContainsKey(response.Category ?? string.Empty)
            ? response.Category!
            : "Other";
        var priority = NormalizePriority(response.Priority) ?? "normal";

        return new IncidentAnalysisResult(
            category,
            CategoryLabels[category],
            priority,
            PriorityLabels[priority],
            FirstNonBlank(response.Summary, "Cần kiểm tra sự cố tại phòng."),
            FirstNonBlank(response.SuggestedAction, SuggestedActionFor(category, priority)),
            FirstNonBlank(response.ExpectedHandlingTime, ExpectedHandlingTimeFor(priority)),
            FirstNonBlank(response.SafetyNote, SafetyNoteFor(category, priority)),
            source,
            response.Keywords?.Where(item => !string.IsNullOrWhiteSpace(item)).Take(8).ToList() ?? []);
    }

    private static bool ContainsAny(string text, List<string> keywords, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!text.Contains(candidate, StringComparison.OrdinalIgnoreCase))
                continue;

            keywords.Add(candidate);
            return true;
        }

        return false;
    }

    private static string? NormalizePriority(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is "low" or "normal" or "high" or "urgent" ? normalized : null;
    }

    private static string RegexCollapseWhitespace(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");

    private static string FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    private sealed record AiIncidentResponse(
        string? Category,
        string? Priority,
        string? Summary,
        string? SuggestedAction,
        string? ExpectedHandlingTime,
        string? SafetyNote,
        List<string>? Keywords);
}
