-- ============================================================
-- AnalistaFinanziarioIA - Schema del Database SQL Server
-- ============================================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AnalistaFinanziarioDB')
BEGIN
    CREATE DATABASE AnalistaFinanziarioDB;
END
GO

USE AnalistaFinanziarioDB;
GO

-- ============================================================
-- Tabella: Titoli
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Titoli')
BEGIN
    CREATE TABLE Titoli (
        Id            INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        Simbolo       NVARCHAR(20)   NOT NULL,
        Nome          NVARCHAR(200)  NOT NULL,
        Settore       NVARCHAR(100)  NOT NULL DEFAULT '',
        Mercato       NVARCHAR(100)  NOT NULL DEFAULT '',
        DataCreazione DATETIME2      NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT UQ_Titoli_Simbolo UNIQUE (Simbolo)
    );
END
GO

-- ============================================================
-- Tabella: QuotazioniStoriche
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'QuotazioniStoriche')
BEGIN
    CREATE TABLE QuotazioniStoriche (
        Id              INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        TitoloId        INT            NOT NULL,
        Data            DATETIME2      NOT NULL,
        PrezzoApertura  DECIMAL(18,4)  NOT NULL,
        PrezzoChiusura  DECIMAL(18,4)  NOT NULL,
        PrezzoMassimo   DECIMAL(18,4)  NOT NULL,
        PrezzoMinimo    DECIMAL(18,4)  NOT NULL,
        Volume          BIGINT         NOT NULL DEFAULT 0,

        CONSTRAINT FK_QuotazioniStoriche_Titoli
            FOREIGN KEY (TitoloId) REFERENCES Titoli(Id) ON DELETE CASCADE,

        CONSTRAINT UQ_QuotazioniStoriche_TitoloData UNIQUE (TitoloId, Data)
    );

    CREATE INDEX IX_QuotazioniStoriche_TitoloId_Data
        ON QuotazioniStoriche (TitoloId, Data DESC);
END
GO

-- ============================================================
-- Tabella: Analisi
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Analisi')
BEGIN
    CREATE TABLE Analisi (
        Id               INT            NOT NULL IDENTITY(1,1) PRIMARY KEY,
        TitoloId         INT            NOT NULL,
        DataAnalisi      DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
        TipoAnalisi      NVARCHAR(100)  NOT NULL,
        Risultato        NVARCHAR(MAX)  NOT NULL,
        Note             NVARCHAR(MAX)  NULL,
        PrezzoTarget     DECIMAL(18,4)  NULL,
        Raccomandazione  NVARCHAR(50)   NOT NULL DEFAULT '',

        CONSTRAINT FK_Analisi_Titoli
            FOREIGN KEY (TitoloId) REFERENCES Titoli(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Analisi_TitoloId_DataAnalisi
        ON Analisi (TitoloId, DataAnalisi DESC);
END
GO
