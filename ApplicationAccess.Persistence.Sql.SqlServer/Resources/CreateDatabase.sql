﻿--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Database
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

-- NOTE: If executing through SQL Server Management Studio, set 'SQKCMD Mode' via the 'Query' menu

:Setvar DatabaseName ApplicationAccess

CREATE DATABASE $(DatabaseName);
GO

USE $(DatabaseName);
GO 


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE TABLE $(DatabaseName).dbo.EventIdToTransactionTimeMap
(
    EventId              uniqueidentifier  NOT NULL PRIMARY KEY, 
    TransactionTime      datetime2         NOT NULL, 
    TransactionSequence  int               NOT NULL, 
);

CREATE INDEX EventIdToTransactionTimeMapEventIdIndex ON $(DatabaseName).dbo.EventIdToTransactionTimeMap (EventId);
CREATE INDEX EventIdToTransactionTimeMapTransactionTimeIndex ON $(DatabaseName).dbo.EventIdToTransactionTimeMap (TransactionTime, TransactionSequence);

CREATE TABLE $(DatabaseName).dbo.Users
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    [User]           nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX UsersUserIndex ON $(DatabaseName).dbo.Users ([User], TransactionTo);
CREATE INDEX UsersTransactionIndex ON $(DatabaseName).dbo.Users (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.Groups
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    [Group]          nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX GroupsGroupIndex ON $(DatabaseName).dbo.Groups ([Group], TransactionTo);
CREATE INDEX GroupsTransactionIndex ON $(DatabaseName).dbo.Groups (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.UserToGroupMappings
(
    Id               bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    UserId           bigint     NOT NULL, 
    GroupId          bigint     NOT NULL, 
    TransactionFrom  datetime2  NOT NULL, 
    TransactionTo    datetime2  NOT NULL
)

CREATE INDEX UserToGroupMappingsUserIndex ON UserToGroupMappings (UserId, TransactionTo);
CREATE INDEX UserToGroupMappingsGroupIndex ON UserToGroupMappings (GroupId, TransactionTo);
CREATE INDEX UserToGroupMappingsTransactionIndex ON UserToGroupMappings (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.GroupToGroupMappings
(
    Id               bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    FromGroupId      bigint     NOT NULL, 
    ToGroupId        bigint     NOT NULL, 
    TransactionFrom  datetime2  NOT NULL, 
    TransactionTo    datetime2  NOT NULL
)

CREATE INDEX GroupToGroupMappingsFromGroupIndex ON GroupToGroupMappings (FromGroupId, TransactionTo);
CREATE INDEX GroupToGroupMappingsToGroupIndex ON GroupToGroupMappings (ToGroupId, TransactionTo);
CREATE INDEX GroupToGroupMappingsTransactionIndex ON GroupToGroupMappings (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.ApplicationComponents
(
    Id                    bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    ApplicationComponent  nvarchar(450)  NOT NULL, 
    TransactionFrom       datetime2      NOT NULL, 
    TransactionTo         datetime2      NOT NULL
);

CREATE INDEX ApplicationComponentsApplicationComponentIndex ON $(DatabaseName).dbo.ApplicationComponents (ApplicationComponent, TransactionTo);
CREATE INDEX ApplicationComponentsTransactionIndex ON $(DatabaseName).dbo.ApplicationComponents (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.AccessLevels
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    AccessLevel      nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX AccessLevelsAccessLevelIndex ON $(DatabaseName).dbo.AccessLevels (AccessLevel, TransactionTo);
CREATE INDEX AccessLevelsTransactionIndex ON $(DatabaseName).dbo.AccessLevels (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.UserToApplicationComponentAndAccessLevelMappings
(
    Id                      bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    UserId                  bigint     NOT NULL, 
    ApplicationComponentId  bigint     NOT NULL, 
    AccessLevelId           bigint     NOT NULL, 
    TransactionFrom         datetime2  NOT NULL, 
    TransactionTo           datetime2  NOT NULL
);

CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsUserIndex ON $(DatabaseName).dbo.UserToApplicationComponentAndAccessLevelMappings (UserId, ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsApplicationComponentIndex ON $(DatabaseName).dbo.UserToApplicationComponentAndAccessLevelMappings (ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsTransactionIndex ON $(DatabaseName).dbo.UserToApplicationComponentAndAccessLevelMappings (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.GroupToApplicationComponentAndAccessLevelMappings
(
    Id                      bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    GroupId                 bigint     NOT NULL, 
    ApplicationComponentId  bigint     NOT NULL, 
    AccessLevelId           bigint     NOT NULL, 
    TransactionFrom         datetime2  NOT NULL, 
    TransactionTo           datetime2  NOT NULL
);

CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsGroupIndex ON $(DatabaseName).dbo.GroupToApplicationComponentAndAccessLevelMappings (GroupId, ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsApplicationComponentIndex ON $(DatabaseName).dbo.GroupToApplicationComponentAndAccessLevelMappings (ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsTransactionIndex ON $(DatabaseName).dbo.GroupToApplicationComponentAndAccessLevelMappings (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.EntityTypes
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    EntityType       nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX EntityTypesEntityTypeIndex ON $(DatabaseName).dbo.EntityTypes (EntityType, TransactionTo);
CREATE INDEX EntityTypesTransactionIndex ON $(DatabaseName).dbo.EntityTypes (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.Entities
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    EntityTypeId     bigint         NOT NULL, 
    Entity           nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX EntitiesEntityIndex ON $(DatabaseName).dbo.Entities (EntityTypeId, Entity, TransactionTo);
CREATE INDEX EntitiesTransactionIndex ON $(DatabaseName).dbo.Entities (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.UserToEntityMappings
(
    Id               bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    UserId           bigint     NOT NULL, 
    EntityTypeId     bigint     NOT NULL, 
    EntityId         bigint     NOT NULL, 
    TransactionFrom  datetime2  NOT NULL, 
    TransactionTo    datetime2  NOT NULL
);

CREATE INDEX UserToEntityMappingsUserIndex ON $(DatabaseName).dbo.UserToEntityMappings (UserId, EntityTypeId, EntityId, TransactionTo);
CREATE INDEX UserToEntityMappingsEntityIndex ON $(DatabaseName).dbo.UserToEntityMappings (EntityTypeId, EntityId, TransactionTo);
CREATE INDEX UserToEntityMappingsTransactionIndex ON $(DatabaseName).dbo.UserToEntityMappings (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.GroupToEntityMappings
(
    Id               bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    GroupId          bigint     NOT NULL, 
    EntityTypeId     bigint     NOT NULL, 
    EntityId         bigint     NOT NULL, 
    TransactionFrom  datetime2  NOT NULL, 
    TransactionTo    datetime2  NOT NULL
);

CREATE INDEX GroupToEntityMappingsGroupIndex ON $(DatabaseName).dbo.GroupToEntityMappings (GroupId, EntityTypeId, EntityId, TransactionTo);
CREATE INDEX GroupToEntityMappingsEntityIndex ON $(DatabaseName).dbo.GroupToEntityMappings (EntityTypeId, EntityId, TransactionTo);
CREATE INDEX GroupToEntityMappingsTransactionIndex ON $(DatabaseName).dbo.GroupToEntityMappings (TransactionFrom, TransactionTo);

CREATE TABLE $(DatabaseName).dbo.SchemaVersions
(
    Id         bigint        NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    [Version]  nvarchar(20)  NOT NULL, 
    Created    datetime2     NOT NULL, 
);

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create User-defined Types
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE $(DatabaseName);
GO 

CREATE TYPE dbo.EventTableType 
AS TABLE
(
    Id            bigint             NOT NULL PRIMARY KEY, 
    EventType     nvarchar(max), 
    EventId       uniqueidentifier, 
    EventAction   nvarchar(max), 
    OccurredTime  datetime2, 
    HashCode      int, 
    EventData1    nvarchar(max), 
    EventData2    nvarchar(max), 
    EventData3    nvarchar(max) 
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
-- dbo.CreateEvent

CREATE PROCEDURE dbo.CreateEvent
(
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @LastTransactionTime      datetime2;
    DECLARE @LastTransactionSequence  int; 
    DECLARE @TransactionSequence      int; 
    DECLARE @ErrorMessage             nvarchar(max);

    SET @TransactionSequence = 0;

    -- Get the last transaction time and sequence
    SELECT  @LastTransactionTime = TransactionTime, 
            @LastTransactionSequence = MAX(TransactionSequence)
    FROM    EventIdToTransactionTimeMap 
    WHERE   TransactionTime = (
                                  SELECT  MAX(TransactionTime) 
                                  FROM    EventIdToTransactionTimeMap 
                              )
    GROUP   BY TransactionTime;

    IF (@LastTransactionTime IS NULL)
        SET @LastTransactionTime = CONVERT(datetime2, '0001-01-01T00:00:00.0000000', 126);

    IF (@TransactionTime < @LastTransactionTime)
    BEGIN
        SET @ErrorMessage = N'Parameter ''TransactionTime'' with value ''' + CONVERT(nvarchar, @TransactionTime, 126) + ''' must be greater than or equal to last transaction time ''' + CONVERT(nvarchar, @LastTransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END
    IF (@TransactionTime = @LastTransactionTime)
        SET @TransactionSequence = @LastTransactionSequence + 1;


    -- Insert the event id and timestamp for the transaction
    BEGIN TRY
        INSERT  
        INTO    dbo.EventIdToTransactionTimeMap
                (
                    EventId, 
                    TransactionTime, 
                    TransactionSequence
                )
        VALUES  (
                    @EventId, 
                    @TransactionTime, 
                    @TransactionSequence
                );
    END TRY
    BEGIN CATCH
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToTransactionTimeMap''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

END
GO

--------------------------------------------------------------------------------
-- dbo.AddUser

CREATE PROCEDURE dbo.AddUser
(
    @User             nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Insert the new row
    BEGIN TRY
        INSERT  
        INTO    dbo.Users 
                (
                    [User], 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    @User, 
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUser

CREATE PROCEDURE dbo.RemoveUser
(
    @User             nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;
    
    BEGIN TRANSACTION
    
    SELECT TOP 1 Id 
    FROM   dbo.Users WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.UserToGroupMappings WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.UserToApplicationComponentAndAccessLevelMappings WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.UserToEntityMappings WITH (UPDLOCK, TABLOCKX);

    SELECT  @CurrentRowId = Id 
    FROM    dbo.Users 
    WHERE   [User] = @User 
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'No Users row exists for User ''' + ISNULL(@User, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any UserToGroupMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToGroupMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   UserId = @CurrentRowId 
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Group mappings for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any UserToApplicationComponentAndAccessLevelMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   UserId = @CurrentRowId 
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to ApplicationComponent and AccessLevel mappings for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any UserToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToEntityMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   UserId = @CurrentRowId 
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Entity mappings for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.Users 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroup

CREATE PROCEDURE dbo.AddGroup
(
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Insert the new row
    BEGIN TRY
        INSERT  
        INTO    dbo.Groups 
                (
                    [Group], 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    @Group, 
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroup

CREATE PROCEDURE dbo.RemoveGroup
(
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;
    
    BEGIN TRANSACTION
    
    SELECT TOP 1 Id 
    FROM   dbo.Groups WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.UserToGroupMappings WITH (UPDLOCK, TABLOCKX);
    
    SELECT TOP 1 Id 
    FROM   dbo.GroupToGroupMappings WITH (UPDLOCK, TABLOCKX);
    
    SELECT TOP 1 Id 
    FROM   dbo.GroupToApplicationComponentAndAccessLevelMappings WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.GroupToEntityMappings WITH (UPDLOCK, TABLOCKX);

    SELECT  @CurrentRowId = Id 
    FROM    dbo.Groups 
    WHERE   [Group] = @Group 
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'No Groups row exists for Group ''' + ISNULL(@Group, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any UserToGroupMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToGroupMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   GroupId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Group mappings for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any GroupToGroupMappings rows
    BEGIN TRY
        UPDATE  dbo.GroupToGroupMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   (
                    FromGroupId = @CurrentRowId
                    OR
                    ToGroupId = @CurrentRowId
                )
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to Group mappings for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any GroupToApplicationComponentAndAccessLevelMappings rows
    BEGIN TRY
        UPDATE  dbo.GroupToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   GroupId = @CurrentRowId 
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to ApplicationComponent and AccessLevel mappings for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any GroupToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.GroupToEntityMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   GroupId = @CurrentRowId 
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to Entity mappings for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.Groups 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddUserToGroupMapping

CREATE PROCEDURE dbo.AddUserToGroupMapping
(
    @User             nvarchar(450),
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        INSERT  
        INTO    dbo.UserToGroupMappings 
                (
                    UserId, 
                    GroupId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.Users 
                        WHERE   [User] = @User 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.Groups 
                        WHERE   [Group] = @Group 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting User to Group mapping between ''' + ISNULL(@User, '(null)') + ''' and ''' + ISNULL(@Group, '(null)') + ''' ; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUserToGroupMapping

CREATE PROCEDURE dbo.RemoveUserToGroupMapping
(
    @User             nvarchar(450),
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

    SELECT  @CurrentRowId = Id 
    FROM    dbo.UserToGroupMappings 
    WHERE   UserId = 
            (
                SELECT  Id 
                FROM    dbo.Users 
                WHERE   [User] = @User 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    GroupId = 
            (
                SELECT  Id 
                FROM    dbo.Groups 
                WHERE   [Group] = @Group 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No UserToGroupMappings row exists for User ''' + ISNULL(@User, '(null)') + ''', Group ''' + ISNULL(@Group, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.UserToGroupMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Group mapping for ''' + ISNULL(@User, '(null)') + ''' and ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroupToGroupMapping

CREATE PROCEDURE dbo.AddGroupToGroupMapping
(
    @FromGroup        nvarchar(450),
    @ToGroup          nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        INSERT  
        INTO    dbo.GroupToGroupMappings 
                (
                    FromGroupId, 
                    ToGroupId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.Groups 
                        WHERE   [Group] = @FromGroup 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.Groups 
                        WHERE   [Group] = @ToGroup 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting Group to Group mapping between ''' + ISNULL(@FromGroup, '(null)') + ''' and ''' + ISNULL(@ToGroup, '(null)') + ''' ; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroupToGroupMapping

CREATE PROCEDURE dbo.RemoveGroupToGroupMapping
(
    @FromGroup        nvarchar(450),
    @ToGroup          nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

    SELECT  @CurrentRowId = Id 
    FROM    dbo.GroupToGroupMappings 
    WHERE   FromGroupId = 
            (
                SELECT  Id 
                FROM    dbo.Groups 
                WHERE   [Group] = @FromGroup 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    ToGroupId = 
            (
                SELECT  Id 
                FROM    dbo.Groups 
                WHERE   [Group] = @ToGroup 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No GroupToGroupMappings row exists for FromGroup ''' + ISNULL(@FromGroup, '(null)') + ''', ToGroup ''' + ISNULL(@ToGroup, '(null)') +  ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.GroupToGroupMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to Group mapping for ''' + ISNULL(@FromGroup, '(null)') + ''' and ''' + ISNULL(@ToGroup, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddUserToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE dbo.AddUserToApplicationComponentAndAccessLevelMapping
(
    @User                  nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        INSERT  
        INTO    dbo.UserToApplicationComponentAndAccessLevelMappings 
                (
                    UserId, 
                    ApplicationComponentId, 
                    AccessLevelId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.Users 
                        WHERE   [User] = @User 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.ApplicationComponents
                        WHERE   ApplicationComponent = @ApplicationComponent 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.AccessLevels
                        WHERE   AccessLevel = @AccessLevel 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ),
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        IF (ERROR_NUMBER() = 515)
        BEGIN
            -- Insert failed due to 'Cannot insert the value NULL into column' error
            --   Need to ensure @ApplicationComponent and @AccessLevel exist
            DECLARE @ApplicationComponentsId  bigint;

            SELECT  @ApplicationComponentsId = Id 
            FROM    dbo.ApplicationComponents 
            WHERE   ApplicationComponent = @ApplicationComponent
              AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
            
            IF (@ApplicationComponentsId IS NULL)
            BEGIN TRY
                -- Insert @ApplicationComponent
                INSERT  
                INTO    dbo.ApplicationComponents 
                        (
                            ApplicationComponent, 
                            TransactionFrom, 
                            TransactionTo 
                        )
                VALUES  (
                            @ApplicationComponent, 
                            @TransactionTime, 
                            dbo.GetTemporalMaxDate()
                        );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting ApplicationComponent ''' + ISNULL(@ApplicationComponent, '(null)') + '''; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH

            DECLARE @AccessLevelsId  bigint;
            
            SELECT  @AccessLevelsId = Id 
            FROM    dbo.AccessLevels 
            WHERE   AccessLevel = @AccessLevel
              AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

            IF (@AccessLevelsId IS NULL)
            BEGIN TRY
                -- Insert @AccessLevel
                INSERT  
                INTO    dbo.AccessLevels 
                        (
                            AccessLevel, 
                            TransactionFrom, 
                            TransactionTo 
                        )
                VALUES  (
                            @AccessLevel, 
                            @TransactionTime, 
                            dbo.GetTemporalMaxDate()
                        );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting AccessLevel ''' + ISNULL(@AccessLevel, '(null)') + '''; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH

            -- Repeat the original insert
            BEGIN TRY
                INSERT  
                INTO    dbo.UserToApplicationComponentAndAccessLevelMappings 
                        (
                            UserId, 
                            ApplicationComponentId, 
                            AccessLevelId, 
                            TransactionFrom, 
                            TransactionTo 
                        )
                VALUES  (
                            ( 
                                SELECT  Id 
                                FROM    dbo.Users 
                                WHERE   [User] = @User 
                                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                            ), 
                            ( 
                                SELECT  Id 
                                FROM    dbo.ApplicationComponents
                                WHERE   ApplicationComponent = @ApplicationComponent 
                                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                            ), 
                            ( 
                                SELECT  Id 
                                FROM    dbo.AccessLevels
                                WHERE   AccessLevel = @AccessLevel 
                                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                            ),
                            @TransactionTime, 
                            dbo.GetTemporalMaxDate()
                        );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting User to ApplicationComponent and AccessLevel mapping between ''' + ISNULL(@User, '(null)') + ''', ''' + ISNULL(@ApplicationComponent, '(null)') + ''' and ''' + ISNULL(@AccessLevel, '(null)') + ''' ; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH
        END
        ELSE  -- i.e. ERROR_NUMBER() != 515
        BEGIN
            ROLLBACK TRANSACTION
            SET @ErrorMessage = N'Error occurred when inserting User to ApplicationComponent and AccessLevel mapping between ''' + ISNULL(@User, '(null)') + ''', ''' + ISNULL(@ApplicationComponent, '(null)') + ''' and ''' + ISNULL(@AccessLevel, '(null)') + ''' ; ' + ERROR_MESSAGE();
            THROW 50001, @ErrorMessage, 1;
        END
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUserToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE dbo.RemoveUserToApplicationComponentAndAccessLevelMapping
(
    @User                  nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

    SELECT  @CurrentRowId = Id 
    FROM    dbo.UserToApplicationComponentAndAccessLevelMappings 
    WHERE   UserId = 
            (
                SELECT  Id 
                FROM    dbo.Users 
                WHERE   [User] = @User 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    ApplicationComponentId = 
            (
                SELECT  Id 
                FROM    dbo.ApplicationComponents 
                WHERE   ApplicationComponent = @ApplicationComponent 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    AccessLevelId = 
            (
                SELECT  Id 
                FROM    dbo.AccessLevels 
                WHERE   AccessLevel = @AccessLevel 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No UserToApplicationComponentAndAccessLevelMappings row exists for User ''' + ISNULL(@User, '(null)') + ''', ApplicationComponent ''' + ISNULL(@ApplicationComponent, '(null)') + ''', AccessLevel ''' + ISNULL(@AccessLevel, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.UserToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to ApplicationComponent and AccessLevel mapping for ''' + ISNULL(@User, '(null)') + ''', ''' + ISNULL(@ApplicationComponent, '(null)') + ''' and ''' + ISNULL(@AccessLevel, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroupToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE dbo.AddGroupToApplicationComponentAndAccessLevelMapping
(
    @Group                 nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        INSERT  
        INTO    dbo.GroupToApplicationComponentAndAccessLevelMappings 
                (
                    GroupId, 
                    ApplicationComponentId, 
                    AccessLevelId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.Groups 
                        WHERE   [Group] = @Group 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.ApplicationComponents
                        WHERE   ApplicationComponent = @ApplicationComponent 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.AccessLevels
                        WHERE   AccessLevel = @AccessLevel 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ),
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        IF (ERROR_NUMBER() = 515)
        BEGIN
            -- Insert failed due to 'Cannot insert the value NULL into column' error
            --   Need to ensure @ApplicationComponent and @AccessLevel exist
            DECLARE @ApplicationComponentsId  bigint;

            SELECT  @ApplicationComponentsId = Id 
            FROM    dbo.ApplicationComponents 
            WHERE   ApplicationComponent = @ApplicationComponent
              AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
            
            IF (@ApplicationComponentsId IS NULL)
            BEGIN TRY
                -- Insert @ApplicationComponent
                INSERT  
                INTO    dbo.ApplicationComponents 
                        (
                            ApplicationComponent, 
                            TransactionFrom, 
                            TransactionTo 
                        )
                VALUES  (
                            @ApplicationComponent, 
                            @TransactionTime, 
                            dbo.GetTemporalMaxDate()
                        );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting ApplicationComponent ''' + ISNULL(@ApplicationComponent, '(null)') + '''; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH

            DECLARE @AccessLevelsId  bigint;
            
            SELECT  @AccessLevelsId = Id 
            FROM    dbo.AccessLevels 
            WHERE   AccessLevel = @AccessLevel
              AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

            IF (@AccessLevelsId IS NULL)
            BEGIN TRY
                -- Insert @AccessLevel
                INSERT  
                INTO    dbo.AccessLevels 
                        (
                            AccessLevel, 
                            TransactionFrom, 
                            TransactionTo 
                        )
                VALUES  (
                            @AccessLevel, 
                            @TransactionTime, 
                            dbo.GetTemporalMaxDate()
                        );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting AccessLevel ''' + ISNULL(@AccessLevel, '(null)') + '''; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH

            -- Repeat the original insert
            BEGIN TRY
                INSERT  
                INTO    dbo.GroupToApplicationComponentAndAccessLevelMappings 
                        (
                            GroupId, 
                            ApplicationComponentId, 
                            AccessLevelId, 
                            TransactionFrom, 
                            TransactionTo 
                        )
                VALUES  (
                            ( 
                                SELECT  Id 
                                FROM    dbo.Groups 
                                WHERE   [Group] = @Group 
                                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                            ), 
                            ( 
                                SELECT  Id 
                                FROM    dbo.ApplicationComponents
                                WHERE   ApplicationComponent = @ApplicationComponent 
                                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                            ), 
                            ( 
                                SELECT  Id 
                                FROM    dbo.AccessLevels
                                WHERE   AccessLevel = @AccessLevel 
                                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                            ),
                            @TransactionTime, 
                            dbo.GetTemporalMaxDate()
                );
            END TRY
            BEGIN CATCH
                ROLLBACK TRANSACTION
                SET @ErrorMessage = N'Error occurred when inserting Group to ApplicationComponent and AccessLevel mapping between ''' + ISNULL(@Group, '(null)') + ''', ''' + ISNULL(@ApplicationComponent, '(null)') + ''' and ''' + ISNULL(@AccessLevel, '(null)') + ''' ; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH
        END
        ELSE  -- i.e. ERROR_NUMBER() != 515
        BEGIN
            ROLLBACK TRANSACTION
            SET @ErrorMessage = N'Error occurred when inserting Group to ApplicationComponent and AccessLevel mapping between ''' + ISNULL(@Group, '(null)') + ''', ''' + ISNULL(@ApplicationComponent, '(null)') + ''' and ''' + ISNULL(@AccessLevel, '(null)') + ''' ; ' + ERROR_MESSAGE();
            THROW 50001, @ErrorMessage, 1;
        END
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroupToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE dbo.RemoveGroupToApplicationComponentAndAccessLevelMapping
(
    @Group                 nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

    SELECT  @CurrentRowId = Id 
    FROM    dbo.GroupToApplicationComponentAndAccessLevelMappings 
    WHERE   GroupId = 
            (
                SELECT  Id 
                FROM    dbo.Groups 
                WHERE   [Group] = @Group 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    ApplicationComponentId = 
            (
                SELECT  Id 
                FROM    dbo.ApplicationComponents 
                WHERE   ApplicationComponent = @ApplicationComponent 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    AccessLevelId = 
            (
                SELECT  Id 
                FROM    dbo.AccessLevels 
                WHERE   AccessLevel = @AccessLevel 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No GroupToApplicationComponentAndAccessLevelMappings row exists for Group ''' + ISNULL(@Group, '(null)') + ''', ApplicationComponent ''' + ISNULL(@ApplicationComponent, '(null)') + ''', AccessLevel ''' + ISNULL(@AccessLevel, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.GroupToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to ApplicationComponent and AccessLevel mapping for ''' + ISNULL(@Group, '(null)') + ''', ''' + ISNULL(@ApplicationComponent, '(null)') + ''' and ''' + ISNULL(@AccessLevel, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddEntityType

CREATE PROCEDURE dbo.AddEntityType
(
    @EntityType       nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Insert the new row
    BEGIN TRY
        INSERT  
        INTO    dbo.EntityTypes 
                (
                    EntityType, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    @EntityType, 
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveEntityType

CREATE PROCEDURE dbo.RemoveEntityType
(
    @EntityType       nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;
    
    BEGIN TRANSACTION
    
    SELECT TOP 1 Id 
    FROM   dbo.EntityTypes WITH (UPDLOCK, TABLOCKX);
    
    SELECT TOP 1 Id 
    FROM   dbo.Entities WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.UserToEntityMappings WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.GroupToEntityMappings WITH (UPDLOCK, TABLOCKX);
    
    SELECT  @CurrentRowId = Id 
    FROM    dbo.EntityTypes 
    WHERE   EntityType = @EntityType
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'No EntityTypes row exists for EntityType ''' + ISNULL(@EntityType, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any UserToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToEntityMappings
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   EntityTypeId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Entity mappings for EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any GroupToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.GroupToEntityMappings
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   EntityTypeId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to Entity mappings for EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any Entities rows
    BEGIN TRY
        UPDATE  dbo.Entities
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   EntityTypeId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Entities for EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.EntityTypes 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddEntity

CREATE PROCEDURE dbo.AddEntity
(
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Insert the new row
    BEGIN TRY
        INSERT  
        INTO    dbo.Entities 
                (
                    EntityTypeId, 
                    Entity, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.EntityTypes 
                        WHERE   EntityType = @EntityType 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    @Entity, 
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting Entity ''' + ISNULL(@Entity, '(null)') + ''' of type ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveEntity

CREATE PROCEDURE dbo.RemoveEntity
(
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;
    
    BEGIN TRANSACTION
    
    SELECT TOP 1 Id 
    FROM   dbo.Entities WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.UserToEntityMappings WITH (UPDLOCK, TABLOCKX);

    SELECT TOP 1 Id 
    FROM   dbo.GroupToEntityMappings WITH (UPDLOCK, TABLOCKX);
    
    SELECT  @CurrentRowId = Id 
    FROM    dbo.Entities 
    WHERE   EntityTypeId = 
            (
                SELECT  Id 
                FROM    dbo.EntityTypes 
                WHERE   EntityType = @EntityType 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   Entity = @Entity
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'No Entities row exists for EntityType ''' + ISNULL(@EntityType, '(null)') + ''', Entity ''' + ISNULL(@Entity, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any UserToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToEntityMappings
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   EntityTypeId = 
                (
                    SELECT  Id 
                    FROM    dbo.EntityTypes 
                    WHERE   EntityType = @EntityType 
                      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
                )
          AND   EntityId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Entity mappings for EntityType''' + ISNULL(@EntityType, '(null)') + ''' and Entity''' + ISNULL(@Entity, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    -- Invalidate any GroupToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.GroupToEntityMappings
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   EntityTypeId = 
                (
                    SELECT  Id 
                    FROM    dbo.EntityTypes 
                    WHERE   EntityType = @EntityType 
                      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
                )
          AND   EntityId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to Entity mappings for EntityType''' + ISNULL(@EntityType, '(null)') + ''' and Entity''' + ISNULL(@Entity, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.Entities 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Entity ''' + ISNULL(@Entity, '(null)') + ''' of EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddUserToEntityMapping

CREATE PROCEDURE dbo.AddUserToEntityMapping
(
    @User             nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        INSERT  
        INTO    dbo.UserToEntityMappings 
                (
                    UserId, 
                    EntityTypeId, 
                    EntityId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.Users 
                        WHERE   [User] = @User 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.EntityTypes
                        WHERE   EntityType = @EntityType 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.Entities
                        WHERE   EntityTypeId = 
                                ( 
                                    SELECT  Id 
                                    FROM    dbo.EntityTypes
                                    WHERE   EntityType = @EntityType 
                                      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                                )
                          AND   Entity = @Entity 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ),
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting User to Entity mapping between ''' + ISNULL(@User, '(null)') + ''', ''' + ISNULL(@EntityType, '(null)') + ''' and ''' + ISNULL(@Entity, '(null)') + ''' ; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUserToEntityMapping

CREATE PROCEDURE dbo.RemoveUserToEntityMapping
(
    @User             nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

    SELECT  @CurrentRowId = Id 
    FROM    dbo.UserToEntityMappings 
    WHERE   UserId = 
            (
                SELECT  Id 
                FROM    dbo.Users 
                WHERE   [User] = @User 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   EntityTypeId = 
            (
                SELECT  Id 
                FROM    dbo.EntityTypes
                WHERE   EntityType = @EntityType 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   EntityId = 
            (
                SELECT  Id 
                FROM    dbo.Entities
                WHERE   EntityTypeId = 
                        ( 
                            SELECT  Id 
                            FROM    dbo.EntityTypes
                            WHERE   EntityType = @EntityType 
                                AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                        )
                  AND   Entity = @Entity 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No UserToEntityMappings row exists for User ''' + ISNULL(@User, '(null)') + ''', EntityType ''' + ISNULL(@EntityType, '(null)') + ''', Entity ''' + ISNULL(@Entity, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.UserToEntityMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Entity mapping for ''' + ISNULL(@User, '(null)') + ''', ''' + ISNULL(@EntityType, '(null)') + ''' and ''' + ISNULL(@Entity, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroupToEntityMapping

CREATE PROCEDURE dbo.AddGroupToEntityMapping
(
    @Group            nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        INSERT  
        INTO    dbo.GroupToEntityMappings 
                (
                    GroupId, 
                    EntityTypeId, 
                    EntityId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    dbo.Groups 
                        WHERE   [Group] = @Group 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.EntityTypes
                        WHERE   EntityType = @EntityType 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    dbo.Entities
                        WHERE   EntityTypeId = 
                                ( 
                                    SELECT  Id 
                                    FROM    dbo.EntityTypes
                                    WHERE   EntityType = @EntityType 
                                      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                                )
                          AND   Entity = @Entity 
                          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ),
                    @TransactionTime, 
                    dbo.GetTemporalMaxDate()
                );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting Group to Entity mapping between ''' + ISNULL(@Group, '(null)') + ''', ''' + ISNULL(@EntityType, '(null)') + ''' and ''' + ISNULL(@Entity, '(null)') + ''' ; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroupToEntityMapping

CREATE PROCEDURE dbo.RemoveGroupToEntityMapping
(
    @Group            nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

    SELECT  @CurrentRowId = Id 
    FROM    dbo.GroupToEntityMappings 
    WHERE   GroupId = 
            (
                SELECT  Id 
                FROM    dbo.Groups 
                WHERE   [Group] = @Group 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   EntityTypeId = 
            (
                SELECT  Id 
                FROM    dbo.EntityTypes
                WHERE   EntityType = @EntityType 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   EntityId = 
            (
                SELECT  Id 
                FROM    dbo.Entities
                WHERE   EntityTypeId = 
                        ( 
                            SELECT  Id 
                            FROM    dbo.EntityTypes
                            WHERE   EntityType = @EntityType 
                                AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                        )
                  AND   Entity = @Entity 
                  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No GroupToEntityMappings row exists for Group ''' + ISNULL(@Group, '(null)') + ''', EntityType ''' + ISNULL(@EntityType, '(null)') + ''', Entity ''' + ISNULL(@Entity, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
        THROW 50001, @ErrorMessage, 1;
    END

    BEGIN TRANSACTION

    BEGIN TRY
        EXEC CreateEvent @EventId, @TransactionTime;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    BEGIN TRY
        UPDATE  dbo.GroupToEntityMappings 
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   Id = @CurrentRowId;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing Group to Entity mapping for ''' + ISNULL(@Group, '(null)') + ''', ''' + ISNULL(@EntityType, '(null)') + ''' and ''' + ISNULL(@Entity, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.ProcessEvents

CREATE PROCEDURE dbo.ProcessEvents
(
    @Events                   EventTableType  READONLY, 
    @IgnorePreExistingEvents  bit
)
AS
BEGIN

    DECLARE @UserEventTypeValue nvarchar(max) = 'user';
    DECLARE @GroupEventTypeValue nvarchar(max) = 'group';
    DECLARE @UserToGroupMappingEventTypeValue nvarchar(max) = 'userToGroupMapping';
    DECLARE @GroupToGroupMappingEventTypeValue nvarchar(max) = 'groupToGroupMapping';
    DECLARE @UserToApplicationComponentAndAccessLevelMappingEventTypeValue nvarchar(max) = 'userToApplicationComponentAndAccessLevelMapping';
    DECLARE @GroupToApplicationComponentAndAccessLevelMappingEventTypeValue nvarchar(max) = 'groupToApplicationComponentAndAccessLevelMapping';
    DECLARE @EntityTypeEventTypeValue nvarchar(max) = 'entityType';
    DECLARE @EntityEventTypeValue nvarchar(max) = 'entity';
    DECLARE @UserToEntityMappingEventTypeValue nvarchar(max) = 'userToEntityMapping';
    DECLARE @GroupToEntityMappingEventTypeValue nvarchar(max) = 'groupToEntityMapping';
    DECLARE @AddEventActionValue nvarchar(max) = 'add';
    DECLARE @RemoveEventActionValue nvarchar(max) = 'remove';

    DECLARE @ErrorMessage  nvarchar(max);

    DECLARE @CurrentEventType     nvarchar(max);
    DECLARE @CurrentEventId       uniqueidentifier;
    DECLARE @CurrentEventAction   nvarchar(max);
    DECLARE @CurrentOccurredTime  datetime2;
    DECLARE @CurrentEventData1    nvarchar(max);
    DECLARE @CurrentEventData2    nvarchar(max);
    DECLARE @CurrentEventData3    nvarchar(max);
    DECLARE @ExistingEventId      uniqueidentifier;

    DECLARE InputTableCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT  EventType, 
            EventId, 
            EventAction, 
            OccurredTime, 
            EventData1, 
            EventData2,
            EventData3
    FROM    @Events
    ORDER   BY Id;

    OPEN InputTableCursor;
    FETCH NEXT 
    FROM        InputTableCursor
    INTO        @CurrentEventType, 
                @CurrentEventId, 
                @CurrentEventAction, 
                @CurrentOccurredTime, 
                @CurrentEventData1, 
                @CurrentEventData2, 
                @CurrentEventData3;

    WHILE (@@FETCH_STATUS = 0)
        BEGIN

            IF (NOT(@CurrentEventAction = @AddEventActionValue OR @CurrentEventAction = @RemoveEventActionValue))
            BEGIN
                SET @ErrorMessage = N'Input table column ''EventAction'' should contain values ''' + @AddEventActionValue +  ''' or ''' + @RemoveEventActionValue + ''' but contained ''' + @CurrentEventAction + '''.';
                THROW 50001, @ErrorMessage, 1;
            END

            IF (@IgnorePreExistingEvents = 1)
            BEGIN
                SET @ExistingEventId = NULL;

                SELECT  @ExistingEventId = EventId
                FROM    EventIdToTransactionTimeMap
                WHERE   EventId = @CurrentEventId;

                IF (NOT(@ExistingEventId IS NULL))
                BEGIN
                    FETCH NEXT 
                    FROM        InputTableCursor
                    INTO        @CurrentEventType, 
                                @CurrentEventId, 
                                @CurrentEventAction, 
                                @CurrentOccurredTime, 
                                @CurrentEventData1, 
                                @CurrentEventData2, 
                                @CurrentEventData3;
                    CONTINUE;
                END
            END

            BEGIN TRY

                -- Handle 'user' event
                IF (@CurrentEventType = @UserEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddUser @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUser @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'group' event
                ELSE IF (@CurrentEventType = @GroupEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroup @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroup @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'user to group mapping' event
                ELSE IF (@CurrentEventType = @UserToGroupMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddUserToGroupMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUserToGroupMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'group to group mapping' event
                ELSE IF (@CurrentEventType = @GroupToGroupMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroupToGroupMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroupToGroupMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'user to application component and acccess level mapping' event
                ELSE IF (@CurrentEventType = @UserToApplicationComponentAndAccessLevelMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddUserToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUserToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'group to application component and acccess level mapping' event
                ELSE IF (@CurrentEventType = @GroupToApplicationComponentAndAccessLevelMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroupToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroupToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'entity type' event
                ELSE IF (@CurrentEventType = @EntityTypeEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddEntityType @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveEntityType @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'entity' event
                ELSE IF (@CurrentEventType = @EntityEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddEntity @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveEntity @CurrentEventData1 ,@CurrentEventData2, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'user to entity mapping' event
                ELSE IF (@CurrentEventType = @UserToEntityMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddUserToEntityMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUserToEntityMapping @CurrentEventData1 ,@CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END

                -- Handle 'group to entity mapping' event
                ELSE IF (@CurrentEventType = @GroupToEntityMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroupToEntityMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroupToEntityMapping @CurrentEventData1 ,@CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime;
                        END

                ELSE
                    BEGIN
                        SET @ErrorMessage = N'Input table column ''EventType'' contained unhandled event type ''' + @CurrentEventType + '''.';
                        THROW 50001, @ErrorMessage, 1;
                    END

            END TRY
            BEGIN CATCH
                SET @ErrorMessage = N'Error occurred processing events; ' + ERROR_MESSAGE();
                THROW 50001, @ErrorMessage, 1;
            END CATCH

            FETCH NEXT 
            FROM        InputTableCursor
            INTO        @CurrentEventType, 
                        @CurrentEventId, 
                        @CurrentEventAction, 
                        @CurrentOccurredTime, 
                        @CurrentEventData1, 
                        @CurrentEventData2, 
                        @CurrentEventData3;
        END;

    CLOSE InputTableCursor;
    DEALLOCATE InputTableCursor;

END
GO


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Update 'SchemaVersions' table
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

INSERT 
INTO    dbo.SchemaVersions
        (
            [Version], 
            Created
        )
VALUES  (
            '2.0.0', 
            GETDATE()
        );
GO