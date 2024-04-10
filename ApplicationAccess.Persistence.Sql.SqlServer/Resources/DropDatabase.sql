--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE ApplicationAccess
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

DROP TABLE ApplicationAccess.dbo.SchemaVersions;
DROP TABLE ApplicationAccess.dbo.GroupToEntityMappings;
DROP TABLE ApplicationAccess.dbo.UserToEntityMappings;
DROP TABLE ApplicationAccess.dbo.Entities;
DROP TABLE ApplicationAccess.dbo.EntityTypes;
DROP TABLE ApplicationAccess.dbo.GroupToApplicationComponentAndAccessLevelMappings;
DROP TABLE ApplicationAccess.dbo.UserToApplicationComponentAndAccessLevelMappings;
DROP TABLE ApplicationAccess.dbo.AccessLevels;
DROP TABLE ApplicationAccess.dbo.ApplicationComponents;
DROP TABLE ApplicationAccess.dbo.GroupToGroupMappings;
DROP TABLE ApplicationAccess.dbo.UserToGroupMappings;
DROP TABLE ApplicationAccess.dbo.Groups;
DROP TABLE ApplicationAccess.dbo.Users;
DROP TABLE ApplicationAccess.dbo.EventIdToTransactionTimeMap;