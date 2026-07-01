using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using ContractStudentService.DTOs.Integration;
using ContractStudentService.Entities;
using ContractStudentService.Interfaces;

namespace ContractStudentService.Services;

public class RoomGatewayClient : IRoomGatewayClient
{
    private readonly HttpClient _httpClient;

    public RoomGatewayClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Gateway");
    }

    public async Task<AvailableRoomDto?> FindAvailableRoomAsync(
        RoomRegistration registration,
        Student student,
        long? requestedRoomId)
    {
        var query = new List<string>
        {
            $"gender={student.Gender.ToString().ToLowerInvariant()}"
        };

        if (requestedRoomId.HasValue)
        {
            query.Add($"roomId={requestedRoomId.Value}");
        }
        else
        {
            query.Add($"buildingName={Uri.EscapeDataString(registration.BuildingName)}");
            query.Add($"roomType={Uri.EscapeDataString(registration.RoomType)}");
        }

        var response = await _httpClient.GetAsync(
            $"/api/rooms/available?{string.Join("&", query)}");

        if (response.IsSuccessStatusCode)
            return await ReadRoomResponseAsync(response);

        if (response.StatusCode is not HttpStatusCode.NotFound
            and not HttpStatusCode.MethodNotAllowed)
        {
            throw new Exception("RoomService check phong trong that bai.");
        }

        return await FindAvailableRoomFromRoomListAsync(
            registration,
            student,
            requestedRoomId);
    }

    public async Task<AvailableRoomDto> OccupyRoomAsync(
        long roomId,
        long studentId,
        long registrationId,
        string contractCode,
        bool allowMaintenance = false)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/rooms/{roomId}/occupy",
            new OccupyRoomRequestDto
            {
                StudentId = studentId,
                RegistrationId = registrationId,
                ContractCode = contractCode,
                AllowMaintenance = allowMaintenance
            });

        if (response.IsSuccessStatusCode)
        {
            return await ReadRoomResponseAsync(response)
                ?? throw new Exception("RoomService khong tra ve du lieu phong.");
        }

        if (response.StatusCode is HttpStatusCode.NotFound
            or HttpStatusCode.MethodNotAllowed)
        {
            return await OccupyRoomWithGenericUpdateAsync(roomId);
        }

        var downstreamError = await ReadRoomServiceErrorAsync(response);

        if (downstreamError.IsStudentAlreadyInAnotherRoom &&
            downstreamError.RoomId.HasValue)
        {
            await ReleaseStudentOccupancyAsync(
                downstreamError.RoomId.Value,
                studentId);

            var retryResponse = await _httpClient.PostAsJsonAsync(
                $"/api/rooms/{roomId}/occupy",
                new OccupyRoomRequestDto
                {
                    StudentId = studentId,
                    RegistrationId = registrationId,
                    ContractCode = contractCode,
                    AllowMaintenance = allowMaintenance
                });

            if (retryResponse.IsSuccessStatusCode)
            {
                return await ReadRoomResponseAsync(retryResponse)
                    ?? throw new Exception("RoomService khong tra ve du lieu phong.");
            }

            var retryError = await ReadRoomServiceErrorAsync(retryResponse);
            throw new Exception(BuildOccupyErrorMessage(retryError, retryResponse.StatusCode));
        }

        throw new Exception(BuildOccupyErrorMessage(downstreamError, response.StatusCode));
    }

    public async Task ReleaseRoomAsync(
        long roomId,
        long studentId,
        string contractCode)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/rooms/{roomId}/release",
            new ReleaseRoomRequestDto
            {
                StudentId = studentId,
                ContractCode = contractCode
            });

        if (response.IsSuccessStatusCode)
            return;

        var downstreamError = await ReadRoomServiceErrorAsync(response);
        throw new Exception(BuildReleaseErrorMessage(downstreamError, response.StatusCode));
    }

    public async Task<AvailableRoomDto?> GetRoomByIdAsync(long roomId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/rooms/{roomId}");

            if (response.IsSuccessStatusCode)
                return await ReadRoomResponseAsync(response);

            var listResponse = await _httpClient.GetAsync("/api/rooms");

            if (!listResponse.IsSuccessStatusCode)
                return null;

            var rooms = await ReadRoomListAsync(listResponse);
            return rooms.FirstOrDefault(room => room.RoomId == roomId);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException)
        {
            return null;
        }
    }

    private async Task<AvailableRoomDto?> FindAvailableRoomFromRoomListAsync(
        RoomRegistration registration,
        Student student,
        long? requestedRoomId)
    {
        var response = await _httpClient.GetAsync("/api/rooms");

        if (!response.IsSuccessStatusCode)
            throw new Exception("RoomService khong tra ve danh sach phong.");

        var rooms = await ReadRoomListAsync(response);

        return rooms
            .Where(room => !requestedRoomId.HasValue || room.RoomId == requestedRoomId.Value)
            .Where(room => room.Gender == student.Gender)
            .Where(room => IsAvailable(room))
            .Where(room => requestedRoomId.HasValue ||
                RoomAssignmentScore(registration, room) > 0)
            .OrderByDescending(room => RoomAssignmentScore(registration, room))
            .ThenBy(room => room.MonthlyFee <= 0 ? decimal.MaxValue : room.MonthlyFee)
            .ThenByDescending(room => room.AvailableBeds)
            .ThenBy(room => room.RoomId)
            .FirstOrDefault();
    }

    private async Task<AvailableRoomDto> OccupyRoomWithGenericUpdateAsync(long roomId)
    {
        var roomsResponse = await _httpClient.GetAsync("/api/rooms");

        if (!roomsResponse.IsSuccessStatusCode)
            throw new Exception("RoomService khong tra ve danh sach phong de cap nhat.");

        var json = await roomsResponse.Content.ReadAsStringAsync();
        var node = JsonNode.Parse(json);
        var roomNode = FindRoomNode(node, roomId);

        if (roomNode == null)
            throw new Exception("RoomService khong tim thay phong can cap nhat.");

        var room = MapRoom(roomNode);

        if (!IsAvailable(room))
            throw new Exception("Phong da day hoac dang sua chua.");

        var newOccupiedBeds = room.OccupiedBeds + 1;
        var newAvailableBeds = Math.Max(room.Capacity - newOccupiedBeds, 0);

        SetNumber(roomNode, "currentOccupancy", newOccupiedBeds);
        SetNumber(roomNode, "occupiedBeds", newOccupiedBeds);
        SetNumber(roomNode, "availableBeds", newAvailableBeds);

        if (HasProperty(roomNode, "status"))
            roomNode["status"] = newAvailableBeds == 0 ? "Full" : "Available";

        var updatePayload = BuildRoomUpdatePayload(
            roomNode,
            roomId,
            newOccupiedBeds,
            newAvailableBeds);

        var updateResponse = await _httpClient.PutAsJsonAsync(
            $"/api/rooms/{roomId}",
            updatePayload);

        if (updateResponse.IsSuccessStatusCode)
        {
            if ((updateResponse.Content.Headers.ContentLength ?? 0) == 0)
                return MapRoom(roomNode);

            return await ReadRoomResponseAsync(updateResponse) ?? MapRoom(roomNode);
        }

        throw new Exception(
            "RoomService chua co API cap nhat phong phu hop. Can API POST /api/rooms/{id}/occupy hoac PUT /api/rooms/{id}.");
    }

    private async Task ReleaseStudentOccupancyAsync(
        long roomId,
        long studentId)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"/api/rooms/{roomId}/release",
            new ReleaseRoomRequestDto
            {
                StudentId = studentId,
                ContractCode = string.Empty
            });

        if (response.IsSuccessStatusCode)
            return;

        var downstreamError = await ReadRoomServiceErrorAsync(response);
        throw new Exception(
            "RoomService dang ghi nhan sinh vien o phong cu, nhung khong tu tra duoc phong cu. "
            + BuildReleaseErrorMessage(downstreamError, response.StatusCode));
    }

    private static async Task<AvailableRoomDto?> ReadRoomResponseAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return null;

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var first = root.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Undefined
                ? null
                : MapRoom(first);
        }

        if (TryGetProperty(root, "data", out var data))
            return MapRoom(data);

        if (TryGetProperty(root, "value", out var value))
            return MapRoom(value);

        return MapRoom(root);
    }

    private static async Task<List<AvailableRoomDto>> ReadRoomListAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
            return new List<AvailableRoomDto>();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (TryGetProperty(root, "data", out var data))
            root = data;
        else if (TryGetProperty(root, "value", out var value))
            root = value;

        if (root.ValueKind != JsonValueKind.Array)
            return new List<AvailableRoomDto> { MapRoom(root) };

        return root.EnumerateArray()
            .Select(MapRoom)
            .Where(room => room.RoomId > 0)
            .ToList();
    }

    private static async Task<RoomServiceError> ReadRoomServiceErrorAsync(
        HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(json))
        {
            return new RoomServiceError(
                string.Empty,
                null);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var message = GetString(root, "message", "detail", "title");
            var roomId = GetNullableLong(root, "roomId", "assignedRoomId");

            return new RoomServiceError(message, roomId);
        }
        catch (JsonException)
        {
            return new RoomServiceError(json, null);
        }
    }

    private static string BuildOccupyErrorMessage(
        RoomServiceError error,
        HttpStatusCode statusCode)
    {
        if (error.IsStudentAlreadyInAnotherRoom)
        {
            var roomText = error.RoomId.HasValue
                ? $" phong {error.RoomId.Value}"
                : " phong cu";

            return "RoomService dang ghi nhan sinh vien o" + roomText
                + ". He thong da thu dong bo lai nhung chua xep duoc phong moi. "
                + "Vui long kiem tra hop dong/lich su luu tru cu cua sinh vien.";
        }

        if (error.Message.Contains("Room is full", StringComparison.OrdinalIgnoreCase))
            return "Phong da het giuong. Vui long lam moi danh sach phong va chon phong khac.";

        if (error.Message.Contains("maintenance", StringComparison.OrdinalIgnoreCase))
            return "Phong dang sua chua hoac bao tri, khong the xep sinh vien vao phong nay.";

        if (!string.IsNullOrWhiteSpace(error.Message))
            return $"RoomService tu choi cap nhat phong: {error.Message}";

        return $"RoomService cap nhat trang thai phong that bai (HTTP {(int)statusCode}).";
    }

    private static string BuildReleaseErrorMessage(
        RoomServiceError error,
        HttpStatusCode statusCode)
    {
        if (!string.IsNullOrWhiteSpace(error.Message))
            return $"RoomService tra giuong that bai: {error.Message}";

        return $"RoomService tra giuong that bai (HTTP {(int)statusCode}).";
    }

    private static AvailableRoomDto MapRoom(JsonNode roomNode)
    {
        using var document = JsonDocument.Parse(roomNode.ToJsonString());

        return MapRoom(document.RootElement);
    }

    private static AvailableRoomDto MapRoom(JsonElement room)
    {
        var capacity = GetInt(room, "capacity");
        var occupiedBeds = GetInt(room, "occupiedBeds", "currentOccupancy");
        var availableBeds = GetInt(room, "availableBeds");

        if (availableBeds == 0 && capacity > 0)
            availableBeds = Math.Max(capacity - occupiedBeds, 0);

        return new AvailableRoomDto
        {
            RoomId = GetLong(room, "roomId", "id"),
            BuildingName = GetString(room, "buildingName"),
            RoomType = GetString(room, "roomType", "roomTypeName"),
            Gender = GetBool(room, "gender"),
            Capacity = capacity,
            OccupiedBeds = occupiedBeds,
            AvailableBeds = availableBeds,
            MonthlyFee = GetDecimal(room, "monthlyFee", "price", "roomFee"),
            Status = GetString(room, "status")
        };
    }

    private static JsonObject? FindRoomNode(JsonNode? node, long roomId)
    {
        if (node is JsonObject rootObject)
        {
            if (rootObject.TryGetPropertyValue("data", out var dataNode))
                return FindRoomNode(dataNode, roomId);

            if (rootObject.TryGetPropertyValue("value", out var valueNode))
                return FindRoomNode(valueNode, roomId);

            return MapRoom(rootObject).RoomId == roomId ? rootObject : null;
        }

        if (node is not JsonArray array)
            return null;

        foreach (var item in array)
        {
            if (item is JsonObject itemObject && MapRoom(itemObject).RoomId == roomId)
                return itemObject;
        }

        return null;
    }

    private static int RoomAssignmentScore(
        RoomRegistration registration,
        AvailableRoomDto room)
    {
        var matchesBuilding = IsBuildingMatch(
            registration.BuildingName,
            room.BuildingName);
        var matchesRoomType = IsRoomTypeMatch(
            registration.RoomType,
            room.RoomType);

        var score = (matchesBuilding, matchesRoomType) switch
        {
            (true, true) => 400,
            (false, true) => 300,
            (true, false) => 220,
            _ => 100
        };

        return score + Math.Min(room.AvailableBeds, 20);
    }

    private static bool IsAvailable(AvailableRoomDto room)
    {
        var status = FoldText(room.Status);

        if (status.Contains("day") ||
            status.Contains("full") ||
            status.Contains("sua") ||
            status.Contains("repair") ||
            status.Contains("maintenance"))
        {
            return false;
        }

        return room.AvailableBeds > 0;
    }

    private static bool IsBuildingMatch(string requestedValue, string roomValue)
    {
        if (string.IsNullOrWhiteSpace(requestedValue))
            return true;

        var requestedTokens = Tokens(requestedValue);
        var roomTokens = Tokens(roomValue);

        var requestedCode = requestedTokens.FirstOrDefault(token => token.Length == 1);

        if (!string.IsNullOrWhiteSpace(requestedCode))
            return roomTokens.Contains(requestedCode);

        return FoldText(roomValue).Contains(FoldText(requestedValue));
    }

    private static bool IsRoomTypeMatch(string requestedValue, string roomValue)
    {
        if (string.IsNullOrWhiteSpace(requestedValue))
            return true;

        var requestedNumber = Tokens(requestedValue)
            .FirstOrDefault(token => token.All(char.IsDigit));
        var roomNumber = Tokens(roomValue)
            .FirstOrDefault(token => token.All(char.IsDigit));

        if (!string.IsNullOrWhiteSpace(requestedNumber) &&
            !string.IsNullOrWhiteSpace(roomNumber))
        {
            return requestedNumber == roomNumber;
        }

        return FoldText(roomValue).Contains(FoldText(requestedValue));
    }

    private static HashSet<string> Tokens(string value)
    {
        return FoldText(value)
            .ToUpperInvariant()
            .Split(new[] { ' ', '-', '_', '/', '.', ',', ';', ':' },
                StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet();
    }

    private static string FoldText(string value)
    {
        var normalized = (value ?? string.Empty).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (character == '\u0111')
            {
                builder.Append('d');
                continue;
            }

            if (character == '\u0110')
            {
                builder.Append('D');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character) !=
                UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC)
            .ToLowerInvariant();
    }

    private static bool TryGetProperty(
        JsonElement element,
        string name,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string GetString(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value) &&
                value.ValueKind != JsonValueKind.Null)
            {
                return value.ToString();
            }
        }

        return string.Empty;
    }

    private static long GetLong(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number &&
                    value.TryGetInt64(out var number))
                    return number;

                if (long.TryParse(value.ToString(), out number))
                    return number;
            }
        }

        return 0;
    }

    private static long? GetNullableLong(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number &&
                    value.TryGetInt64(out var number))
                    return number;

                if (long.TryParse(value.ToString(), out number))
                    return number;
            }
        }

        return null;
    }

    private static int GetInt(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number &&
                    value.TryGetInt32(out var number))
                    return number;

                if (int.TryParse(value.ToString(), out number))
                    return number;
            }
        }

        return 0;
    }

    private static decimal GetDecimal(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value))
            {
                if (value.ValueKind == JsonValueKind.Number &&
                    value.TryGetDecimal(out var number))
                    return number;

                if (decimal.TryParse(value.ToString(), out number))
                    return number;
            }
        }

        return 0;
    }

    private static bool GetBool(JsonElement element, params string[] names)
    {
        foreach (var name in names)
        {
            if (TryGetProperty(element, name, out var value))
            {
                if (value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    return value.GetBoolean();

                if (bool.TryParse(value.ToString(), out var boolean))
                    return boolean;
            }
        }

        return false;
    }

    private static bool HasProperty(JsonObject node, string name)
    {
        return node.Any(property =>
            property.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static JsonObject BuildRoomUpdatePayload(
        JsonObject roomNode,
        long roomId,
        int occupiedBeds,
        int availableBeds)
    {
        var payload = new JsonObject();

        CopyProperty(roomNode, payload, "id");
        CopyProperty(roomNode, payload, "roomNumber");
        CopyProperty(roomNode, payload, "floor");
        CopyProperty(roomNode, payload, "capacity");
        CopyProperty(roomNode, payload, "gender");
        CopyProperty(roomNode, payload, "buildingId");
        CopyProperty(roomNode, payload, "roomTypeId");

        if (!HasProperty(payload, "id"))
            payload["id"] = roomId;

        payload["currentOccupancy"] = occupiedBeds;
        payload["status"] = availableBeds == 0 ? "Full" : "Available";

        return payload;
    }

    private static void CopyProperty(
        JsonObject source,
        JsonObject target,
        string name)
    {
        var property = source.FirstOrDefault(item =>
            item.Key.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(property.Key))
            return;

        target[name] = property.Value?.DeepClone();
    }

    private static void SetNumber(JsonObject node, string name, int value)
    {
        var key = node.FirstOrDefault(property =>
            property.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Key;

        if (!string.IsNullOrWhiteSpace(key))
            node[key] = value;
    }

    private sealed record RoomServiceError(
        string Message,
        long? RoomId)
    {
        public bool IsStudentAlreadyInAnotherRoom =>
            Message.Contains(
                "Student already occupies another room",
                StringComparison.OrdinalIgnoreCase);
    }
}
