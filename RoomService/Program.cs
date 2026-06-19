using System.Text.Json;

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

var roomDataFilePath = app.Configuration["RoomData:FilePath"]
    ?? Path.Combine(app.Environment.ContentRootPath, "data", "room-data.json");
var roomStore = LoadRoomState(roomDataFilePath);
var buildings = roomStore.Buildings;
var roomTypes = roomStore.RoomTypes;
var rooms = roomStore.Rooms;
var roomLock = new object();

app.MapGet("/health", () => Results.Ok(new
{
    service = "RoomService",
    status = "Healthy"
}));

app.MapGet("/api/buildings", () =>
{
    lock (roomLock)
    {
        return Results.Ok(BuildingResponses(buildings, rooms));
    }
});

app.MapGet("/api/buildings/{buildingName}", (string buildingName) =>
{
    lock (roomLock)
    {
        var building = FindBuilding(buildings, buildingName);

        return building == null
            ? Results.NotFound(new { message = "Building not found." })
            : Results.Ok(BuildingResponse.FromBuilding(building, rooms));
    }
});

app.MapPost("/api/buildings", (BuildingRequest request) =>
{
    lock (roomLock)
    {
        var normalizedName = NormalizeCode(request.BuildingName);

        if (string.IsNullOrWhiteSpace(normalizedName))
            return Results.BadRequest(new { message = "BuildingName is required." });

        if (buildings.Any(item => IsSameCode(item.BuildingName, normalizedName)))
            return Results.BadRequest(new { message = "Building already exists." });

        if (request.Floors <= 0)
            return Results.BadRequest(new { message = "Floors must be greater than zero." });

        var building = new DormBuilding(
            normalizedName,
            CleanOrDefault(request.DisplayName, $"Tòa {normalizedName}"),
            request.Floors,
            request.Description?.Trim() ?? string.Empty);

        buildings.Add(building);
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Created(
            $"/api/buildings/{building.BuildingName}",
            BuildingResponse.FromBuilding(building, rooms));
    }
});

app.MapPut("/api/buildings/{buildingName}", (
    string buildingName,
    BuildingRequest request) =>
{
    lock (roomLock)
    {
        var building = FindBuilding(buildings, buildingName);

        if (building == null)
            return Results.NotFound(new { message = "Building not found." });

        if (request.Floors <= 0)
            return Results.BadRequest(new { message = "Floors must be greater than zero." });

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
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Ok(BuildingResponse.FromBuilding(building, rooms));
    }
});

app.MapDelete("/api/buildings/{buildingName}", (string buildingName) =>
{
    lock (roomLock)
    {
        var building = FindBuilding(buildings, buildingName);

        if (building == null)
            return Results.NotFound(new { message = "Building not found." });

        if (rooms.Any(room => IsSameCode(room.BuildingName, building.BuildingName)))
            return Results.BadRequest(new { message = "Cannot delete a building that still has rooms." });

        buildings.Remove(building);
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.NoContent();
    }
});

app.MapGet("/api/roomtypes", () => GetRoomTypes());
app.MapGet("/api/room-types", () => GetRoomTypes());

IResult GetRoomTypes()
{
    lock (roomLock)
    {
        return Results.Ok(RoomTypeResponses(roomTypes, rooms));
    }
}

app.MapGet("/api/roomtypes/{roomType}", (string roomType) => GetRoomType(roomType));
app.MapGet("/api/room-types/{roomType}", (string roomType) => GetRoomType(roomType));

IResult GetRoomType(string roomType)
{
    lock (roomLock)
    {
        var existing = FindRoomType(roomTypes, roomType);

        return existing == null
            ? Results.NotFound(new { message = "Room type not found." })
            : Results.Ok(RoomTypeResponse.FromRoomType(existing, rooms));
    }
}

app.MapPost("/api/roomtypes", (RoomTypeRequest request) => CreateRoomType(request));
app.MapPost("/api/room-types", (RoomTypeRequest request) => CreateRoomType(request));

