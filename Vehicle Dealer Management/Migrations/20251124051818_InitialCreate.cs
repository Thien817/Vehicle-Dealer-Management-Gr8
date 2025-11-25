using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vehicle_Dealer_Management.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdCardNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Dealers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TaxCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dealers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsOperational = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VariantName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SpecJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    DealerId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Promotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DealerId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    RuleJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Promotions_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Promotions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    SalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SaleDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sales_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    OwnerId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.CheckConstraint("CK_Stock_Qty", "[Qty] >= 0");
                    table.ForeignKey(
                        name: "FK_Stocks_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdentityNo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerProfiles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DealerOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DealerOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DealerOrders_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DealerOrders_Users_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DealerOrders_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LinkUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RelatedEntityId = table.Column<int>(type: "int", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    DealerId = table.Column<int>(type: "int", nullable: true),
                    Msrp = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WholesalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OriginalMsrp = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OriginalWholesalePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiscountRuleJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PromotionId = table.Column<int>(type: "int", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricePolicies_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PricePolicies_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PricePolicies_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalesDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PromotionId = table.Column<int>(type: "int", nullable: true),
                    SignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesDocuments_CustomerProfiles_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDocuments_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesDocuments_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalTable: "Promotions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesDocuments_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TestDrives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    VehicleId = table.Column<int>(type: "int", nullable: true),
                    ScheduleTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsSlot = table.Column<bool>(type: "bit", nullable: false),
                    SlotStartTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    SlotEndTime = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    MaxSlots = table.Column<int>(type: "int", nullable: true),
                    ParentSlotId = table.Column<int>(type: "int", nullable: true),
                    AvailableVehicleIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestDrives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TestDrives_CustomerProfiles_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestDrives_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestDrives_TestDrives_ParentSlotId",
                        column: x => x.ParentSlotId,
                        principalTable: "TestDrives",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TestDrives_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Deliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesDocumentId = table.Column<int>(type: "int", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeliveredDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    HandoverNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CustomerConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    CustomerConfirmedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deliveries_SalesDocuments_SalesDocumentId",
                        column: x => x.SalesDocumentId,
                        principalTable: "SalesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplyContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReplyByUserId = table.Column<int>(type: "int", nullable: true),
                    ReplyAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedbacks_CustomerProfiles_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedbacks_SalesDocuments_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesDocuments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Feedbacks_Users_ReplyByUserId",
                        column: x => x.ReplyByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesDocumentId = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MetaJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payment_Amount", "[Amount] > 0");
                    table.ForeignKey(
                        name: "FK_Payments_SalesDocuments_SalesDocumentId",
                        column: x => x.SalesDocumentId,
                        principalTable: "SalesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuoteId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    DealerId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    CustomerSignatureUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CustomerSignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DealerVerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesContracts_CustomerProfiles_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "CustomerProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_Dealers_DealerId",
                        column: x => x.DealerId,
                        principalTable: "Dealers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_SalesDocuments_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SalesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_SalesDocuments_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "SalesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalesContracts_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesDocumentLines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalesDocumentId = table.Column<int>(type: "int", nullable: false),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    ColorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesDocumentLines", x => x.Id);
                    table.CheckConstraint("CK_SalesDocumentLine_Discount", "[DiscountValue] <= [UnitPrice] * [Qty]");
                    table.CheckConstraint("CK_SalesDocumentLine_Qty", "[Qty] > 0");
                    table.ForeignKey(
                        name: "FK_SalesDocumentLines_SalesDocuments_SalesDocumentId",
                        column: x => x.SalesDocumentId,
                        principalTable: "SalesDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesDocumentLines_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_UserId",
                table: "ActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_Email",
                table: "CustomerProfiles",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_Phone",
                table: "CustomerProfiles",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerProfiles_UserId",
                table: "CustomerProfiles",
                column: "UserId",
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DealerOrders_ApprovedBy",
                table: "DealerOrders",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DealerOrders_CreatedBy",
                table: "DealerOrders",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_DealerOrders_DealerId_Status_CreatedAt",
                table: "DealerOrders",
                columns: new[] { "DealerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dealers_Code",
                table: "Dealers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SalesDocumentId",
                table: "Deliveries",
                column: "SalesDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SalesDocumentId_ScheduledDate",
                table: "Deliveries",
                columns: new[] { "SalesDocumentId", "ScheduledDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CustomerId_CreatedAt",
                table: "Feedbacks",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DealerId_Status",
                table: "Feedbacks",
                columns: new[] { "DealerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_DealerId_Type_Rating",
                table: "Feedbacks",
                columns: new[] { "DealerId", "Type", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_OrderId",
                table: "Feedbacks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ReplyByUserId",
                table: "Feedbacks",
                column: "ReplyByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Type_OrderId",
                table: "Feedbacks",
                columns: new[] { "Type", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId",
                table: "Notifications",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_SalesDocumentId_PaidAt",
                table: "Payments",
                columns: new[] { "SalesDocumentId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PricePolicies_DealerId",
                table: "PricePolicies",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_PricePolicies_PromotionId",
                table: "PricePolicies",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_PricePolicies_VehicleId_DealerId_ValidFrom_ValidTo",
                table: "PricePolicies",
                columns: new[] { "VehicleId", "DealerId", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_DealerId",
                table: "Promotions",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_Scope_DealerId_VehicleId_ValidFrom_ValidTo",
                table: "Promotions",
                columns: new[] { "Scope", "DealerId", "VehicleId", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_VehicleId",
                table: "Promotions",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Code",
                table: "Roles",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_VehicleId",
                table: "Sales",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_CreatedBy",
                table: "SalesContracts",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_CustomerId_Status",
                table: "SalesContracts",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_DealerId_Status",
                table: "SalesContracts",
                columns: new[] { "DealerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_OrderId",
                table: "SalesContracts",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesContracts_QuoteId",
                table: "SalesContracts",
                column: "QuoteId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocumentLines_SalesDocumentId_VehicleId",
                table: "SalesDocumentLines",
                columns: new[] { "SalesDocumentId", "VehicleId" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocumentLines_VehicleId",
                table: "SalesDocumentLines",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocuments_CreatedBy",
                table: "SalesDocuments",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocuments_CustomerId_CreatedAt",
                table: "SalesDocuments",
                columns: new[] { "CustomerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocuments_DealerId",
                table: "SalesDocuments",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocuments_PromotionId",
                table: "SalesDocuments",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesDocuments_Type_DealerId_Status_CreatedAt",
                table: "SalesDocuments",
                columns: new[] { "Type", "DealerId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_OwnerType_OwnerId_VehicleId_ColorCode",
                table: "Stocks",
                columns: new[] { "OwnerType", "OwnerId", "VehicleId", "ColorCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_VehicleId",
                table: "Stocks",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_TestDrives_CustomerId_Status",
                table: "TestDrives",
                columns: new[] { "CustomerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TestDrives_DealerId_ScheduleTime",
                table: "TestDrives",
                columns: new[] { "DealerId", "ScheduleTime" });

            migrationBuilder.CreateIndex(
                name: "IX_TestDrives_ParentSlotId",
                table: "TestDrives",
                column: "ParentSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_TestDrives_VehicleId",
                table: "TestDrives",
                column: "VehicleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DealerId",
                table: "Users",
                column: "DealerId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_ModelName_VariantName",
                table: "Vehicles",
                columns: new[] { "ModelName", "VariantName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_Status",
                table: "Vehicles",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "DealerOrders");

            migrationBuilder.DropTable(
                name: "Deliveries");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "PricePolicies");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "SalesContracts");

            migrationBuilder.DropTable(
                name: "SalesDocumentLines");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "TestDrives");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "SalesDocuments");

            migrationBuilder.DropTable(
                name: "CustomerProfiles");

            migrationBuilder.DropTable(
                name: "Promotions");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Vehicles");

            migrationBuilder.DropTable(
                name: "Dealers");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
