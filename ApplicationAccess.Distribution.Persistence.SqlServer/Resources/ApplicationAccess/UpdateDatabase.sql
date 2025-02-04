--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Update Database
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

-- NOTE: If executing through SQL Server Management Studio, set 'SQKCMD Mode' via the 'Query' menu

:Setvar DatabaseName ApplicationAccess

USE $(DatabaseName);
GO 

--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE TABLE $(DatabaseName).dbo.Actions
(
    Id        bigint         NOT NULL IDENTITY(1,1) PRIMARY KEY,  
    [Action]  nvarchar(450)  NOT NULL 
);

CREATE INDEX ActionsActionIndex ON $(DatabaseName).dbo.Actions ([Action]);

CREATE TABLE $(DatabaseName).dbo.EventIdToUserMap
(
    EventId   uniqueidentifier  NOT NULL PRIMARY KEY, 
    UserId    bigint            NOT NULL, 
    ActionId  bigint            NOT NULL, 
    HashCode  int               NOT NULL 
);

CREATE INDEX EventIdToUserMapEventIdIndex ON $(DatabaseName).dbo.EventIdToUserMap (EventId, HashCode);
CREATE INDEX EventIdToUserMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToUserMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToGroupMap
(
    EventId   uniqueidentifier  NOT NULL PRIMARY KEY, 
    GroupId   bigint            NOT NULL, 
    ActionId  bigint            NOT NULL, 
    HashCode  int               NOT NULL 
);

CREATE INDEX EventIdToGroupMapEventIdIndex ON $(DatabaseName).dbo.EventIdToGroupMap (EventId, HashCode);
CREATE INDEX EventIdToGroupMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToGroupMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToUserToGroupMap
(
    EventId                 uniqueidentifier  NOT NULL PRIMARY KEY, 
    UserToGroupMappingId    bigint            NOT NULL, 
    ActionId                bigint            NOT NULL, 
    HashCode                int               NOT NULL 
);

CREATE INDEX EventIdToUserToGroupMapEventIdIndex ON $(DatabaseName).dbo.EventIdToUserToGroupMap (EventId, HashCode);
CREATE INDEX EventIdToUserToGroupMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToUserToGroupMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToUserToApplicationComponentAndAccessLevelMap
(
    EventId                                              uniqueidentifier  NOT NULL PRIMARY KEY, 
    UserToApplicationComponentAndAccessLevelMappingId    bigint            NOT NULL, 
    ActionId                                             bigint            NOT NULL, 
    HashCode                                             int               NOT NULL 
);

CREATE INDEX EventIdToUserToApplicationComponentAndAccessLevelMapEventIdIndex ON $(DatabaseName).dbo.EventIdToUserToApplicationComponentAndAccessLevelMap (EventId, HashCode);
CREATE INDEX EventIdToUserToApplicationComponentAndAccessLevelMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToUserToApplicationComponentAndAccessLevelMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToGroupToApplicationComponentAndAccessLevelMap
(
    EventId                                              uniqueidentifier  NOT NULL PRIMARY KEY, 
    GroupToApplicationComponentAndAccessLevelMappingId   bigint            NOT NULL, 
    ActionId                                             bigint            NOT NULL, 
    HashCode                                             int               NOT NULL 
);

CREATE INDEX EventIdToGroupToApplicationComponentAndAccessLevelMapEventIdIndex ON $(DatabaseName).dbo.EventIdToGroupToApplicationComponentAndAccessLevelMap (EventId, HashCode);
CREATE INDEX EventIdToGroupToApplicationComponentAndAccessLevelMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToGroupToApplicationComponentAndAccessLevelMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToEntityTypeMap
(
    EventId        uniqueidentifier  NOT NULL PRIMARY KEY, 
    EntityTypeId   bigint            NOT NULL, 
    ActionId       bigint            NOT NULL, 
    HashCode       int               NOT NULL 
);

CREATE INDEX EventIdToEntityTypeMapEventIdIndex ON $(DatabaseName).dbo.EventIdToEntityTypeMap (EventId, HashCode);
CREATE INDEX EventIdToEntityTypeMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToEntityTypeMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToEntityMap
(
    EventId    uniqueidentifier  NOT NULL PRIMARY KEY, 
    EntityId   bigint            NOT NULL, 
    ActionId   bigint            NOT NULL, 
    HashCode   int               NOT NULL 
);

CREATE INDEX EventIdToEntityMapEventIdIndex ON $(DatabaseName).dbo.EventIdToEntityMap (EventId, HashCode);
CREATE INDEX EventIdToEntityMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToEntityMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToUserToEntityMap
(
    EventId                   uniqueidentifier  NOT NULL PRIMARY KEY, 
    UserToEntityMappingId     bigint            NOT NULL, 
    ActionId                  bigint            NOT NULL, 
    HashCode                  int               NOT NULL 
);

CREATE INDEX EventIdToUserToEntityMapEventIdIndex ON $(DatabaseName).dbo.EventIdToUserToEntityMap (EventId, HashCode);
CREATE INDEX EventIdToUserToEntityMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToUserToEntityMap (HashCode);

