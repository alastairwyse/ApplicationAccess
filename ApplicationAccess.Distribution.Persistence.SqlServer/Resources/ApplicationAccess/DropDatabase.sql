--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

-- NOTE: If executing through SQL Server Management Studio, set 'SQKCMD Mode' via the 'Query' menu

:Setvar DatabaseName ApplicationAccess

USE $(DatabaseName);
GO 

DROP TABLE $(DatabaseName).dbo.EventIdToGroupToEntityMap;
DROP TABLE $(DatabaseName).dbo.EventIdToUserToEntityMap;
DROP TABLE $(DatabaseName).dbo.EventIdToEntityMap;
DROP TABLE $(DatabaseName).dbo.EventIdToEntityTypeMap;
DROP TABLE $(DatabaseName).dbo.EventIdToGroupToApplicationComponentAndAccessLevelMap;
DROP TABLE $(DatabaseName).dbo.EventIdToUserToApplicationComponentAndAccessLevelMap;
DROP TABLE $(DatabaseName).dbo.EventIdToUserToGroupMap;
DROP TABLE $(DatabaseName).dbo.EventIdToGroupMap;
DROP TABLE $(DatabaseName).dbo.EventIdToUserMap;
DROP TABLE $(DatabaseName).dbo.Actions;
