# Vehicle-Dealer-Management-Gr8

> EVM Dealer Portal - Hệ thống quản lý đại lý xe điện

## 📚 Tài liệu

- **[ROADMAP.md](ROADMAP.md)** - 📖 **Tài liệu tổng hợp duy nhất** (Requirements + Database + Roadmap + Progress + Guidelines)
- **[requirements.md](requirements.md)** - Yêu cầu chức năng chi tiết (tham khảo)
- **[database.md](database.md)** - Thiết kế database chi tiết (tham khảo)

## 🚀 Quick Start

### 1. Migration & Database

```bash
cd "Vehicle Dealer Management"

# Tạo migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### 2. Chạy ứng dụng

```bash
dotnet run
```

Seed data sẽ tự động chạy trong Development mode.

## 🔑 Test Accounts

Sau khi seed data, dùng các tài khoản sau để đăng nhập:

| Email | Password | Role | Mô tả |
|-------|----------|------|-------|
| `customer@test.com` | `123456` | CUSTOMER | Khách hàng - Xem xe, đặt hàng, lái thử |
| `dealerstaff@test.com` | `123456` | DEALER_STAFF | NV Đại lý - Tạo báo giá, quản lý bán hàng |
| `dealermanager@test.com` | `123456` | DEALER_MANAGER | QL Đại lý - Báo cáo doanh số, công nợ |
| `evmstaff@test.com` | `123456` | EVM_STAFF | NV Hãng - Quản lý xe, giá, tồn kho, đại lý |
| `admin@test.com` | `123456` | EVM_ADMIN | Admin - Dashboard, báo cáo phân tích |


