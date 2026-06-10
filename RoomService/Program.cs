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

app.MapGet("/health", () => Results.Ok(new
{
    service = "RoomService",
    status = "Healthy"
}));

app.MapGet("/api/rooms", () =>
{
    return Results.Ok(rooms.Select(RoomResponse.FromRoom));
});

app.MapGet("/api/rooms/available", (
    string? buildingName,
    string? roomType,
    bool? gender,
    long? roomId) =>
{
    var candidates = rooms
        .Where(room => room.Status != "Maintenance")
        .Where(room => room.OccupiedBeds < room.Capacity)
        .Where(room => !roomId.HasValue || room.RoomId == roomId.Value)
        .Where(room => string.IsNullOrWhiteSpace(buildingName) ||
            room.BuildingName.Equals(buildingName, StringComparison.OrdinalIgnoreCase))
        .Where(room => string.IsNullOrWhiteSpace(roomType) ||
            room.RoomType.Equals(roomType, StringComparison.OrdinalIgnoreCase))
        .Where(room => !gender.HasValue || room.Gender == gender.Value)
        .OrderBy(room => room.MonthlyFee)
        .ThenByDescending(room => room.Capacity - room.OccupiedBeds)
        .ThenBy(room => room.RoomId)
        .Select(RoomResponse.FromRoom)
        .ToList();

    return candidates.Count == 0
        ? Results.NotFound(new { message = "No available room matched the request." })
        : Results.Ok(candidates.First());
});

app.MapPost("/api/rooms/{roomId:long}/occupy", (
    long roomId,
    OccupyRoomRequest request) =>
{
    var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    if (room.Status == "Maintenance")
        return Results.BadRequest(new { message = "Room is under maintenance." });

    if (room.OccupiedBeds >= room.Capacity)
        return Results.BadRequest(new { message = "Room is full." });

    room.OccupiedBeds++;
    room.Status = room.OccupiedBeds >= room.Capacity ? "Full" : "Available";
    room.LastContractCode = request.ContractCode;

    return Results.Ok(RoomResponse.FromRoom(room));
});

app.MapPost("/api/rooms/{roomId:long}/release", (long roomId) =>
{
    var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    if (room.OccupiedBeds > 0)
        room.OccupiedBeds--;

    room.Status = room.OccupiedBeds >= room.Capacity ? "Full" : "Available";

    return Results.Ok(RoomResponse.FromRoom(room));
});

app.Run();

static List<DormRoom> SeedRooms()
{
    return new List<DormRoom>
    {
        new(101, "A", "4-bed", true, 4, 0, 800000, "Available"),
        new(102, "A", "6-bed", true, 6, 0, 650000, "Available"),
        new(103, "A", "4-bed", false, 4, 0, 800000, "Available"),
        new(104, "A", "6-bed", false, 6, 0, 650000, "Available"),
        new(201, "B", "4-bed", false, 4, 0, 850000, "Available"),
        new(202, "B", "6-bed", false, 6, 0, 700000, "Available"),
        new(203, "B", "4-bed", true, 4, 0, 850000, "Available"),
        new(204, "B", "6-bed", true, 6, 0, 700000, "Available"),
        new(301, "C", "8-bed", true, 8, 0, 550000, "Available"),
        new(302, "C", "8-bed", false, 8, 0, 550000, "Available")
    };
}

public sealed class DormRoom
{
    public DormRoom(
        long roomId,
        string buildingName,
        string roomType,
        bool gender,
        int capacity,
        int occupiedBeds,
        decimal monthlyFee,
        string status)
    {
        RoomId = roomId;
        BuildingName = buildingName;
        RoomType = roomType;
        Gender = gender;
        Capacity = capacity;
        OccupiedBeds = occupiedBeds;
        MonthlyFee = monthlyFee;
        Status = status;
    }

    public long RoomId { get; set; }

    public string BuildingName { get; set; }

    public string RoomType { get; set; }

    public bool Gender { get; set; }

    public int Capacity { get; set; }

    public int OccupiedBeds { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Status { get; set; }

    public string LastContractCode { get; set; } = string.Empty;
}

public sealed record OccupyRoomRequest(
    long StudentId,
    long RegistrationId,
    string ContractCode);

public sealed record RoomResponse(
    long RoomId,
    string BuildingName,
    string RoomType,
    bool Gender,
    int Capacity,
    int OccupiedBeds,
    int AvailableBeds,
    decimal MonthlyFee,
    string Status)
{
    public static RoomResponse FromRoom(DormRoom room)
    {
        return new RoomResponse(
            room.RoomId,
            room.BuildingName,
            room.RoomType,
            room.Gender,
            room.Capacity,
            room.OccupiedBeds,
            room.Capacity - room.OccupiedBeds,
            room.MonthlyFee,
            room.Status);
    }
}
