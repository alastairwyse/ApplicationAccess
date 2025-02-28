--------------------------------------------------------------------------------
--------------------------------------------------------------------------------
-- Drop Functions / Stored Procedures
--------------------------------------------------------------------------------
--------------------------------------------------------------------------------

-- NOTE: If executing through SQL Server Management Studio, set 'SQKCMD Mode' via the 'Query' menu

:Setvar DatabaseName ApplicationAccessConfiguration

USE $(DatabaseName);
GO 

DROP PROCEDURE dbo.UpdateShardConfiguration;
DROP FUNCTION dbo.SubtractTemporalMinimumTimeUnit;
DROP FUNCTION dbo.GetTemporalMaxDate;


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

DROP TABLE $(DatabaseName).dbo.ShardConfiguration;