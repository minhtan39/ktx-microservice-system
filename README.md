# KTX Microservice System

Repo tong de trien khai he thong quan ly ky tuc xa theo kien truc microservices.

## Thanh phan

- `ApiGateway`: diem vao duy nhat cho frontend.
- `AuthService`: service dang nhap/xac thuc cua nhom 3.
- `RoomService`: service phong/toa nha cua nhom 1, so huu DB rieng `SmartDormitory_RoomBuildingDB`.
- `ContractStudentService`: service ho so sinh vien, dang ky phong, duyet xep phong, hop dong cua nhom 2.
- `BillingService`: service khoan thu/hoa don.
- `QLKTX`: frontend Vue.
- `sqlserver`: SQL Server container dung cho cac database tach rieng theo service.

## Ghi chu mon Microservices

Bai huong dan mon hoc lay vi du nhom 1 la Product & Inventory. Trong de tai KTX nay, nhom 1 duoc anh xa thanh `RoomService`: quan ly toa nha, loai phong, phong, so giuong con trong va trang thai bao tri. Service nay khong join DB voi cac nhom khac; neu can lien ket sinh vien/hop dong thi chi luu `StudentId`, `RegistrationId`, `ContractCode` dang tham chieu.

Xem them tai lieu nhom 1: `docs/nhom-1-room-building-microservices.md`.

## Chay tren VPS

```bash
cd /opt/ktx-microservice-system
cp .env.example .env
docker-compose up -d --build
```

Kiem tra:

```bash
docker-compose ps
curl http://localhost:8080/health
curl http://localhost:8080/api/rooms
```

Frontend:

```text
http://IP_VPS
```

Gateway:

```text
http://IP_VPS:8080
```