IResult CreateRoomType(RoomTypeRequest request)
{
    lock (roomLock)
    {
        var normalizedType = NormalizeRoomType(request.RoomType);

        if (string.IsNullOrWhiteSpace(normalizedType))
            return Results.BadRequest(new { message = "RoomType is required." });

        if (roomTypes.Any(item => IsSameCode(item.RoomType, normalizedType)))
            return Results.BadRequest(new { message = "Room type already exists." });

        if (request.Capacity <= 0)
            return Results.BadRequest(new { message = "Capacity must be greater than zero." });

        if (request.MonthlyFee < 0)
            return Results.BadRequest(new { message = "MonthlyFee cannot be negative." });

        var roomType = new DormRoomType(
            normalizedType,
            request.Capacity,
            request.MonthlyFee,
            request.Description?.Trim() ?? string.Empty,
            request.Amenities?.Trim() ?? DefaultAmenities(normalizedType));

        roomTypes.Add(roomType);
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Created(
            $"/api/room-types/{Uri.EscapeDataString(roomType.RoomType)}",
            RoomTypeResponse.FromRoomType(roomType, rooms));
    }
}

app.MapPut("/api/roomtypes/{roomType}", (
    string roomType,
    RoomTypeRequest request) => UpdateRoomType(roomType, request));
app.MapPut("/api/room-types/{roomType}", (
    string roomType,
    RoomTypeRequest request) => UpdateRoomType(roomType, request));

IResult UpdateRoomType(string roomType, RoomTypeRequest request)
{
    lock (roomLock)
    {
        var existing = FindRoomType(roomTypes, roomType);

        if (existing == null)
            return Results.NotFound(new { message = "Room type not found." });

        if (request.Capacity <= 0)
            return Results.BadRequest(new { message = "Capacity must be greater than zero." });

        if (request.MonthlyFee < 0)
            return Results.BadRequest(new { message = "MonthlyFee cannot be negative." });

        var maxOccupiedBeds = rooms
            .Where(room => IsSameCode(room.RoomType, existing.RoomType))
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

        foreach (var room in rooms.Where(room => IsSameCode(room.RoomType, existing.RoomType)))
        {
            room.Capacity = existing.Capacity;
            room.MonthlyFee = existing.MonthlyFee;
            room.Amenities = existing.Amenities;
            room.RefreshStatus();
        }
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Ok(RoomTypeResponse.FromRoomType(existing, rooms));
    }
}

app.MapDelete("/api/roomtypes/{roomType}", (string roomType) => DeleteRoomType(roomType));
app.MapDelete("/api/room-types/{roomType}", (string roomType) => DeleteRoomType(roomType));

IResult DeleteRoomType(string roomType)
{
    lock (roomLock)
    {
        var existing = FindRoomType(roomTypes, roomType);

        if (existing == null)
            return Results.NotFound(new { message = "Room type not found." });

        if (rooms.Any(room => IsSameCode(room.RoomType, existing.RoomType)))
            return Results.BadRequest(new { message = "Cannot delete a room type that is used by rooms." });

        roomTypes.Remove(existing);
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.NoContent();
    }
}

app.MapGet("/api/rooms", (
    string? buildingName,
    int? floor,
    string? roomType,
    bool? gender,
    string? status) =>
{
    lock (roomLock)
    {
        var filteredRooms = rooms
            .Where(room => string.IsNullOrWhiteSpace(buildingName) ||
                IsSameCode(room.BuildingName, buildingName))
            .Where(room => !floor.HasValue || room.Floor == floor.Value)
            .Where(room => string.IsNullOrWhiteSpace(roomType) ||
                IsSameCode(room.RoomType, roomType))
            .Where(room => !gender.HasValue || room.Gender == gender.Value)
            .Where(room => string.IsNullOrWhiteSpace(status) ||
                IsSameCode(room.Status, NormalizeStatus(status)))
            .OrderBy(room => room.BuildingName)
            .ThenBy(room => room.Floor)
            .ThenBy(room => room.RoomNumber)
            .Select(room => RoomResponse.FromRoom(room, buildings))
            .ToList();

        return Results.Ok(filteredRooms);
    }
});

