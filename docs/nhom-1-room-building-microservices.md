# Nhom 1 - Room & Building Service

Tai lieu nay anh xa yeu cau mon Microservices vao he thong KTX dang lam. Trong file huong dan, nhom 1 duoc mo ta la `Product & Inventory`. Voi de tai KTX, vai tro tuong duong la `RoomService`: quan ly nguon luc phong o, toa nha, loai phong, so giuong con trong va trang thai van hanh.

## 1. Ranh gioi service

`RoomService` so huu nghiep vu va du lieu sau:

- Toa nha: ma toa, ten hien thi, so tang, mo ta.
- Loai phong: ma loai phong, suc chua, don gia thang, tien nghi.
- Phong: ma phong, so phong, toa, tang, gioi tinh, suc chua, so giuong da xep, gia, trang thai.
- Tham chieu cu tru: `StudentId`, `RegistrationId`, `ContractCode` de biet phong dang co ai o.

`RoomService` khong quan ly ho so sinh vien, hop dong, hoa don hay tai khoan dang nhap. Cac phan do thuoc service khac.

## 2. Database rieng

Theo dung yeu cau mon hoc, moi nhom co database rieng. Nhom 1 dung:

```text
SmartDormitory_RoomBuildingDB
```

Bang chinh:

- `Buildings`
- `RoomTypes`
- `Rooms`
- `RoomOccupancyReferences`

`RoomOccupancyReferences` chi luu ID/mã tham chieu tu service khac. Khong tao khoa ngoai chao sang database sinh vien/hop dong. Cach nay dam bao khong join cheo DB va service co the deploy doc lap.

## 3. Endpoint chinh

Tat ca request tu frontend di qua `ApiGateway`, sau do gateway route ve `RoomService`.

```text
GET    /api/buildings
POST   /api/buildings
PUT    /api/buildings/{buildingName}
DELETE /api/buildings/{buildingName}

GET    /api/room-types
POST   /api/room-types
PUT    /api/room-types/{roomType}
DELETE /api/room-types/{roomType}

GET    /api/rooms
GET    /api/rooms/floor-map
GET    /api/rooms/available
POST   /api/rooms
PUT    /api/rooms/{roomId}
PATCH  /api/rooms/{roomId}/status
POST   /api/rooms/{roomId}/occupy
POST   /api/rooms/{roomId}/release
```

## 4. Luong phoi hop voi nhom khac

### Nhom 2 duyet xep phong

1. Nhom 2 nhan yeu cau dang ky phong cua sinh vien.
2. Nhom 2 goi Gateway toi `GET /api/rooms/available` de lay phong con giuong, dung gioi tinh, dung loai phong neu co.
3. Neu khong co phong phu hop, nhom 2 tra ve loi nghiep vu.
4. Neu co phong, nhom 2 tao hop dong/duyet dang ky trong DB rieng cua minh.
5. Sau khi thanh cong, nhom 2 goi `POST /api/rooms/{roomId}/occupy` de RoomService cap nhat so giuong da xep.

### Bao tri/sua chua phong

1. Service bao tri tao yeu cau sua chua.
2. Khi phong dang sua, goi `PATCH /api/rooms/{roomId}/status` voi `Maintenance`.
3. Khi hoan thanh, goi lai endpoint nay voi `Auto` hoac `Completed`.
4. RoomService tu tinh lai trang thai: phong trong, con giuong, day phong.

## 5. Quy tac logic

- Khong xoa toa neu con phong.
- Khong xoa loai phong neu dang duoc phong su dung.
- Khong xoa phong neu phong dang co nguoi o.
- Khong giam suc chua loai phong/phong thap hon so giuong da xep.
- Khong xep sinh vien vao phong dang bao tri.
- Khong xep mot sinh vien vao hai phong cung luc.
- Khi phong het giuong, trang thai tu dong la `Full`.
- Khi phong bao tri xong, trang thai duoc tinh lai theo so giuong con trong.

## 6. Chay trong Visual Studio

Mo solution:

```text
SmartDormitorySystem.sln
```

Neu chay rieng `RoomService` bang Visual Studio, service dung LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=SmartDormitory_RoomBuildingDB
```

Trong moi truong Development, neu LocalDB tren may chua tao duoc instance, service tu chuyen sang `InMemoryLocalDemo` de giao dien van chay duoc khi demo. Khi deploy Docker/VPS, fallback nay bi tat va service bat buoc dung SQL Server dung yeu cau mon hoc.

Neu chay toan he thong bang Docker, `docker-compose.yml` se dung SQL Server container va tu tao database:

```text
SmartDormitory_RoomBuildingDB
```

## 7. Diem bam theo file huong dan mon hoc

- `.NET Web API`: `RoomService`.
- `VueJS 3 Frontend`: `QLKTX`.
- `SQL Server rieng moi nhom`: `SmartDormitory_RoomBuildingDB`.
- `API Gateway`: `ApiGateway`.
- `Dockerfile rieng`: `RoomService/Dockerfile`.
- `docker-compose.yml` chung: cau hinh `room-service`, `api-gateway`, `sqlserver`.
- `Khong join DB cheo`: chi luu ID tham chieu trong `RoomOccupancyReferences`.
