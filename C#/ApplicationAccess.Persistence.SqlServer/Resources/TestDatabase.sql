BEGIN
    DECLARE @result  datetime2;
    DECLARE @ErrorMessage         nvarchar(max);

    SET @result = dbo.GetTemporalMaxDate();

    IF (@result != CONVERT(datetime2, '9999-12-31T23:59:59.9999999', 126))
    BEGIN
        SET @ErrorMessage = N'Unexpected result calling function GetTemporalMaxDate()';
        THROW 50001, @ErrorMessage, 1;
    END
END
GO

BEGIN
    DECLARE @TestName              nvarchar(max);
    DECLARE @InternalErrorMessage  nvarchar(max);
    DECLARE @ExternalErrorMessage  nvarchar(max);
    DECLARE @EventId               uniqueidentifier;
    DECLARE @CurrentTime           datetime2;

    SET @TestName = 'AddUser_UserAlreadyExists';
    SET @EventId = NEWID();
    SET @CurrentTime = GETDATE();
    EXEC dbo.AddUser 'Alastair', @EventId, @CurrentTime;

    BEGIN TRY
        EXEC dbo.AddUser 'Alastair', @EventId, @CurrentTime;
    END TRY
    BEGIN CATCH
        SET @InternalErrorMessage = ERROR_MESSAGE();
		DECLARE @StringPosition  int;
		SET @StringPosition = CHARINDEX('Unexpected Error Message: Error occurred calling stored procedure ''CreateEvent'': Error occurred when inserting into table ''EventIdToTransactionTimeMap'': Violation of PRIMARY KEY constraint', @InternalErrorMessage);
		IF @StringPosition = 0
		BEGIN
		    SET @ExternalErrorMessage = @TestName + ': Unexpected Error Message: ' + @InternalErrorMessage;
	        THROW 50001, @ExternalErrorMessage, 1;
	    END

    END CATCH

END
GO