CREATE TABLE $(DatabaseName).dbo.EventIdToGroupToEntityMap
(
    EventId                   uniqueidentifier  NOT NULL PRIMARY KEY, 
    GroupToEntityMappingId    bigint            NOT NULL, 
    ActionId                  bigint            NOT NULL, 
    HashCode                  int               NOT NULL 
);

CREATE INDEX EventIdToGroupToEntityMapEventIdIndex ON $(DatabaseName).dbo.EventIdToGroupToEntityMap (EventId, HashCode);
CREATE INDEX EventIdToGroupToEntityMapHashCodeIndex ON $(DatabaseName).dbo.EventIdToGroupToEntityMap (HashCode);


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE $(DatabaseName);
GO 

--------------------------------------------------------------------------------
-- dbo.AddUser

ALTER PROCEDURE dbo.AddUser
(
    @User             nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToUserMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserMap
                    (
                        EventId, 
                        UserId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUser

ALTER PROCEDURE dbo.RemoveUser
(
    @User             nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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
        SET @ErrorMessage = N'Error occurred when removing User from Group mappings for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
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

    -- Insert into EventIdToUserMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserMap
                    (
                        EventId, 
                        UserId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroup

ALTER PROCEDURE dbo.AddGroup
(
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToGroupMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToGroupMap
                    (
                        EventId, 
                        GroupId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToGroupMap'' for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroup

ALTER PROCEDURE dbo.RemoveGroup
(
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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

    -- Insert into EventIdToGroupMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToGroupMap
                    (
                        EventId, 
                        GroupId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToGroupMap'' for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddUserToGroupMapping

ALTER PROCEDURE dbo.AddUserToGroupMapping
(
    @User             nvarchar(450),
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToUserToGroupMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserToGroupMap
                    (
                        EventId, 
                        UserToGroupMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserToGroupMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUserToGroupMapping

ALTER PROCEDURE dbo.RemoveUserToGroupMapping
(
    @User             nvarchar(450),
    @Group            nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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

    -- Insert into EventIdToUserToGroupMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserToGroupMap
                    (
                        EventId, 
                        UserToGroupMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserToGroupMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddUserToApplicationComponentAndAccessLevelMapping

ALTER PROCEDURE dbo.AddUserToApplicationComponentAndAccessLevelMapping
(
    @User                  nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2, 
    @HashCode              int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToUserToApplicationComponentAndAccessLevelMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserToApplicationComponentAndAccessLevelMap
                    (
                        EventId, 
                        UserToApplicationComponentAndAccessLevelMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserToApplicationComponentAndAccessLevelMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUserToApplicationComponentAndAccessLevelMapping

ALTER PROCEDURE dbo.RemoveUserToApplicationComponentAndAccessLevelMapping
(
    @User                  nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2, 
    @HashCode              int
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

    -- Insert into EventIdToUserToApplicationComponentAndAccessLevelMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserToApplicationComponentAndAccessLevelMap
                    (
                        EventId, 
                        UserToApplicationComponentAndAccessLevelMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserToApplicationComponentAndAccessLevelMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroupToApplicationComponentAndAccessLevelMapping

ALTER PROCEDURE dbo.AddGroupToApplicationComponentAndAccessLevelMapping
(
    @Group                 nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2, 
    @HashCode              int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToGroupToApplicationComponentAndAccessLevelMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToGroupToApplicationComponentAndAccessLevelMap
                    (
                        EventId, 
                        GroupToApplicationComponentAndAccessLevelMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToGroupToApplicationComponentAndAccessLevelMap'' for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroupToApplicationComponentAndAccessLevelMapping

ALTER PROCEDURE dbo.RemoveGroupToApplicationComponentAndAccessLevelMapping
(
    @Group                 nvarchar(450),
    @ApplicationComponent  nvarchar(450),
    @AccessLevel           nvarchar(450),
    @EventId               uniqueidentifier, 
    @TransactionTime       datetime2, 
    @HashCode              int
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

    -- Insert into EventIdToGroupToApplicationComponentAndAccessLevelMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToGroupToApplicationComponentAndAccessLevelMap
                    (
                        EventId, 
                        GroupToApplicationComponentAndAccessLevelMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToGroupToApplicationComponentAndAccessLevelMap'' for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddEntityType

ALTER PROCEDURE dbo.AddEntityType
(
    @EntityType       nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToEntityTypeMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToEntityTypeMap
                    (
                        EventId, 
                        EntityTypeId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToEntityTypeMap'' for EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveEntityType

ALTER PROCEDURE dbo.RemoveEntityType
(
    @EntityType       nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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

    -- Insert into EventIdToEntityTypeMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToEntityTypeMap
                    (
                        EventId, 
                        EntityTypeId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToEntityTypeMap'' for EntityType ''' + ISNULL(@EntityType, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddEntity

ALTER PROCEDURE dbo.AddEntity
(
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToEntityMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToEntityMap
                    (
                        EventId, 
                        EntityId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToEntityMap'' for Entity ''' + ISNULL(@Entity, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveEntity

ALTER PROCEDURE dbo.RemoveEntity
(
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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

    -- Insert into EventIdToEntityMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToEntityMap
                    (
                        EventId, 
                        EntityId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToEntityMap'' for Entity ''' + ISNULL(@Entity, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION
    
END
GO

--------------------------------------------------------------------------------
-- dbo.AddUserToEntityMapping

ALTER PROCEDURE dbo.AddUserToEntityMapping
(
    @User             nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToUserToEntityMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserToEntityMap
                    (
                        EventId, 
                        UserToEntityMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserToEntityMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveUserToEntityMapping

ALTER PROCEDURE dbo.RemoveUserToEntityMapping
(
    @User             nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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

    -- Insert into EventIdToUserToEntityMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToUserToEntityMap
                    (
                        EventId, 
                        UserToEntityMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToUserToEntityMap'' for User ''' + ISNULL(@User, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.AddGroupToEntityMapping

ALTER PROCEDURE dbo.AddGroupToEntityMapping
(
    @Group            nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
)
AS
BEGIN

    DECLARE @ErrorMessage  nvarchar(max);
    DECLARE @CurrentRowId  bigint;

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

    -- Insert into EventIdToGroupToEntityMap
    SET @CurrentRowId = SCOPE_IDENTITY()
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToGroupToEntityMap
                    (
                        EventId, 
                        GroupToEntityMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'add'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToGroupToEntityMap'' for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.RemoveGroupToEntityMapping

ALTER PROCEDURE dbo.RemoveGroupToEntityMapping
(
    @Group            nvarchar(450),
    @EntityType       nvarchar(450),
    @Entity           nvarchar(450),
    @EventId          uniqueidentifier, 
    @TransactionTime  datetime2, 
    @HashCode         int
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

    -- Insert into EventIdToGroupToEntityMap
    BEGIN TRY
            INSERT 
            INTO    dbo.EventIdToGroupToEntityMap
                    (
                        EventId, 
                        GroupToEntityMappingId, 
                        ActionId, 
                        HashCode
                    )
            VALUES  (
                        @EventId, 
                        @CurrentRowId, 
                        ( 
                            SELECT  Id 
                            FROM    dbo.Actions 
                            WHERE   [Action] = 'remove'
                        ), 
                        @HashCode
                    );
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION
        SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToGroupToEntityMap'' for Group ''' + ISNULL(@Group, '(null)') + '''; ' + ERROR_MESSAGE();
        THROW 50001, @ErrorMessage, 1;
    END CATCH

    COMMIT TRANSACTION

END
GO

--------------------------------------------------------------------------------
-- dbo.ProcessEvents

ALTER PROCEDURE dbo.ProcessEvents
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
    DECLARE @CurrentHashCode      int; 
    DECLARE @CurrentEventData1    nvarchar(max);
    DECLARE @CurrentEventData2    nvarchar(max);
    DECLARE @CurrentEventData3    nvarchar(max);
    DECLARE @ExistingEventId      uniqueidentifier;

    DECLARE InputTableCursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT  EventType, 
            EventId, 
            EventAction, 
            OccurredTime, 
            HashCode, 
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
                @CurrentHashCode, 
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
                                @CurrentHashCode, 
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
                            EXEC AddUser @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUser @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'group' event
                ELSE IF (@CurrentEventType = @GroupEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroup @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroup @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'user to group mapping' event
                ELSE IF (@CurrentEventType = @UserToGroupMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddUserToGroupMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUserToGroupMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
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
                            EXEC AddUserToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUserToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'group to application component and acccess level mapping' event
                ELSE IF (@CurrentEventType = @GroupToApplicationComponentAndAccessLevelMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroupToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroupToApplicationComponentAndAccessLevelMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'entity type' event
                ELSE IF (@CurrentEventType = @EntityTypeEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddEntityType @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveEntityType @CurrentEventData1, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'entity' event
                ELSE IF (@CurrentEventType = @EntityEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddEntity @CurrentEventData1, @CurrentEventData2, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveEntity @CurrentEventData1 ,@CurrentEventData2, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'user to entity mapping' event
                ELSE IF (@CurrentEventType = @UserToEntityMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddUserToEntityMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveUserToEntityMapping @CurrentEventData1 ,@CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END

                -- Handle 'group to entity mapping' event
                ELSE IF (@CurrentEventType = @GroupToEntityMappingEventTypeValue)
                    IF (@CurrentEventAction = @AddEventActionValue)
                        BEGIN
                            EXEC AddGroupToEntityMapping @CurrentEventData1, @CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
                        END
                    ELSE
                        BEGIN
                            EXEC RemoveGroupToEntityMapping @CurrentEventData1 ,@CurrentEventData2, @CurrentEventData3, @CurrentEventId, @CurrentOccurredTime, @CurrentHashCode;
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
                        @CurrentHashCode, 
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
-- Insert into the 'Actions' table
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

INSERT 
INTO    dbo.Actions
        (
            [Action]
        )
VALUES  (
            'add'
        );
GO

INSERT 
INTO    dbo.Actions
        (
            [Action]
        )
VALUES  (
            'remove'
        );
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
            '1.0.0 HashCode', 
            GETDATE()
        );
GO


