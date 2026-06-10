var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowGateway");

var rooms = SeedRooms();
var roomLock = new object();

app.MapGet("/health", () => Results.Ok(new
{
    service = "RoomService",
    status = "Healthy"
}));

app.MapGet("/api/rooms", () =>
{
    lock (roomLock)
    {
        return Results.Ok(rooms.Select(RoomResponse.FromRoom).ToList());
    }
});

app.MapGet("/api/rooms/{roomId:long}", (long roomId) =>
{
    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        return room == null
            ? Results.NotFound(new { message = "Room not found." })
            : Results.Ok(RoomResponse.FromRoom(room));
    }
});

app.MapGet("/api/buildings", () =>
{
    lock (roomLock)
    {
        var buildings = rooms
            .GroupBy(room => room.BuildingName)
            .OrderBy(group => group.Key)
            .Select(group => new BuildingResponse(
                group.Key,
                $"Tòa {group.Key}",
                group.Count(),
                group.Sum(room => room.Capacity),
                group.Sum(room => room.OccupiedBeds),
                group.Sum(room => room.AvailableBeds)))
            .ToList();

        return Results.Ok(buildings);
    }
});

app.MapGet("/api/roomtypes", () =>
{
    lock (roomLock)
    {
        var roomTypes = rooms
            .GroupBy(room => room.RoomType)
            .OrderBy(group => group.Key)
            .Select(group => new RoomTypeResponse(
                group.Key,
                group.First().Capacity,
                group.Min(room => room.MonthlyFee),
                group.Max(room => room.MonthlyFee)))
            .ToList();

        return Results.Ok(roomTypes);
    }
});

app.MapGet("/api/rooms/available", (
    string? buildingName,
    string? roomType,
    bool? gender,
    long? roomId) =>
{
    List<RoomResponse> candidates;

    lock (roomLock)
    {
        candidates = rooms
            .Where(room => !room.IsMaintenance)
            .Where(room => room.AvailableBeds > 0)
            .Where(room => !roomId.HasValue || room.RoomId == roomId.Value)
            .Where(room => string.IsNullOrWhiteSpace(buildingName) ||
                room.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase))
            .Where(room => string.IsNullOrWhiteSpace(roomType) ||
                room.RoomType.Equals(roomType, StringComparison.OrdinalIgnoreCase))
            .Where(room => !gender.HasValue || room.Gender == gender.Value)
            .OrderBy(room => room.MonthlyFee)
            .ThenByDescending(room => room.AvailableBeds)
            .ThenBy(room => room.RoomId)
            .Select(RoomResponse.FromRoom)
            .ToList();
    }

    return candidates.Count == 0
        ? Results.NotFound(new { message = "No available room matched the request." })
        : Results.Ok(candidates.First());
});

app.MapPost("/api/rooms/{roomId:long}/occupy", (
    long roomId,
    OccupyRoomRequest request) =>
{
    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        if (room == null)
            return Results.NotFound(new { message = "Room not found." });

        if (request.StudentId <= 0 || request.RegistrationId <= 0)
            return Results.BadRequest(new { message = "StudentId and RegistrationId are required." });

        if (string.IsNullOrWhiteSpace(request.ContractCode))
            return Results.BadRequest(new { message = "ContractCode is required." });

        if (room.IsMaintenance)
            return Results.BadRequest(new { message = "Room is under maintenance." });

        if (room.AvailableBeds <= 0)
            return Results.BadRequest(new { message = "Room is full." });

        var existingRoom = rooms.FirstOrDefault(item =>
            item.RoomId != roomId &&
            item.OccupancyReferences.Any(reference =>
                reference.StudentId == request.StudentId));

        if (existingRoom != null)
        {
            return Results.BadRequest(new
            {
                message = "Student already occupies another room.",
                roomId = existingRoom.RoomId
            });
        }

        var existingReference = room.OccupancyReferences.FirstOrDefault(
            reference => reference.StudentId == request.StudentId ||
                reference.RegistrationId == request.RegistrationId);

        if (existingReference == null)
        {
            room.OccupancyReferences.Add(new RoomOccupancyReference(
                request.StudentId,
                request.RegistrationId,
                request.ContractCode,
                DateTime.UtcNow));
            room.OccupiedBeds++;
        }

        room.LastContractCode = request.ContractCode;
        room.RefreshStatus();

        return Results.Ok(RoomResponse.FromRoom(room));
    }
});

app.MapPost("/api/rooms/{roomId:long}/release", (long roomId) =>
{
    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        if (room == null)
            return Results.NotFound(new { message = "Room not found." });

        if (room.OccupancyReferences.Count > 0)
            room.OccupancyReferences.RemoveAt(room.OccupancyReferences.Count - 1);

        if (room.OccupiedBeds > 0)
            room.OccupiedBeds--;

        room.RefreshStatus();

        return Results.Ok(RoomResponse.FromRoom(room));
    }
});

app.Run();

