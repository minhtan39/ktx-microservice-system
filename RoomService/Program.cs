using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=localhost,1433;Database=SmartDormitory_RoomBuildingDB;User Id=sa;Password=KtxServer@2026Demo!;TrustServerCertificate=True;Encrypt=False";

var configuredProvider = builder.Configuration["Database:Provider"];
var fallbackToInMemory = builder.Configuration.GetValue("Database:FallbackToInMemory", builder.Environment.IsDevelopment());
var useInMemoryDatabase =
    string.Equals(configuredProvider, "InMemory", StringComparison.OrdinalIgnoreCase) ||
    (fallbackToInMemory && !CanOpenSqlServer(connectionString));
var databaseProvider = useInMemoryDatabase ? "InMemoryLocalDemo" : "SqlServer";

builder.Services.AddSingleton(new RoomDatabaseRuntime(databaseProvider));
builder.Services.AddDbContext<RoomBuildingDbContext>(options =>
{
    if (useInMemoryDatabase)
        options.UseInMemoryDatabase("SmartDormitory_RoomBuildingDB");
    else
        options.UseSqlServer(
            connectionString,
            sqlOptions => sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(3),
                errorNumbersToAdd: null));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowGateway");

if (app.Configuration.GetValue("Database:AutoCreate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<RoomBuildingDbContext>();
    await db.Database.EnsureCreatedAsync();

    if (!useInMemoryDatabase)
        await EnsureRoomBuildingSchemaAsync(db);

    await SeedRoomBuildingDataAsync(db);
}

app.MapGet("/health", async (
    RoomBuildingDbContext db,
    RoomDatabaseRuntime runtime) =>
{
    var databaseReady = false;

    try
    {
        databaseReady = await db.Database.CanConnectAsync();
    }
    catch
    {
        databaseReady = false;
    }

    return Results.Ok(new
    {
        service = "RoomService",
        status = databaseReady ? "Healthy" : "Degraded",
        database = databaseReady
            ? $"RoomBuildingDB ready ({runtime.Provider})"
            : $"RoomBuildingDB unavailable ({runtime.Provider})"
    });
});

app.MapGet("/api/buildings", async (RoomBuildingDbContext db) =>
{
    var buildings = await LoadBuildingsAsync(db);
    var rooms = await LoadRoomsAsync(db);

    return Results.Ok(BuildingResponses(buildings, rooms));
});

app.MapGet("/api/buildings/{buildingName}", async (
    string buildingName,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    var rooms = await LoadRoomsAsync(db);

    return Results.Ok(BuildingResponse.FromBuilding(building, rooms));
});

app.MapPost("/api/buildings", async (
    BuildingRequest request,
    RoomBuildingDbContext db) =>
{
    var normalizedName = NormalizeCode(request.BuildingName);

    if (string.IsNullOrWhiteSpace(normalizedName))
        return Results.BadRequest(new { message = "BuildingName is required." });

    if (await db.Buildings.AnyAsync(item => item.BuildingName == normalizedName))
        return Results.BadRequest(new { message = "Building already exists." });

    if (request.Floors <= 0)
        return Results.BadRequest(new { message = "Floors must be greater than zero." });

    var building = new DormBuilding
    {
        BuildingName = normalizedName,
        DisplayName = CleanOrDefault(request.DisplayName, $"Tòa {normalizedName}"),
        Floors = request.Floors,
        Description = request.Description?.Trim() ?? string.Empty
    };

    db.Buildings.Add(building);
    await db.SaveChangesAsync();

    var rooms = await LoadRoomsAsync(db);

    return Results.Created(
        $"/api/buildings/{building.BuildingName}",
        BuildingResponse.FromBuilding(building, rooms));
});

app.MapPut("/api/buildings/{buildingName}", async (
    string buildingName,
    BuildingRequest request,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName, tracking: true);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    if (request.Floors <= 0)
        return Results.BadRequest(new { message = "Floors must be greater than zero." });

    var rooms = await LoadRoomsAsync(db);
    var maxUsedFloor = rooms
        .Where(room => IsSameCode(room.BuildingName, building.BuildingName))
        .Select(room => room.Floor)
        .DefaultIfEmpty(0)
        .Max();

    if (request.Floors < maxUsedFloor)
    {
        return Results.BadRequest(new
        {
            message = "Building floors cannot be lower than existing room floors.",
            maxUsedFloor
        });
    }

    building.DisplayName = CleanOrDefault(request.DisplayName, $"Tòa {building.BuildingName}");
    building.Floors = request.Floors;
    building.Description = request.Description?.Trim() ?? string.Empty;

    await db.SaveChangesAsync();
    rooms = await LoadRoomsAsync(db);

    return Results.Ok(BuildingResponse.FromBuilding(building, rooms));
});

app.MapDelete("/api/buildings/{buildingName}", async (
    string buildingName,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName, tracking: true);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    if (await db.Rooms.AnyAsync(room => room.BuildingName == building.BuildingName))
        return Results.BadRequest(new { message = "Cannot delete a building that still has rooms." });

    db.Buildings.Remove(building);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/api/buildings/{buildingName}/facility-notices", async (
    string buildingName,
    bool? activeOnly,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    var query = db.BuildingFacilityNotices
        .AsNoTracking()
        .Where(notice => notice.BuildingName == building.BuildingName);

    if (activeOnly.GetValueOrDefault(true))
        query = query.Where(notice => notice.IsActive && notice.Status != "Resolved");

    var notices = await query
        .OrderByDescending(notice => notice.Severity == "Critical")
        .ThenByDescending(notice => notice.StartedAt)
        .ThenBy(notice => notice.AreaName)
        .Select(notice => BuildingFacilityNoticeResponse.FromNotice(notice))
        .ToListAsync();

    return Results.Ok(notices);
});

app.MapPost("/api/buildings/{buildingName}/facility-notices", async (
    string buildingName,
    BuildingFacilityNoticeRequest request,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    var validation = ValidateBuildingNoticeRequest(request, out var normalized);

    if (validation != null)
        return validation;

    var now = DateTime.UtcNow;
    var notice = new BuildingFacilityNotice
    {
        BuildingName = building.BuildingName,
        AreaName = normalized.AreaName,
        Category = normalized.Category,
        Status = normalized.Status,
        Severity = normalized.Severity,
        Title = normalized.Title,
        Description = normalized.Description,
        StartedAt = normalized.StartedAt ?? now,
        ExpectedResolvedAt = normalized.ExpectedResolvedAt,
        ResolvedAt = IsResolvedBuildingNotice(normalized.Status, normalized.IsActive)
            ? now
            : null,
        UpdatedAt = now,
        IsActive = normalized.IsActive && !IsResolvedBuildingNotice(normalized.Status, normalized.IsActive)
    };

    db.BuildingFacilityNotices.Add(notice);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/buildings/{Uri.EscapeDataString(building.BuildingName)}/facility-notices/{notice.Id}",
        BuildingFacilityNoticeResponse.FromNotice(notice));
});

