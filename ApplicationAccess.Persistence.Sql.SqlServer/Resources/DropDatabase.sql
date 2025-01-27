--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

-- NOTE: If executing through SQL Server Management Studio, set 'SQKCMD Mode' via the 'Query' menu

:Setvar DatabaseName ApplicationAccess

USE $(DatabaseName);
GO 

DROP PROCEDURE dbo.ProcessEvents;
DROP PROCEDURE dbo.RemoveGroupToEntityMapping;
DROP PROCEDURE dbo.AddGroupToEntityMapping;
DROP PROCEDURE dbo.RemoveUserToEntityMapping;
DROP PROCEDURE dbo.AddUserToEntityMapping;
DROP PROCEDURE dbo.RemoveEntity;
DROP PROCEDURE dbo.AddEntity;
DROP PROCEDURE dbo.RemoveEntityType;
DROP PROCEDURE dbo.AddEntityType;
DROP PROCEDURE dbo.RemoveGroupToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE dbo.AddGroupToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE dbo.RemoveUserToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE dbo.AddUserToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE dbo.RemoveGroupToGroupMapping;
DROP PROCEDURE dbo.AddGroupToGroupMapping;
DROP PROCEDURE dbo.RemoveUserToGroupMapping;
DROP PROCEDURE dbo.AddUserToGroupMapping;
DROP PROCEDURE dbo.RemoveGroup;
DROP PROCEDURE dbo.AddGroup;
DROP PROCEDURE dbo.RemoveUser;
DROP PROCEDURE dbo.AddUser;
DROP PROCEDURE dbo.CreateEvent;
DROP FUNCTION dbo.SubtractTemporalMinimumTimeUnit;
DROP FUNCTION dbo.GetTemporalMaxDate;


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop User-defined Types
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

DROP TYPE dbo.EventTableType;


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

DROP TABLE $(DatabaseName).dbo.SchemaVersions;
DROP TABLE $(DatabaseName).dbo.GroupToEntityMappings;
DROP TABLE $(DatabaseName).dbo.UserToEntityMappings;
DROP TABLE $(DatabaseName).dbo.Entities;
DROP TABLE $(DatabaseName).dbo.EntityTypes;
DROP TABLE $(DatabaseName).dbo.GroupToApplicationComponentAndAccessLevelMappings;
DROP TABLE $(DatabaseName).dbo.UserToApplicationComponentAndAccessLevelMappings;
DROP TABLE $(DatabaseName).dbo.AccessLevels;
DROP TABLE $(DatabaseName).dbo.ApplicationComponents;
DROP TABLE $(DatabaseName).dbo.GroupToGroupMappings;
DROP TABLE $(DatabaseName).dbo.UserToGroupMappings;
DROP TABLE $(DatabaseName).dbo.Groups;
DROP TABLE $(DatabaseName).dbo.Users;
DROP TABLE $(DatabaseName).dbo.EventIdToTransactionTimeMap;