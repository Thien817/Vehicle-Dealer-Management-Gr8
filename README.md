# Vehicle-Dealer-Management-Gr8

> EVM Dealer Portal - Hệ thống quản lý đại lý xe điện

## 📚 Tài liệu

- **[Roadmap.md](Roadmap.md)** - Lộ trình phát triển (5-6 tuần)
- **[requirements.md](requirements.md)** - Yêu cầu chức năng
- **[database.md](database.md)** - Thiết kế database (15 tables)

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

| Email | Password | Role |
|-------|----------|------|
| `customer@test.com` | `123456` | CUSTOMER |
| `dealerstaff@test.com` | `123456` | DEALER_STAFF |
| `dealermanager@test.com` | `123456` | DEALER_MANAGER |
| `evmstaff@test.com` | `123456` | EVM_STAFF |
| `admin@test.com` | `123456` | EVM_ADMIN |

## 📊 Seed Data

- ✅ 5 Roles + 5 Users
- ✅ 2 Dealers (Hà Nội, TP.HCM)
- ✅ 3 Vehicles (Model S, Model 3, Model X)
- ✅ Price Policies, Stocks, Customer Profiles, Promotions

## 📖 Tham khảo

- **Roadmap:** Xem `Roadmap.md` để biết implementation plan
- **Database:** Xem `database.md` cho schema details
- **Requirements:** Xem `requirements.md` cho features & roles
