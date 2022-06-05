﻿
----------------------------------------
-- Drop Everything

DROP PROCEDURE dbo.RemoveUserToGroupMapping;
DROP PROCEDURE dbo.AddUserToGroupMapping;
DROP PROCEDURE dbo.RemoveGroup;
DROP PROCEDURE dbo.AddGroup;
DROP PROCEDURE dbo.RemoveUser;
DROP PROCEDURE dbo.AddUser;
DROP PROCEDURE dbo.CreateEvent;
DROP FUNCTION dbo.SubtractTemporalMinimumTimeUnit;
DROP FUNCTION dbo.GetTemporalMaxDate;
DROP TABLE ApplicationAccess.dbo.UserToGroupMappings;
DROP TABLE ApplicationAccess.dbo.Groups;
DROP TABLE ApplicationAccess.dbo.Users;
DROP TABLE ApplicationAccess.dbo.EventIdToTransactionTimeMap;




----------------------------------------
-- Create the DB

CREATE DATABASE ApplicationAccess;


----------------------------------------
-- Create Tables

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

CREATE INDEX UserIndex ON UserToGroupMappings (UserId, TransactionTo);
CREATE INDEX GroupIndex ON UserToGroupMappings (GroupId, TransactionTo);


----------------------------------------
-- Create Stored Procedures

USE ApplicationAccess
GO 

CREATE FUNCTION dbo.GetTemporalMaxDate
(
)
RETURNS datetime2
AS
BEGIN
	RETURN CONVERT(datetime2, '9999-12-31T23:59:59.9999999', 126);
END
GO

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
		SET @ErrorMessage = N'Error occurred when inserting into table ''EventIdToTransactionTimeMap'': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

END
GO

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
		SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + ''': ' + ERROR_MESSAGE() + '.';
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
		SET @ErrorMessage = N'Error occurred when inserting User ''' + @User + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	COMMIT TRANSACTION

END
GO

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
		SET @ErrorMessage = N'No Users row exists with User ''' + @User +  ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
		THROW 50001, @ErrorMessage, 1;
	END

	BEGIN TRANSACTION

	BEGIN TRY
		EXEC CreateEvent @EventId, @TransactionTime;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH
	
	-- TODO: Removes for...
	--   UserToAppComponent
	--   UserToEntity

	BEGIN TRY
		UPDATE  dbo.UserToGroupMappings 
		SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
		WHERE   UserId = 
				( 
					SELECT  Id 
					FROM    dbo.Users Users
					WHERE   [User] = @User 
					  AND   @TransactionTime BETWEEN Users.TransactionFrom AND Users.TransactionTo 
				)
	      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred when removing User to Group mappings for User ''' + @User + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	BEGIN TRY
		UPDATE  dbo.Users 
		SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
		WHERE   Id = @CurrentRowId;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred when removing User ''' + @User + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	COMMIT TRANSACTION

END
GO


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
		SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + ''': ' + ERROR_MESSAGE() + '.';
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
		SET @ErrorMessage = N'Error occurred when inserting Group ''' + @Group + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	COMMIT TRANSACTION

END
GO

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
		SET @ErrorMessage = N'No Groups row exists with Group ''' + @Group +  ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
		THROW 50001, @ErrorMessage, 1;
	END

	BEGIN TRANSACTION

	BEGIN TRY
		EXEC CreateEvent @EventId, @TransactionTime;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH
	
	-- TODO: Removes for...
	--   GroupToGroup
	--   GroupToAppComponent
	--   GroupToEntity

	BEGIN TRY
		UPDATE  dbo.UserToGroupMappings 
		SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
		WHERE   GroupId = 
				( 
					SELECT  Id 
					FROM    dbo.Groups Groups
					WHERE   [Group] = @Group 
					  AND   @TransactionTime BETWEEN Groups.TransactionFrom AND Groups.TransactionTo 
				)
	      AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred when removing User to Group mappings for Group ''' + @Group + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	BEGIN TRY
		UPDATE  dbo.Groups 
		SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
		WHERE   Id = @CurrentRowId;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred when removing Group ''' + @Group + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	COMMIT TRANSACTION

