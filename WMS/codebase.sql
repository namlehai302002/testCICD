USE [HeThongNaNaNa]
GO
/****** Object:  Table [dbo].[__EFMigrationsHistory]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__EFMigrationsHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AiOcrAdjustments]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AiOcrAdjustments](
	[AdjustmentId] [bigint] IDENTITY(1,1) NOT NULL,
	[AiOcrLogId] [bigint] NOT NULL,
	[FieldName] [nvarchar](100) NOT NULL,
	[AiOriginalValue] [nvarchar](500) NULL,
	[UserCorrectedValue] [nvarchar](500) NULL,
	[ItemId] [int] NULL,
	[LineNumber] [int] NULL,
	[Reason] [nvarchar](300) NULL,
	[CorrectedBy] [nvarchar](100) NOT NULL,
	[CorrectedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AiOcrAdjustments] PRIMARY KEY CLUSTERED 
(
	[AdjustmentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AiOcrLogs]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AiOcrLogs](
	[AiOcrLogId] [bigint] IDENTITY(1,1) NOT NULL,
	[ImageUrl] [nvarchar](1000) NOT NULL,
	[FileName] [nvarchar](255) NULL,
	[FileSize] [bigint] NULL,
	[OcrProvider] [nvarchar](50) NOT NULL,
	[ModelVersion] [nvarchar](50) NULL,
	[RawJsonResponse] [nvarchar](max) NULL,
	[ParsedData] [nvarchar](max) NULL,
	[ConfidenceScore] [decimal](5, 4) NULL,
	[DetectedItems] [int] NULL,
	[ProcessingTimeMs] [int] NULL,
	[Status] [tinyint] NOT NULL,
	[ErrorMessage] [nvarchar](1000) NULL,
	[VoucherId] [bigint] NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AiOcrLogs] PRIMARY KEY CLUSTERED 
(
	[AiOcrLogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppRoles]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppRoles](
	[RoleId] [int] IDENTITY(1,1) NOT NULL,
	[RoleName] [nvarchar](50) NOT NULL,
	[Description] [nvarchar](200) NULL,
 CONSTRAINT [PK_AppRoles] PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AppUsers]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AppUsers](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[UserName] [nvarchar](100) NOT NULL,
	[FullName] [nvarchar](200) NOT NULL,
	[Email] [nvarchar](200) NULL,
	[PasswordHash] [nvarchar](500) NOT NULL,
	[Phone] [nvarchar](20) NULL,
	[Department] [nvarchar](100) NULL,
	[WarehouseId] [int] NULL,
	[RoleId] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[LastLoginAt] [datetime2](7) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_AppUsers] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AuditLogs]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AuditLogs](
	[AuditLogId] [bigint] IDENTITY(1,1) NOT NULL,
	[TableName] [nvarchar](128) NOT NULL,
	[RecordId] [nvarchar](50) NOT NULL,
	[ActionType] [nvarchar](10) NOT NULL,
	[ColumnChanged] [nvarchar](128) NULL,
	[OldValue] [nvarchar](max) NULL,
	[NewValue] [nvarchar](max) NULL,
	[ChangedBy] [nvarchar](100) NULL,
	[ChangedAt] [datetime2](7) NOT NULL,
	[IpAddress] [nvarchar](45) NULL,
	[AppModule] [nvarchar](100) NULL,
	[SessionId] [nvarchar](100) NULL,
 CONSTRAINT [PK_AuditLogs] PRIMARY KEY CLUSTERED 
(
	[AuditLogId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[BillOfMaterials]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[BillOfMaterials](
	[BomId] [int] IDENTITY(1,1) NOT NULL,
	[ParentItemId] [int] NOT NULL,
	[ChildItemId] [int] NOT NULL,
	[Quantity] [decimal](18, 6) NOT NULL,
	[UomId] [int] NOT NULL,
	[ScrapPercent] [decimal](5, 2) NOT NULL,
	[BomLevel] [int] NOT NULL,
	[EffectiveFrom] [datetime2](7) NULL,
	[EffectiveTo] [datetime2](7) NULL,
	[IsActive] [bit] NOT NULL,
	[Notes] [nvarchar](500) NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_BillOfMaterials] PRIMARY KEY CLUSTERED 
(
	[BomId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ItemCategories]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItemCategories](
	[CategoryId] [int] IDENTITY(1,1) NOT NULL,
	[CategoryCode] [nvarchar](20) NOT NULL,
	[CategoryName] [nvarchar](100) NOT NULL,
	[ParentCategoryId] [int] NULL,
	[SortOrder] [int] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_ItemCategories] PRIMARY KEY CLUSTERED 
(
	[CategoryId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ItemLocations]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ItemLocations](
	[ItemLocationId] [int] IDENTITY(1,1) NOT NULL,
	[ItemId] [int] NOT NULL,
	[LocationId] [int] NOT NULL,
	[Quantity] [decimal](18, 4) NOT NULL,
	[ExpiryDate] [datetime2](7) NULL,
	[UpdatedAt] [datetime2](7) NOT NULL,
	[LotNumber] [nvarchar](50) NULL,
	[MaxCapacity] [decimal](18, 4) NULL,
	[TotalCapacity] [decimal](18, 4) NULL,
 CONSTRAINT [PK_ItemLocations] PRIMARY KEY CLUSTERED 
(
	[ItemLocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Items]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Items](
	[ItemId] [int] IDENTITY(1,1) NOT NULL,
	[ItemCode] [nvarchar](50) NOT NULL,
	[ItemName] [nvarchar](200) NOT NULL,
	[Barcode] [nvarchar](100) NULL,
	[SkuCode] [nvarchar](50) NULL,
	[CategoryId] [int] NULL,
	[ItemType] [tinyint] NOT NULL,
	[BaseUomId] [int] NOT NULL,
	[CurrentStock] [decimal](18, 4) NOT NULL,
	[MinThreshold] [decimal](18, 4) NOT NULL,
	[MaxThreshold] [decimal](18, 4) NULL,
	[ReorderPoint] [decimal](18, 4) NULL,
	[UnitCost] [decimal](18, 4) NOT NULL,
	[LastCost] [decimal](18, 4) NULL,
	[TotalStockValue] [decimal](18, 4) NOT NULL,
	[ImageUrl] [nvarchar](500) NULL,
	[Description] [nvarchar](500) NULL,
	[Specifications] [nvarchar](max) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[CreatedBy] [nvarchar](100) NULL,
	[DefaultLocationId] [int] NULL,
	[Weight] [decimal](18, 4) NULL,
 CONSTRAINT [PK_Items] PRIMARY KEY CLUSTERED 
(
	[ItemId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Locations]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Locations](
	[LocationId] [int] IDENTITY(1,1) NOT NULL,
	[ZoneId] [int] NOT NULL,
	[LocationCode] [nvarchar](50) NOT NULL,
	[RackCode] [nvarchar](20) NULL,
	[ShelfCode] [nvarchar](20) NULL,
	[BinCode] [nvarchar](20) NULL,
	[CurrentLoad] [decimal](18, 4) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[Barcode] [nvarchar](100) NULL,
 CONSTRAINT [PK_Locations] PRIMARY KEY CLUSTERED 
(
	[LocationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[PackagingUnits]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PackagingUnits](
	[PackagingUnitId] [int] IDENTITY(1,1) NOT NULL,
	[TenDongGoi] [nvarchar](100) NOT NULL,
	[BaseUomId] [int] NOT NULL,
	[GiaTri] [decimal](18, 4) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_PackagingUnits] PRIMARY KEY CLUSTERED 
(
	[PackagingUnitId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Partners]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Partners](
	[PartnerId] [int] IDENTITY(1,1) NOT NULL,
	[PartnerCode] [nvarchar](20) NOT NULL,
	[PartnerName] [nvarchar](200) NOT NULL,
	[PartnerType] [tinyint] NOT NULL,
	[TaxCode] [nvarchar](20) NULL,
	[Phone] [nvarchar](20) NULL,
	[Email] [nvarchar](100) NULL,
	[Address] [nvarchar](300) NULL,
	[ContactPerson] [nvarchar](100) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Partners] PRIMARY KEY CLUSTERED 
(
	[PartnerId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StockAlerts]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StockAlerts](
	[AlertId] [bigint] IDENTITY(1,1) NOT NULL,
	[ItemId] [int] NOT NULL,
	[AlertType] [tinyint] NOT NULL,
	[CurrentStock] [decimal](18, 4) NOT NULL,
	[Threshold] [decimal](18, 4) NOT NULL,
	[IsRead] [bit] NOT NULL,
	[IsResolved] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[ResolvedAt] [datetime2](7) NULL,
 CONSTRAINT [PK_StockAlerts] PRIMARY KEY CLUSTERED 
(
	[AlertId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[StockSnapshots]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[StockSnapshots](
	[SnapshotId] [bigint] IDENTITY(1,1) NOT NULL,
	[SnapshotDate] [date] NOT NULL,
	[ItemId] [int] NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[ClosingStock] [decimal](18, 4) NOT NULL,
	[UnitCost] [decimal](18, 4) NOT NULL,
	[TotalValue] [decimal](18, 4) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_StockSnapshots] PRIMARY KEY CLUSTERED 
(
	[SnapshotId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UnitConversions]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UnitConversions](
	[ConversionId] [int] IDENTITY(1,1) NOT NULL,
	[ItemId] [int] NULL,
	[FromUomId] [int] NOT NULL,
	[ToUomId] [int] NOT NULL,
	[ConversionRate] [decimal](18, 6) NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_UnitConversions] PRIMARY KEY CLUSTERED 
(
	[ConversionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UnitsOfMeasure]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UnitsOfMeasure](
	[UomId] [int] IDENTITY(1,1) NOT NULL,
	[UomCode] [nvarchar](10) NOT NULL,
	[UomName] [nvarchar](50) NOT NULL,
	[IsActive] [bit] NOT NULL,
	[UomGroup] [nvarchar](50) NULL,
 CONSTRAINT [PK_UnitsOfMeasure] PRIMARY KEY CLUSTERED 
(
	[UomId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[VoucherDetails]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[VoucherDetails](
	[VoucherDetailId] [bigint] IDENTITY(1,1) NOT NULL,
	[VoucherId] [bigint] NOT NULL,
	[ItemId] [int] NOT NULL,
	[LocationId] [int] NULL,
	[DestLocationId] [int] NULL,
	[TransactionQty] [decimal](18, 4) NOT NULL,
	[TransactionUomId] [int] NOT NULL,
	[DefectQty] [decimal](18, 4) NOT NULL,
	[ConversionRate] [decimal](18, 6) NOT NULL,
	[BaseQty] [decimal](18, 4) NOT NULL,
	[UnitPrice] [decimal](18, 4) NOT NULL,
	[LineAmount] [decimal](18, 4) NOT NULL,
	[QualityStatus] [tinyint] NOT NULL,
	[ExpiryDate] [date] NULL,
	[Notes] [nvarchar](300) NULL,
	[LineNumber] [int] NOT NULL,
	[DefectBaseQty] [decimal](18, 4) NOT NULL,
	[PackagingUnitId] [int] NULL,
 CONSTRAINT [PK_VoucherDetails] PRIMARY KEY CLUSTERED 
(
	[VoucherDetailId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Vouchers]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Vouchers](
	[VoucherId] [bigint] IDENTITY(1,1) NOT NULL,
	[VoucherCode] [nvarchar](30) NOT NULL,
	[VoucherType] [tinyint] NOT NULL,
	[VoucherDate] [date] NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[DestWarehouseId] [int] NULL,
	[PartnerId] [int] NULL,
	[SourceType] [tinyint] NOT NULL,
	[ReferenceNo] [nvarchar](50) NULL,
	[Description] [nvarchar](500) NULL,
	[TotalAmount] [decimal](18, 4) NOT NULL,
	[TotalLines] [int] NOT NULL,
	[IsPosted] [bit] NOT NULL,
	[IsCancelled] [bit] NOT NULL,
	[CancelledBy] [nvarchar](100) NULL,
	[CancelledAt] [datetime2](7) NULL,
	[CancelReason] [nvarchar](500) NULL,
	[CreatedBy] [nvarchar](100) NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
	[UpdatedAt] [datetime2](7) NULL,
	[IpAddress] [nvarchar](45) NULL,
	[AiOcrLogId] [bigint] NULL,
	[ParentVoucherId] [bigint] NULL,
 CONSTRAINT [PK_Vouchers] PRIMARY KEY CLUSTERED 
(
	[VoucherId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Warehouses]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Warehouses](
	[WarehouseId] [int] IDENTITY(1,1) NOT NULL,
	[WarehouseCode] [nvarchar](20) NOT NULL,
	[WarehouseName] [nvarchar](100) NOT NULL,
	[Address] [nvarchar](300) NULL,
	[ManagerName] [nvarchar](100) NULL,
	[Phone] [nvarchar](20) NULL,
	[IsActive] [bit] NOT NULL,
	[CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_Warehouses] PRIMARY KEY CLUSTERED 
(
	[WarehouseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Zones]    Script Date: 4/13/2026 8:25:21 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Zones](
	[ZoneId] [int] IDENTITY(1,1) NOT NULL,
	[WarehouseId] [int] NOT NULL,
	[ZoneCode] [nvarchar](20) NOT NULL,
	[ZoneName] [nvarchar](100) NOT NULL,
	[ZoneType] [tinyint] NOT NULL,
	[IsActive] [bit] NOT NULL,
 CONSTRAINT [PK_Zones] PRIMARY KEY CLUSTERED 
(
	[ZoneId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260326164214_AddDefectQty', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260327155959_AddItemFixesV2', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260401223127_AddWeightToItem', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260401223232_AddWeightField', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260402035621_SeedAdminRole', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260403075738_AddCapacityFields', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260403082839_AddTotalCapacityToItemLocation', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260403161223_RemoveLotsAndCapacity', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260403165629_AddPackagingUnitIdToVoucherDetails', N'8.0.0')
INSERT [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260408063102_AddUomGroup', N'8.0.0')
GO
SET IDENTITY_INSERT [dbo].[AiOcrLogs] ON 

INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (1, N'/uploads/receipts/9cc49d6ea72c4339aa3af313b397a437_unnamed.jpg', N'unnamed.jpg', 249033, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"VT-XIMANG","ItemName":"Xi măng Hà Tiên Đa Dụng 50kg","Quantity":200.0,"UnitPrice":88000.0},{"ItemCode":"VT-THEP01","ItemName":"Thép cuộn mạ kẽm Ø6 Hòa Phát","Quantity":1500.0,"UnitPrice":16500.0},{"ItemCode":"VT-DAYDIEN","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":50.0,"UnitPrice":485000.0},{"ItemCode":"VT-KEOB","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":30.0,"UnitPrice":65000.0},{"ItemCode":"VT-SONNC","ItemName":"Sơn nội thất Dulux EasyClean 18L","Quantity":15.0,"UnitPrice":1350000.0},{"ItemCode":"VT-NHUAPVC","ItemName":"Ống nhựa Bình Minh PVC Ø21","Quantity":100.0,"UnitPrice":18000.0}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-02T05:19:22.4568787' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (2, N'/uploads/receipts/462cc8a537e74b58a192003cbd1ee5eb_fb4f4fe7cfb44eea17a5.jpg', N'fb4f4fe7cfb44eea17a5.jpg', 240731, N'Gemini_Vision', NULL, NULL, N'[
  {
    "ItemCode": "M41500003950",
    "ItemName": "Air-bubble roll, Clear, Normal (W1000mm x L160m x D10mm) 46g/m2",
    "Quantity": 1.0,
    "UnitPrice": null
  },
  {
    "ItemCode": "M41500003960",
    "ItemName": "Air-bubble roll, Clear, Normal (W870mm x L160m x D10mm) 46g/m2",
    "Quantity": 28.0,
    "UnitPrice": null
  }
]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-08T08:43:48.5407425' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (3, N'/uploads/receipts/a09fc3005b5f4b99850d9a78087fdf97_fb4f4fe7cfb44eea17a5.jpg', N'fb4f4fe7cfb44eea17a5.jpg', 240731, N'Gemini_Vision', NULL, NULL, N'[
  {
    "ItemCode": "M41500003950",
    "ItemName": "Air-bubble roll, Clear, Normal (W1000mm x L160m x D10mm) 40g/m2",
    "Quantity": 5.0,
    "UnitPrice": null
  },
  {
    "ItemCode": "M41500003960",
    "ItemName": "Air bubble roll, Clear, Normal (W870mm x L160m x D10mm) 46g/m2",
    "Quantity": 28.0,
    "UnitPrice": null
  }
]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-08T09:34:36.4118272' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (4, N'/uploads/receipts/38461c3778984e3e9d2e8cfaa77a84c6_Gemini_Generated_Image_ju9roxju9roxju9r.png', N'Gemini_Generated_Image_ju9roxju9roxju9r.png', 5200570, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"BL-NEO-M2000","ItemName":"Bu-lông neo M20x500 (Bộ)","Quantity":100.0,"UnitPrice":15000.0},{"ItemCode":"CAP-CDV-CV2.5","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":200.0,"UnitPrice":80000.0},{"ItemCode":"GACH-MEN-000","ItemName":"Gạch men 60x60","Quantity":500.0,"UnitPrice":120000.0},{"ItemCode":"GACH-VGL-000","ItemName":"Gạch ốp tường 30x60 Ceramic Viglacera","Quantity":300.0,"UnitPrice":135000.0},{"ItemCode":"KEO-APL-FOAM","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":100.0,"UnitPrice":45000.0}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:01:54.3267589' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (5, N'/uploads/receipts/e5394bc5a72e47cab078284ad4649c01_Gemini_Generated_Image_ju9roxju9roxju9r.png', N'Gemini_Generated_Image_ju9roxju9roxju9r.png', 5200570, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"BL-NEO-M2000","ItemName":"Bu-lông neo M20x500 (Bộ)","Quantity":100.0,"UnitPrice":15000.0},{"ItemCode":"CAP-CDV-CV2.5","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":200.0,"UnitPrice":80000.0},{"ItemCode":"GACH-MEN-000","ItemName":"Gạch men 60x60","Quantity":500.0,"UnitPrice":120000.0},{"ItemCode":"GACH-VGL-000","ItemName":"Gạch ốp tường 30x60 Ceramic Viglacera","Quantity":300.0,"UnitPrice":135000.0},{"ItemCode":"KEO-APL-FOAM","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":100.0,"UnitPrice":45000.0}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:03:34.6660486' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (6, N'/uploads/receipts/c2922dc097024c4aa26fe45164a7150c_123.jpg', N'123.jpg', 157395, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"BL-NEO-M2000","ItemName":"Bu-lông neo M20x500 (Bộ)","Quantity":100.0,"UnitPrice":15000.0},{"ItemCode":"CAP-CDV-CV2.5","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":200.0,"UnitPrice":80000.0},{"ItemCode":"GACH-MEN-000","ItemName":"Gạch men 60x60","Quantity":500.0,"UnitPrice":120000.0},{"ItemCode":"GACH-VGL-000","ItemName":"Gạch ốp tường 30x60 Ceramic Viglacera","Quantity":300.0,"UnitPrice":135000.0},{"ItemCode":"KEO-APL-FOAM","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":100.0,"UnitPrice":45000.0}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:06:00.4876750' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (7, N'/uploads/receipts/c2ca6ca69b964e0f894e99ad31e10a24_123.jpg', N'123.jpg', 157395, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"BL-NEO-M2000","ItemName":"Bu-lông neo M20x500 (Bộ)","Quantity":100.0,"UnitPrice":15000.0},{"ItemCode":"CAP-CDV-CV2.5","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":200.0,"UnitPrice":80000.0},{"ItemCode":"GACH-MEN-000","ItemName":"Gạch men 60x60","Quantity":500.0,"UnitPrice":120000.0},{"ItemCode":"GACH-VGL-000","ItemName":"Gạch ốp tường 30x60 Ceramic Viglacera","Quantity":300.0,"UnitPrice":135000.0},{"ItemCode":"KEO-APL-FOAM","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":100.0,"UnitPrice":45000.0}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:09:00.7803458' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (8, N'/uploads/receipts/dcdc9de9940547c892b81e23cc455518_123.jpg', N'123.jpg', 157395, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"BL-NEO-M2000","ItemName":"Bu-lông neo M20x500 (Bộ)","Quantity":100.0,"UnitPrice":15000.0},{"ItemCode":"CAP-CDV-CV2.5","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":200.0,"UnitPrice":80000.0},{"ItemCode":"GACH-MEN-000","ItemName":"Gạch men 60x60","Quantity":500.0,"UnitPrice":120000.0},{"ItemCode":"GACH-VGL-000","ItemName":"Gạch ốp tường 30x60 Ceramic Viglacera","Quantity":300.0,"UnitPrice":135000.0},{"ItemCode":"KEO-APL-FOAM","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":100.0,"UnitPrice":45000.0}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:11:11.5804661' AS DateTime2))
INSERT [dbo].[AiOcrLogs] ([AiOcrLogId], [ImageUrl], [FileName], [FileSize], [OcrProvider], [ModelVersion], [RawJsonResponse], [ParsedData], [ConfidenceScore], [DetectedItems], [ProcessingTimeMs], [Status], [ErrorMessage], [VoucherId], [CreatedBy], [CreatedAt]) VALUES (9, N'/uploads/receipts/18636120e60840df91cbeffd997d1f37_123.jpg', N'123.jpg', 157395, N'Gemini_Vision', NULL, NULL, N'[{"ItemCode":"BL-NEO-M2000","ItemName":"Bu-lông neo M20x500","Quantity":100.0,"UnitPrice":15000.0,"UnitName":"Bộ"},{"ItemCode":"CAP-CDV-CV2.5","ItemName":"Cuộn dây cáp điện Cadivi CV-2.5","Quantity":200.0,"UnitPrice":80000.0,"UnitName":"Cuộn"},{"ItemCode":"GACH-MEN-000","ItemName":"Gạch men 60x60","Quantity":500.0,"UnitPrice":120000.0,"UnitName":"m²"},{"ItemCode":"GACH-VGL-000","ItemName":"Gạch ốp tường 30x60 Ceramic Viglacera","Quantity":300.0,"UnitPrice":135000.0,"UnitName":"m²"},{"ItemCode":"KEO-APL-FOAM","ItemName":"Keo bọt nở chống thấm Apollo Foam","Quantity":100.0,"UnitPrice":45000.0,"UnitName":"Chai"}]', NULL, NULL, NULL, 1, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:27:09.6332132' AS DateTime2))
SET IDENTITY_INSERT [dbo].[AiOcrLogs] OFF
GO
SET IDENTITY_INSERT [dbo].[AppRoles] ON 

INSERT [dbo].[AppRoles] ([RoleId], [RoleName], [Description]) VALUES (1, N'Admin', N'Quản trị viên hệ thống')
INSERT [dbo].[AppRoles] ([RoleId], [RoleName], [Description]) VALUES (2, N'Manager', N'Quản lý kho')
INSERT [dbo].[AppRoles] ([RoleId], [RoleName], [Description]) VALUES (3, N'Staff', N'Nhân viên kho')
INSERT [dbo].[AppRoles] ([RoleId], [RoleName], [Description]) VALUES (4, N'Viewer', N'Xem báo cáo')
SET IDENTITY_INSERT [dbo].[AppRoles] OFF
GO
SET IDENTITY_INSERT [dbo].[AppUsers] ON 

INSERT [dbo].[AppUsers] ([UserId], [UserName], [FullName], [Email], [PasswordHash], [Phone], [Department], [WarehouseId], [RoleId], [IsActive], [LastLoginAt], [CreatedAt]) VALUES (1, N'hieuctttb01413@gmail.com', N'Giám Đốc Hiếu', N'hieuctttb01413@gmail.com', N'$2a$11$O7quh3C9EXPyjSiblb0dEOqU.mdL9hV3WYJtGVESo/.bUJpte/T9m', N'0999999999', N'BOD', NULL, 1, 1, CAST(N'2026-04-13T13:01:04.8733323' AS DateTime2), CAST(N'2026-04-02T04:00:01.7917586' AS DateTime2))
INSERT [dbo].[AppUsers] ([UserId], [UserName], [FullName], [Email], [PasswordHash], [Phone], [Department], [WarehouseId], [RoleId], [IsActive], [LastLoginAt], [CreatedAt]) VALUES (2, N'huyhgtb01372@gmail.com', N'GiaHuyy', N'huyhgtb01372@gmail.com', N'$2a$11$9uwP9oP0njAesw2Fp7UH5..EyfFkTNmmOU.dZfQBhoK0X8hgOBoNC', NULL, NULL, NULL, 1, 1, CAST(N'2026-04-07T06:02:34.1075024' AS DateTime2), CAST(N'2026-04-04T05:19:37.4835156' AS DateTime2))
INSERT [dbo].[AppUsers] ([UserId], [UserName], [FullName], [Email], [PasswordHash], [Phone], [Department], [WarehouseId], [RoleId], [IsActive], [LastLoginAt], [CreatedAt]) VALUES (3, N'hieu', N'hieu', N'hieucaotrantrung@gmail.com', N'$2a$11$ItlMhMhzQPr9WmbvxQAKhunbxWHqOxCexUYju9TyzyAtZ7He6gMP.', NULL, NULL, NULL, 2, 1, CAST(N'2026-04-13T12:31:02.5634129' AS DateTime2), CAST(N'2026-04-13T12:19:33.3246587' AS DateTime2))
INSERT [dbo].[AppUsers] ([UserId], [UserName], [FullName], [Email], [PasswordHash], [Phone], [Department], [WarehouseId], [RoleId], [IsActive], [LastLoginAt], [CreatedAt]) VALUES (4, N'hieu2', N'hieu2', N'hieucaotrantrung2008@gmail.com', N'$2a$11$206JKmjuniuova60hc7liekoB560sZMOdjUeOmJdFqyy1HjOaPV5u', NULL, NULL, NULL, 3, 1, CAST(N'2026-04-13T12:28:52.8622142' AS DateTime2), CAST(N'2026-04-13T12:19:56.4138021' AS DateTime2))
SET IDENTITY_INSERT [dbo].[AppUsers] OFF
GO
SET IDENTITY_INSERT [dbo].[AuditLogs] ON 

INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (1, N'Voucher', N'4', N'INSERT', NULL, NULL, N'{"VoucherId":4,"AiOcrLogId":null,"CancelReason":null,"CancelledAt":null,"CancelledBy":null,"CreatedAt":"2026-04-11T02:49:09.2777526Z","CreatedBy":"hieuctttb01413@gmail.com","Description":null,"DestWarehouseId":null,"IpAddress":"127.0.0.1","IsCancelled":false,"IsPosted":false,"ParentVoucherId":null,"PartnerId":34,"ReferenceNo":null,"SourceType":1,"TotalAmount":0,"TotalLines":0,"UpdatedAt":null,"VoucherCode":"PN-20260411-00001","VoucherDate":"2026-04-11T00:00:00Z","VoucherType":1,"WarehouseId":15}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:09.5087320' AS DateTime2), N'127.0.0.1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (2, N'Voucher', N'4', N'UPDATE', N'TotalLines', N'{"TotalLines":0}', N'{"TotalLines":1}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:09.6223391' AS DateTime2), N'127.0.0.1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (3, N'VoucherDetail', N'4', N'INSERT', NULL, NULL, N'{"VoucherDetailId":4,"BaseQty":200,"ConversionRate":2,"DefectBaseQty":0,"DefectQty":0,"DestLocationId":null,"ExpiryDate":null,"ItemId":135,"LineAmount":0,"LineNumber":1,"LocationId":1138,"Notes":null,"PackagingUnitId":null,"QualityStatus":1,"TransactionQty":100,"TransactionUomId":137,"UnitPrice":0,"VoucherId":4}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:09.6475671' AS DateTime2), N'127.0.0.1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (4, N'Voucher', N'4', N'UPDATE', N'IsPosted, UpdatedAt', N'{"IsPosted":false,"UpdatedAt":null}', N'{"IsPosted":true,"UpdatedAt":"2026-04-11T02:49:18.6737595Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:18.6821848' AS DateTime2), N'127.0.0.1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (5, N'Item', N'135', N'UPDATE', N'CurrentStock, TotalStockValue, UpdatedAt', N'{"CurrentStock":20.0000,"TotalStockValue":500000.0000,"UpdatedAt":"2026-04-11T02:28:15.5201075"}', N'{"CurrentStock":220.0000,"TotalStockValue":5500000.00000000,"UpdatedAt":"2026-04-11T02:49:18.6670633Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:18.6823000' AS DateTime2), N'127.0.0.1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (6, N'ItemLocation', N'57', N'UPDATE', N'Quantity, UpdatedAt', N'{"Quantity":20.0000,"UpdatedAt":"2026-04-09T10:09:29.4687262"}', N'{"Quantity":220.0000,"UpdatedAt":"2026-04-11T02:49:18.6669209Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:18.6823283' AS DateTime2), N'127.0.0.1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (7, N'Voucher', N'5', N'INSERT', NULL, NULL, N'{"VoucherId":5,"AiOcrLogId":null,"CancelReason":null,"CancelledAt":null,"CancelledBy":null,"CreatedAt":"2026-04-11T03:09:21.3183127Z","CreatedBy":"hieuctttb01413@gmail.com","Description":null,"DestWarehouseId":null,"IpAddress":"::1","IsCancelled":false,"IsPosted":false,"ParentVoucherId":null,"PartnerId":34,"ReferenceNo":null,"SourceType":1,"TotalAmount":0,"TotalLines":0,"UpdatedAt":null,"VoucherCode":"PN-20260411-00002","VoucherDate":"2026-04-11T00:00:00Z","VoucherType":1,"WarehouseId":15}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:09:21.5427057' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (8, N'Voucher', N'5', N'UPDATE', N'TotalLines', N'{"TotalLines":0}', N'{"TotalLines":1}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:09:21.6363155' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (9, N'VoucherDetail', N'5', N'INSERT', NULL, NULL, N'{"VoucherDetailId":5,"BaseQty":100,"ConversionRate":10,"DefectBaseQty":0,"DefectQty":0,"DestLocationId":null,"ExpiryDate":null,"ItemId":133,"LineAmount":0,"LineNumber":1,"LocationId":1255,"Notes":null,"PackagingUnitId":null,"QualityStatus":1,"TransactionQty":10,"TransactionUomId":137,"UnitPrice":0,"VoucherId":5}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:09:21.6540881' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (10, N'Voucher', N'5', N'UPDATE', N'IsPosted, UpdatedAt', N'{"IsPosted":false,"UpdatedAt":null}', N'{"IsPosted":true,"UpdatedAt":"2026-04-11T03:10:01.6481806Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:10:01.6596642' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (11, N'Item', N'133', N'UPDATE', N'CurrentStock, TotalStockValue, UpdatedAt', N'{"CurrentStock":95.0000,"TotalStockValue":47500000.0000,"UpdatedAt":"2026-04-11T02:00:39.1428574"}', N'{"CurrentStock":195.0000,"TotalStockValue":97500000.00000000,"UpdatedAt":"2026-04-11T03:10:01.6418974Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:10:01.6598186' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (12, N'ItemLocation', N'58', N'INSERT', NULL, NULL, N'{"ItemLocationId":58,"ExpiryDate":null,"ItemId":133,"LocationId":1255,"LotNumber":null,"MaxCapacity":null,"Quantity":100.0000,"TotalCapacity":null,"UpdatedAt":"2026-04-11T03:10:01.6416687Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:10:01.6797595' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (13, N'Voucher', N'6', N'INSERT', NULL, NULL, N'{"VoucherId":6,"AiOcrLogId":null,"CancelReason":null,"CancelledAt":null,"CancelledBy":null,"CreatedAt":"2026-04-11T03:11:54.3514989Z","CreatedBy":"hieuctttb01413@gmail.com","Description":null,"DestWarehouseId":null,"IpAddress":"::1","IsCancelled":false,"IsPosted":false,"ParentVoucherId":null,"PartnerId":34,"ReferenceNo":null,"SourceType":1,"TotalAmount":0,"TotalLines":0,"UpdatedAt":null,"VoucherCode":"PN-20260411-00003","VoucherDate":"2026-04-11T00:00:00Z","VoucherType":1,"WarehouseId":15}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:11:54.6284870' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (14, N'Voucher', N'6', N'UPDATE', N'TotalLines', N'{"TotalLines":0}', N'{"TotalLines":1}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:11:54.7502587' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (15, N'VoucherDetail', N'6', N'INSERT', NULL, NULL, N'{"VoucherDetailId":6,"BaseQty":50,"ConversionRate":10,"DefectBaseQty":0,"DefectQty":0,"DestLocationId":null,"ExpiryDate":null,"ItemId":140,"LineAmount":0,"LineNumber":1,"LocationId":1254,"Notes":null,"PackagingUnitId":null,"QualityStatus":1,"TransactionQty":5,"TransactionUomId":137,"UnitPrice":0,"VoucherId":6}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:11:54.7744953' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (16, N'Voucher', N'6', N'UPDATE', N'IsPosted, UpdatedAt', N'{"IsPosted":false,"UpdatedAt":null}', N'{"IsPosted":true,"UpdatedAt":"2026-04-11T03:12:31.0098544Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:12:31.0151548' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (17, N'Item', N'140', N'UPDATE', N'CurrentStock, TotalStockValue, UpdatedAt', N'{"CurrentStock":0.0000,"TotalStockValue":0.0000,"UpdatedAt":"2026-04-11T02:00:35.9078568"}', N'{"CurrentStock":50.0000,"TotalStockValue":60000000.00000000,"UpdatedAt":"2026-04-11T03:12:31.0055224Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:12:31.0152205' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (18, N'ItemLocation', N'59', N'INSERT', NULL, NULL, N'{"ItemLocationId":59,"ExpiryDate":null,"ItemId":140,"LocationId":1254,"LotNumber":null,"MaxCapacity":null,"Quantity":50.0000,"TotalCapacity":null,"UpdatedAt":"2026-04-11T03:12:31.0054267Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:12:31.0268012' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (19, N'Item', N'135', N'UPDATE', N'UpdatedAt', N'{"UpdatedAt":"2026-04-11T02:49:18.6670633"}', N'{"UpdatedAt":"2026-04-11T03:51:35.8430211Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:51:35.9261862' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (20, N'AppUser', N'1', N'UPDATE', N'PasswordHash', N'{"PasswordHash":"$2a$11$1ysSDj2N4l305U4hb9AwT.kW23sG3TvQuCxVeuxIvy744b0O1V.6O"}', N'{"PasswordHash":"$2a$11$O7quh3C9EXPyjSiblb0dEOqU.mdL9hV3WYJtGVESo/.bUJpte/T9m"}', N'system', CAST(N'2026-04-13T11:57:23.1319107' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (21, N'AppUser', N'1', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":"2026-04-11T01:58:13.5550769"}', N'{"LastLoginAt":"2026-04-13T11:57:32.2360391Z"}', N'system', CAST(N'2026-04-13T11:57:32.2440428' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (22, N'Item', N'135', N'UPDATE', N'UpdatedAt', N'{"UpdatedAt":"2026-04-11T03:51:35.8430211"}', N'{"UpdatedAt":"2026-04-13T11:57:51.4914567Z"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T11:57:51.5177986' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (23, N'AppUser', N'3', N'INSERT', NULL, NULL, N'{"UserId":3,"CreatedAt":"2026-04-13T12:19:33.3246587Z","Department":null,"Email":"hieucaotrantrung@gmail.com","FullName":"hieu","IsActive":true,"LastLoginAt":null,"PasswordHash":"$2a$11$zIktXQ0IWOkuXQPJPa110.XHND77DBJeLpF0iTcQxLEhMhkZpOND2","Phone":null,"RoleId":2,"UserName":"hieu","WarehouseId":null}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T12:19:33.5177236' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (24, N'AppUser', N'4', N'INSERT', NULL, NULL, N'{"UserId":4,"CreatedAt":"2026-04-13T12:19:56.4138021Z","Department":null,"Email":"hieucaotrantrung2008@gmail.com","FullName":"hieu2","IsActive":true,"LastLoginAt":null,"PasswordHash":"$2a$11$eZDQA1dGFVqM4up.wQIzi.2uFq6Fwh8O4jyhcZ8kfuUdfU3P2soRa","Phone":null,"RoleId":3,"UserName":"hieu2","WarehouseId":null}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T12:19:56.4171181' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (25, N'AppUser', N'1', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":"2026-04-13T11:57:32.2360391"}', N'{"LastLoginAt":"2026-04-13T12:21:33.0471105Z"}', N'system', CAST(N'2026-04-13T12:21:33.0526997' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (26, N'AppUser', N'3', N'UPDATE', N'PasswordHash', N'{"PasswordHash":"$2a$11$zIktXQ0IWOkuXQPJPa110.XHND77DBJeLpF0iTcQxLEhMhkZpOND2"}', N'{"PasswordHash":"$2a$11$4ZhbhEX4gLA4R2TEPBIzYe1uwXX0Q8vL/0M9auuKAh1GbqpxqvCLG"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T12:21:41.2202848' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (27, N'AppUser', N'4', N'UPDATE', N'PasswordHash', N'{"PasswordHash":"$2a$11$eZDQA1dGFVqM4up.wQIzi.2uFq6Fwh8O4jyhcZ8kfuUdfU3P2soRa"}', N'{"PasswordHash":"$2a$11$G1GOjipsXLBii1D/5aN/YuccuBM0hZX7vfnNmjKn/tYFX2c4/sGdC"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T12:21:44.5419672' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (28, N'AppUser', N'1', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":"2026-04-13T12:21:33.0471105"}', N'{"LastLoginAt":"2026-04-13T12:23:12.4055689Z"}', N'system', CAST(N'2026-04-13T12:23:12.4058586' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (29, N'AppUser', N'3', N'UPDATE', N'PasswordHash', N'{"PasswordHash":"$2a$11$4ZhbhEX4gLA4R2TEPBIzYe1uwXX0Q8vL/0M9auuKAh1GbqpxqvCLG"}', N'{"PasswordHash":"$2a$11$ItlMhMhzQPr9WmbvxQAKhunbxWHqOxCexUYju9TyzyAtZ7He6gMP."}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T12:25:59.0379972' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (30, N'AppUser', N'4', N'UPDATE', N'PasswordHash', N'{"PasswordHash":"$2a$11$G1GOjipsXLBii1D/5aN/YuccuBM0hZX7vfnNmjKn/tYFX2c4/sGdC"}', N'{"PasswordHash":"$2a$11$206JKmjuniuova60hc7liekoB560sZMOdjUeOmJdFqyy1HjOaPV5u"}', N'hieuctttb01413@gmail.com', CAST(N'2026-04-13T12:26:08.0095144' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (31, N'AppUser', N'3', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":null}', N'{"LastLoginAt":"2026-04-13T12:28:14.840895Z"}', N'system', CAST(N'2026-04-13T12:28:14.8411810' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (32, N'AppUser', N'4', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":null}', N'{"LastLoginAt":"2026-04-13T12:28:52.8622142Z"}', N'system', CAST(N'2026-04-13T12:28:52.8624651' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (33, N'Voucher', N'7', N'INSERT', NULL, NULL, N'{"VoucherId":7,"AiOcrLogId":null,"CancelReason":null,"CancelledAt":null,"CancelledBy":null,"CreatedAt":"2026-04-13T12:30:06.7449522Z","CreatedBy":"hieu2","Description":null,"DestWarehouseId":null,"IpAddress":"::1","IsCancelled":false,"IsPosted":false,"ParentVoucherId":null,"PartnerId":33,"ReferenceNo":null,"SourceType":1,"TotalAmount":0,"TotalLines":0,"UpdatedAt":null,"VoucherCode":"PN-20260413-00001","VoucherDate":"2026-04-13T00:00:00Z","VoucherType":1,"WarehouseId":15}', N'hieu2', CAST(N'2026-04-13T12:30:06.8053786' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (34, N'Voucher', N'7', N'UPDATE', N'TotalLines', N'{"TotalLines":0}', N'{"TotalLines":1}', N'hieu2', CAST(N'2026-04-13T12:30:06.8632066' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (35, N'VoucherDetail', N'7', N'INSERT', NULL, NULL, N'{"VoucherDetailId":7,"BaseQty":10,"ConversionRate":2,"DefectBaseQty":0,"DefectQty":0,"DestLocationId":null,"ExpiryDate":null,"ItemId":135,"LineAmount":0,"LineNumber":1,"LocationId":1138,"Notes":null,"PackagingUnitId":null,"QualityStatus":1,"TransactionQty":5,"TransactionUomId":137,"UnitPrice":0,"VoucherId":7}', N'hieu2', CAST(N'2026-04-13T12:30:06.8901426' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (36, N'AppUser', N'3', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":"2026-04-13T12:28:14.840895"}', N'{"LastLoginAt":"2026-04-13T12:31:02.5634129Z"}', N'system', CAST(N'2026-04-13T12:31:02.5638760' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (37, N'Voucher', N'7', N'UPDATE', N'IsPosted, UpdatedAt', N'{"IsPosted":false,"UpdatedAt":null}', N'{"IsPosted":true,"UpdatedAt":"2026-04-13T12:31:11.9228529Z"}', N'hieu', CAST(N'2026-04-13T12:31:11.9318768' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (38, N'Item', N'135', N'UPDATE', N'CurrentStock, TotalStockValue, UpdatedAt', N'{"CurrentStock":220.0000,"TotalStockValue":5500000.0000,"UpdatedAt":"2026-04-13T11:57:51.4914567"}', N'{"CurrentStock":230.0000,"TotalStockValue":5750000.00000000,"UpdatedAt":"2026-04-13T12:31:11.9181944Z"}', N'hieu', CAST(N'2026-04-13T12:31:11.9319409' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (39, N'ItemLocation', N'57', N'UPDATE', N'Quantity, UpdatedAt', N'{"Quantity":220.0000,"UpdatedAt":"2026-04-11T02:49:18.6669209"}', N'{"Quantity":230.0000,"UpdatedAt":"2026-04-13T12:31:11.9180296Z"}', N'hieu', CAST(N'2026-04-13T12:31:11.9319781' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
INSERT [dbo].[AuditLogs] ([AuditLogId], [TableName], [RecordId], [ActionType], [ColumnChanged], [OldValue], [NewValue], [ChangedBy], [ChangedAt], [IpAddress], [AppModule], [SessionId]) VALUES (40, N'AppUser', N'1', N'UPDATE', N'LastLoginAt', N'{"LastLoginAt":"2026-04-13T12:23:12.4055689"}', N'{"LastLoginAt":"2026-04-13T13:01:04.8733323Z"}', N'system', CAST(N'2026-04-13T13:01:04.9295583' AS DateTime2), N'::1', N'EF_AutoAudit', NULL)
SET IDENTITY_INSERT [dbo].[AuditLogs] OFF
GO
SET IDENTITY_INSERT [dbo].[ItemCategories] ON 

INSERT [dbo].[ItemCategories] ([CategoryId], [CategoryCode], [CategoryName], [ParentCategoryId], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (27, N'VLXD', N'Vật Liệu Xây Dựng', NULL, 1, 1, CAST(N'2026-04-08T15:20:55.3900000' AS DateTime2), NULL)
INSERT [dbo].[ItemCategories] ([CategoryId], [CategoryCode], [CategoryName], [ParentCategoryId], [SortOrder], [IsActive], [CreatedAt], [UpdatedAt]) VALUES (28, N'VTNT', N'Vật Tư Nội Thất', NULL, 2, 1, CAST(N'2026-04-08T15:20:55.3900000' AS DateTime2), NULL)
SET IDENTITY_INSERT [dbo].[ItemCategories] OFF
GO
SET IDENTITY_INSERT [dbo].[ItemLocations] ON 

INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (53, 131, 1134, CAST(30.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), NULL, CAST(2000.0000 AS Decimal(18, 4)), CAST(2000.0000 AS Decimal(18, 4)))
INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (54, 132, 1135, CAST(8.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-08T15:20:55.3966667' AS DateTime2), NULL, CAST(2000.0000 AS Decimal(18, 4)), CAST(2000.0000 AS Decimal(18, 4)))
INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (55, 133, 1136, CAST(95.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-08T15:20:55.3966667' AS DateTime2), NULL, CAST(2000.0000 AS Decimal(18, 4)), CAST(2000.0000 AS Decimal(18, 4)))
INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (56, 134, 1137, CAST(120.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-08T15:20:55.3966667' AS DateTime2), NULL, CAST(2000.0000 AS Decimal(18, 4)), CAST(2000.0000 AS Decimal(18, 4)))
INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (57, 135, 1138, CAST(230.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-13T12:31:11.9180296' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (58, 133, 1255, CAST(100.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-11T03:10:01.6416687' AS DateTime2), NULL, NULL, NULL)
INSERT [dbo].[ItemLocations] ([ItemLocationId], [ItemId], [LocationId], [Quantity], [ExpiryDate], [UpdatedAt], [LotNumber], [MaxCapacity], [TotalCapacity]) VALUES (59, 140, 1254, CAST(50.0000 AS Decimal(18, 4)), NULL, CAST(N'2026-04-11T03:12:31.0054267' AS DateTime2), NULL, NULL, NULL)
SET IDENTITY_INSERT [dbo].[ItemLocations] OFF
GO
SET IDENTITY_INSERT [dbo].[Items] ON 

INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (131, N'XM-PC40-0000', N'Xi măng PC40', N'XM-PC40-0000', NULL, 27, 1, 136, CAST(30.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), CAST(40.0000 AS Decimal(18, 4)), NULL, CAST(100000.0000 AS Decimal(18, 4)), NULL, CAST(3000000.0000 AS Decimal(18, 4)), NULL, N'Bao 50kg', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:01:04.3320354' AS DateTime2), NULL, 1134, CAST(10.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (132, N'THEPCUON-000', N'Thép cuộn D10', N'THEPCUON-000', NULL, 27, 1, 105, CAST(8.0000 AS Decimal(18, 4)), CAST(2.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), NULL, CAST(2000000.0000 AS Decimal(18, 4)), NULL, CAST(16000000.0000 AS Decimal(18, 4)), NULL, N'Cuộn 200kg', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:00:44.0470259' AS DateTime2), NULL, 1135, CAST(5.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (133, N'SON-NOIHT-00', N'Sơn nội thất 18L', N'SON-NOIHT-00', NULL, 28, 1, 102, CAST(195.0000 AS Decimal(18, 4)), CAST(20.0000 AS Decimal(18, 4)), CAST(100.0000 AS Decimal(18, 4)), NULL, CAST(500000.0000 AS Decimal(18, 4)), NULL, CAST(97500000.0000 AS Decimal(18, 4)), NULL, N'Thùng 20kg', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T03:10:01.6418974' AS DateTime2), NULL, 1255, CAST(2.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (134, N'GACH-MEN-000', N'Gạch men 60×60', N'GACH-MEN-000', NULL, 28, 1, 133, CAST(120.0000 AS Decimal(18, 4)), CAST(50.0000 AS Decimal(18, 4)), CAST(130.0000 AS Decimal(18, 4)), NULL, CAST(150000.0000 AS Decimal(18, 4)), NULL, CAST(18000000.0000 AS Decimal(18, 4)), NULL, N'Hộp 15kg', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:00:10.5181857' AS DateTime2), NULL, 1137, CAST(10.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (135, N'BL-NEO-M2000', N'Bu-lông neo M20×500 (Bộ)', N'BL-NEO-M2000', NULL, 27, 1, 133, CAST(230.0000 AS Decimal(18, 4)), CAST(50.0000 AS Decimal(18, 4)), CAST(200.0000 AS Decimal(18, 4)), NULL, CAST(25000.0000 AS Decimal(18, 4)), NULL, CAST(5750000.0000 AS Decimal(18, 4)), NULL, N'Bộ bu-lông', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-13T12:31:11.9181944' AS DateTime2), NULL, 1138, CAST(0.0500 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (136, N'CAP-CDV-CV25', N'Cuộn dây cáp điện Cadivi CV-2.5', N'CAP-CDV-CV25', NULL, 27, 1, 105, CAST(0.0000 AS Decimal(18, 4)), CAST(5.0000 AS Decimal(18, 4)), CAST(20.0000 AS Decimal(18, 4)), NULL, CAST(1500000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Cáp điện', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T01:59:56.6074704' AS DateTime2), NULL, 1139, CAST(1.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (137, N'GACH-VGL-000', N'Gạch ốp tường 30×60 Ceramic Viglacera', N'GACH-VGL-000', NULL, 28, 1, 133, CAST(0.0000 AS Decimal(18, 4)), CAST(20.0000 AS Decimal(18, 4)), CAST(100.0000 AS Decimal(18, 4)), NULL, CAST(200000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Hộp gạch', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:00:06.9618067' AS DateTime2), NULL, 1140, CAST(5.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (138, N'KEO-APL-FOAM', N'Keo bọt nở chống thấm Apollo Foam', N'KEO-APL-FOAM', NULL, 27, 1, 141, CAST(0.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), CAST(50.0000 AS Decimal(18, 4)), NULL, CAST(85000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Keo bọt', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:03:19.7708574' AS DateTime2), NULL, 1188, CAST(0.5000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (139, N'ONG-PVC-BM21', N'Ống nhựa Bình Minh PVC Ø21', N'ONG-PVC-BM21', NULL, 27, 1, 105, CAST(0.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), CAST(50.0000 AS Decimal(18, 4)), NULL, CAST(250000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Ống nước', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:00:25.3909007' AS DateTime2), NULL, 1189, CAST(2.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (140, N'SON-DLX-E18L', N'Sơn nội thất Dulux EasyClean 18L', N'SON-DLX-E18L', NULL, 28, 1, 102, CAST(50.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), CAST(50.0000 AS Decimal(18, 4)), NULL, CAST(1200000.0000 AS Decimal(18, 4)), NULL, CAST(60000000.0000 AS Decimal(18, 4)), NULL, N'Thùng sơn', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T03:12:31.0055224' AS DateTime2), NULL, 1254, CAST(2.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (141, N'THEPKEM-HP06', N'Thép cuộn mạ kẽm Ø6 Hòa Phát', N'THEPKEM-HP06', NULL, 27, 1, 105, CAST(0.0000 AS Decimal(18, 4)), CAST(2.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), NULL, CAST(3500000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Thép cuộn', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-08T08:24:27.1417038' AS DateTime2), NULL, 1191, CAST(200.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (142, N'XM-HT-DD5000', N'Xi măng Hà Tiên Đa Dụng ', N'XM-HT-DD5000', NULL, 27, 1, 136, CAST(0.0000 AS Decimal(18, 4)), CAST(20.0000 AS Decimal(18, 4)), CAST(100.0000 AS Decimal(18, 4)), NULL, CAST(95000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Bao 50kg', NULL, 1, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-11T02:00:59.0756607' AS DateTime2), NULL, 1192, CAST(10.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (143, N'HOP-TUDIEN00', N'Hộp kim loại đựng tủ điện công nghiệp', NULL, NULL, 27, 1, 108, CAST(0.0000 AS Decimal(18, 4)), CAST(5.0000 AS Decimal(18, 4)), CAST(20.0000 AS Decimal(18, 4)), CAST(5.0000 AS Decimal(18, 4)), CAST(450000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Tủ điện', NULL, 0, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-08T08:23:31.3491617' AS DateTime2), NULL, NULL, CAST(5.0000 AS Decimal(18, 4)))
INSERT [dbo].[Items] ([ItemId], [ItemCode], [ItemName], [Barcode], [SkuCode], [CategoryId], [ItemType], [BaseUomId], [CurrentStock], [MinThreshold], [MaxThreshold], [ReorderPoint], [UnitCost], [LastCost], [TotalStockValue], [ImageUrl], [Description], [Specifications], [IsActive], [CreatedAt], [UpdatedAt], [CreatedBy], [DefaultLocationId], [Weight]) VALUES (144, N'CUA-XF-55000', N'Cửa nhôm Xingfa hệ 55 thành phẩm', NULL, NULL, 28, 1, 110, CAST(0.0000 AS Decimal(18, 4)), CAST(2.0000 AS Decimal(18, 4)), CAST(10.0000 AS Decimal(18, 4)), CAST(3.0000 AS Decimal(18, 4)), CAST(2500000.0000 AS Decimal(18, 4)), NULL, CAST(0.0000 AS Decimal(18, 4)), NULL, N'Cửa nhôm', NULL, 0, CAST(N'2026-04-08T15:20:55.3933333' AS DateTime2), CAST(N'2026-04-08T08:22:50.4884700' AS DateTime2), NULL, NULL, CAST(30.0000 AS Decimal(18, 4)))
SET IDENTITY_INSERT [dbo].[Items] OFF
GO
SET IDENTITY_INSERT [dbo].[Locations] ON 

INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1134, 191, N'RK-A1-01', N'A1', N'1', N'1', CAST(1500.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1135, 191, N'RK-A1-02', N'A1', N'2', N'1', CAST(1600.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1136, 191, N'RK-A1-03', N'A1', N'3', N'1', CAST(1900.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1137, 191, N'RK-A1-04', N'A1', N'4', N'1', CAST(1800.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1138, 191, N'RK-A1-05', N'A1', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1139, 191, N'RK-A1-06', N'A1', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1140, 192, N'RK-A2-01', N'A2', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1141, 192, N'RK-A2-02', N'A2', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1142, 192, N'RK-A2-03', N'A2', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1143, 192, N'RK-A2-04', N'A2', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1144, 192, N'RK-A2-05', N'A2', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1145, 192, N'RK-A2-06', N'A2', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1146, 193, N'RK-A3-01', N'A3', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1147, 193, N'RK-A3-02', N'A3', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1148, 193, N'RK-A3-03', N'A3', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1149, 193, N'RK-A3-04', N'A3', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1150, 193, N'RK-A3-05', N'A3', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1151, 193, N'RK-A3-06', N'A3', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1152, 194, N'RK-A4-01', N'A4', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1153, 194, N'RK-A4-02', N'A4', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1154, 194, N'RK-A4-03', N'A4', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1155, 194, N'RK-A4-04', N'A4', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1156, 194, N'RK-A4-05', N'A4', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1157, 194, N'RK-A4-06', N'A4', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1158, 195, N'RK-A5-01', N'A5', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1159, 195, N'RK-A5-02', N'A5', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1160, 195, N'RK-A5-03', N'A5', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1161, 195, N'RK-A5-04', N'A5', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1162, 195, N'RK-A5-05', N'A5', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1163, 195, N'RK-A5-06', N'A5', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1164, 196, N'RK-A6-01', N'A6', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1165, 196, N'RK-A6-02', N'A6', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1166, 196, N'RK-A6-03', N'A6', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1167, 196, N'RK-A6-04', N'A6', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1168, 196, N'RK-A6-05', N'A6', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1169, 196, N'RK-A6-06', N'A6', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1170, 197, N'RK-A7-01', N'A7', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1171, 197, N'RK-A7-02', N'A7', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1172, 197, N'RK-A7-03', N'A7', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1173, 197, N'RK-A7-04', N'A7', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1174, 197, N'RK-A7-05', N'A7', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1175, 197, N'RK-A7-06', N'A7', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1176, 198, N'RK-A8-01', N'A8', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1177, 198, N'RK-A8-02', N'A8', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1178, 198, N'RK-A8-03', N'A8', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1179, 198, N'RK-A8-04', N'A8', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1180, 198, N'RK-A8-05', N'A8', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1181, 198, N'RK-A8-06', N'A8', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1182, 199, N'RK-A9-01', N'A9', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1183, 199, N'RK-A9-02', N'A9', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1184, 199, N'RK-A9-03', N'A9', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1185, 199, N'RK-A9-04', N'A9', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1186, 199, N'RK-A9-05', N'A9', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1187, 199, N'RK-A9-06', N'A9', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1188, 200, N'RK-A10-01', N'A10', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1189, 200, N'RK-A10-02', N'A10', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1190, 200, N'RK-A10-03', N'A10', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1191, 200, N'RK-A10-04', N'A10', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1192, 200, N'RK-A10-05', N'A10', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1193, 200, N'RK-A10-06', N'A10', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1194, 201, N'RK-A11-01', N'A11', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1195, 201, N'RK-A11-02', N'A11', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1196, 201, N'RK-A11-03', N'A11', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1197, 201, N'RK-A11-04', N'A11', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1198, 201, N'RK-A11-05', N'A11', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1199, 201, N'RK-A11-06', N'A11', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1200, 202, N'RK-A12-01', N'A12', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1201, 202, N'RK-A12-02', N'A12', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1202, 202, N'RK-A12-03', N'A12', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1203, 202, N'RK-A12-04', N'A12', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1204, 202, N'RK-A12-05', N'A12', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1205, 202, N'RK-A12-06', N'A12', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1206, 203, N'RK-A13-01', N'A13', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1207, 203, N'RK-A13-02', N'A13', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1208, 203, N'RK-A13-03', N'A13', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1209, 203, N'RK-A13-04', N'A13', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1210, 203, N'RK-A13-05', N'A13', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1211, 203, N'RK-A13-06', N'A13', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1212, 204, N'RK-A14-01', N'A14', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1213, 204, N'RK-A14-02', N'A14', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1214, 204, N'RK-A14-03', N'A14', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1215, 204, N'RK-A14-04', N'A14', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1216, 204, N'RK-A14-05', N'A14', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1217, 204, N'RK-A14-06', N'A14', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1218, 205, N'RK-A15-01', N'A15', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1219, 205, N'RK-A15-02', N'A15', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1220, 205, N'RK-A15-03', N'A15', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1221, 205, N'RK-A15-04', N'A15', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1222, 205, N'RK-A15-05', N'A15', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1223, 205, N'RK-A15-06', N'A15', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1224, 206, N'RK-A16-01', N'A16', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1225, 206, N'RK-A16-02', N'A16', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1226, 206, N'RK-A16-03', N'A16', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1227, 206, N'RK-A16-04', N'A16', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1228, 206, N'RK-A16-05', N'A16', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1229, 206, N'RK-A16-06', N'A16', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1230, 207, N'RK-A17-01', N'A17', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1231, 207, N'RK-A17-02', N'A17', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1232, 207, N'RK-A17-03', N'A17', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
GO
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1233, 207, N'RK-A17-04', N'A17', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1234, 207, N'RK-A17-05', N'A17', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1235, 207, N'RK-A17-06', N'A17', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1236, 208, N'RK-A18-01', N'A18', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1237, 208, N'RK-A18-02', N'A18', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1238, 208, N'RK-A18-03', N'A18', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1239, 208, N'RK-A18-04', N'A18', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1240, 208, N'RK-A18-05', N'A18', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1241, 208, N'RK-A18-06', N'A18', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1242, 209, N'RK-A19-01', N'A19', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1243, 209, N'RK-A19-02', N'A19', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1244, 209, N'RK-A19-03', N'A19', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1245, 209, N'RK-A19-04', N'A19', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1246, 209, N'RK-A19-05', N'A19', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1247, 209, N'RK-A19-06', N'A19', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1248, 210, N'RK-A20-01', N'A20', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1249, 210, N'RK-A20-02', N'A20', N'2', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1250, 210, N'RK-A20-03', N'A20', N'3', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1251, 210, N'RK-A20-04', N'A20', N'4', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1252, 210, N'RK-A20-05', N'A20', N'5', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1253, 210, N'RK-A20-06', N'A20', N'6', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1254, 211, N'CL-B1-Tank01', N'CL-B1', N'1', N'1', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1255, 211, N'CL-B1-Tank02', N'CL-B1', N'1', N'2', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1256, 211, N'CL-B1-Tank03', N'CL-B1', N'1', N'3', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
INSERT [dbo].[Locations] ([LocationId], [ZoneId], [LocationCode], [RackCode], [ShelfCode], [BinCode], [CurrentLoad], [IsActive], [Barcode]) VALUES (1257, 211, N'CL-B1-Tank04', N'CL-B1', N'1', N'4', CAST(0.0000 AS Decimal(18, 4)), 1, NULL)
SET IDENTITY_INSERT [dbo].[Locations] OFF
GO
SET IDENTITY_INSERT [dbo].[Partners] ON 

INSERT [dbo].[Partners] ([PartnerId], [PartnerCode], [PartnerName], [PartnerType], [TaxCode], [Phone], [Email], [Address], [ContactPerson], [IsActive], [CreatedAt]) VALUES (33, N'NCC-XM-001', N'Công ty CP Xi Măng Vicem Hà Tiên', 1, N'0301112223', N'0909111222', N'info@vicemhatien.com', N'TP.HCM', N'Đại diện KDD', 1, CAST(N'2026-04-08T15:20:55.2800000' AS DateTime2))
INSERT [dbo].[Partners] ([PartnerId], [PartnerCode], [PartnerName], [PartnerType], [TaxCode], [Phone], [Email], [Address], [ContactPerson], [IsActive], [CreatedAt]) VALUES (34, N'NCC-THEP-01', N'Tập đoàn Hòa Phát', 1, N'0301112224', N'0911222333', N'contact@hoaphat.com', N'Hà Nội', N'Đại diện Sale', 1, CAST(N'2026-04-08T15:20:55.2800000' AS DateTime2))
INSERT [dbo].[Partners] ([PartnerId], [PartnerCode], [PartnerName], [PartnerType], [TaxCode], [Phone], [Email], [Address], [ContactPerson], [IsActive], [CreatedAt]) VALUES (35, N'NCC-SON-001', N'Công ty TNHH Sơn Jotun VN', 1, N'0301112225', N'0988333444', N'sales@jotun.vn', N'Bình Dương', N'Đại diện Jotun', 1, CAST(N'2026-04-08T15:20:55.2800000' AS DateTime2))
INSERT [dbo].[Partners] ([PartnerId], [PartnerCode], [PartnerName], [PartnerType], [TaxCode], [Phone], [Email], [Address], [ContactPerson], [IsActive], [CreatedAt]) VALUES (36, N'NCC-GACH-01', N'Tập đoàn Gạch Men Đồng Tâm', 1, N'0301112226', N'0955444555', N'dongtam@dongtam.com', N'Long An', N'Đại diện Đồng Tâm', 1, CAST(N'2026-04-08T15:20:55.2800000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Partners] OFF
GO
SET IDENTITY_INSERT [dbo].[StockSnapshots] ON 

INSERT [dbo].[StockSnapshots] ([SnapshotId], [SnapshotDate], [ItemId], [WarehouseId], [ClosingStock], [UnitCost], [TotalValue], [CreatedAt]) VALUES (1, CAST(N'2026-04-13' AS Date), 131, 15, CAST(30.0000 AS Decimal(18, 4)), CAST(100000.0000 AS Decimal(18, 4)), CAST(3000000.0000 AS Decimal(18, 4)), CAST(N'2026-04-13T13:17:01.6572777' AS DateTime2))
INSERT [dbo].[StockSnapshots] ([SnapshotId], [SnapshotDate], [ItemId], [WarehouseId], [ClosingStock], [UnitCost], [TotalValue], [CreatedAt]) VALUES (2, CAST(N'2026-04-13' AS Date), 132, 15, CAST(8.0000 AS Decimal(18, 4)), CAST(2000000.0000 AS Decimal(18, 4)), CAST(16000000.0000 AS Decimal(18, 4)), CAST(N'2026-04-13T13:17:01.6573187' AS DateTime2))
INSERT [dbo].[StockSnapshots] ([SnapshotId], [SnapshotDate], [ItemId], [WarehouseId], [ClosingStock], [UnitCost], [TotalValue], [CreatedAt]) VALUES (3, CAST(N'2026-04-13' AS Date), 133, 15, CAST(195.0000 AS Decimal(18, 4)), CAST(500000.0000 AS Decimal(18, 4)), CAST(97500000.0000 AS Decimal(18, 4)), CAST(N'2026-04-13T13:17:01.6573191' AS DateTime2))
INSERT [dbo].[StockSnapshots] ([SnapshotId], [SnapshotDate], [ItemId], [WarehouseId], [ClosingStock], [UnitCost], [TotalValue], [CreatedAt]) VALUES (4, CAST(N'2026-04-13' AS Date), 134, 15, CAST(120.0000 AS Decimal(18, 4)), CAST(150000.0000 AS Decimal(18, 4)), CAST(18000000.0000 AS Decimal(18, 4)), CAST(N'2026-04-13T13:17:01.6573195' AS DateTime2))
INSERT [dbo].[StockSnapshots] ([SnapshotId], [SnapshotDate], [ItemId], [WarehouseId], [ClosingStock], [UnitCost], [TotalValue], [CreatedAt]) VALUES (5, CAST(N'2026-04-13' AS Date), 135, 15, CAST(230.0000 AS Decimal(18, 4)), CAST(25000.0000 AS Decimal(18, 4)), CAST(5750000.0000 AS Decimal(18, 4)), CAST(N'2026-04-13T13:17:01.6573198' AS DateTime2))
INSERT [dbo].[StockSnapshots] ([SnapshotId], [SnapshotDate], [ItemId], [WarehouseId], [ClosingStock], [UnitCost], [TotalValue], [CreatedAt]) VALUES (6, CAST(N'2026-04-13' AS Date), 140, 15, CAST(50.0000 AS Decimal(18, 4)), CAST(1200000.0000 AS Decimal(18, 4)), CAST(60000000.0000 AS Decimal(18, 4)), CAST(N'2026-04-13T13:17:01.6573201' AS DateTime2))
SET IDENTITY_INSERT [dbo].[StockSnapshots] OFF
GO
SET IDENTITY_INSERT [dbo].[UnitConversions] ON 

INSERT [dbo].[UnitConversions] ([ConversionId], [ItemId], [FromUomId], [ToUomId], [ConversionRate], [IsActive]) VALUES (18, NULL, 99, 100, CAST(1000.000000 AS Decimal(18, 6)), 1)
INSERT [dbo].[UnitConversions] ([ConversionId], [ItemId], [FromUomId], [ToUomId], [ConversionRate], [IsActive]) VALUES (19, NULL, 101, 99, CAST(1000.000000 AS Decimal(18, 6)), 1)
INSERT [dbo].[UnitConversions] ([ConversionId], [ItemId], [FromUomId], [ToUomId], [ConversionRate], [IsActive]) VALUES (20, NULL, 102, 103, CAST(1000.000000 AS Decimal(18, 6)), 1)
INSERT [dbo].[UnitConversions] ([ConversionId], [ItemId], [FromUomId], [ToUomId], [ConversionRate], [IsActive]) VALUES (21, NULL, 105, 106, CAST(100.000000 AS Decimal(18, 6)), 1)
SET IDENTITY_INSERT [dbo].[UnitConversions] OFF
GO
SET IDENTITY_INSERT [dbo].[UnitsOfMeasure] ON 

INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (99, N'kg', N'Kilôgam', 1, N'Khối lượng')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (100, N'g', N'Gam', 0, N'Khối lượng')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (101, N'tấn', N'Tấn', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (102, N'L', N'Lít', 1, N'Thể tích')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (103, N'mL', N'Mililít', 0, N'Thể tích')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (104, N'm3', N'Mét khối', 0, N'Thể tích')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (105, N'm', N'Mét', 1, N'Độ dài')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (106, N'cm', N'Centimét', 0, N'Độ dài')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (107, N'mm', N'Milimét', 0, N'Độ dài')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (108, N'cái', N'Cái', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (109, N'chiếc', N'Chiếc', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (110, N'bộ', N'Bộ', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (111, N'cuộn', N'Cuộn', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (112, N'thanh', N'Thanh', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (113, N'viên', N'Viên', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (114, N'bao', N'Bao', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (115, N'thùng', N'Thùng', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (116, N'hộp', N'Hộp', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (117, N'túi', N'Túi', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (118, N'can', N'Can', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (119, N'chai', N'Chai', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (120, N'bình', N'Bình', 0, NULL)
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (121, N'km', N'Kilômét', 1, N'Độ dài')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (122, N'in', N'Inch', 1, N'Độ dài')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (123, N'ft', N'Feet', 1, N'Độ dài')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (124, N'mg', N'Miligam', 1, N'Khối lượng')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (125, N't', N'Tấn', 1, N'Khối lượng')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (126, N'm2', N'Mét vuông', 1, N'Diện tích')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (127, N'ha', N'Hécta', 1, N'Diện tích')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (128, N'ms', N'Mili giây', 1, N'Thời gian')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (129, N's', N'Giây', 1, N'Thời gian')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (130, N'min', N'Phút', 1, N'Thời gian')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (131, N'h', N'Giờ', 1, N'Thời gian')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (132, N'd', N'Ngày', 1, N'Thời gian')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (133, N'Pcs', N'Cái / Chiếc', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (134, N'Set', N'Bộ', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (135, N'Pair', N'Cặp', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (136, N'Bag', N'Gói', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (137, N'Box', N'Thùng', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (138, N'Roll', N'Cuộn', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (139, N'Lot', N'Lô', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (140, N'Pallet', N'Kiện', 1, N'Thương mại')
INSERT [dbo].[UnitsOfMeasure] ([UomId], [UomCode], [UomName], [IsActive], [UomGroup]) VALUES (141, N'Bottle', N'Chai / Bình', 1, N'Thương mại')
SET IDENTITY_INSERT [dbo].[UnitsOfMeasure] OFF
GO
SET IDENTITY_INSERT [dbo].[VoucherDetails] ON 

INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (1, 1, 135, 1138, NULL, CAST(10.0000 AS Decimal(18, 4)), 134, CAST(0.0000 AS Decimal(18, 4)), CAST(1.000000 AS Decimal(18, 6)), CAST(20.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (2, 2, 135, 1138, NULL, CAST(5.0000 AS Decimal(18, 4)), 133, CAST(0.0000 AS Decimal(18, 4)), CAST(1.000000 AS Decimal(18, 6)), CAST(5.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (3, 3, 135, 1138, NULL, CAST(5.0000 AS Decimal(18, 4)), 133, CAST(0.0000 AS Decimal(18, 4)), CAST(1.000000 AS Decimal(18, 6)), CAST(5.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (4, 4, 135, 1138, NULL, CAST(100.0000 AS Decimal(18, 4)), 137, CAST(0.0000 AS Decimal(18, 4)), CAST(2.000000 AS Decimal(18, 6)), CAST(200.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (5, 5, 133, 1255, NULL, CAST(10.0000 AS Decimal(18, 4)), 137, CAST(0.0000 AS Decimal(18, 4)), CAST(10.000000 AS Decimal(18, 6)), CAST(100.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (6, 6, 140, 1254, NULL, CAST(5.0000 AS Decimal(18, 4)), 137, CAST(0.0000 AS Decimal(18, 4)), CAST(10.000000 AS Decimal(18, 6)), CAST(50.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
INSERT [dbo].[VoucherDetails] ([VoucherDetailId], [VoucherId], [ItemId], [LocationId], [DestLocationId], [TransactionQty], [TransactionUomId], [DefectQty], [ConversionRate], [BaseQty], [UnitPrice], [LineAmount], [QualityStatus], [ExpiryDate], [Notes], [LineNumber], [DefectBaseQty], [PackagingUnitId]) VALUES (7, 7, 135, 1138, NULL, CAST(5.0000 AS Decimal(18, 4)), 137, CAST(0.0000 AS Decimal(18, 4)), CAST(2.000000 AS Decimal(18, 6)), CAST(10.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), CAST(0.0000 AS Decimal(18, 4)), 1, NULL, NULL, 1, CAST(0.0000 AS Decimal(18, 4)), NULL)
SET IDENTITY_INSERT [dbo].[VoucherDetails] OFF
GO
SET IDENTITY_INSERT [dbo].[Vouchers] ON 

INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (1, N'PN-20260409-00001', 1, CAST(N'2026-04-09' AS Date), 15, NULL, 34, 1, NULL, NULL, CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-09T07:35:33.6347559' AS DateTime2), CAST(N'2026-04-09T07:35:38.0343382' AS DateTime2), N'::1', NULL, NULL)
INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (2, N'PX-20260409-00001', 2, CAST(N'2026-04-09' AS Date), 15, NULL, NULL, 1, N'INVOICE11111', N'cmm', CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-09T07:38:48.5323708' AS DateTime2), NULL, N'::1', NULL, NULL)
INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (3, N'PN-20260409-00002', 1, CAST(N'2026-04-09' AS Date), 15, NULL, 33, 1, N'INVOICE11111', N'ForFun', CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-09T10:09:25.9827844' AS DateTime2), CAST(N'2026-04-09T10:09:29.4689668' AS DateTime2), N'::1', NULL, NULL)
INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (4, N'PN-20260411-00001', 1, CAST(N'2026-04-11' AS Date), 15, NULL, 34, 1, NULL, NULL, CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T02:49:09.2777526' AS DateTime2), CAST(N'2026-04-11T02:49:18.6737595' AS DateTime2), N'127.0.0.1', NULL, NULL)
INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (5, N'PN-20260411-00002', 1, CAST(N'2026-04-11' AS Date), 15, NULL, 34, 1, NULL, NULL, CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:09:21.3183127' AS DateTime2), CAST(N'2026-04-11T03:10:01.6481806' AS DateTime2), N'::1', NULL, NULL)
INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (6, N'PN-20260411-00003', 1, CAST(N'2026-04-11' AS Date), 15, NULL, 34, 1, NULL, NULL, CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieuctttb01413@gmail.com', CAST(N'2026-04-11T03:11:54.3514989' AS DateTime2), CAST(N'2026-04-11T03:12:31.0098544' AS DateTime2), N'::1', NULL, NULL)
INSERT [dbo].[Vouchers] ([VoucherId], [VoucherCode], [VoucherType], [VoucherDate], [WarehouseId], [DestWarehouseId], [PartnerId], [SourceType], [ReferenceNo], [Description], [TotalAmount], [TotalLines], [IsPosted], [IsCancelled], [CancelledBy], [CancelledAt], [CancelReason], [CreatedBy], [CreatedAt], [UpdatedAt], [IpAddress], [AiOcrLogId], [ParentVoucherId]) VALUES (7, N'PN-20260413-00001', 1, CAST(N'2026-04-13' AS Date), 15, NULL, 33, 1, NULL, NULL, CAST(0.0000 AS Decimal(18, 4)), 1, 1, 0, NULL, NULL, NULL, N'hieu2', CAST(N'2026-04-13T12:30:06.7449522' AS DateTime2), CAST(N'2026-04-13T12:31:11.9228529' AS DateTime2), N'::1', NULL, NULL)
SET IDENTITY_INSERT [dbo].[Vouchers] OFF
GO
SET IDENTITY_INSERT [dbo].[Warehouses] ON 

INSERT [dbo].[Warehouses] ([WarehouseId], [WarehouseCode], [WarehouseName], [Address], [ManagerName], [Phone], [IsActive], [CreatedAt]) VALUES (15, N'KHO-CHINH', N'Kho Tổng Hợp Miền Nam', N'Khu Công Nghiệp X, TP.HCM', N'Nguyễn Văn Quản Lý', N'0901234567', 1, CAST(N'2026-04-08T15:20:55.2800000' AS DateTime2))
SET IDENTITY_INSERT [dbo].[Warehouses] OFF
GO
SET IDENTITY_INSERT [dbo].[Zones] ON 

INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (191, 15, N'RK-A1', N'A1', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (192, 15, N'RK-A2', N'A2', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (193, 15, N'RK-A3', N'A3', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (194, 15, N'RK-A4', N'A4', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (195, 15, N'RK-A5', N'A5', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (196, 15, N'RK-A6', N'A6', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (197, 15, N'RK-A7', N'A7', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (198, 15, N'RK-A8', N'A8', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (199, 15, N'RK-A9', N'A9', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (200, 15, N'RK-A10', N'A10', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (201, 15, N'RK-A11', N'A11', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (202, 15, N'RK-A12', N'A12', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (203, 15, N'RK-A13', N'A13', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (204, 15, N'RK-A14', N'A14', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (205, 15, N'RK-A15', N'A15', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (206, 15, N'RK-A16', N'A16', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (207, 15, N'RK-A17', N'A17', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (208, 15, N'RK-A18', N'A18', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (209, 15, N'RK-A19', N'A19', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (210, 15, N'RK-A20', N'A20', 1, 1)
INSERT [dbo].[Zones] ([ZoneId], [WarehouseId], [ZoneCode], [ZoneName], [ZoneType], [IsActive]) VALUES (211, 15, N'CL-B1', N'B1', 3, 1)
SET IDENTITY_INSERT [dbo].[Zones] OFF
GO
ALTER TABLE [dbo].[VoucherDetails] ADD  DEFAULT ((0.0)) FOR [DefectBaseQty]
GO
ALTER TABLE [dbo].[AiOcrAdjustments]  WITH CHECK ADD  CONSTRAINT [FK_AiOcrAdjustments_AiOcrLogs_AiOcrLogId] FOREIGN KEY([AiOcrLogId])
REFERENCES [dbo].[AiOcrLogs] ([AiOcrLogId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AiOcrAdjustments] CHECK CONSTRAINT [FK_AiOcrAdjustments_AiOcrLogs_AiOcrLogId]
GO
ALTER TABLE [dbo].[AiOcrAdjustments]  WITH CHECK ADD  CONSTRAINT [FK_AiOcrAdjustments_Items_ItemId] FOREIGN KEY([ItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[AiOcrAdjustments] CHECK CONSTRAINT [FK_AiOcrAdjustments_Items_ItemId]
GO
ALTER TABLE [dbo].[AiOcrLogs]  WITH CHECK ADD  CONSTRAINT [FK_AiOcrLogs_Vouchers_VoucherId] FOREIGN KEY([VoucherId])
REFERENCES [dbo].[Vouchers] ([VoucherId])
GO
ALTER TABLE [dbo].[AiOcrLogs] CHECK CONSTRAINT [FK_AiOcrLogs_Vouchers_VoucherId]
GO
ALTER TABLE [dbo].[AppUsers]  WITH CHECK ADD  CONSTRAINT [FK_AppUsers_AppRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AppRoles] ([RoleId])
GO
ALTER TABLE [dbo].[AppUsers] CHECK CONSTRAINT [FK_AppUsers_AppRoles_RoleId]
GO
ALTER TABLE [dbo].[AppUsers]  WITH CHECK ADD  CONSTRAINT [FK_AppUsers_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([WarehouseId])
GO
ALTER TABLE [dbo].[AppUsers] CHECK CONSTRAINT [FK_AppUsers_Warehouses_WarehouseId]
GO
ALTER TABLE [dbo].[BillOfMaterials]  WITH CHECK ADD  CONSTRAINT [FK_BillOfMaterials_Items_ChildItemId] FOREIGN KEY([ChildItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[BillOfMaterials] CHECK CONSTRAINT [FK_BillOfMaterials_Items_ChildItemId]
GO
ALTER TABLE [dbo].[BillOfMaterials]  WITH CHECK ADD  CONSTRAINT [FK_BillOfMaterials_Items_ParentItemId] FOREIGN KEY([ParentItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[BillOfMaterials] CHECK CONSTRAINT [FK_BillOfMaterials_Items_ParentItemId]
GO
ALTER TABLE [dbo].[BillOfMaterials]  WITH CHECK ADD  CONSTRAINT [FK_BillOfMaterials_UnitsOfMeasure_UomId] FOREIGN KEY([UomId])
REFERENCES [dbo].[UnitsOfMeasure] ([UomId])
GO
ALTER TABLE [dbo].[BillOfMaterials] CHECK CONSTRAINT [FK_BillOfMaterials_UnitsOfMeasure_UomId]
GO
ALTER TABLE [dbo].[ItemCategories]  WITH CHECK ADD  CONSTRAINT [FK_ItemCategories_ItemCategories_ParentCategoryId] FOREIGN KEY([ParentCategoryId])
REFERENCES [dbo].[ItemCategories] ([CategoryId])
GO
ALTER TABLE [dbo].[ItemCategories] CHECK CONSTRAINT [FK_ItemCategories_ItemCategories_ParentCategoryId]
GO
ALTER TABLE [dbo].[ItemLocations]  WITH CHECK ADD  CONSTRAINT [FK_ItemLocations_Items_ItemId] FOREIGN KEY([ItemId])
REFERENCES [dbo].[Items] ([ItemId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ItemLocations] CHECK CONSTRAINT [FK_ItemLocations_Items_ItemId]
GO
ALTER TABLE [dbo].[ItemLocations]  WITH CHECK ADD  CONSTRAINT [FK_ItemLocations_Locations_LocationId] FOREIGN KEY([LocationId])
REFERENCES [dbo].[Locations] ([LocationId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[ItemLocations] CHECK CONSTRAINT [FK_ItemLocations_Locations_LocationId]
GO
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_ItemCategories_CategoryId] FOREIGN KEY([CategoryId])
REFERENCES [dbo].[ItemCategories] ([CategoryId])
ON DELETE SET NULL
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_ItemCategories_CategoryId]
GO
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_Locations_DefaultLocationId] FOREIGN KEY([DefaultLocationId])
REFERENCES [dbo].[Locations] ([LocationId])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_Locations_DefaultLocationId]
GO
ALTER TABLE [dbo].[Items]  WITH CHECK ADD  CONSTRAINT [FK_Items_UnitsOfMeasure_BaseUomId] FOREIGN KEY([BaseUomId])
REFERENCES [dbo].[UnitsOfMeasure] ([UomId])
GO
ALTER TABLE [dbo].[Items] CHECK CONSTRAINT [FK_Items_UnitsOfMeasure_BaseUomId]
GO
ALTER TABLE [dbo].[Locations]  WITH CHECK ADD  CONSTRAINT [FK_Locations_Zones_ZoneId] FOREIGN KEY([ZoneId])
REFERENCES [dbo].[Zones] ([ZoneId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Locations] CHECK CONSTRAINT [FK_Locations_Zones_ZoneId]
GO
ALTER TABLE [dbo].[PackagingUnits]  WITH CHECK ADD  CONSTRAINT [FK_PackagingUnits_UnitsOfMeasure_BaseUomId] FOREIGN KEY([BaseUomId])
REFERENCES [dbo].[UnitsOfMeasure] ([UomId])
GO
ALTER TABLE [dbo].[PackagingUnits] CHECK CONSTRAINT [FK_PackagingUnits_UnitsOfMeasure_BaseUomId]
GO
ALTER TABLE [dbo].[StockAlerts]  WITH CHECK ADD  CONSTRAINT [FK_StockAlerts_Items_ItemId] FOREIGN KEY([ItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[StockAlerts] CHECK CONSTRAINT [FK_StockAlerts_Items_ItemId]
GO
ALTER TABLE [dbo].[StockSnapshots]  WITH CHECK ADD  CONSTRAINT [FK_StockSnapshots_Items_ItemId] FOREIGN KEY([ItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[StockSnapshots] CHECK CONSTRAINT [FK_StockSnapshots_Items_ItemId]
GO
ALTER TABLE [dbo].[StockSnapshots]  WITH CHECK ADD  CONSTRAINT [FK_StockSnapshots_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([WarehouseId])
GO
ALTER TABLE [dbo].[StockSnapshots] CHECK CONSTRAINT [FK_StockSnapshots_Warehouses_WarehouseId]
GO
ALTER TABLE [dbo].[UnitConversions]  WITH CHECK ADD  CONSTRAINT [FK_UnitConversions_Items_ItemId] FOREIGN KEY([ItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[UnitConversions] CHECK CONSTRAINT [FK_UnitConversions_Items_ItemId]
GO
ALTER TABLE [dbo].[UnitConversions]  WITH CHECK ADD  CONSTRAINT [FK_UnitConversions_UnitsOfMeasure_FromUomId] FOREIGN KEY([FromUomId])
REFERENCES [dbo].[UnitsOfMeasure] ([UomId])
GO
ALTER TABLE [dbo].[UnitConversions] CHECK CONSTRAINT [FK_UnitConversions_UnitsOfMeasure_FromUomId]
GO
ALTER TABLE [dbo].[UnitConversions]  WITH CHECK ADD  CONSTRAINT [FK_UnitConversions_UnitsOfMeasure_ToUomId] FOREIGN KEY([ToUomId])
REFERENCES [dbo].[UnitsOfMeasure] ([UomId])
GO
ALTER TABLE [dbo].[UnitConversions] CHECK CONSTRAINT [FK_UnitConversions_UnitsOfMeasure_ToUomId]
GO
ALTER TABLE [dbo].[VoucherDetails]  WITH CHECK ADD  CONSTRAINT [FK_VoucherDetails_Items_ItemId] FOREIGN KEY([ItemId])
REFERENCES [dbo].[Items] ([ItemId])
GO
ALTER TABLE [dbo].[VoucherDetails] CHECK CONSTRAINT [FK_VoucherDetails_Items_ItemId]
GO
ALTER TABLE [dbo].[VoucherDetails]  WITH CHECK ADD  CONSTRAINT [FK_VoucherDetails_Locations_DestLocationId] FOREIGN KEY([DestLocationId])
REFERENCES [dbo].[Locations] ([LocationId])
GO
ALTER TABLE [dbo].[VoucherDetails] CHECK CONSTRAINT [FK_VoucherDetails_Locations_DestLocationId]
GO
ALTER TABLE [dbo].[VoucherDetails]  WITH CHECK ADD  CONSTRAINT [FK_VoucherDetails_Locations_LocationId] FOREIGN KEY([LocationId])
REFERENCES [dbo].[Locations] ([LocationId])
GO
ALTER TABLE [dbo].[VoucherDetails] CHECK CONSTRAINT [FK_VoucherDetails_Locations_LocationId]
GO
ALTER TABLE [dbo].[VoucherDetails]  WITH CHECK ADD  CONSTRAINT [FK_VoucherDetails_PackagingUnits_PackagingUnitId] FOREIGN KEY([PackagingUnitId])
REFERENCES [dbo].[PackagingUnits] ([PackagingUnitId])
GO
ALTER TABLE [dbo].[VoucherDetails] CHECK CONSTRAINT [FK_VoucherDetails_PackagingUnits_PackagingUnitId]
GO
ALTER TABLE [dbo].[VoucherDetails]  WITH CHECK ADD  CONSTRAINT [FK_VoucherDetails_UnitsOfMeasure_TransactionUomId] FOREIGN KEY([TransactionUomId])
REFERENCES [dbo].[UnitsOfMeasure] ([UomId])
GO
ALTER TABLE [dbo].[VoucherDetails] CHECK CONSTRAINT [FK_VoucherDetails_UnitsOfMeasure_TransactionUomId]
GO
ALTER TABLE [dbo].[VoucherDetails]  WITH CHECK ADD  CONSTRAINT [FK_VoucherDetails_Vouchers_VoucherId] FOREIGN KEY([VoucherId])
REFERENCES [dbo].[Vouchers] ([VoucherId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[VoucherDetails] CHECK CONSTRAINT [FK_VoucherDetails_Vouchers_VoucherId]
GO
ALTER TABLE [dbo].[Vouchers]  WITH CHECK ADD  CONSTRAINT [FK_Vouchers_Partners_PartnerId] FOREIGN KEY([PartnerId])
REFERENCES [dbo].[Partners] ([PartnerId])
GO
ALTER TABLE [dbo].[Vouchers] CHECK CONSTRAINT [FK_Vouchers_Partners_PartnerId]
GO
ALTER TABLE [dbo].[Vouchers]  WITH CHECK ADD  CONSTRAINT [FK_Vouchers_Warehouses_DestWarehouseId] FOREIGN KEY([DestWarehouseId])
REFERENCES [dbo].[Warehouses] ([WarehouseId])
GO
ALTER TABLE [dbo].[Vouchers] CHECK CONSTRAINT [FK_Vouchers_Warehouses_DestWarehouseId]
GO
ALTER TABLE [dbo].[Vouchers]  WITH CHECK ADD  CONSTRAINT [FK_Vouchers_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([WarehouseId])
GO
ALTER TABLE [dbo].[Vouchers] CHECK CONSTRAINT [FK_Vouchers_Warehouses_WarehouseId]
GO
ALTER TABLE [dbo].[Zones]  WITH CHECK ADD  CONSTRAINT [FK_Zones_Warehouses_WarehouseId] FOREIGN KEY([WarehouseId])
REFERENCES [dbo].[Warehouses] ([WarehouseId])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[Zones] CHECK CONSTRAINT [FK_Zones_Warehouses_WarehouseId]
GO