app.MapGet("/api/rooms/floor-map", (
    string? buildingName,
    int? floor) =>
{
    lock (roomLock)
    {
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
                IsSameCode(room.BuildingName, buildingName))
            .Where(room => string.IsNullOrWhiteSpace(roomType) ||
                IsSameCode(room.RoomType, roomType))
            .Where(room => !gender.HasValue || room.Gender == gender.Value)
            .OrderBy(room => room.MonthlyFee)
            .ThenByDescending(room => room.AvailableBeds)
            .ThenBy(room => room.RoomId)
            .Select(room => RoomResponse.FromRoom(room, buildings))
            .ToList();
    }

    return candidates.Count == 0
        ? Results.NotFound(new { message = "No available room matched the request." })
        : Results.Ok(candidates.First());
});

app.MapGet("/api/rooms/{roomId:long}", (long roomId) =>
{
    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        return room == null
            ? Results.NotFound(new { message = "Room not found." })
            : Results.Ok(RoomResponse.FromRoom(room, buildings));
    }
});

app.MapPost("/api/rooms", (RoomRequest request) =>
{
    lock (roomLock)
    {
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

        var room = new DormRoom(
            request.RoomId,
            CleanOrDefault(request.RoomNumber, request.RoomId.ToString()),
            building!.BuildingName,
            request.Floor,
            roomType!.RoomType,
            request.Gender,
            request.Capacity ?? roomType.Capacity,
            0,
            request.MonthlyFee ?? roomType.MonthlyFee,
            "Available",
            CleanOrDefault(request.Amenities, roomType.Amenities));

        room.RefreshStatus();
        rooms.Add(room);
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Created(
            $"/api/rooms/{room.RoomId}",
            RoomResponse.FromRoom(room, buildings));
    }
});

app.MapPut("/api/rooms/{roomId:long}", (
    long roomId,
    RoomRequest request) =>
{
    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        if (room == null)
            return Results.NotFound(new { message = "Room not found." });

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

        var newCapacity = request.Capacity ?? roomType!.Capacity;

        if (newCapacity < room.OccupiedBeds)
            return Results.BadRequest(new { message = "Capacity cannot be lower than occupied beds." });

        room.RoomNumber = CleanOrDefault(request.RoomNumber, room.RoomNumber);
        room.BuildingName = building!.BuildingName;
        room.Floor = request.Floor;
        room.RoomType = roomType!.RoomType;
        room.Gender = request.Gender;
        room.Capacity = newCapacity;
        room.MonthlyFee = request.MonthlyFee ?? roomType.MonthlyFee;
        room.Amenities = CleanOrDefault(request.Amenities, roomType.Amenities);
        room.RefreshStatus();
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Ok(RoomResponse.FromRoom(room, buildings));
    }
});

app.MapDelete("/api/rooms/{roomId:long}", (long roomId) =>
{
    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        if (room == null)
            return Results.NotFound(new { message = "Room not found." });

        if (room.OccupiedBeds > 0 || room.OccupancyReferences.Count > 0)
            return Results.BadRequest(new { message = "Cannot delete an occupied room." });

        rooms.Remove(room);
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.NoContent();
    }
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

        if (existingReference != null)
        {
            room.LastContractCode = request.ContractCode;
            room.RefreshStatus();
            SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

            return Results.Ok(RoomResponse.FromRoom(room, buildings));
        }

        if (room.AvailableBeds <= 0)
            return Results.BadRequest(new { message = "Room is full." });

        room.OccupancyReferences.Add(new RoomOccupancyReference(
            request.StudentId,
            request.RegistrationId,
            request.ContractCode,
            DateTime.UtcNow));
        room.OccupiedBeds++;

        room.LastContractCode = request.ContractCode;
        room.RefreshStatus();
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Ok(RoomResponse.FromRoom(room, buildings));
    }
});

