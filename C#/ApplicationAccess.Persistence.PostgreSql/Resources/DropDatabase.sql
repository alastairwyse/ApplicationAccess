--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

DROP PROCEDURE ProcessEvents;
DROP PROCEDURE RemoveGroupToEntityMapping;
DROP PROCEDURE AddGroupToEntityMapping;
DROP PROCEDURE RemoveUserToEntityMapping;
DROP PROCEDURE AddUserToEntityMapping;
DROP PROCEDURE RemoveEntity;
DROP PROCEDURE AddEntity;
DROP PROCEDURE RemoveEntityType;
DROP PROCEDURE AddEntityType;
DROP PROCEDURE RemoveGroupToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE AddGroupToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE RemoveUserToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE AddUserToApplicationComponentAndAccessLevelMapping;
DROP PROCEDURE RemoveGroupToGroupMapping;
DROP PROCEDURE AddGroupToGroupMapping;
DROP PROCEDURE RemoveUserToGroupMapping;
DROP PROCEDURE AddUserToGroupMapping;
DROP PROCEDURE RemoveGroup;
DROP PROCEDURE AddGroup;
DROP PROCEDURE RemoveUser;
DROP PROCEDURE AddUser;
DROP PROCEDURE CreateEvent;
DROP FUNCTION SubtractTemporalMinimumTimeUnit;
DROP FUNCTION GetTemporalMaxDate;


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

DROP TABLE SchemaVersions;
DROP TABLE GroupToEntityMappings;
DROP TABLE UserToEntityMappings;
DROP TABLE Entities;
DROP TABLE EntityTypes;
DROP TABLE GroupToApplicationComponentAndAccessLevelMappings;
DROP TABLE UserToApplicationComponentAndAccessLevelMappings;
DROP TABLE AccessLevels;
DROP TABLE ApplicationComponents;
DROP TABLE GroupToGroupMappings;
DROP TABLE UserToGroupMappings;
DROP TABLE Groups;
DROP TABLE Users;
DROP TABLE EventIdToTransactionTimeMap;

