DROP PROCEDURE dbo.RemoveUserToGroupMapping;
DROP PROCEDURE dbo.AddUserToGroupMapping;
DROP PROCEDURE dbo.RemoveGroup;
DROP PROCEDURE dbo.AddGroup;
DROP PROCEDURE dbo.RemoveUser;
DROP PROCEDURE dbo.AddUser;
DROP PROCEDURE dbo.CreateEvent;
DROP FUNCTION dbo.SubtractTemporalMinimumTimeUnit;
DROP FUNCTION dbo.GetTemporalMaxDate;


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