END
GO


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
		SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + ''': ' + ERROR_MESSAGE() + '.';
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
		SET @ErrorMessage = N'Error occurred when inserting User to Group mapping between ''' + @User + ''' and ''' + @Group + ''' : ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	COMMIT TRANSACTION

END
GO


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
	WHERE   [UserId] = 
			(
				SELECT  Id 
				FROM    dbo.Users 
				WHERE   [User] = @User 
				  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
			)
	  AND	[GroupId] = 
			(
				SELECT  Id 
				FROM    dbo.Groups 
				WHERE   [Group] = @Group 
				  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo
			)
	  AND   @TransactionTime BETWEEN TransactionFrom AND TransactionTo;

	IF (@CurrentRowId IS NULL)
	BEGIN
		SET @ErrorMessage = N'No UserToGroupMappings row exists for User ''' + @User + ''', Group ''' + @Group +  ''' and for transaction time ''' + CONVERT(nvarchar, @TransactionTime, 126) + '''.';
		THROW 50001, @ErrorMessage, 1;
	END

	BEGIN TRANSACTION

	BEGIN TRY
		EXEC CreateEvent @EventId, @TransactionTime;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred calling stored procedure ''' + ERROR_PROCEDURE() + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	BEGIN TRY
		UPDATE  dbo.UserToGroupMappings 
		SET     TransactionTo = dbo.SubtractTemporalMinimumTimeUnit(@TransactionTime)
		WHERE   Id = @CurrentRowId;
	END TRY
	BEGIN CATCH
		ROLLBACK TRANSACTION
		SET @ErrorMessage = N'Error occurred when removing User to Group mapping for ''' + @User + ''' and ''' + @Group + ''': ' + ERROR_MESSAGE() + '.';
		THROW 50001, @ErrorMessage, 1;
	END CATCH

	COMMIT TRANSACTION

END
GO



----------------------------------------
-- Test Commmands

BEGIN TRY
  DECLARE @EventId uniqueidentifier;
  DECLARE @CurrentTime datetime2;
  SET @EventId = NEWID();
  --SELECT @CurrentTime = CONVERT(datetime2, '2021-05-25T23:04:00.1234567', 126);
  SET @CurrentTime = GETDATE();
  EXEC dbo.AddUserToGroupMapping 'Dave', 'Sales', @EventId, @CurrentTime;
END TRY
BEGIN CATCH
  PRINT  ERROR_MESSAGE();
  THROW
END CATCH

BEGIN TRY
  DECLARE @EventId uniqueidentifier;
  DECLARE @CurrentTime datetime2;
  SET @EventId = NEWID();
  --SELECT @CurrentTime = CONVERT(datetime2, '2021-05-25T23:04:00.1234567', 126);
  SET @CurrentTime = GETDATE();
  EXEC dbo.RemoveUserToGroupMapping 'Dave', 'Sales', @EventId, @CurrentTime;
END TRY
BEGIN CATCH
  PRINT  ERROR_MESSAGE();
  THROW
END CATCH

SELECT ERROR_MESSAGE();

SELECT  *
FROM    EventIdToTransactionTimeMap

SELECT  *
FROM    Users
WHERE   CONVERT(datetime2, '2022-05-28T08:48:10.0400000', 126) BETWEEN TransactionFrom AND TransactionTo;

SELECT  *
FROM    Groups
WHERE   CONVERT(datetime2, '2022-05-28T08:48:10.0400000', 126) BETWEEN TransactionFrom AND TransactionTo;

SELECT  *
FROM    UserToGroupMappings
WHERE   GETDATE() BETWEEN TransactionFrom AND TransactionTo;

TRUNCATE TABLE UserToGroupMappings

SELECT dbo.GetTemporalMaxDate();

SELECT CONVERT(datetime2, '9999-12-31T23:59:59.9999999', 126);

SELECT  'IS_IT?'
WHERE   6 BETWEEN 1 AND 5;

SELECT  'IS_IT?'
WHERE   1 = 2;

----------------------------------------
-- TODO

--   Pass datatbase name in as a param??  Might need to change when we have multiple instances of DB (sharding case)
--   If I use datetime2 have to expect it's in UTC
--   If I want to include timezone, need to use datetimeoffset