app.MapPut("/api/buildings/{buildingName}/facility-notices/{noticeId:long}", async (
    string buildingName,
    long noticeId,
    BuildingFacilityNoticeRequest request,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    var notice = await db.BuildingFacilityNotices.FirstOrDefaultAsync(item =>
        item.Id == noticeId &&
        item.BuildingName == building.BuildingName);

    if (notice == null)
        return Results.NotFound(new { message = "Building facility notice not found." });

    var validation = ValidateBuildingNoticeRequest(request, out var normalized);

    if (validation != null)
        return validation;

    var now = DateTime.UtcNow;
    var isResolved = IsResolvedBuildingNotice(normalized.Status, normalized.IsActive);

    notice.AreaName = normalized.AreaName;
    notice.Category = normalized.Category;
    notice.Status = normalized.Status;
    notice.Severity = normalized.Severity;
    notice.Title = normalized.Title;
    notice.Description = normalized.Description;
    notice.StartedAt = normalized.StartedAt ?? notice.StartedAt;
    notice.ExpectedResolvedAt = normalized.ExpectedResolvedAt;
    notice.IsActive = normalized.IsActive && !isResolved;
    notice.ResolvedAt = isResolved ? notice.ResolvedAt ?? now : null;
    notice.UpdatedAt = now;

    await db.SaveChangesAsync();

    return Results.Ok(BuildingFacilityNoticeResponse.FromNotice(notice));
});

app.MapPatch("/api/buildings/{buildingName}/facility-notices/{noticeId:long}/resolve", async (
    string buildingName,
    long noticeId,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    var notice = await db.BuildingFacilityNotices.FirstOrDefaultAsync(item =>
        item.Id == noticeId &&
        item.BuildingName == building.BuildingName);

    if (notice == null)
        return Results.NotFound(new { message = "Building facility notice not found." });

    var now = DateTime.UtcNow;
    notice.Status = "Resolved";
    notice.IsActive = false;
    notice.ResolvedAt = now;
    notice.UpdatedAt = now;

    await db.SaveChangesAsync();

    return Results.Ok(BuildingFacilityNoticeResponse.FromNotice(notice));
});

