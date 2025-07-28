CREATE DATABASE WebLuuTruFile;
GO

USE WebLuuTruFile;
GO

-- Tạo bảng AspNetUsers
CREATE TABLE AspNetUsers (
    Id NVARCHAR(450) PRIMARY KEY,
    UserName NVARCHAR(256) UNIQUE NOT NULL,
    NormalizedUserName NVARCHAR(256) UNIQUE,
    Email NVARCHAR(256) UNIQUE NOT NULL,
    NormalizedEmail NVARCHAR(256) UNIQUE,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX),
    SecurityStamp NVARCHAR(MAX),
    ConcurrencyStamp NVARCHAR(MAX),
    PhoneNumber NVARCHAR(20),
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    LockoutEnd DATETIMEOFFSET,
    LockoutEnabled BIT NOT NULL DEFAULT 1,
    AccessFailedCount INT NOT NULL DEFAULT 0
);
GO

-- Tạo bảng AspNetRoles
CREATE TABLE AspNetRoles (
    Id NVARCHAR(450) PRIMARY KEY,
    Name NVARCHAR(256) UNIQUE NOT NULL,
    NormalizedName NVARCHAR(256) UNIQUE,
    ConcurrencyStamp NVARCHAR(MAX)
);
GO

-- Tạo bảng AspNetUserRoles
CREATE TABLE AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng Files (tham chiếu đến AspNetUsers)
CREATE TABLE Files (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId NVARCHAR(450) NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(MAX) NOT NULL,
    FileSize BIGINT NOT NULL,
    FileType NVARCHAR(255) NOT NULL,
    UploadedBy NVARCHAR(255) NOT NULL,
    UploadDate DATETIME DEFAULT GETDATE(),
    EncryptionKey NVARCHAR(512),
    WatermarkText NVARCHAR(255),
    IsProtected BIT DEFAULT 0,
	IsDeleted BIT DEFAULT 0,
    DeletedAt DATETIME NULL,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng FileVersions (tham chiếu đến Files)
CREATE TABLE FileVersions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FileId UNIQUEIDENTIFIER NOT NULL,
    VersionNumber INT NOT NULL,
    FilePath NVARCHAR(MAX) NOT NULL,
    UploadDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (FileId) REFERENCES Files(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng AspNetUserClaims (tham chiếu đến AspNetUsers)
CREATE TABLE AspNetUserClaims (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX),
    ClaimValue NVARCHAR(MAX),
    CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng AspNetUserLogins (tham chiếu đến AspNetUsers)
CREATE TABLE AspNetUserLogins (
    LoginProvider NVARCHAR(128) NOT NULL,
    ProviderKey NVARCHAR(128) NOT NULL,
    ProviderDisplayName NVARCHAR(MAX),
    UserId NVARCHAR(450) NOT NULL,
    PRIMARY KEY (LoginProvider, ProviderKey),
    CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng AspNetUserTokens (tham chiếu đến AspNetUsers)
CREATE TABLE AspNetUserTokens (
    UserId NVARCHAR(450) NOT NULL,
    LoginProvider NVARCHAR(128) NOT NULL,
    Name NVARCHAR(128) NOT NULL,
    Value NVARCHAR(MAX),
    PRIMARY KEY (UserId, LoginProvider, Name),
    CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId)
        REFERENCES AspNetUsers(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng AspNetRoleClaims (tham chiếu đến AspNetRoles)
CREATE TABLE AspNetRoleClaims (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    RoleId NVARCHAR(450) NOT NULL,
    ClaimType NVARCHAR(MAX),
    ClaimValue NVARCHAR(MAX),
    CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId)
        REFERENCES AspNetRoles(Id) ON DELETE CASCADE
);
GO

-- Tạo bảng DownloadLogs (tham chiếu đến AspNetUsers và Files)
CREATE TABLE DownloadLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId NVARCHAR(450) NOT NULL,
    FileId UNIQUEIDENTIFIER NOT NULL,
    DownloadDate DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id) ON DELETE NO ACTION,
    FOREIGN KEY (FileId) REFERENCES Files(Id) ON DELETE NO ACTION
);
GO