app.MapPost("/api/rooms/{roomId:long}/release", async (long roomId, HttpRequest request) =>
{
    ReleaseRoomRequest? releaseRequest = null;

    if (request.ContentLength.GetValueOrDefault() > 0)
    {
        try
        {
            releaseRequest = await JsonSerializer.DeserializeAsync<ReleaseRoomRequest>(
                request.Body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return Results.BadRequest(new { message = "Invalid release request body." });
        }
    }

    lock (roomLock)
    {
        var room = rooms.FirstOrDefault(item => item.RoomId == roomId);

        if (room == null)
            return Results.NotFound(new { message = "Room not found." });

        var hasSpecificReleaseTarget =
            releaseRequest?.StudentId > 0 ||
            releaseRequest?.RegistrationId > 0 ||
            !string.IsNullOrWhiteSpace(releaseRequest?.ContractCode);

        var reference = room.OccupancyReferences.FirstOrDefault(item =>
            (releaseRequest?.StudentId > 0 && item.StudentId == releaseRequest.StudentId) ||
            (releaseRequest?.RegistrationId > 0 && item.RegistrationId == releaseRequest.RegistrationId) ||
            (!string.IsNullOrWhiteSpace(releaseRequest?.ContractCode) &&
                item.ContractCode.Equals(releaseRequest.ContractCode, StringComparison.OrdinalIgnoreCase)));

        if (reference != null)
        {
            room.OccupancyReferences.Remove(reference);
        }
        else if (hasSpecificReleaseTarget)
        {
            if (room.OccupiedBeds == 0 && room.OccupancyReferences.Count == 0)
            {
                room.RefreshStatus();
                SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);
                return Results.Ok(RoomResponse.FromRoom(room, buildings));
            }

            return Results.NotFound(new { message = "Occupancy reference not found in this room." });
        }
        else if (room.OccupancyReferences.Count > 0)
        {
            room.OccupancyReferences.RemoveAt(room.OccupancyReferences.Count - 1);
        }

        if (room.OccupiedBeds > 0)
            room.OccupiedBeds--;

        room.RefreshStatus();
        SaveRoomState(roomDataFilePath, buildings, roomTypes, rooms);

        return Results.Ok(RoomResponse.FromRoom(room, buildings));
    }
});

app.Run();

static RoomStoreState LoadRoomState(string filePath)
{
    try
    {
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize<RoomStoreState>(
                json,
                RoomJsonOptions());

            if (state is { Buildings.Count: > 0, RoomTypes.Count: > 0, Rooms.Count: > 0 })
            {
                NormalizeRoomState(state);
                return state;
            }
        }
    }
    catch (JsonException)
    {
        // Fall back to seed data if the persisted file is malformed.
    }
    catch (IOException)
    {
        // Fall back to seed data if the file cannot be read during startup.
    }

    var seededState = new RoomStoreState
    {
        Buildings = SeedBuildings(),
        RoomTypes = SeedRoomTypes(),
        Rooms = SeedRooms()
    };

    SaveRoomState(
        filePath,
        seededState.Buildings,
        seededState.RoomTypes,
        seededState.Rooms);

    return seededState;
}

static void SaveRoomState(
    string filePath,
    List<DormBuilding> buildings,
    List<DormRoomType> roomTypes,
    List<DormRoom> rooms)
{
    var directory = Path.GetDirectoryName(filePath);

    if (!string.IsNullOrWhiteSpace(directory))
        Directory.CreateDirectory(directory);

    var state = new RoomStoreState
    {
        Buildings = buildings,
        RoomTypes = roomTypes,
        Rooms = rooms
    };

    File.WriteAllText(
        filePath,
        JsonSerializer.Serialize(state, RoomJsonOptions()));
}

static JsonSerializerOptions RoomJsonOptions()
{
    return new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };
}

static void NormalizeRoomState(RoomStoreState state)
{
    foreach (var room in state.Rooms)
    {
        room.OccupancyReferences ??= new List<RoomOccupancyReference>();
        room.OccupiedBeds = Math.Min(
            room.Capacity,
            Math.Max(room.OccupiedBeds, room.OccupancyReferences.Count));
        room.RefreshStatus();
    }
}

static List<DormBuilding> SeedBuildings()
{
    return new List<DormBuilding>
    {
        new("A", "Tòa A", 5, "Khu phòng nam/nữ tiêu chuẩn"),
        new("B", "Tòa B", 5, "Khu phòng có tiện nghi mở rộng"),
        new("C", "Tòa C", 6, "Khu phòng nhiều giường")
    };
}

