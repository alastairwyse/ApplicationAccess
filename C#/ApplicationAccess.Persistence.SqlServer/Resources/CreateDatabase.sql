-- TODO:
--   See if database name can be parameterized


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Database
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE DATABASE ApplicationAccess;


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE TABLE ApplicationAccess.dbo.EventIdToTransactionTimeMap
(
    EventId          uniqueidentifier  NOT NULL PRIMARY KEY, 
    TransactionTime  datetime2         NOT NULL
);

CREATE INDEX EventIdToTransactionTimeMapEventIdIndex ON ApplicationAccess.dbo.EventIdToTransactionTimeMap (EventId);
CREATE INDEX EventIdToTransactionTimeMapTransactionTimeIndex ON ApplicationAccess.dbo.EventIdToTransactionTimeMap (TransactionTime);

CREATE TABLE ApplicationAccess.dbo.Users
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    [User]           nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX UsersUserIndex ON ApplicationAccess.dbo.Users ([User], TransactionTo);
CREATE INDEX UsersTransactionIndex ON ApplicationAccess.dbo.Users (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.Groups
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    [Group]          nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX GroupsGroupIndex ON ApplicationAccess.dbo.Groups ([Group], TransactionTo);
CREATE INDEX GroupsTransactionIndex ON ApplicationAccess.dbo.Groups (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.UserToGroupMappings
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

CREATE TABLE ApplicationAccess.dbo.GroupToGroupMappings
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

CREATE TABLE ApplicationAccess.dbo.ApplicationComponents
(
    Id                    bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    ApplicationComponent  nvarchar(450)  NOT NULL, 
    TransactionFrom       datetime2      NOT NULL, 
    TransactionTo         datetime2      NOT NULL
);

CREATE INDEX ApplicationComponentsApplicationComponentIndex ON ApplicationAccess.dbo.ApplicationComponents (ApplicationComponent, TransactionTo);
CREATE INDEX ApplicationComponentsTransactionIndex ON ApplicationAccess.dbo.ApplicationComponents (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.AccessLevels
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    AccessLevel      nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
);

CREATE INDEX AccessLevelsAccessLevelIndex ON ApplicationAccess.dbo.AccessLevels (AccessLevel, TransactionTo);
CREATE INDEX AccessLevelsTransactionIndex ON ApplicationAccess.dbo.AccessLevels (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.UserToApplicationComponentAndAccessLevelMappings
(
    Id                      bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    UserId                  bigint     NOT NULL, 
    ApplicationComponentId  bigint     NOT NULL, 
    AccessLevelId           bigint     NOT NULL, 
    TransactionFrom         datetime2  NOT NULL, 
    TransactionTo           datetime2  NOT NULL
)

CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsUserIndex ON ApplicationAccess.dbo.UserToApplicationComponentAndAccessLevelMappings (UserId, ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsTransactionIndex ON ApplicationAccess.dbo.UserToApplicationComponentAndAccessLevelMappings (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.GroupToApplicationComponentAndAccessLevelMappings
(
    Id                      bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    GroupId                 bigint     NOT NULL, 
    ApplicationComponentId  bigint     NOT NULL, 
    AccessLevelId           bigint     NOT NULL, 
    TransactionFrom         datetime2  NOT NULL, 
    TransactionTo           datetime2  NOT NULL
)

CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsGroupIndex ON ApplicationAccess.dbo.GroupToApplicationComponentAndAccessLevelMappings (GroupId, ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsTransactionIndex ON ApplicationAccess.dbo.GroupToApplicationComponentAndAccessLevelMappings (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.EntityTypes
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    EntityType       nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
)

CREATE INDEX EntityTypesEntityTypeIndex ON ApplicationAccess.dbo.EntityTypes (EntityType, TransactionTo);
CREATE INDEX EntityTypesTransactionIndex ON ApplicationAccess.dbo.EntityTypes (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.Entities
(
    Id               bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    EntityTypeId     bigint         NOT NULL, 
    Entity           nvarchar(450)  NOT NULL, 
    TransactionFrom  datetime2      NOT NULL, 
    TransactionTo    datetime2      NOT NULL
)

CREATE INDEX EntitiesEntityIndex ON ApplicationAccess.dbo.Entities (EntityTypeId, Entity, TransactionTo);
CREATE INDEX EntitiesTransactionIndex ON ApplicationAccess.dbo.Entities (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.UserToEntityMappings
(
    Id               bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    UserId           bigint     NOT NULL, 
    EntityTypeId     bigint     NOT NULL, 
    EntityId         bigint     NOT NULL, 
    TransactionFrom  datetime2  NOT NULL, 
    TransactionTo    datetime2  NOT NULL
)

CREATE INDEX UserToEntityMappingsUserIndex ON ApplicationAccess.dbo.UserToEntityMappings (UserId, EntityTypeId, EntityId, TransactionTo);
CREATE INDEX UserToEntityMappingsEntityIndex ON ApplicationAccess.dbo.UserToEntityMappings (EntityTypeId, EntityId, TransactionTo);
CREATE INDEX UserToEntityMappingsTransactionIndex ON ApplicationAccess.dbo.UserToEntityMappings (TransactionFrom, TransactionTo);

CREATE TABLE ApplicationAccess.dbo.GroupToEntityMappings
(
    Id               bigint     NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    GroupId          bigint     NOT NULL, 
    EntityTypeId     bigint     NOT NULL, 
    EntityId         bigint     NOT NULL, 
    TransactionFrom  datetime2  NOT NULL, 
    TransactionTo    datetime2  NOT NULL
)

CREATE INDEX GroupToEntityMappingsUserIndex ON ApplicationAccess.dbo.GroupToEntityMappings (GroupId, EntityTypeId, EntityId, TransactionTo);
CREATE INDEX GroupToEntityMappingsEntityIndex ON ApplicationAccess.dbo.GroupToEntityMappings (EntityTypeId, EntityId, TransactionTo);
CREATE INDEX GroupToEntityMappingsTransactionIndex ON ApplicationAccess.dbo.GroupToEntityMappings (TransactionFrom, TransactionTo);


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE ApplicationAccess
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

    DECLARE @LastTransactionTime  datetime2;
    DECLARE @ErrorMessage         nvarchar(max);

    -- Check that the transaction time is greater than or equal to the last
    SELECT  @LastTransactionTime = MAX(TransactionTime)
    FROM    EventIdToTransactionTimeMap;

    IF (@LastTransactionTime IS NULL)
      SET @LastTransactionTime = CONVERT(datetime2, '0001-01-01T00:00:00.0000000', 126);;

    IF (@TransactionTime < @LastTransactionTime)
    BEGIN
        SET @ErrorMessage = N'Parameter ''TransactionTime'' with value ' + CONVERT(nvarchar, @TransactionTime, 126) + ' must be greater than or equal to last transaction time ' + CONVERT(nvarchar, @LastTransactionTime, 126) + '.';
        THROW 50001, @ErrorMessage, 1;
    END

    -- Insert the event id and timestamp for the transaction
    BEGIN TRY
        INSERT  
        INTO    dbo.EventIdToTransactionTimeMap
                (
                    EventId, 
                    TransactionTime
                )
        VALUES  (
                    @EventId, 
                    @TransactionTime
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

    SELECT  @CurrentRowId = Id 
    FROM    dbo.Users 
    WHERE   [User] = @User 
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No Users row exists for User ''' + ISNULL(@User, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
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

    SELECT  @CurrentRowId = Id 
    FROM    dbo.Groups 
    WHERE   [Group] = @Group 
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No Groups row exists for Group ''' + ISNULL(@Group, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
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
        WHERE   GroupId = @Group 
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
        WHERE   GroupId = @Group 
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
            FROM    ApplicationComponents 
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
            FROM    AccessLevels 
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
            FROM    ApplicationComponents 
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
            FROM    AccessLevels 
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

    SELECT  @CurrentRowId = Id 
    FROM    dbo.EntityTypes 
    WHERE   EntityType = @EntityType
      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (@CurrentRowId IS NULL)
    BEGIN
        SET @ErrorMessage = N'No EntityTypes row exists for EntityType ''' + ISNULL(@EntityType, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
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

    -- Invalidate any UserToEntityMappings rows
    BEGIN TRY
        UPDATE  dbo.UserToEntityMappings
        SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
        WHERE   EntityTypeId = @CurrentRowId
          AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when removing User to Entity mappings for EntityType''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
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
        SET @ErrorMessage = N'Error occurred when removing Group to Entity mappings for EntityType''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
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
        SET @ErrorMessage = N'Error occurred when removing Entities for EntityType''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
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
        SET @ErrorMessage = N'No Entities row exists for EntityType ''' + ISNULL(@EntityType, '(null)') + ''', Entity ''' + ISNULL(@Entity, '(null)') + ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
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
