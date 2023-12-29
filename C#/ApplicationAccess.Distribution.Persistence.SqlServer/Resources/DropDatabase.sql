--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

USE ApplicationAccess
GO 

DROP PROCEDURE dbo.UpdateShardConfiguration;


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop User-defined Types
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

DROP TYPE dbo.ShardConfigurationStagingTableType;


--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Tables
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

DROP TABLE ApplicationAccess.dbo.ShardConfiguration;