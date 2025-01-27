-- NOTE: If executing through SQL Server Management Studio, set 'SQKCMD Mode' via the 'Query' menu

:Setvar DatabaseName ApplicationAccessConfiguration

USE $(DatabaseName);
GO 

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE TABLE $(DatabaseName).dbo.ShardConfiguration
(
    Id                   bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    DataElementType      nvarchar(450)  NOT NULL, 
    OperationType        nvarchar(450)  NOT NULL, 
    HashRangeStart       int            NOT NULL, 
    ClientConfiguration  nvarchar(max)  NOT NULL CHECK (ISJSON(ClientConfiguration)=1), 
    TransactionFrom      datetime2      NOT NULL, 
    TransactionTo        datetime2      NOT NULL
);

CREATE INDEX ShardConfigurationTransactionIndex ON ShardConfiguration (TransactionFrom, TransactionTo);


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create User-defined Types
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE $(DatabaseName);
GO 

CREATE TYPE dbo.ShardConfigurationStagingTableType 
AS TABLE
(
    DataElementType      nvarchar(450)  NOT NULL, 
    OperationType        nvarchar(450)  NOT NULL, 
    HashRangeStart       int            NOT NULL, 
    ClientConfiguration  nvarchar(max)  NOT NULL
);
GO


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE $(DatabaseName);
GO 

--------------------------------------------------------------------------------
-- dbo.GetTemporalMaxDate

CREATE FUNCTION dbo.GetTemporalMaxDate
(
)
RETURNS datetime2
AS
BEGIN
    RETURN CONVERT(datetime2, '9999-12-31T23:59:59.9999999', 126);
END
GO

--------------------------------------------------------------------------------
-- dbo.SubtractTemporalMinimumTimeUnit

CREATE FUNCTION dbo.SubtractTemporalMinimumTimeUnit
(
    @InputTime  datetime2
)
RETURNS datetime2
AS
BEGIN
    RETURN DATEADD(NANOSECOND, -100, @InputTime);
END
GO

--------------------------------------------------------------------------------
-- dbo.ProcessEvents

CREATE PROCEDURE dbo.UpdateShardConfiguration
(
    @ShardConfigurationItems  ShardConfigurationStagingTableType  READONLY
)
AS
BEGIN

    DECLARE @ErrorMessage      nvarchar(max);
    DECLARE @CurrentTimestamp  datetime2;

    DECLARE @CurrentDataElementType      nvarchar(450);
    DECLARE @CurrentOperationType        nvarchar(450);
    DECLARE @CurrentHashRangeStart       int;
    DECLARE @CurrentClientConfiguration  nvarchar(max);

    DECLARE InputTableCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT  DataElementType, 
            OperationType, 
            HashRangeStart, 
            ClientConfiguration
    FROM    @ShardConfigurationItems;

    BEGIN TRANSACTION

    SELECT @CurrentTimestamp = SYSUTCDATETIME();

    -- Take an exclusive lock on the ShardConfiguration table
    SELECT TOP 1 Id 
    FROM   dbo.ShardConfiguration WITH (UPDLOCK, TABLOCKX);

    -- Delete the existing configuration
    BEGIN TRY
        UPDATE  dbo.ShardConfiguration 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@CurrentTimestamp)
        WHERE   @CurrentTimestamp BETWEEN TransactionFrom AND TransactionTo;
        END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when deleting existing shard configuration; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Inser the new configuration
    OPEN InputTableCursor;
    FETCH NEXT 
    FROM        InputTableCursor
    INTO        @CurrentDataElementType, 
                @CurrentOperationType, 
                @CurrentHashRangeStart, 
                @CurrentClientConfiguration;

    WHILE (@@FETCH_STATUS) = 0

        BEGIN
            BEGIN TRY
                INSERT  
                INTO    dbo.ShardConfiguration 
                        (
                            DataElementType, 
                            OperationType, 
                            HashRangeStart, 
                            ClientConfiguration, 
                            TransactionFrom, 
                            TransactionTo
                        )
                VALUES  (
                            @CurrentDataElementType, 
                            @CurrentOperationType, 
                            @CurrentHashRangeStart, 
                            @CurrentClientConfiguration, 
                            @CurrentTimestamp, 
                            dbo.GetTemporalMaxDate()
                        );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting shard configuration for DataElementType ''' + ISNULL(@CurrentDataElementType, '(null)') + ''' OperationType ''' + ISNULL(@CurrentOperationType, '(null)') + ''' and HashRangeStart ''' + ISNULL(CONVERT(nvarchar, @CurrentHashRangeStart), '(null)') + ''' ; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH

            FETCH NEXT 
            FROM        InputTableCursor
            INTO        @CurrentDataElementType, 
                        @CurrentOperationType, 
                        @CurrentHashRangeStart, 
                        @CurrentClientConfiguration;
        END;

    CLOSE InputTableCursor;
    DEALLOCATE InputTableCursor;

    COMMIT TRANSACTION

END
GO

