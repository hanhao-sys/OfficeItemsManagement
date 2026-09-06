-- ============================================================
-- 办公物品管理系统 — 建表脚本 (含完整性命名子句)
-- ============================================================
USE OfficeItemsDB;

-- 物品类别字典表
CREATE TABLE Categories (
    Id      INT AUTO_INCREMENT,
    Name    VARCHAR(50) NOT NULL,
    CONSTRAINT PK_Categories PRIMARY KEY (Id),
    CONSTRAINT UQ_Categories_Name UNIQUE (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 产地字典表
CREATE TABLE Origins (
    Id      INT AUTO_INCREMENT,
    Name    VARCHAR(100) NOT NULL,
    CONSTRAINT PK_Origins PRIMARY KEY (Id),
    CONSTRAINT UQ_Origins_Name UNIQUE (Name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 物品信息表
CREATE TABLE Items (
    ItemCode        VARCHAR(20)     NOT NULL,
    ItemName        VARCHAR(100)    NOT NULL,
    Category        INT             NOT NULL,
    Origin          VARCHAR(100)    NOT NULL DEFAULT '',
    Specification   VARCHAR(100)    NOT NULL DEFAULT '',
    Model           VARCHAR(100)    NOT NULL DEFAULT '',
    ImagePath       VARCHAR(500)    NOT NULL DEFAULT '',
    Quantity        INT             NOT NULL DEFAULT 0,
    CONSTRAINT PK_Items PRIMARY KEY (ItemCode),
    CONSTRAINT FK_Items_Category FOREIGN KEY (Category) REFERENCES Categories(Id)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT CK_Items_Quantity CHECK (Quantity >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 入库记录表
CREATE TABLE StockIn (
    StockInNo       VARCHAR(20)     NOT NULL,
    ItemCode        VARCHAR(20)     NOT NULL,
    PurchaseDate    DATETIME        NOT NULL,
    Quantity        INT             NOT NULL,
    UnitPrice       DECIMAL(18,2)   NOT NULL,
    TotalPrice      DECIMAL(18,2)   NOT NULL,
    CONSTRAINT PK_StockIn PRIMARY KEY (StockInNo),
    CONSTRAINT FK_StockIn_ItemCode FOREIGN KEY (ItemCode) REFERENCES Items(ItemCode)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT CK_StockIn_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_StockIn_UnitPrice CHECK (UnitPrice >= 0),
    CONSTRAINT CK_StockIn_TotalPrice CHECK (TotalPrice >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 领用/出库记录表
CREATE TABLE StockOut (
    StockOutNo      VARCHAR(20)     NOT NULL,
    ItemCode        VARCHAR(20)     NOT NULL,
    Quantity        INT             NOT NULL,
    ApplicantId     VARCHAR(50)     NOT NULL,
    ApplyDate       DATETIME        NOT NULL,
    Status          INT             NOT NULL DEFAULT 0,
    ApproveDate     DATETIME        NULL,
    Remark          VARCHAR(500)    NULL,
    CONSTRAINT PK_StockOut PRIMARY KEY (StockOutNo),
    CONSTRAINT FK_StockOut_ItemCode FOREIGN KEY (ItemCode) REFERENCES Items(ItemCode)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT CK_StockOut_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_StockOut_Status CHECK (Status IN (0, 1, 2))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 索引（提升查询性能）
CREATE INDEX IX_StockIn_ItemCode ON StockIn(ItemCode);
CREATE INDEX IX_StockIn_PurchaseDate ON StockIn(PurchaseDate);
CREATE INDEX IX_StockOut_ItemCode ON StockOut(ItemCode);
CREATE INDEX IX_StockOut_ApplicantId ON StockOut(ApplicantId);
CREATE INDEX IX_StockOut_Status ON StockOut(Status);
CREATE INDEX IX_Items_Category ON Items(Category);
CREATE INDEX IX_Items_ItemName ON Items(ItemName);