static List<DormRoom> SeedRooms()
{
    return new List<DormRoom>
    {
        new(101, "101", "A", 1, "4-bed", true, 4, 0, 800000, "Available", "Điều hòa, quạt trần, tủ đồ"),
        new(102, "102", "A", 1, "6-bed", true, 6, 0, 650000, "Available", "Quạt trần, tủ đồ"),
        new(103, "103", "A", 1, "4-bed", false, 4, 0, 800000, "Available", "Điều hòa, quạt trần, tủ đồ"),
        new(104, "104", "A", 1, "6-bed", false, 6, 0, 650000, "Available", "Quạt trần, tủ đồ"),
        new(201, "201", "B", 2, "4-bed", false, 4, 0, 850000, "Available", "Điều hòa, bình nóng lạnh, tủ đồ"),
        new(202, "202", "B", 2, "6-bed", false, 6, 0, 700000, "Available", "Quạt trần, bình nóng lạnh, tủ đồ"),
        new(203, "203", "B", 2, "4-bed", true, 4, 0, 850000, "Available", "Điều hòa, bình nóng lạnh, tủ đồ"),
        new(204, "204", "B", 2, "6-bed", true, 6, 0, 700000, "Available", "Quạt trần, bình nóng lạnh, tủ đồ"),
        new(301, "301", "C", 3, "8-bed", true, 8, 0, 550000, "Available", "Quạt trần, tủ đồ"),
        new(302, "302", "C", 3, "8-bed", false, 8, 0, 550000, "Available", "Quạt trần, tủ đồ")
    };
}

public sealed class DormRoom
{
    public DormRoom(
        long roomId,
        string roomNumber,
        string buildingName,
        int floor,
        string roomType,
        bool gender,
        int capacity,
        int occupiedBeds,
        decimal monthlyFee,
        string status,
        string amenities)
    {
        RoomId = roomId;
        RoomNumber = roomNumber;
        BuildingName = buildingName;
        Floor = floor;
        RoomType = roomType;
        Gender = gender;
        Capacity = capacity;
        OccupiedBeds = occupiedBeds;
        MonthlyFee = monthlyFee;
        Status = status;
        Amenities = amenities;
    }

    public long RoomId { get; set; }

    public string RoomNumber { get; set; }

    public string BuildingName { get; set; }

    public int Floor { get; set; }

    public string RoomType { get; set; }

    public bool Gender { get; set; }

    public int Capacity { get; set; }

    public int OccupiedBeds { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Status { get; set; }

    public string Amenities { get; set; }

    public string LastContractCode { get; set; } = string.Empty;

    public List<RoomOccupancyReference> OccupancyReferences { get; } = new();

    public int AvailableBeds => Math.Max(Capacity - OccupiedBeds, 0);

    public bool IsMaintenance =>
        Status.Equals("Maintenance", StringComparison.OrdinalIgnoreCase);

    public void RefreshStatus()
    {
        if (IsMaintenance)
            return;

        Status = AvailableBeds == 0 ? "Full" : "Available";
    }
}

public sealed record OccupyRoomRequest(
    long StudentId,
    long RegistrationId,
    string ContractCode);

public sealed record RoomResponse(
    long RoomId,
    string RoomNumber,
    string BuildingName,
    string BuildingDisplayName,
    int Floor,
    string FloorName,
    string RoomType,
    bool Gender,
    string GenderText,
    int Capacity,
    int OccupiedBeds,
    int AvailableBeds,
    decimal MonthlyFee,
    string Status,
    string Amenities,
    IReadOnlyList<long> StudentIds,
    IReadOnlyList<RoomOccupancyResponse> OccupancyReferences)
{
    public static RoomResponse FromRoom(DormRoom room)
    {
        return new RoomResponse(
            room.RoomId,
            room.RoomNumber,
            room.BuildingName,
            $"Tòa {room.BuildingName}",
            room.Floor,
            $"Tầng {room.Floor}",
            room.RoomType,
            room.Gender,
            room.Gender ? "Nam" : "Nữ",
            room.Capacity,
            room.OccupiedBeds,
            room.AvailableBeds,
            room.MonthlyFee,
            room.Status,
            room.Amenities,
            room.OccupancyReferences
                .Select(reference => reference.StudentId)
                .ToList(),
            room.OccupancyReferences
                .Select(RoomOccupancyResponse.FromReference)
                .ToList());
    }
}

public sealed record RoomOccupancyReference(
    long StudentId,
    long RegistrationId,
    string ContractCode,
    DateTime OccupiedAt);

public sealed record RoomOccupancyResponse(
    long StudentId,
    long RegistrationId,
    string ContractCode,
    DateTime OccupiedAt)
{
    public static RoomOccupancyResponse FromReference(
        RoomOccupancyReference reference)
    {
        return new RoomOccupancyResponse(
            reference.StudentId,
            reference.RegistrationId,
            reference.ContractCode,
            reference.OccupiedAt);
    }
}

public sealed record BuildingResponse(
    string BuildingName,
    string DisplayName,
    int TotalRooms,
    int Capacity,
    int OccupiedBeds,
    int AvailableBeds);

public sealed record RoomTypeResponse(
    string RoomType,
    int Capacity,
    decimal MinMonthlyFee,
    decimal MaxMonthlyFee);
