--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Database
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

TODO

--CREATE DATABASE ApplicationAccess;
--GO

--USE ApplicationAccess;
--GO 


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE TABLE public.EventIdToTransactionTimeMap
(
    EventId          uuid         NOT NULL PRIMARY KEY, 
    TransactionTime  timestamptz  NOT NULL
);

CREATE INDEX EventIdToTransactionTimeMapEventIdIndex ON public.EventIdToTransactionTimeMap (EventId);
CREATE INDEX EventIdToTransactionTimeMapTransactionTimeIndex ON public.EventIdToTransactionTimeMap (TransactionTime);

CREATE TABLE public.Users
(
    Id               bigserial      NOT NULL PRIMARY KEY,  
    "User"           varchar(450)   NOT NULL, 
    TransactionFrom  timestamptz    NOT NULL, 
    TransactionTo    timestamptz    NOT NULL
);

CREATE INDEX UsersUserIndex ON public.Users ("User", TransactionTo);
CREATE INDEX UsersTransactionIndex ON public.Users (TransactionFrom, TransactionTo);

CREATE TABLE public.Groups
(
    Id               bigserial      NOT NULL PRIMARY KEY,  
    "Group"          varchar(450)   NOT NULL, 
    TransactionFrom  timestamptz    NOT NULL, 
    TransactionTo    timestamptz    NOT NULL
);

CREATE INDEX GroupsGroupIndex ON public.Groups ("Group", TransactionTo);
CREATE INDEX GroupsTransactionIndex ON public.Groups (TransactionFrom, TransactionTo);

CREATE TABLE public.UserToGroupMappings
(
    Id               bigserial    NOT NULL PRIMARY KEY,  
    UserId           bigint       NOT NULL, 
    GroupId          bigint       NOT NULL, 
    TransactionFrom  timestamptz  NOT NULL, 
    TransactionTo    timestamptz  NOT NULL
);

CREATE INDEX UserToGroupMappingsUserIndex ON UserToGroupMappings (UserId, TransactionTo);
CREATE INDEX UserToGroupMappingsGroupIndex ON UserToGroupMappings (GroupId, TransactionTo);
CREATE INDEX UserToGroupMappingsTransactionIndex ON UserToGroupMappings (TransactionFrom, TransactionTo);

CREATE TABLE public.GroupToGroupMappings
(
    Id               bigserial    NOT NULL PRIMARY KEY,  
    FromGroupId      bigint       NOT NULL, 
    ToGroupId        bigint       NOT NULL, 
    TransactionFrom  timestamptz  NOT NULL, 
    TransactionTo    timestamptz  NOT NULL
);

CREATE INDEX GroupToGroupMappingsFromGroupIndex ON GroupToGroupMappings (FromGroupId, TransactionTo);
CREATE INDEX GroupToGroupMappingsToGroupIndex ON GroupToGroupMappings (ToGroupId, TransactionTo);
CREATE INDEX GroupToGroupMappingsTransactionIndex ON GroupToGroupMappings (TransactionFrom, TransactionTo);

CREATE TABLE public.ApplicationComponents
(
    Id                    bigserial      NOT NULL PRIMARY KEY,  
    ApplicationComponent  varchar(450)   NOT NULL, 
    TransactionFrom       timestamptz    NOT NULL, 
    TransactionTo         timestamptz    NOT NULL
);

CREATE INDEX ApplicationComponentsApplicationComponentIndex ON public.ApplicationComponents (ApplicationComponent, TransactionTo);
CREATE INDEX ApplicationComponentsTransactionIndex ON public.ApplicationComponents (TransactionFrom, TransactionTo);

CREATE TABLE public.AccessLevels
(
    Id               bigserial      NOT NULL PRIMARY KEY,  
    AccessLevel      varchar(450)   NOT NULL, 
    TransactionFrom  timestamptz    NOT NULL, 
    TransactionTo    timestamptz    NOT NULL
);

CREATE INDEX AccessLevelsAccessLevelIndex ON public.AccessLevels (AccessLevel, TransactionTo);
CREATE INDEX AccessLevelsTransactionIndex ON public.AccessLevels (TransactionFrom, TransactionTo);

CREATE TABLE public.UserToApplicationComponentAndAccessLevelMappings
(
    Id                      bigserial    NOT NULL PRIMARY KEY,  
    UserId                  bigint       NOT NULL, 
    ApplicationComponentId  bigint       NOT NULL, 
    AccessLevelId           bigint       NOT NULL, 
    TransactionFrom         timestamptz  NOT NULL, 
    TransactionTo           timestamptz  NOT NULL
);

CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsUserIndex ON public.UserToApplicationComponentAndAccessLevelMappings (UserId, ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX UserToApplicationComponentAndAccessLevelMappingsTransIndex ON public.UserToApplicationComponentAndAccessLevelMappings (TransactionFrom, TransactionTo);

CREATE TABLE public.GroupToApplicationComponentAndAccessLevelMappings
(
    Id                      bigserial    NOT NULL PRIMARY KEY,  
    GroupId                 bigint       NOT NULL, 
    ApplicationComponentId  bigint       NOT NULL, 
    AccessLevelId           bigint       NOT NULL, 
    TransactionFrom         timestamptz  NOT NULL, 
    TransactionTo           timestamptz  NOT NULL
);

CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsGroupIndex ON public.GroupToApplicationComponentAndAccessLevelMappings (GroupId, ApplicationComponentId, AccessLevelId, TransactionTo);
CREATE INDEX GroupToApplicationComponentAndAccessLevelMappingsTransIndex ON public.GroupToApplicationComponentAndAccessLevelMappings (TransactionFrom, TransactionTo);

CREATE TABLE public.EntityTypes
(
    Id               bigserial        NOT NULL PRIMARY KEY,  
    EntityType       varchar(450)     NOT NULL, 
    TransactionFrom  timestamptz      NOT NULL, 
    TransactionTo    timestamptz      NOT NULL
);

CREATE INDEX EntityTypesEntityTypeIndex ON public.EntityTypes (EntityType, TransactionTo);
CREATE INDEX EntityTypesTransactionIndex ON public.EntityTypes (TransactionFrom, TransactionTo);

CREATE TABLE public.Entities
(
    Id               bigserial        NOT NULL PRIMARY KEY,  
    EntityTypeId     bigint           NOT NULL, 
    Entity           varchar(450)     NOT NULL, 
    TransactionFrom  timestamptz      NOT NULL, 
    TransactionTo    timestamptz      NOT NULL
);

CREATE INDEX EntitiesEntityIndex ON public.Entities (EntityTypeId, Entity, TransactionTo);
CREATE INDEX EntitiesTransactionIndex ON public.Entities (TransactionFrom, TransactionTo);

CREATE TABLE public.UserToEntityMappings
(
    Id               bigserial    NOT NULL PRIMARY KEY,  
    UserId           bigint       NOT NULL, 
    EntityTypeId     bigint       NOT NULL, 
    EntityId         bigint       NOT NULL, 
    TransactionFrom  timestamptz  NOT NULL, 
    TransactionTo    timestamptz  NOT NULL
);

CREATE INDEX UserToEntityMappingsUserIndex ON public.UserToEntityMappings (UserId, EntityTypeId, EntityId, TransactionTo);
CREATE INDEX UserToEntityMappingsEntityIndex ON public.UserToEntityMappings (EntityTypeId, EntityId, TransactionTo);
CREATE INDEX UserToEntityMappingsTransactionIndex ON public.UserToEntityMappings (TransactionFrom, TransactionTo);

CREATE TABLE public.GroupToEntityMappings
(
    Id               bigserial    NOT NULL PRIMARY KEY,  
    GroupId          bigint       NOT NULL, 
    EntityTypeId     bigint       NOT NULL, 
    EntityId         bigint       NOT NULL, 
    TransactionFrom  timestamptz  NOT NULL, 
    TransactionTo    timestamptz  NOT NULL
);

CREATE INDEX GroupToEntityMappingsGroupIndex ON public.GroupToEntityMappings (GroupId, EntityTypeId, EntityId, TransactionTo);
CREATE INDEX GroupToEntityMappingsEntityIndex ON public.GroupToEntityMappings (EntityTypeId, EntityId, TransactionTo);
CREATE INDEX GroupToEntityMappingsTransactionIndex ON public.GroupToEntityMappings (TransactionFrom, TransactionTo);

CREATE TABLE public.SchemaVersions
(
    Id         bigserial     NOT NULL PRIMARY KEY,  
    Version    varchar(20)   NOT NULL, 
    Created    timestamptz   NOT NULL 
);


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create User-defined Types
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

CREATE TYPE EventTableType 
AS 
(
    Id            bigint,  
    EventType     varchar, 
    EventId       uuid, 
    EventAction   varchar, 
    OccurredTime  timestamptz, 
    EventData1    varchar, 
    EventData2    varchar, 
    EventData3    varchar
);


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Create Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

--------------------------------------------------------------------------------
-- GetTemporalMaxDate

CREATE FUNCTION GetTemporalMaxDate() 
RETURNS timestamptz
AS 
$$
    SELECT  TO_TIMESTAMP('9999-12-31 23:59:59.999999+00', 'YYYY-MM-DD HH24:MI:ss.USTZH')  AS timestamptz;

$$ LANGUAGE SQL;

--------------------------------------------------------------------------------
-- SubtractTemporalMinimumTimeUnit

CREATE FUNCTION SubtractTemporalMinimumTimeUnit
(
    InputTime  timestamptz
)
RETURNS timestamptz
AS
$$
BEGIN
    RETURN InputTime - interval '1 microsecond';
END
$$ LANGUAGE plpgsql;

--------------------------------------------------------------------------------
-- CreateEvent

CREATE PROCEDURE CreateEvent
(
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    LastTransactionTime  timestamptz;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    -- Check that the transaction time is greater than or equal to the last
    SELECT  MAX(EventIdToTransactionTimeMap.TransactionTime)
    INTO    LastTransactionTime
    FROM    EventIdToTransactionTimeMap;

    IF (LastTransactionTime IS NULL) THEN
        LastTransactionTime := TO_TIMESTAMP('0001-01-01 00:00:00.999999+00', 'YYYY-MM-DD HH24:MI:ss.USTZH') AS timestamptz;
    END IF;

    IF (TransactionTime < LastTransactionTime) THEN
        RAISE EXCEPTION 'Parameter ''TransactionTime'' with value ''%'' must be greater than or equal to last transaction time ''%''.', TO_CHAR(TransactionTime, TimeStampCharFormat), TO_CHAR(LastTransactionTime, TimeStampCharFormat) 
        USING ERRCODE = 'invalid_parameter_value';
    END IF;
    
    BEGIN
        INSERT  
        INTO    EventIdToTransactionTimeMap
                (
                    EventId, 
                    TransactionTime 
                )
        VALUES  (
                    EventId, 
                    TransactionTime
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting into table ''EventIdToTransactionTimeMap''; %', SQLERRM;
    END;

END 
$$;

--------------------------------------------------------------------------------
-- AddUser

CREATE PROCEDURE AddUser
(
    "User"           varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Insert the new row
    BEGIN
        INSERT  
        INTO    Users 
                (
                    "User", 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    "User", 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting User ''%''; %', COALESCE("User", '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveUser

CREATE PROCEDURE RemoveUser
(
    "User"           varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    LOCK TABLE public.Users IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.UserToGroupMappings IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.UserToApplicationComponentAndAccessLevelMappings IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.UserToEntityMappings IN ACCESS EXCLUSIVE MODE;

    SELECT  Id 
    INTO    CurrentRowId
    FROM    Users u
    WHERE   u."User" = $1 
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
      
    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No Users row exists for User ''%'' and for transaction time ''%''.', COALESCE("User", '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Invalidate any UserToGroupMappings rows
    BEGIN
        UPDATE  UserToGroupMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   UserId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Group mappings for User ''%''; %', COALESCE("User", '(null)'), SQLERRM;
    END;

    -- Invalidate any UserToApplicationComponentAndAccessLevelMappings rows
    BEGIN
        UPDATE  UserToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   UserId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to ApplicationComponent and AccessLevel mappings for User ''%''; %', COALESCE("User", '(null)'), SQLERRM;
    END;

    -- Invalidate any UserToEntityMappings rows
    BEGIN
        UPDATE  UserToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   UserId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Entity mappings for User ''%''; %', COALESCE("User", '(null)'), SQLERRM;
    END;
    
    BEGIN
        UPDATE  Users 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User ''%''; %', COALESCE("User", '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddGroup

CREATE PROCEDURE AddGroup
(
    "Group"          varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Insert the new row
    BEGIN
        INSERT  
        INTO    Groups 
                (
                    "Group", 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    "Group", 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting Group ''%''; %', COALESCE("Group", '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveGroup

CREATE PROCEDURE RemoveGroup
(
    "Group"          varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    LOCK TABLE public.Groups IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.UserToGroupMappings IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.GroupToGroupMappings IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.GroupToApplicationComponentAndAccessLevelMappings IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.GroupToEntityMappings IN ACCESS EXCLUSIVE MODE;

    SELECT  Id 
    INTO    CurrentRowId
    FROM    Groups g
    WHERE   g."Group" = $1 
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
      
    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No Groups row exists for Group ''%'' and for transaction time ''%''.', COALESCE("Group", '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Invalidate any UserToGroupMappings rows
    BEGIN
        UPDATE  UserToGroupMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   GroupId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Group mappings for Group ''%''; %', COALESCE("Group", '(null)'), SQLERRM;
    END;

    -- Invalidate any GroupToGroupMappings rows
    BEGIN
        UPDATE  GroupToGroupMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   (
                    FromGroupIdGroupId = CurrentRowId 
                    OR
                    ToGroupId = CurrentRowId 
                )
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to Group mappings for Group ''%''; %', COALESCE("Group", '(null)'), SQLERRM;
    END;

    -- Invalidate any GroupToApplicationComponentAndAccessLevelMappings rows
    BEGIN
        UPDATE  GroupToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   GroupId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to ApplicationComponent and AccessLevel mappings for Group ''%''; %', COALESCE("Group", '(null)'), SQLERRM;
    END;

    -- Invalidate any GroupToEntityMappings rows
    BEGIN
        UPDATE  GroupToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   GroupId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to Entity mappings for Group ''%''; %', COALESCE("Group", '(null)'), SQLERRM;
    END;
    
    BEGIN
        UPDATE  Groups 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group ''%''; %', COALESCE("User", '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddUserToGroupMapping

CREATE PROCEDURE AddUserToGroupMapping
(
    "User"           varchar, 
    "Group"          varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Insert the new row
    BEGIN
        INSERT  
        INTO    UserToGroupMappings 
                (
                    UserId, 
                    GroupId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    (
                        SELECT  Id 
                        FROM    Users u
                        WHERE   u."User" = $1 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    (
                        SELECT  Id 
                        FROM    Groups g
                        WHERE   g."Group" = $2 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting User to Group mapping between ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE("Group", '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveUserToGroupMapping

CREATE PROCEDURE RemoveUserToGroupMapping
(
    "User"           varchar, 
    "Group"          varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    SELECT  Id 
    INTO    CurrentRowId
    FROM    UserToGroupMappings
    WHERE   UserId = 
            (
                SELECT  Id 
                FROM    Users u
                WHERE   u."User" = $1 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   GroupId = 
            (
                SELECT  Id 
                FROM    Groups g
                WHERE   g."Group" = $2 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
      
    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No UserToGroupMappings row exists for User ''%'', Group ''%'' and for transaction time ''%''.', COALESCE("User", '(null)'), COALESCE("Group", '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        UPDATE  UserToGroupMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Group mapping for ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE("Group", '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddGroupToGroupMapping

CREATE PROCEDURE AddGroupToGroupMapping
(
    FromGroup        varchar, 
    ToGroup          varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Insert the new row
    BEGIN
        INSERT  
        INTO    GroupToGroupMappings 
                (
                    FromGroupId, 
                    ToGroupId, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    (
                        SELECT  Id 
                        FROM    Groups g
                        WHERE   g."Group" = FromGroup
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    (
                        SELECT  Id 
                        FROM    Groups g
                        WHERE   g."Group" = ToGroup
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting Group to Group mapping between ''%'' and ''%''; %', COALESCE(FromGroup, '(null)'), COALESCE(ToGroup, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveGroupToGroupMapping

CREATE PROCEDURE RemoveGroupToGroupMapping
(
    FromGroup        varchar, 
    ToGroup          varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    SELECT  Id 
    INTO    CurrentRowId
    FROM    GroupToGroupMappings
    WHERE   FromGroupId = 
            (
                SELECT  Id 
                FROM    Groups g
                WHERE   g."Group" = FromGroup 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   ToGroupId = 
            (
                SELECT  Id 
                FROM    Groups g
                WHERE   g."Group" = ToGroup 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
  
    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No GroupToGroupMappings row exists for FromGroup  ''%'', ToGroup  ''%'' and for transaction time ''%''.', COALESCE("User", '(null)'), COALESCE("Group", '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        UPDATE  GroupToGroupMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to Group mapping for ''%'' and ''%''; %', COALESCE(FromGroup, '(null)'), COALESCE(ToGroup, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddUserToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE AddUserToApplicationComponentAndAccessLevelMapping
(
    "User"                varchar, 
    ApplicationComponent  varchar, 
    AccessLevel           varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    ApplicationComponentId bigint;
    AccessLevelId bigint;
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        INSERT  
        INTO    UserToApplicationComponentAndAccessLevelMappings 
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
                        FROM    Users u
                        WHERE   u."User" = $1 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    (
                        SELECT  Id 
                        FROM    ApplicationComponents ac
                        WHERE   ac.ApplicationComponent = $2
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    (
                        SELECT  Id 
                        FROM    AccessLevels al
                        WHERE   al.AccessLevel = $3
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN not_null_violation THEN
            -- Insert failed due to 'not_null_violation' error
            --   Need to ensure ApplicationComponent and AccessLevel exist

            SELECT  Id 
            INTO    ApplicationComponentId 
            FROM    ApplicationComponents ac
            WHERE   ac.ApplicationComponent = $2
              AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;

            IF (ApplicationComponentId IS NULL) THEN
                BEGIN
                    -- Insert ApplicationComponent
                    INSERT  
                    INTO    ApplicationComponents 
                            (
                                ApplicationComponent, 
                                TransactionFrom, 
                                TransactionTo 
                            )
                    VALUES  (
                                $2, 
                                TransactionTime, 
                                GetTemporalMaxDate()
                            );
                EXCEPTION
                    WHEN OTHERS THEN
                        RAISE EXCEPTION 'Error occurred when inserting ApplicationComponent ''%''; %', COALESCE(ApplicationComponent, '(null)'), SQLERRM;
                END;
            END IF;

            SELECT  Id 
            INTO    AccessLevelId 
            FROM    AccessLevels al
            WHERE   al.AccessLevel = $3
              AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;

            IF (AccessLevelId IS NULL) THEN
                BEGIN
                    -- Insert AccessLevel
                    INSERT  
                    INTO    AccessLevels 
                            (
                                AccessLevel, 
                                TransactionFrom, 
                                TransactionTo 
                            )
                    VALUES  (
                                $3, 
                                TransactionTime, 
                                GetTemporalMaxDate()
                            );
                EXCEPTION
                    WHEN OTHERS THEN
                        RAISE EXCEPTION 'Error occurred when inserting AccessLevel ''%''; %', COALESCE(AccessLevel, '(null)'), SQLERRM;
                END;
            END IF;

            -- Repeat the original insert
            BEGIN
                INSERT  
                INTO    UserToApplicationComponentAndAccessLevelMappings 
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
                                FROM    Users u
                                WHERE   u."User" = $1 
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                            ), 
                            (
                                SELECT  Id 
                                FROM    ApplicationComponents ac
                                WHERE   ac.ApplicationComponent = $2
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                            ), 
                            (
                                SELECT  Id 
                                FROM    AccessLevels al
                                WHERE   al.AccessLevel = $3
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                            ), 
                            TransactionTime, 
                            GetTemporalMaxDate()
                        );
            EXCEPTION
                WHEN OTHERS THEN    
                    RAISE EXCEPTION 'Error occurred when inserting User to ApplicationComponent and AccessLevel mapping between ''%'', ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), SQLERRM;
            END;

        WHEN OTHERS THEN
            -- I.e. other exception in original INSERT
            RAISE EXCEPTION 'Error occurred when inserting User to ApplicationComponent and AccessLevel mapping between ''%'', ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveUserToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE RemoveUserToApplicationComponentAndAccessLevelMapping
(
    "User"                varchar, 
    ApplicationComponent  varchar, 
    AccessLevel           varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    SELECT  Id 
    INTO    CurrentRowId
    FROM    UserToApplicationComponentAndAccessLevelMappings 
    WHERE   UserId = 
            (
                SELECT  Id 
                FROM    Users u 
                WHERE   u."User" = $1
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    ApplicationComponentId = 
            (
                SELECT  Id 
                FROM    ApplicationComponents ac
                WHERE   ac.ApplicationComponent = $2
                    AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    AccessLevelId = 
            (
                SELECT  Id 
                FROM    AccessLevels al
                WHERE   al.AccessLevel = $3
                    AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;


    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No UserToApplicationComponentAndAccessLevelMappings row exists for User ''%'', ApplicationComponent ''%'', AccessLevel ''%'' and for transaction time ''%''.', COALESCE("User", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        UPDATE  UserToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to ApplicationComponent and AccessLevel mapping for ''%'', ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddGroupToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE AddGroupToApplicationComponentAndAccessLevelMapping
(
    "Group"               varchar, 
    ApplicationComponent  varchar, 
    AccessLevel           varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    ApplicationComponentId bigint;
    AccessLevelId bigint;
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        INSERT  
        INTO    GroupToApplicationComponentAndAccessLevelMappings 
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
                        FROM    Groups g
                        WHERE   g."Group" = $1 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    (
                        SELECT  Id 
                        FROM    ApplicationComponents ac
                        WHERE   ac.ApplicationComponent = $2
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    (
                        SELECT  Id 
                        FROM    AccessLevels al
                        WHERE   al.AccessLevel = $3
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN not_null_violation THEN
            -- Insert failed due to 'not_null_violation' error
            --   Need to ensure ApplicationComponent and AccessLevel exist

            SELECT  Id 
            INTO    ApplicationComponentId 
            FROM    ApplicationComponents ac
            WHERE   ac.ApplicationComponent = $2
              AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;

            IF (ApplicationComponentId IS NULL) THEN
                BEGIN
                    -- Insert ApplicationComponent
                    INSERT  
                    INTO    ApplicationComponents 
                            (
                                ApplicationComponent, 
                                TransactionFrom, 
                                TransactionTo 
                            )
                    VALUES  (
                                $2, 
                                TransactionTime, 
                                GetTemporalMaxDate()
                            );
                EXCEPTION
                    WHEN OTHERS THEN
                        RAISE EXCEPTION 'Error occurred when inserting ApplicationComponent ''%''; %', COALESCE(ApplicationComponent, '(null)'), SQLERRM;
                END;
            END IF;

            SELECT  Id 
            INTO    AccessLevelId 
            FROM    AccessLevels al
            WHERE   al.AccessLevel = $3
              AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;

            IF (AccessLevelId IS NULL) THEN
                BEGIN
                    -- Insert AccessLevel
                    INSERT  
                    INTO    AccessLevels 
                            (
                                AccessLevel, 
                                TransactionFrom, 
                                TransactionTo 
                            )
                    VALUES  (
                                $3, 
                                TransactionTime, 
                                GetTemporalMaxDate()
                            );
                EXCEPTION
                    WHEN OTHERS THEN
                        RAISE EXCEPTION 'Error occurred when inserting AccessLevel ''%''; %', COALESCE(AccessLevel, '(null)'), SQLERRM;
                END;
            END IF;

            -- Repeat the original insert
            BEGIN
                INSERT  
                INTO    GroupToApplicationComponentAndAccessLevelMappings 
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
                                FROM    Groups g 
                                WHERE   g."Group" = $1 
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                            ), 
                            (
                                SELECT  Id 
                                FROM    ApplicationComponents ac
                                WHERE   ac.ApplicationComponent = $2
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                            ), 
                            (
                                SELECT  Id 
                                FROM    AccessLevels al
                                WHERE   al.AccessLevel = $3
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                            ), 
                            TransactionTime, 
                            GetTemporalMaxDate()
                        );
            EXCEPTION
                WHEN OTHERS THEN    
                    RAISE EXCEPTION 'Error occurred when inserting Group to ApplicationComponent and AccessLevel mapping between ''%'', ''%'' and ''%''; %', COALESCE("Group", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), SQLERRM;
            END;

        WHEN OTHERS THEN
            -- I.e. other exception in original INSERT
            RAISE EXCEPTION 'Error occurred when inserting Group to ApplicationComponent and AccessLevel mapping between ''%'', ''%'' and ''%''; %', COALESCE("Group", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveGroupToApplicationComponentAndAccessLevelMapping

CREATE PROCEDURE RemoveGroupToApplicationComponentAndAccessLevelMapping
(
    "Group"               varchar, 
    ApplicationComponent  varchar, 
    AccessLevel           varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    SELECT  Id 
    INTO    CurrentRowId
    FROM    GroupToApplicationComponentAndAccessLevelMappings 
    WHERE   GroupId = 
            (
                SELECT  Id 
                FROM    Groups g 
                WHERE   g."Group" = $1
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    ApplicationComponentId = 
            (
                SELECT  Id 
                FROM    ApplicationComponents ac
                WHERE   ac.ApplicationComponent = $2
                    AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND    AccessLevelId = 
            (
                SELECT  Id 
                FROM    AccessLevels al
                WHERE   al.AccessLevel = $3
                    AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;


    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No GroupToApplicationComponentAndAccessLevelMappings row exists for Group ''%'', ApplicationComponent ''%'', AccessLevel ''%'' and for transaction time ''%''.', COALESCE("Group", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        UPDATE  GroupToApplicationComponentAndAccessLevelMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to ApplicationComponent and AccessLevel mapping for ''%'', ''%'' and ''%''; %', COALESCE("Group", '(null)'), COALESCE(ApplicationComponent, '(null)'), COALESCE(AccessLevel, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddEntityType

CREATE PROCEDURE AddEntityType
(
    EntityType       varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Insert the new row
    BEGIN
        INSERT  
        INTO    EntityTypes 
                (
                    EntityType, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    EntityType, 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting EntityType ''%''; %', COALESCE(EntityType, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveEntityType

CREATE PROCEDURE RemoveEntityType
(
    EntityType       varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    LOCK TABLE public.EntityTypes IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.Entities IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.UserToEntityMappings IN ACCESS EXCLUSIVE MODE;

    LOCK TABLE public.GroupToEntityMappings IN ACCESS EXCLUSIVE MODE;

    SELECT  Id 
    INTO    CurrentRowId
    FROM    EntityTypes et
    WHERE   et.EntityType = $1 
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
      
    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No EntityTypes row exists for EntityType ''%'' and for transaction time ''%''.', COALESCE(EntityType, '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Invalidate any UserToEntityMappings rows
    BEGIN
        UPDATE  UserToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   EntityTypeId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Entity mappings mappings for EntityType ''%''; %', COALESCE(EntityType, '(null)'), SQLERRM;
    END;

    -- Invalidate any GroupToEntityMappings rows
    BEGIN
        UPDATE  GroupToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   EntityTypeId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to Entity and AccessLevel mappings for EntityType ''%''; %', COALESCE(EntityType, '(null)'), SQLERRM;
    END;

    -- Invalidate any Entities rows
    BEGIN
        UPDATE  Entities 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   EntityTypeId = CurrentRowId 
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Entities for EntityType ''%''; %', COALESCE(EntityType, '(null)'), SQLERRM;
    END;
    
    BEGIN
        UPDATE  EntityTypes 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing EntityTypes ''%''; %', COALESCE(EntityTypes, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddEntity

CREATE PROCEDURE AddEntity
(
    EntityType       varchar, 
    Entity           varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Insert the new row
    BEGIN
        INSERT  
        INTO    Entities 
                (
                    EntityTypeId, 
                    Entity, 
                    TransactionFrom, 
                    TransactionTo 
                )
        VALUES  (
                    ( 
                        SELECT  Id 
                        FROM    EntityTypes et 
                        WHERE   et.EntityType = $1 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    Entity, 
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting Entity ''%'' of type ''%''; %', COALESCE(Entity, '(null)'), COALESCE(EntityType, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveEntity

CREATE PROCEDURE RemoveEntity
(
    EntityType       varchar, 
    Entity           varchar, 
    EventId          uuid, 
    TransactionTime  timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    LOCK TABLE public.Entities IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.UserToEntityMappings IN ACCESS EXCLUSIVE MODE;
    
    LOCK TABLE public.GroupToEntityMappings IN ACCESS EXCLUSIVE MODE;

    SELECT  Id 
    INTO    CurrentRowId
    FROM    Entities e
    WHERE   e.EntityTypeId = 
            (
                SELECT  Id 
                FROM    EntityTypes et 
                WHERE   et.EntityType = $1 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   e.Entity = $2 
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
      
    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No Entities row exists for EntityType ''%'', Entity ''%'' and for transaction time ''%''.', COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    -- Invalidate any UserToEntityMappings rows
    BEGIN
        UPDATE  UserToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   EntityTypeId = 
                (
                    SELECT  Id 
                    FROM    EntityTypes et 
                    WHERE   et.EntityType = $1 
                      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                )
          AND   EntityId = CurrentRowId
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Entity mappings for EntityType ''%'' and Entity ''%''; %', COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), SQLERRM;
    END;

    -- Invalidate any GroupToEntityMappings rows
    BEGIN
        UPDATE  GroupToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   EntityTypeId = 
                (
                    SELECT  Id 
                    FROM    EntityTypes et 
                    WHERE   et.EntityType = $1 
                      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                )
          AND   EntityId = CurrentRowId
          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to Entity mappings for EntityType ''%'' and Entity ''%''; %', COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), SQLERRM;
    END;

    BEGIN
        UPDATE  Entities 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Entity ''%''; %', COALESCE(Entity, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddUserToEntityMapping

CREATE PROCEDURE AddUserToEntityMapping
(
    "User"                varchar, 
    EntityType            varchar, 
    Entity                varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        INSERT  
        INTO    UserToEntityMappings 
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
                        FROM    Users u
                        WHERE   u."User" = $1 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    EntityTypes et 
                        WHERE   et.EntityType = $2 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    Entities e
                        WHERE   e.EntityTypeId = 
                                ( 
                                    SELECT  Id 
                                    FROM    EntityTypes et 
                                    WHERE   et.EntityType = $2 
                                      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                                )
                          AND   e.Entity = $3 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ),
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting User to Entity mapping between ''%'', ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveUserToEntityMapping

CREATE PROCEDURE RemoveUserToEntityMapping
(
    "User"                varchar, 
    EntityType            varchar, 
    Entity                varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    SELECT  Id 
    INTO    CurrentRowId
    FROM    UserToEntityMappings 
    WHERE   UserId = 
            (
                SELECT  Id 
                FROM    Users u
                WHERE   u."User" = $1 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   EntityTypeId = 
            (
                SELECT  Id 
                FROM    EntityTypes et 
                WHERE   et.EntityType = $2 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   EntityId = 
            (
                SELECT  Id 
                FROM    Entities e
                WHERE   e.EntityTypeId = 
                        ( 
                            SELECT  Id 
                            FROM    EntityTypes et 
                            WHERE   et.EntityType = $2 
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                        )
                  AND   e.Entity = $3 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No UserToEntityMappings row exists for User ''%'', EntityType ''%'', Entity ''%'' and for transaction time ''%''.', COALESCE("User", '(null)'), COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        UPDATE  UserToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing User to Entity mapping for ''%'', ''%'' and ''%''; %', COALESCE("User", '(null)'), COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- AddGroupToEntityMapping

CREATE PROCEDURE AddGroupToEntityMapping
(
    "Group"               varchar, 
    EntityType            varchar, 
    Entity                varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
BEGIN 

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        INSERT  
        INTO    GroupToEntityMappings 
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
                        FROM    Groups g
                        WHERE   g."Group" = $1 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    EntityTypes et 
                        WHERE   et.EntityType = $2 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ), 
                    ( 
                        SELECT  Id 
                        FROM    Entities e
                        WHERE   e.EntityTypeId = 
                                ( 
                                    SELECT  Id 
                                    FROM    EntityTypes et 
                                    WHERE   et.EntityType = $2 
                                      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                                )
                          AND   e.Entity = $3 
                          AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                    ),
                    TransactionTime, 
                    GetTemporalMaxDate()
                );
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when inserting Group to Entity mapping between ''%'', ''%'' and ''%''; %', COALESCE("Group", '(null)'), COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), SQLERRM;
    END;

END;
$$;

--------------------------------------------------------------------------------
-- RemoveGroupToEntityMapping

CREATE PROCEDURE RemoveGroupToEntityMapping
(
    "Group"               varchar, 
    EntityType            varchar, 
    Entity                varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

    SELECT  Id 
    INTO    CurrentRowId
    FROM    GroupToEntityMappings 
    WHERE   GroupId = 
            (
                SELECT  Id 
                FROM    Groups g
                WHERE   g."Group" = $1 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo
            )
      AND   EntityTypeId = 
            (
                SELECT  Id 
                FROM    EntityTypes et 
                WHERE   et.EntityType = $2 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   EntityId = 
            (
                SELECT  Id 
                FROM    Entities e
                WHERE   e.EntityTypeId = 
                        ( 
                            SELECT  Id 
                            FROM    EntityTypes et 
                            WHERE   et.EntityType = $2 
                                AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
                        )
                  AND   e.Entity = $3 
                  AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo 
            )
      AND   TransactionTime BETWEEN TransactionFrom AND TransactionTo;

    IF (CurrentRowId IS NULL) THEN
        RAISE EXCEPTION 'No GroupToEntityMappings row exists for Group ''%'', EntityType ''%'', Entity ''%'' and for transaction time ''%''.', COALESCE("Group", '(null)'), COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), TO_CHAR(LastTransactionTime, TimeStampCharFormat)
        USING ERRCODE = 'no_data_found';
    END IF;

    BEGIN
        CALL CreateEvent(EventId, TransactionTime);
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred calling stored procedure ''CreateEvent''; %', SQLERRM;
    END;

    BEGIN
        UPDATE  GroupToEntityMappings 
        SET     TransactionTo = SubtractTemporalMinimumTimeUnit(TransactionTime)
        WHERE   Id = CurrentRowId;
    EXCEPTION
        WHEN OTHERS THEN
            RAISE EXCEPTION 'Error occurred when removing Group to Entity mapping for ''%'', ''%'' and ''%''; %', COALESCE("Group", '(null)'), COALESCE(EntityType, '(null)'), COALESCE(Entity, '(null)'), SQLERRM;
    END;

END;
$$;




CREATE PROCEDURE RemoveUserToApplicationComponentAndAccessLevelMapping
(
    "User"                varchar, 
    ApplicationComponent  varchar, 
    AccessLevel           varchar, 
    EventId               uuid, 
    TransactionTime       timestamptz
)
LANGUAGE plpgsql 
AS $$
DECLARE
    CurrentRowId  bigint;
    TimeStampCharFormat  varchar := 'YYYY-MM-DD HH24:MI::ss.USTZH';
BEGIN 

END;
$$;