app.MapDelete("/api/buildings/{buildingName}/facility-notices/{noticeId:long}", async (
    string buildingName,
    long noticeId,
    RoomBuildingDbContext db) =>
{
    var building = await FindBuildingAsync(db, buildingName);

    if (building == null)
        return Results.NotFound(new { message = "Building not found." });

    var notice = await db.BuildingFacilityNotices.FirstOrDefaultAsync(item =>
        item.Id == noticeId &&
        item.BuildingName == building.BuildingName);

    if (notice == null)
        return Results.NotFound(new { message = "Building facility notice not found." });

    db.BuildingFacilityNotices.Remove(notice);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/api/roomtypes", GetRoomTypesAsync);
app.MapGet("/api/room-types", GetRoomTypesAsync);

async Task<IResult> GetRoomTypesAsync(RoomBuildingDbContext db)
{
    var roomTypes = await LoadRoomTypesAsync(db);
    var rooms = await LoadRoomsAsync(db);

    return Results.Ok(RoomTypeResponses(roomTypes, rooms));
}

app.MapGet("/api/roomtypes/{roomType}", GetRoomTypeAsync);
app.MapGet("/api/room-types/{roomType}", GetRoomTypeAsync);

async Task<IResult> GetRoomTypeAsync(string roomType, RoomBuildingDbContext db)
{
    var existing = await FindRoomTypeAsync(db, roomType);

    if (existing == null)
        return Results.NotFound(new { message = "Room type not found." });

    var rooms = await LoadRoomsAsync(db);

    return Results.Ok(RoomTypeResponse.FromRoomType(existing, rooms));
}

app.MapPost("/api/roomtypes", CreateRoomTypeAsync);
app.MapPost("/api/room-types", CreateRoomTypeAsync);

async Task<IResult> CreateRoomTypeAsync(RoomTypeRequest request, RoomBuildingDbContext db)
{
    var normalizedType = NormalizeRoomType(request.RoomType);

    if (string.IsNullOrWhiteSpace(normalizedType))
        return Results.BadRequest(new { message = "RoomType is required." });

    if (await db.RoomTypes.AnyAsync(item => item.RoomType == normalizedType))
        return Results.BadRequest(new { message = "Room type already exists." });

    if (request.Capacity <= 0)
        return Results.BadRequest(new { message = "Capacity must be greater than zero." });

    if (request.MonthlyFee < 0)
        return Results.BadRequest(new { message = "MonthlyFee cannot be negative." });

    var roomType = new DormRoomType
    {
        RoomType = normalizedType,
        Capacity = request.Capacity,
        MonthlyFee = request.MonthlyFee,
        Description = request.Description?.Trim() ?? string.Empty,
        Amenities = request.Amenities?.Trim() ?? DefaultAmenities(normalizedType)
    };

    db.RoomTypes.Add(roomType);
    await db.SaveChangesAsync();

    var rooms = await LoadRoomsAsync(db);

    return Results.Created(
        $"/api/room-types/{Uri.EscapeDataString(roomType.RoomType)}",
        RoomTypeResponse.FromRoomType(roomType, rooms));
}

app.MapPut("/api/roomtypes/{roomType}", UpdateRoomTypeAsync);
app.MapPut("/api/room-types/{roomType}", UpdateRoomTypeAsync);

async Task<IResult> UpdateRoomTypeAsync(
    string roomType,
    RoomTypeRequest request,
    RoomBuildingDbContext db)
{
    var existing = await FindRoomTypeAsync(db, roomType, tracking: true);

    if (existing == null)
        return Results.NotFound(new { message = "Room type not found." });

    if (request.Capacity <= 0)
        return Results.BadRequest(new { message = "Capacity must be greater than zero." });

    if (request.MonthlyFee < 0)
        return Results.BadRequest(new { message = "MonthlyFee cannot be negative." });

    var roomsForType = await db.Rooms
        .Include(room => room.OccupancyReferences)
        .Where(room => room.RoomType == existing.RoomType)
        .ToListAsync();

    var maxOccupiedBeds = roomsForType
        .Select(room => room.OccupiedBeds)
        .DefaultIfEmpty(0)
        .Max();

    if (request.Capacity < maxOccupiedBeds)
    {
        return Results.BadRequest(new
        {
            message = "Capacity cannot be lower than occupied beds in existing rooms.",
            maxOccupiedBeds
        });
    }

    existing.Capacity = request.Capacity;
    existing.MonthlyFee = request.MonthlyFee;
    existing.Description = request.Description?.Trim() ?? string.Empty;
    existing.Amenities = request.Amenities?.Trim() ?? DefaultAmenities(existing.RoomType);

    foreach (var room in roomsForType)
    {
        room.Capacity = existing.Capacity;
        room.MonthlyFee = existing.MonthlyFee;
        room.Amenities = existing.Amenities;
        room.RefreshStatus();
    }

    await db.SaveChangesAsync();

    var rooms = await LoadRoomsAsync(db);

    return Results.Ok(RoomTypeResponse.FromRoomType(existing, rooms));
}

app.MapDelete("/api/roomtypes/{roomType}", DeleteRoomTypeAsync);
app.MapDelete("/api/room-types/{roomType}", DeleteRoomTypeAsync);

async Task<IResult> DeleteRoomTypeAsync(string roomType, RoomBuildingDbContext db)
{
    var existing = await FindRoomTypeAsync(db, roomType, tracking: true);

    if (existing == null)
        return Results.NotFound(new { message = "Room type not found." });

    if (await db.Rooms.AnyAsync(room => room.RoomType == existing.RoomType))
        return Results.BadRequest(new { message = "Cannot delete a room type that is used by rooms." });

    db.RoomTypes.Remove(existing);
    await db.SaveChangesAsync();

    return Results.NoContent();
}

app.MapGet("/api/rooms", async (
    string? buildingName,
    int? floor,
    string? roomType,
    bool? gender,
    string? status,
    RoomBuildingDbContext db) =>
{
    string? normalizedStatus = null;

    if (!string.IsNullOrWhiteSpace(status))
    {
        try
        {
            normalizedStatus = NormalizeStatus(status);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }
    }

    var buildings = await LoadBuildingsAsync(db);
    var rooms = await LoadRoomsAsync(db);

    var filteredRooms = rooms
        .Where(room => string.IsNullOrWhiteSpace(buildingName) ||
            IsSameCode(room.BuildingName, buildingName))
        .Where(room => !floor.HasValue || room.Floor == floor.Value)
        .Where(room => string.IsNullOrWhiteSpace(roomType) ||
            IsSameCode(room.RoomType, roomType))
        .Where(room => !gender.HasValue || room.Gender == gender.Value)
        .Where(room => normalizedStatus == null ||
            IsSameCode(room.Status, normalizedStatus))
        .OrderBy(room => room.BuildingName)
        .ThenBy(room => room.Floor)
        .ThenBy(room => room.RoomNumber)
        .Select(room => RoomResponse.FromRoom(room, buildings))
        .ToList();

    return Results.Ok(filteredRooms);
});

app.MapGet("/api/rooms/floor-map", async (
    string? buildingName,
    int? floor,
    RoomBuildingDbContext db) =>
{
    var buildings = await LoadBuildingsAsync(db);
    var rooms = await LoadRoomsAsync(db);

    var filteredRooms = rooms
        .Where(room => string.IsNullOrWhiteSpace(buildingName) ||
            IsSameCode(room.BuildingName, buildingName))
        .Where(room => !floor.HasValue || room.Floor == floor.Value)
        .OrderBy(room => room.BuildingName)
        .ThenBy(room => room.Floor)
        .ThenBy(room => room.RoomNumber)
        .Select(room => RoomResponse.FromRoom(room, buildings))
        .ToList();

    return Results.Ok(new FloorMapResponse(
        buildingName,
        floor,
        filteredRooms.Count,
        filteredRooms.Sum(room => room.OccupiedBeds),
        filteredRooms.Sum(room => room.AvailableBeds),
        filteredRooms));
});

app.MapGet("/api/rooms/available", async (
    string? buildingName,
    string? roomType,
    bool? gender,
    long? roomId,
    RoomBuildingDbContext db) =>
{
    var buildings = await LoadBuildingsAsync(db);
    var rooms = await LoadRoomsAsync(db);

    var candidates = rooms
        .Where(room => !room.IsMaintenance)
        .Where(room => room.AvailableBeds > 0)
        .Where(room => !roomId.HasValue || room.RoomId == roomId.Value)
        .Where(room => string.IsNullOrWhiteSpace(buildingName) ||
            IsSameCode(room.BuildingName, buildingName))
        .Where(room => string.IsNullOrWhiteSpace(roomType) ||
            IsSameCode(room.RoomType, roomType))
        .Where(room => !gender.HasValue || room.Gender == gender.Value)
        .OrderBy(room => room.MonthlyFee)
        .ThenByDescending(room => room.AvailableBeds)
        .ThenBy(room => room.RoomId)
        .Select(room => RoomResponse.FromRoom(room, buildings))
        .ToList();

    return candidates.Count == 0
        ? Results.NotFound(new { message = "No available room matched the request." })
        : Results.Ok(candidates.First());
});

app.MapGet("/api/rooms/{roomId:long}", async (
    long roomId,
    RoomBuildingDbContext db) =>
{
    var room = await LoadRoomAsync(db, roomId);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    var buildings = await LoadBuildingsAsync(db);

    return Results.Ok(RoomResponse.FromRoom(room, buildings));
});

app.MapPost("/api/rooms", async (
    RoomRequest request,
    RoomBuildingDbContext db) =>
{
    var buildings = await LoadBuildingsAsync(db);
    var roomTypes = await LoadRoomTypesAsync(db);
    var rooms = await LoadRoomsAsync(db);

    var validation = ValidateRoomRequest(
        request,
        buildings,
        roomTypes,
        rooms,
        null,
        out var building,
        out var roomType);

    if (validation != null)
        return validation;

    var room = new DormRoom
    {
        RoomId = request.RoomId,
        RoomNumber = CleanOrDefault(request.RoomNumber, request.RoomId.ToString()),
        BuildingName = building!.BuildingName,
        Floor = request.Floor,
        RoomType = roomType!.RoomType,
        Gender = request.Gender,
        Capacity = request.Capacity ?? roomType.Capacity,
        OccupiedBeds = 0,
        MonthlyFee = request.MonthlyFee ?? roomType.MonthlyFee,
        Status = "Available",
        Amenities = CleanOrDefault(request.Amenities, roomType.Amenities)
    };

    room.RefreshStatus();
    db.Rooms.Add(room);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/rooms/{room.RoomId}",
        RoomResponse.FromRoom(room, buildings));
});

app.MapPut("/api/rooms/{roomId:long}", async (
    long roomId,
    RoomRequest request,
    RoomBuildingDbContext db) =>
{
    var room = await LoadRoomAsync(db, roomId, tracking: true);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    var buildings = await LoadBuildingsAsync(db);
    var roomTypes = await LoadRoomTypesAsync(db);
    var rooms = await LoadRoomsAsync(db);

    var validation = ValidateRoomRequest(
        request with { RoomId = roomId },
        buildings,
        roomTypes,
        rooms,
        roomId,
        out var building,
        out var roomType);

    if (validation != null)
        return validation;

    var selectedBuilding = building!;
    var selectedRoomType = roomType!;
    var newCapacity = request.Capacity ?? selectedRoomType.Capacity;

    if (newCapacity < room.OccupiedBeds)
        return Results.BadRequest(new { message = "Capacity cannot be lower than occupied beds." });

    room.RoomNumber = CleanOrDefault(request.RoomNumber, room.RoomNumber);
    room.BuildingName = selectedBuilding.BuildingName;
    room.Floor = request.Floor;
    room.RoomType = selectedRoomType.RoomType;
    room.Gender = request.Gender;
    room.Capacity = newCapacity;
    room.MonthlyFee = request.MonthlyFee ?? selectedRoomType.MonthlyFee;
    room.Amenities = CleanOrDefault(request.Amenities, selectedRoomType.Amenities);
    room.RefreshStatus();

    await db.SaveChangesAsync();

    return Results.Ok(RoomResponse.FromRoom(room, buildings));
});

app.MapMethods(
    "/api/rooms/{roomId:long}/status",
    ["PATCH", "PUT"],
    async (long roomId, RoomStatusRequest request, RoomBuildingDbContext db) =>
    {
        var room = await LoadRoomAsync(db, roomId, tracking: true);

        if (room == null)
            return Results.NotFound(new { message = "Room not found." });

        string normalizedStatus;

        try
        {
            normalizedStatus = NormalizeRoomStatusUpdate(request.Status);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(new { message = exception.Message });
        }

        if (normalizedStatus.Equals("Maintenance", StringComparison.OrdinalIgnoreCase))
        {
            room.Status = "Maintenance";
        }
        else
        {
            room.Status = "Available";
            room.RefreshStatus();
        }

        await db.SaveChangesAsync();
        var buildings = await LoadBuildingsAsync(db);

        return Results.Ok(RoomResponse.FromRoom(room, buildings));
    });

app.MapDelete("/api/rooms/{roomId:long}", async (
    long roomId,
    RoomBuildingDbContext db) =>
{
    var room = await LoadRoomAsync(db, roomId, tracking: true);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    if (room.OccupiedBeds > 0 || room.OccupancyReferences.Count > 0)
        return Results.BadRequest(new { message = "Cannot delete an occupied room." });

    db.Rooms.Remove(room);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapPost("/api/rooms/{roomId:long}/occupy", async (
    long roomId,
    OccupyRoomRequest request,
    RoomBuildingDbContext db) =>
{
    var room = await LoadRoomAsync(db, roomId, tracking: true);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    if (request.StudentId <= 0 || request.RegistrationId <= 0)
        return Results.BadRequest(new { message = "StudentId and RegistrationId are required." });

    if (string.IsNullOrWhiteSpace(request.ContractCode))
        return Results.BadRequest(new { message = "ContractCode is required." });

    if (room.IsMaintenance)
        return Results.BadRequest(new { message = "Room is under maintenance." });

    var existingRoom = await db.Rooms
        .AsNoTracking()
        .Include(item => item.OccupancyReferences)
        .FirstOrDefaultAsync(item =>
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

    if (existingReference != null)
    {
        room.LastContractCode = request.ContractCode.Trim();
        room.RefreshStatus();
        await db.SaveChangesAsync();

        var buildings = await LoadBuildingsAsync(db);

        return Results.Ok(RoomResponse.FromRoom(room, buildings));
    }

    if (room.AvailableBeds <= 0)
        return Results.BadRequest(new { message = "Room is full." });

    room.OccupancyReferences.Add(new RoomOccupancyReference
    {
        StudentId = request.StudentId,
        RegistrationId = request.RegistrationId,
        ContractCode = request.ContractCode.Trim(),
        OccupiedAt = DateTime.UtcNow
    });

    room.OccupiedBeds++;
    room.LastContractCode = request.ContractCode.Trim();
    room.RefreshStatus();

    await db.SaveChangesAsync();

    var currentBuildings = await LoadBuildingsAsync(db);

    return Results.Ok(RoomResponse.FromRoom(room, currentBuildings));
});

app.MapPost("/api/rooms/{roomId:long}/release", async (
    long roomId,
    RoomBuildingDbContext db) =>
{
    var room = await LoadRoomAsync(db, roomId, tracking: true);

    if (room == null)
        return Results.NotFound(new { message = "Room not found." });

    var lastReference = room.OccupancyReferences
        .OrderByDescending(reference => reference.OccupiedAt)
        .FirstOrDefault();

    if (lastReference != null)
        db.RoomOccupancyReferences.Remove(lastReference);

    if (room.OccupiedBeds > 0)
        room.OccupiedBeds--;

    room.RefreshStatus();
    await db.SaveChangesAsync();

    var buildings = await LoadBuildingsAsync(db);

    return Results.Ok(RoomResponse.FromRoom(room, buildings));
});

app.Run();

static async Task<List<DormBuilding>> LoadBuildingsAsync(
    RoomBuildingDbContext db,
    bool tracking = false)
{
    var query = db.Buildings
        .Include(building => building.FacilityNotices)
        .AsQueryable();

    if (!tracking)
        query = query.AsNoTracking();

    return await query
        .OrderBy(building => building.BuildingName)
        .ToListAsync();
}

static async Task<List<DormRoomType>> LoadRoomTypesAsync(
    RoomBuildingDbContext db,
    bool tracking = false)
{
    var query = db.RoomTypes.AsQueryable();

    if (!tracking)
        query = query.AsNoTracking();

    return await query
        .OrderBy(roomType => roomType.Capacity)
        .ThenBy(roomType => roomType.RoomType)
        .ToListAsync();
}

static async Task<List<DormRoom>> LoadRoomsAsync(
    RoomBuildingDbContext db,
    bool tracking = false)
{
    var query = db.Rooms
        .Include(room => room.OccupancyReferences)
        .AsQueryable();

    if (!tracking)
        query = query.AsNoTracking();

    return await query.ToListAsync();
}

static async Task<DormRoom?> LoadRoomAsync(
    RoomBuildingDbContext db,
    long roomId,
    bool tracking = false)
{
    var query = db.Rooms
        .Include(room => room.OccupancyReferences)
        .AsQueryable();

    if (!tracking)
        query = query.AsNoTracking();

    return await query.FirstOrDefaultAsync(room => room.RoomId == roomId);
}

static Task<DormBuilding?> FindBuildingAsync(
    RoomBuildingDbContext db,
    string? buildingName,
    bool tracking = false)
{
    var normalizedName = NormalizeCode(buildingName);

    if (string.IsNullOrWhiteSpace(normalizedName))
        return Task.FromResult<DormBuilding?>(null);

    var query = db.Buildings
        .Include(building => building.FacilityNotices)
        .AsQueryable();

    if (!tracking)
        query = query.AsNoTracking();

    return query.FirstOrDefaultAsync(building => building.BuildingName == normalizedName);
}

static Task<DormRoomType?> FindRoomTypeAsync(
    RoomBuildingDbContext db,
    string? roomType,
    bool tracking = false)
{
    var normalizedType = NormalizeRoomType(roomType);

    if (string.IsNullOrWhiteSpace(normalizedType))
        return Task.FromResult<DormRoomType?>(null);

    var query = db.RoomTypes.AsQueryable();

    if (!tracking)
        query = query.AsNoTracking();

    return query.FirstOrDefaultAsync(type => type.RoomType == normalizedType);
}

static List<BuildingResponse> BuildingResponses(
    List<DormBuilding> buildings,
    List<DormRoom> rooms)
{
    return buildings
        .OrderBy(building => building.BuildingName)
        .Select(building => BuildingResponse.FromBuilding(building, rooms))
        .ToList();
}

static List<RoomTypeResponse> RoomTypeResponses(
    List<DormRoomType> roomTypes,
    List<DormRoom> rooms)
{
    return roomTypes
        .OrderBy(roomType => roomType.Capacity)
        .ThenBy(roomType => roomType.RoomType)
        .Select(roomType => RoomTypeResponse.FromRoomType(roomType, rooms))
        .ToList();
}

static IResult? ValidateRoomRequest(
    RoomRequest request,
    List<DormBuilding> buildings,
    List<DormRoomType> roomTypes,
    List<DormRoom> rooms,
    long? existingRoomId,
    out DormBuilding? building,
    out DormRoomType? roomType)
{
    building = FindBuilding(buildings, request.BuildingName);
    roomType = FindRoomType(roomTypes, request.RoomType);

    if (request.RoomId <= 0)
        return Results.BadRequest(new { message = "RoomId is required." });

    if (!existingRoomId.HasValue &&
        rooms.Any(room => room.RoomId == request.RoomId))
    {
        return Results.BadRequest(new { message = "Room already exists." });
    }

    if (building == null)
        return Results.BadRequest(new { message = "Building does not exist." });

    if (roomType == null)
        return Results.BadRequest(new { message = "Room type does not exist." });

    if (request.Floor <= 0 || request.Floor > building.Floors)
        return Results.BadRequest(new { message = "Floor is outside building range." });

    var roomNumber = CleanOrDefault(request.RoomNumber, request.RoomId.ToString());
    var buildingNameForDuplicateCheck = building.BuildingName;
    var duplicateRoomNumber = rooms.Any(room =>
        (!existingRoomId.HasValue || room.RoomId != existingRoomId.Value) &&
        IsSameCode(room.BuildingName, buildingNameForDuplicateCheck) &&
        room.Floor == request.Floor &&
        room.RoomNumber.Equals(roomNumber, StringComparison.OrdinalIgnoreCase));

    if (duplicateRoomNumber)
        return Results.BadRequest(new { message = "Room number already exists on this floor." });

    if (request.Capacity.HasValue && request.Capacity.Value <= 0)
        return Results.BadRequest(new { message = "Capacity must be greater than zero." });

    if (request.MonthlyFee.HasValue && request.MonthlyFee.Value < 0)
        return Results.BadRequest(new { message = "MonthlyFee cannot be negative." });

    return null;
}

static DormBuilding? FindBuilding(
    List<DormBuilding> buildings,
    string? buildingName)
{
    return buildings.FirstOrDefault(building =>
        IsSameCode(building.BuildingName, buildingName));
}

static DormRoomType? FindRoomType(
    List<DormRoomType> roomTypes,
    string? roomType)
{
    return roomTypes.FirstOrDefault(type =>
        IsSameCode(type.RoomType, roomType));
}

static string NormalizeCode(string? value)
{
    return (value ?? string.Empty).Trim().ToUpperInvariant();
}

static bool CanOpenSqlServer(string connectionString)
{
    for (var attempt = 1; attempt <= 5; attempt++)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                ConnectTimeout = 3
            };

            if (!string.IsNullOrWhiteSpace(builder.InitialCatalog))
                builder.InitialCatalog = "master";

            using var connection = new SqlConnection(builder.ConnectionString);
            connection.Open();

            return true;
        }
        catch
        {
            if (attempt == 5)
                return false;

            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
    }

    return false;
}

static Task EnsureRoomBuildingSchemaAsync(RoomBuildingDbContext db)
{
    return db.Database.ExecuteSqlRawAsync("""
        IF OBJECT_ID(N'[dbo].[BuildingFacilityNotices]', N'U') IS NULL
        BEGIN
            CREATE TABLE [dbo].[BuildingFacilityNotices] (
                [Id] bigint IDENTITY(1,1) NOT NULL,
                [BuildingName] nvarchar(32) NOT NULL,
                [AreaName] nvarchar(120) NOT NULL,
                [Category] nvarchar(40) NOT NULL,
                [Status] nvarchar(40) NOT NULL,
                [Severity] nvarchar(40) NOT NULL,
                [Title] nvarchar(180) NOT NULL,
                [Description] nvarchar(1000) NOT NULL,
                [StartedAt] datetime2 NOT NULL,
                [ExpectedResolvedAt] datetime2 NULL,
                [ResolvedAt] datetime2 NULL,
                [UpdatedAt] datetime2 NOT NULL,
                [IsActive] bit NOT NULL,
                CONSTRAINT [PK_BuildingFacilityNotices] PRIMARY KEY ([Id]),
                CONSTRAINT [FK_BuildingFacilityNotices_Buildings_BuildingName]
                    FOREIGN KEY ([BuildingName]) REFERENCES [dbo].[Buildings]([BuildingName])
                    ON DELETE CASCADE
            );

            CREATE INDEX [IX_BuildingFacilityNotices_BuildingName_IsActive]
                ON [dbo].[BuildingFacilityNotices] ([BuildingName], [IsActive]);
        END
        """);
}

static string NormalizeRoomType(string? value)
{
    return (value ?? string.Empty).Trim().ToLowerInvariant();
}

static bool IsSameCode(string? first, string? second)
{
    return string.Equals(
        first?.Trim(),
        second?.Trim(),
        StringComparison.OrdinalIgnoreCase);
}

static string CleanOrDefault(string? value, string fallback)
{
    return string.IsNullOrWhiteSpace(value)
        ? fallback
        : value.Trim();
}

static string DefaultAmenities(string roomType)
{
    if (roomType.Contains("4", StringComparison.OrdinalIgnoreCase))
        return "Điều hòa, quạt trần, tủ đồ";

    if (roomType.Contains("6", StringComparison.OrdinalIgnoreCase))
        return "Quạt trần, tủ đồ";

    return "Quạt trần, giường tầng, tủ đồ";
}

static string NormalizeStatus(string? status)
{
    var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();

    return normalized switch
    {
        "" or "available" or "trong" or "trống" or "con cho" or "còn chỗ" => "Available",
        "full" or "day" or "đầy" or "day phong" or "đầy phòng" => "Full",
        "maintenance" or "repair" or "repairing" or "under-maintenance" or "under maintenance" or
            "bao tri" or "bảo trì" or "dang bao tri" or "đang bảo trì" or
            "sua chua" or "sửa chữa" or "dang sua chua" or "đang sửa chữa" => "Maintenance",
        _ => throw new ArgumentException("Status must be Available, Full, or Maintenance.")
    };
}

static string NormalizeRoomStatusUpdate(string? status)
{
    var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();

    return normalized switch
    {
        "maintenance" or "repair" or "repairing" or "under-maintenance" or "under maintenance" or
            "bao tri" or "bao-tri" or "bảo trì" or "dang bao tri" or "đang bảo trì" or
            "sua chua" or "sửa chữa" or "dang sua chua" or "đang sửa chữa" => "Maintenance",
        "" or "auto" or "available" or "normal" or "open" or "release" or "done" or "completed" or "hoan thanh" or "hoàn thành" => "Auto",
        _ => throw new ArgumentException("Status must be Maintenance or Auto.")
    };
}

static IResult? ValidateBuildingNoticeRequest(
    BuildingFacilityNoticeRequest request,
    out NormalizedBuildingFacilityNotice normalized)
{
    normalized = new NormalizedBuildingFacilityNotice(
        CleanOrDefault(request.AreaName, string.Empty),
        NormalizeBuildingNoticeCategory(request.Category),
        NormalizeBuildingNoticeStatus(request.Status),
        NormalizeBuildingNoticeSeverity(request.Severity),
        CleanOrDefault(request.Title, string.Empty),
        request.Description?.Trim() ?? string.Empty,
        request.StartedAt,
        request.ExpectedResolvedAt,
        request.IsActive ?? true);

    if (string.IsNullOrWhiteSpace(normalized.AreaName))
        return Results.BadRequest(new { message = "AreaName is required." });

    if (string.IsNullOrWhiteSpace(normalized.Title))
        return Results.BadRequest(new { message = "Title is required." });

    if (normalized.ExpectedResolvedAt.HasValue &&
        normalized.StartedAt.HasValue &&
        normalized.ExpectedResolvedAt.Value < normalized.StartedAt.Value)
    {
        return Results.BadRequest(new { message = "ExpectedResolvedAt cannot be earlier than StartedAt." });
    }

    return null;
}

static string NormalizeBuildingNoticeCategory(string? category)
{
    var normalized = (category ?? string.Empty).Trim().ToLowerInvariant();

    return normalized switch
    {
        "" or "other" or "khac" or "khác" => "Other",
        "elevator" or "thangmay" or "thang may" or "thang máy" => "Elevator",
        "stair" or "stairs" or "thangbo" or "thang bo" or "thang bộ" => "Stair",
        "learningroom" or "classroom" or "studyroom" or "phonghoc" or "phong hoc" or "phòng học" or "tu hoc" or "tự học" => "LearningRoom",
        "electricity" or "dien" or "điện" => "Electricity",
        "water" or "nuoc" or "nước" => "Water",
        "safety" or "antoan" or "an toan" or "an toàn" => "Safety",
        _ => "Other"
    };
}

static string NormalizeBuildingNoticeStatus(string? status)
{
    var normalized = (status ?? string.Empty).Trim().ToLowerInvariant();

    return normalized switch
    {
        "" or "notice" or "info" or "thong bao" or "thông báo" => "Notice",
        "outofservice" or "out-of-service" or "closed" or "broken" or "hong" or "hỏng" or "tam dung" or "tạm dừng" => "OutOfService",
        "maintenance" or "bao tri" or "bảo trì" or "bao duong" or "bảo dưỡng" => "Maintenance",
        "replacing" or "replace" or "thay the" or "thay thế" or "thay thiet bi" or "thay thiết bị" => "Replacing",
        "inspection" or "checking" or "kiem tra" or "kiểm tra" => "Inspection",
        "resolved" or "done" or "completed" or "hoan thanh" or "hoàn thành" => "Resolved",
        _ => "Notice"
    };
}

static string NormalizeBuildingNoticeSeverity(string? severity)
{
    var normalized = (severity ?? string.Empty).Trim().ToLowerInvariant();

    return normalized switch
    {
        "critical" or "danger" or "urgent" or "khan cap" or "khẩn cấp" => "Critical",
        "warning" or "can chu y" or "cần chú ý" => "Warning",
        _ => "Info"
    };
}

static bool IsResolvedBuildingNotice(string status, bool isActive)
{
    return !isActive || status.Equals("Resolved", StringComparison.OrdinalIgnoreCase);
}

static async Task SeedRoomBuildingDataAsync(RoomBuildingDbContext db)
{
    if (await db.Buildings.AnyAsync() ||
        await db.RoomTypes.AnyAsync() ||
        await db.Rooms.AnyAsync())
    {
        return;
    }

    db.Buildings.AddRange(
        new DormBuilding
        {
            BuildingName = "A",
            DisplayName = "Tòa A",
            Floors = 5,
            Description = "Khu phòng nam/nữ tiêu chuẩn"
        },
        new DormBuilding
        {
            BuildingName = "B",
            DisplayName = "Tòa B",
            Floors = 5,
            Description = "Khu phòng có tiện nghi mở rộng"
        },
        new DormBuilding
        {
            BuildingName = "C",
            DisplayName = "Tòa C",
            Floors = 6,
            Description = "Khu phòng nhiều giường"
        });

    db.RoomTypes.AddRange(
        new DormRoomType
        {
            RoomType = "4-bed",
            Capacity = 4,
            MonthlyFee = 800000,
            Description = "Phòng 4 giường",
            Amenities = "Điều hòa, quạt trần, tủ đồ"
        },
        new DormRoomType
        {
            RoomType = "6-bed",
            Capacity = 6,
            MonthlyFee = 650000,
            Description = "Phòng 6 giường",
            Amenities = "Quạt trần, tủ đồ"
        },
        new DormRoomType
        {
            RoomType = "8-bed",
            Capacity = 8,
            MonthlyFee = 550000,
            Description = "Phòng 8 giường",
            Amenities = "Quạt trần, giường tầng, tủ đồ"
        });

    db.Rooms.AddRange(
        NewSeedRoom(101, "101", "A", 1, "4-bed", true, 4, 800000, "Available", "Điều hòa, quạt trần, tủ đồ"),
        NewSeedRoom(102, "102", "A", 1, "6-bed", true, 6, 650000, "Available", "Quạt trần, tủ đồ"),
        NewSeedRoom(103, "103", "A", 1, "4-bed", false, 4, 800000, "Available", "Điều hòa, quạt trần, tủ đồ"),
        NewSeedRoom(104, "104", "A", 1, "6-bed", false, 6, 650000, "Available", "Quạt trần, tủ đồ"),
        NewSeedRoom(201, "201", "B", 2, "4-bed", false, 4, 850000, "Available", "Điều hòa, bình nóng lạnh, tủ đồ"),
        NewSeedRoom(202, "202", "B", 2, "6-bed", false, 6, 700000, "Available", "Quạt trần, bình nóng lạnh, tủ đồ"),
        NewSeedRoom(203, "203", "B", 2, "4-bed", true, 4, 850000, "Available", "Điều hòa, bình nóng lạnh, tủ đồ"),
        NewSeedRoom(204, "204", "B", 2, "6-bed", true, 6, 700000, "Maintenance", "Quạt trần, bình nóng lạnh, tủ đồ"),
        NewSeedRoom(301, "301", "C", 3, "8-bed", true, 8, 550000, "Available", "Quạt trần, tủ đồ"),
        NewSeedRoom(302, "302", "C", 3, "8-bed", false, 8, 550000, "Available", "Quạt trần, tủ đồ"));

    var now = DateTime.UtcNow;
    db.BuildingFacilityNotices.AddRange(
        NewSeedBuildingNotice("A", "Thang máy A01", "Elevator", "OutOfService", "Critical", "Thang máy A01 tạm dừng", "Đang kiểm tra hộp điều khiển, sinh viên vui lòng dùng thang bộ khu A.", now.AddHours(-3), now.AddHours(8)),
        NewSeedBuildingNotice("B", "Thang bộ phía đông", "Stair", "Maintenance", "Warning", "Thang bộ phía đông đang bảo trì", "Đội vận hành đang sửa lan can, tạm thời di chuyển bằng thang bộ trung tâm.", now.AddDays(-1), now.AddDays(1)),
        NewSeedBuildingNotice("C", "Phòng tự học tầng 3", "LearningRoom", "Replacing", "Info", "Thay thế thiết bị phòng tự học", "Đang thay bộ đèn và ổ cắm, khu tự học mở lại sau khi nghiệm thu.", now.AddHours(-5), now.AddDays(2)));

    await db.SaveChangesAsync();
}

static DormRoom NewSeedRoom(
    long roomId,
    string roomNumber,
    string buildingName,
    int floor,
    string roomType,
    bool gender,
    int capacity,
    decimal monthlyFee,
    string status,
    string amenities)
{
    return new DormRoom
    {
        RoomNumber = roomNumber,
        BuildingName = buildingName,
        Floor = floor,
        RoomType = roomType,
        Gender = gender,
        Capacity = capacity,
        OccupiedBeds = 0,
        MonthlyFee = monthlyFee,
        Status = status,
        Amenities = amenities
    };
}

static BuildingFacilityNotice NewSeedBuildingNotice(
    string buildingName,
    string areaName,
    string category,
    string status,
    string severity,
    string title,
    string description,
    DateTime startedAt,
    DateTime? expectedResolvedAt)
{
    return new BuildingFacilityNotice
    {
        BuildingName = buildingName,
        AreaName = areaName,
        Category = category,
        Status = status,
        Severity = severity,
        Title = title,
        Description = description,
        StartedAt = startedAt,
        ExpectedResolvedAt = expectedResolvedAt,
        UpdatedAt = DateTime.UtcNow,
        IsActive = true
    };
}

public sealed record RoomDatabaseRuntime(string Provider);

public sealed class RoomBuildingDbContext(DbContextOptions<RoomBuildingDbContext> options)
    : DbContext(options)
{
    public DbSet<DormBuilding> Buildings => Set<DormBuilding>();

    public DbSet<DormRoomType> RoomTypes => Set<DormRoomType>();

    public DbSet<DormRoom> Rooms => Set<DormRoom>();

    public DbSet<RoomOccupancyReference> RoomOccupancyReferences => Set<RoomOccupancyReference>();

    public DbSet<BuildingFacilityNotice> BuildingFacilityNotices => Set<BuildingFacilityNotice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DormBuilding>(entity =>
        {
            entity.ToTable("Buildings");
            entity.HasKey(building => building.BuildingName);
            entity.Property(building => building.BuildingName).HasMaxLength(32);
            entity.Property(building => building.DisplayName).HasMaxLength(120);
            entity.Property(building => building.Description).HasMaxLength(500);
            entity.HasMany(building => building.FacilityNotices)
                .WithOne()
                .HasForeignKey(notice => notice.BuildingName)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DormRoomType>(entity =>
        {
            entity.ToTable("RoomTypes");
            entity.HasKey(roomType => roomType.RoomType);
            entity.Property(roomType => roomType.RoomType).HasMaxLength(64);
            entity.Property(roomType => roomType.MonthlyFee).HasPrecision(18, 2);
            entity.Property(roomType => roomType.Description).HasMaxLength(500);
            entity.Property(roomType => roomType.Amenities).HasMaxLength(500);
        });

        modelBuilder.Entity<DormRoom>(entity =>
        {
            entity.ToTable("Rooms");
            entity.HasKey(room => room.RoomId);
            entity.Property(room => room.RoomNumber).HasMaxLength(32);
            entity.Property(room => room.BuildingName).HasMaxLength(32);
            entity.Property(room => room.RoomType).HasMaxLength(64);
            entity.Property(room => room.MonthlyFee).HasPrecision(18, 2);
            entity.Property(room => room.Status).HasMaxLength(32);
            entity.Property(room => room.Amenities).HasMaxLength(500);
            entity.Property(room => room.LastContractCode).HasMaxLength(64);
            entity.Ignore(room => room.AvailableBeds);
            entity.Ignore(room => room.IsMaintenance);
            entity.HasIndex(room => new { room.BuildingName, room.Floor, room.RoomNumber })
                .IsUnique();
            entity.HasIndex(room => room.RoomType);
        });

        modelBuilder.Entity<RoomOccupancyReference>(entity =>
        {
            entity.ToTable("RoomOccupancyReferences");
            entity.HasKey(reference => reference.Id);
            entity.Property(reference => reference.ContractCode).HasMaxLength(64);
            entity.HasIndex(reference => reference.StudentId).IsUnique();
            entity.HasIndex(reference => reference.RegistrationId).IsUnique();
            entity.HasOne<DormRoom>()
                .WithMany(room => room.OccupancyReferences)
                .HasForeignKey(reference => reference.RoomId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BuildingFacilityNotice>(entity =>
        {
            entity.ToTable("BuildingFacilityNotices");
            entity.HasKey(notice => notice.Id);
            entity.Property(notice => notice.BuildingName).HasMaxLength(32);
            entity.Property(notice => notice.AreaName).HasMaxLength(120);
            entity.Property(notice => notice.Category).HasMaxLength(40);
            entity.Property(notice => notice.Status).HasMaxLength(40);
            entity.Property(notice => notice.Severity).HasMaxLength(40);
            entity.Property(notice => notice.Title).HasMaxLength(180);
            entity.Property(notice => notice.Description).HasMaxLength(1000);
            entity.HasIndex(notice => new { notice.BuildingName, notice.IsActive });
        });
    }
}

public sealed class DormBuilding
{
    public string BuildingName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Floors { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<BuildingFacilityNotice> FacilityNotices { get; set; } = new();
}

public sealed class BuildingFacilityNotice
{
    public long Id { get; set; }

    public string BuildingName { get; set; } = string.Empty;

    public string AreaName { get; set; } = string.Empty;

    public string Category { get; set; } = "Other";

    public string Status { get; set; } = "Notice";

    public string Severity { get; set; } = "Info";

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime StartedAt { get; set; }

    public DateTime? ExpectedResolvedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;
}

public sealed class DormRoomType
{
    public string RoomType { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Description { get; set; } = string.Empty;

    public string Amenities { get; set; } = string.Empty;
}

public sealed class DormRoom
{
    public long RoomId { get; set; }

    public string RoomNumber { get; set; } = string.Empty;

    public string BuildingName { get; set; } = string.Empty;

    public int Floor { get; set; }

    public string RoomType { get; set; } = string.Empty;

    public bool Gender { get; set; }

    public int Capacity { get; set; }

    public int OccupiedBeds { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Status { get; set; } = "Available";

    public string Amenities { get; set; } = string.Empty;

    public string LastContractCode { get; set; } = string.Empty;

    public List<RoomOccupancyReference> OccupancyReferences { get; set; } = new();

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

public sealed class RoomOccupancyReference
{
    public long Id { get; set; }

    public long RoomId { get; set; }

    public long StudentId { get; set; }

    public long RegistrationId { get; set; }

    public string ContractCode { get; set; } = string.Empty;

    public DateTime OccupiedAt { get; set; }
}

public sealed record BuildingRequest(
    string? BuildingName,
    string? DisplayName,
    int Floors,
    string? Description);

public sealed record RoomTypeRequest(
    string? RoomType,
    int Capacity,
    decimal MonthlyFee,
    string? Description,
    string? Amenities);

public sealed record RoomRequest(
    long RoomId,
    string? RoomNumber,
    string? BuildingName,
    int Floor,
    string? RoomType,
    bool Gender,
    int? Capacity,
    decimal? MonthlyFee,
    string? Amenities);

public sealed record RoomStatusRequest(string Status);

public sealed record OccupyRoomRequest(
    long StudentId,
    long RegistrationId,
    string ContractCode);

public sealed record BuildingFacilityNoticeRequest(
    string? AreaName,
    string? Category,
    string? Status,
    string? Severity,
    string? Title,
    string? Description,
    DateTime? StartedAt,
    DateTime? ExpectedResolvedAt,
    bool? IsActive);

public sealed record NormalizedBuildingFacilityNotice(
    string AreaName,
    string Category,
    string Status,
    string Severity,
    string Title,
    string Description,
    DateTime? StartedAt,
    DateTime? ExpectedResolvedAt,
    bool IsActive);

public sealed record BuildingResponse(
    string BuildingName,
    string DisplayName,
    int Floors,
    string Description,
    int TotalRooms,
    int Capacity,
    int OccupiedBeds,
    int AvailableBeds,
    string OperationalStatus,
    string OperationalStatusText,
    int ActiveNoticeCount,
    int CriticalNoticeCount,
    IReadOnlyList<BuildingFacilityNoticeResponse> FacilityNotices)
{
    public static BuildingResponse FromBuilding(
        DormBuilding building,
        List<DormRoom> rooms)
    {
        var buildingRooms = rooms
            .Where(room => RoomServiceShared.IsSameCode(room.BuildingName, building.BuildingName))
            .ToList();
        var activeNotices = building.FacilityNotices
            .Where(notice => notice.IsActive && !notice.Status.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(notice => notice.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(notice => notice.StartedAt)
            .ToList();
        var operationalStatus = GetOperationalStatus(activeNotices);

        return new BuildingResponse(
            building.BuildingName,
            building.DisplayName,
            building.Floors,
            building.Description,
            buildingRooms.Count,
            buildingRooms.Sum(room => room.Capacity),
            buildingRooms.Sum(room => room.OccupiedBeds),
            buildingRooms.Sum(room => room.AvailableBeds),
            operationalStatus,
            GetOperationalStatusText(operationalStatus),
            activeNotices.Count,
            activeNotices.Count(notice => notice.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase)),
            activeNotices.Select(BuildingFacilityNoticeResponse.FromNotice).ToList());
    }

    private static string GetOperationalStatus(List<BuildingFacilityNotice> activeNotices)
    {
        if (activeNotices.Any(notice =>
            notice.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase) ||
            notice.Status.Equals("OutOfService", StringComparison.OrdinalIgnoreCase)))
        {
            return "Interrupted";
        }

        if (activeNotices.Any(notice =>
            notice.Status.Equals("Maintenance", StringComparison.OrdinalIgnoreCase) ||
            notice.Status.Equals("Replacing", StringComparison.OrdinalIgnoreCase)))
        {
            return "Maintenance";
        }

        return activeNotices.Count > 0 ? "Notice" : "Normal";
    }

    private static string GetOperationalStatusText(string status)
    {
        return status switch
        {
            "Interrupted" => "Có hạng mục cần chú ý",
            "Maintenance" => "Đang bảo trì/bảo dưỡng",
            "Notice" => "Có thông báo vận hành",
            _ => "Vận hành ổn định"
        };
    }
}

public sealed record RoomTypeResponse(
    string RoomType,
    int Capacity,
    decimal MonthlyFee,
    decimal MinMonthlyFee,
    decimal MaxMonthlyFee,
    string Description,
    string Amenities,
    int TotalRooms)
{
    public static RoomTypeResponse FromRoomType(
        DormRoomType roomType,
        List<DormRoom> rooms)
    {
        var matchingRooms = rooms
            .Where(room => RoomServiceShared.IsSameCode(room.RoomType, roomType.RoomType))
            .ToList();

        return new RoomTypeResponse(
            roomType.RoomType,
            roomType.Capacity,
            roomType.MonthlyFee,
            matchingRooms.Count == 0 ? roomType.MonthlyFee : matchingRooms.Min(room => room.MonthlyFee),
            matchingRooms.Count == 0 ? roomType.MonthlyFee : matchingRooms.Max(room => room.MonthlyFee),
            roomType.Description,
            roomType.Amenities,
            matchingRooms.Count);
    }
}

public sealed record BuildingFacilityNoticeResponse(
    long Id,
    string BuildingName,
    string AreaName,
    string Category,
    string CategoryText,
    string Status,
    string StatusText,
    string Severity,
    string SeverityText,
    string Title,
    string Description,
    DateTime StartedAt,
    DateTime? ExpectedResolvedAt,
    DateTime? ResolvedAt,
    DateTime UpdatedAt,
    bool IsActive)
{
    public static BuildingFacilityNoticeResponse FromNotice(BuildingFacilityNotice notice)
    {
        return new BuildingFacilityNoticeResponse(
            notice.Id,
            notice.BuildingName,
            notice.AreaName,
            notice.Category,
            GetCategoryText(notice.Category),
            notice.Status,
            GetStatusText(notice.Status),
            notice.Severity,
            GetSeverityText(notice.Severity),
            notice.Title,
            notice.Description,
            notice.StartedAt,
            notice.ExpectedResolvedAt,
            notice.ResolvedAt,
            notice.UpdatedAt,
            notice.IsActive);
    }

    private static string GetCategoryText(string category)
    {
        return category switch
        {
            "Elevator" => "Thang máy",
            "Stair" => "Thang bộ",
            "LearningRoom" => "Phòng học/tự học",
            "Electricity" => "Điện",
            "Water" => "Nước",
            "Safety" => "An toàn",
            _ => "Hạng mục khác"
        };
    }

    private static string GetStatusText(string status)
    {
        return status switch
        {
            "OutOfService" => "Tạm dừng sử dụng",
            "Maintenance" => "Đang bảo trì",
            "Replacing" => "Đang thay thế thiết bị",
            "Inspection" => "Đang kiểm tra",
            "Resolved" => "Đã hoàn thành",
            _ => "Thông báo"
        };
    }

    private static string GetSeverityText(string severity)
    {
        return severity switch
        {
            "Critical" => "Khẩn cấp",
            "Warning" => "Cần chú ý",
            _ => "Thông tin"
        };
    }
}

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
    string StatusText,
    string Amenities,
    IReadOnlyList<long> StudentIds,
    IReadOnlyList<RoomOccupancyResponse> OccupancyReferences)
{
    public static RoomResponse FromRoom(
        DormRoom room,
        List<DormBuilding> buildings)
    {
        room.RefreshStatus();
        var building = RoomServiceShared.FindBuilding(buildings, room.BuildingName);

        return new RoomResponse(
            room.RoomId,
            room.RoomNumber,
            room.BuildingName,
            building?.DisplayName ?? $"Tòa {room.BuildingName}",
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
            GetStatusText(room),
            room.Amenities,
            room.OccupancyReferences
                .Select(reference => reference.StudentId)
                .ToList(),
            room.OccupancyReferences
                .OrderBy(reference => reference.OccupiedAt)
                .Select(RoomOccupancyResponse.FromReference)
                .ToList());
    }

    private static string GetStatusText(DormRoom room)
    {
        if (room.IsMaintenance)
            return "Đang bảo trì";

        if (room.AvailableBeds == 0)
            return "Đầy phòng";

        return room.OccupiedBeds == 0
            ? "Trống"
            : "Còn giường";
    }
}

public sealed record FloorMapResponse(
    string? BuildingName,
    int? Floor,
    int TotalRooms,
    int OccupiedBeds,
    int AvailableBeds,
    IReadOnlyList<RoomResponse> Rooms);

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

public static class RoomServiceShared
{
    public static DormBuilding? FindBuilding(
        List<DormBuilding> buildings,
        string? buildingName)
    {
        return buildings.FirstOrDefault(building =>
            IsSameCode(building.BuildingName, buildingName));
    }

    public static bool IsSameCode(string? first, string? second)
    {
        return string.Equals(
            first?.Trim(),
            second?.Trim(),
            StringComparison.OrdinalIgnoreCase);
    }
}