static List<DormRoomType> SeedRoomTypes()
{
    return new List<DormRoomType>
    {
        new("4-bed", 4, 800000, "Phòng 4 giường", "Điều hòa, quạt trần, tủ đồ"),
        new("6-bed", 6, 650000, "Phòng 6 giường", "Quạt trần, tủ đồ"),
        new("8-bed", 8, 550000, "Phòng 8 giường", "Quạt trần, giường tầng, tủ đồ")
    };
}

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
        new(204, "204", "B", 2, "6-bed", true, 6, 0, 700000, "Maintenance", "Quạt trần, bình nóng lạnh, tủ đồ"),
        new(301, "301", "C", 3, "8-bed", true, 8, 0, 550000, "Available", "Quạt trần, tủ đồ"),
        new(302, "302", "C", 3, "8-bed", false, 8, 0, 550000, "Available", "Quạt trần, tủ đồ")
    };
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
        "maintenance" or "repair" or "sua chua" or "sửa chữa" or "dang sua chua" or "đang sửa chữa" => "Maintenance",
        _ => throw new ArgumentException("Status must be Available, Full, or Maintenance.")
    };
}

public sealed class RoomStoreState
{
    public List<DormBuilding> Buildings { get; set; } = new();

    public List<DormRoomType> RoomTypes { get; set; } = new();

    public List<DormRoom> Rooms { get; set; } = new();
}

public sealed class DormBuilding
{
    public DormBuilding()
        : this(string.Empty, string.Empty, 0, string.Empty)
    {
    }

    public DormBuilding(
        string buildingName,
        string displayName,
        int floors,
        string description)
    {
        BuildingName = buildingName;
        DisplayName = displayName;
        Floors = floors;
        Description = description;
    }

    public string BuildingName { get; set; }

    public string DisplayName { get; set; }

    public int Floors { get; set; }

    public string Description { get; set; }
}

public sealed class DormRoomType
{
    public DormRoomType()
        : this(string.Empty, 0, 0, string.Empty, string.Empty)
    {
    }

    public DormRoomType(
        string roomType,
        int capacity,
        decimal monthlyFee,
        string description,
        string amenities)
    {
        RoomType = roomType;
        Capacity = capacity;
        MonthlyFee = monthlyFee;
        Description = description;
        Amenities = amenities;
    }

    public string RoomType { get; set; }

    public int Capacity { get; set; }

    public decimal MonthlyFee { get; set; }

    public string Description { get; set; }

    public string Amenities { get; set; }
}

public sealed class DormRoom
{
    public DormRoom()
        : this(0, string.Empty, string.Empty, 0, string.Empty, true, 0, 0, 0, "Available", string.Empty)
    {
    }

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

public sealed record OccupyRoomRequest(
    long StudentId,
    long RegistrationId,
    string ContractCode);

public sealed record ReleaseRoomRequest(
    long? StudentId,
    long? RegistrationId,
    string? ContractCode);

public sealed record BuildingResponse(
    string BuildingName,
    string DisplayName,
    int Floors,
    string Description,
    int TotalRooms,
    int Capacity,
    int OccupiedBeds,
    int AvailableBeds)
{
    public static BuildingResponse FromBuilding(
        DormBuilding building,
        List<DormRoom> rooms)
    {
        var buildingRooms = rooms
            .Where(room => RoomServiceShared.IsSameCode(room.BuildingName, building.BuildingName))
            .ToList();

        return new BuildingResponse(
            building.BuildingName,
            building.DisplayName,
            building.Floors,
            building.Description,
            buildingRooms.Count,
            buildingRooms.Sum(room => room.Capacity),
            buildingRooms.Sum(room => room.OccupiedBeds),
            buildingRooms.Sum(room => room.AvailableBeds));
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
                .Select(RoomOccupancyResponse.FromReference)
                .ToList());
    }

    private static string GetStatusText(DormRoom room)
    {
        if (room.IsMaintenance)
            return "Đang sửa chữa";

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
