USE [master];
GO

-- =========================
-- Create messaging database
-- =========================
IF (DB_ID('one-c-sharp-service-broker') IS NULL)
BEGIN
	CREATE DATABASE [one-c-sharp-service-broker];
END;
GO

USE [one-c-sharp-service-broker];
GO

-- ================================================
-- Enable service broker for the messaging database
-- ================================================
IF NOT EXISTS(SELECT 1 FROM sys.databases WHERE database_id = DB_ID('one-c-sharp-service-broker') AND is_broker_enabled = 0x01)
BEGIN
	ALTER DATABASE [one-c-sharp-service-broker] SET ENABLE_BROKER;
END;
GO

-- ==========================================
-- Create function to get service broker guid
-- ==========================================
IF OBJECT_ID('dbo.fn_service_broker_guid', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_service_broker_guid];
END;
GO

CREATE FUNCTION [dbo].[fn_service_broker_guid]()
RETURNS uniqueidentifier
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier);
	SELECT @broker_guid = service_broker_guid FROM sys.databases WHERE database_id = DB_ID('one-c-sharp-service-broker');
	RETURN @broker_guid;
END;
GO

-- ====================================
-- Create messaging database master key
-- ====================================
IF NOT EXISTS(SELECT 1 FROM sys.symmetric_keys WHERE name = N'##MS_DatabaseMasterKey##')
BEGIN
	CREATE MASTER KEY ENCRYPTION BY PASSWORD = '{0D71CAEC-7BA4-44CE-8531-66367F31D8F4}';
END;
GO

-- ==========================================
-- Create function to build default user name
-- ==========================================
IF OBJECT_ID('dbo.fn_default_user_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_user_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_user_name]()
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/user';
END;
GO

-- =======================================
-- Create procedure to create default user
-- =======================================
IF OBJECT_ID('dbo.sp_create_default_user', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_default_user];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_default_user]
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @user_name nvarchar(128) = (SELECT [dbo].[fn_default_user_name]());

	IF NOT EXISTS(SELECT 1 FROM sys.database_principals WHERE name = @user_name)
	BEGIN
		EXEC('CREATE USER [' + @user_name + '] WITHOUT LOGIN;');
	END;
END;
GO

-- ============================================================
-- Create service broker user for the messaging database
-- This user gets control over service broker services
-- Export this user to the remote database for message exchange
-- ============================================================
EXEC [dbo].[sp_create_default_user];
GO

-- ========================================================
-- Create function to build default user's certificate name
-- ========================================================
IF OBJECT_ID('dbo.fn_default_certificate_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_certificate_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_certificate_name]()
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/certificate';
END;
GO

-- ==============================================
-- Create procedure to create default certificate
-- ==============================================
IF OBJECT_ID('dbo.sp_create_default_certificate', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_default_certificate];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_default_certificate]
AS
BEGIN
	SET NOCOUNT ON;

    DECLARE @certificate_name nvarchar(128) = (SELECT [dbo].[fn_default_certificate_name]());

	IF NOT EXISTS(SELECT 1 FROM sys.certificates WHERE name = @certificate_name)
	BEGIN
		DECLARE @user_name nvarchar(128) = (SELECT [dbo].[fn_default_user_name]());

		EXEC('CREATE CERTIFICATE [' + @certificate_name + '] AUTHORIZATION [' + @user_name + ']
		WITH SUBJECT = ''Default service broker user certificate'',
			START_DATE = ''20200101'',
			EXPIRY_DATE = ''20300101'';');
	END;
END;
GO

-- ===========================================================================
-- Create service broker authentication certificate for the messaging database
-- This certificate is used to authenticate as default user
-- Export certificate's public key to the remote database for message exchange
-- ===========================================================================
EXEC [dbo].[sp_create_default_certificate];
GO

-- ===================================
-- Create function to build queue name
-- ===================================
IF OBJECT_ID('dbo.fn_create_queue_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_create_queue_name];
END;
GO

CREATE FUNCTION [dbo].[fn_create_queue_name](@name nvarchar(80))
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/queue/' + @name;
END;
GO

-- =====================================
-- Create function to build service name
-- =====================================
IF OBJECT_ID('dbo.fn_create_service_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_create_service_name];
END;
GO

CREATE FUNCTION [dbo].[fn_create_service_name](@name nvarchar(80))
RETURNS nvarchar(128)
AS
BEGIN
	DECLARE @broker_guid uniqueidentifier = [dbo].[fn_service_broker_guid]();
	RETURN CAST(@broker_guid AS nvarchar(36)) + N'/service/' + @name;
END;
GO

-- =============================================
-- Create function to build default service name
-- =============================================
IF OBJECT_ID('dbo.fn_default_service_name', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_default_service_name];
END;
GO

CREATE FUNCTION [dbo].[fn_default_service_name]()
RETURNS nvarchar(128)
AS
BEGIN
	RETURN [dbo].[fn_create_service_name](N'default');
END;
GO

-- ====================================================
-- Create function to check if queue exists and enabled
-- ====================================================
IF OBJECT_ID('dbo.fn_queue_exists', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_queue_exists];
END;
GO

CREATE FUNCTION [dbo].[fn_queue_exists](@name nvarchar(128))
RETURNS int
AS
BEGIN
	DECLARE @enabled bit;

	SELECT @enabled = is_enqueue_enabled FROM sys.service_queues WHERE name = @name;

	IF (@enabled IS NULL) RETURN 1;
	ELSE IF (@enabled = 0x01) RETURN 0;
	
	RETURN 2; -- exists disabled
END;
GO

-- ================================
-- Create procedure to delete queue
-- ================================
IF OBJECT_ID('dbo.sp_delete_queue', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_delete_queue];
END;
GO

CREATE PROCEDURE [dbo].[sp_delete_queue](@name nvarchar(80))
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @queue_name nvarchar(128) = [dbo].[fn_create_queue_name](@name);
	DECLARE @service_name nvarchar(128) = [dbo].[fn_create_service_name](@name);
	DECLARE @default_service_name nvarchar(128) = [dbo].[fn_default_service_name]();

	IF (@default_service_name = @queue_name) THROW 50001, N'The default queue can not be droped!', 1;
	
	IF EXISTS(SELECT 1 FROM sys.services WHERE name = @service_name)
	BEGIN
		EXEC(N'DROP SERVICE [' + @service_name + N'];');
	END;

	IF EXISTS(SELECT 1 FROM sys.service_queues WHERE name = @queue_name)
	BEGIN
		EXEC(N'DROP QUEUE [dbo].[' + @queue_name + N'];');
	END;

	-- ==============================================
	-- Receive error message for default local dialog
	-- ==============================================
	-- http://schemas.microsoft.com/SQL/ServiceBroker/Error
	-- 'Remote service has been dropped.' message !
END;
GO

-- ===========================================
-- Create procedure to get local dialog handle
-- ===========================================
IF OBJECT_ID('dbo.sp_get_local_dialog_handle', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_get_local_dialog_handle];
END;
GO

CREATE PROCEDURE [dbo].[sp_get_local_dialog_handle]
(
	@handle uniqueidentifier OUTPUT,
	@target_queue_name nvarchar(128) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @source_broker_guid nvarchar(36) = [dbo].[fn_service_broker_guid]();
	DECLARE @source_service_name nvarchar(128) = [dbo].[fn_default_service_name]();

	DECLARE @target_broker_guid nvarchar(36);
	DECLARE @target_service_name nvarchar(128);

	IF (@target_queue_name IS NULL)
	BEGIN
		SET @target_broker_guid = CAST(@source_broker_guid AS nvarchar(36));
		SET @target_service_name = @source_service_name;
	END
	ELSE
	BEGIN
		SET @target_broker_guid = SUBSTRING(@target_queue_name, 1, 36);
		SET @target_service_name = REPLACE(@target_queue_name, N'/queue/', N'/service/');
	END

	SELECT @handle = conversation_handle
	FROM sys.conversation_endpoints AS e
	INNER JOIN sys.services AS s ON e.service_id = s.service_id
	AND e.is_initiator = 1
	AND e.state IN ('SO', 'CO')
	AND s.name = @source_service_name
	AND e.far_service = @target_service_name
	AND e.far_broker_instance = @target_broker_guid;

	IF (@handle IS NULL) RETURN 1; -- dialog handle is not found error

	RETURN 0;
END;
GO

-- =================================
-- Create procedure to create queues
-- =================================
IF OBJECT_ID('dbo.sp_create_queue', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_queue];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_queue](@name nvarchar(80))
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @user_name nvarchar(128) = [dbo].[fn_default_user_name]();
	DECLARE @queue_name nvarchar(128) = [dbo].[fn_create_queue_name](@name);
	DECLARE @service_name nvarchar(128) = [dbo].[fn_create_service_name](@name);
	DECLARE @default_service_name nvarchar(128) = [dbo].[fn_default_service_name]();

	IF NOT EXISTS(SELECT 1 FROM sys.service_queues WHERE name = @queue_name)
	BEGIN
		EXEC('CREATE QUEUE [' + @queue_name + '] WITH POISON_MESSAGE_HANDLING (STATUS = OFF);');
	END;

	IF NOT EXISTS(SELECT 1 FROM sys.services WHERE name = @service_name)
	BEGIN
		EXEC('CREATE SERVICE [' + @service_name + '] ON QUEUE [' + @queue_name + '] ([DEFAULT]);
		GRANT CONTROL ON SERVICE::[' + @service_name + '] TO [' + @user_name + '];
		GRANT SEND ON SERVICE::[' + @service_name + '] TO [PUBLIC];');
	END;

	-- ===========================
	-- Create default local dialog
	-- ===========================

	DECLARE @result int;
	DECLARE @handle uniqueidentifier;
	DECLARE @broker_guid nvarchar(36) = CAST([dbo].[fn_service_broker_guid]() AS nvarchar(36));

	EXEC @result = [dbo].[sp_get_local_dialog_handle] @handle OUTPUT, @queue_name;

	IF (@result = 1 OR @handle IS NULL) -- dialog handle is not found
	BEGIN
		DECLARE @sql nvarchar(1024) = 'BEGIN DIALOG @handle
		FROM SERVICE [' + @default_service_name + ']
		TO SERVICE ''' + @service_name + ''', ''' + @broker_guid + '''
		ON CONTRACT [DEFAULT]
		WITH ENCRYPTION = OFF;';
		EXECUTE sp_executesql @sql, N'@handle uniqueidentifier', @handle = @handle;
	END;
END;
GO

-- ====================
-- Create default queue
-- ====================
EXEC [dbo].[sp_create_queue] N'default';
GO

-- =================================
-- Create procedure to send messages
-- =================================
IF OBJECT_ID('dbo.sp_send_message', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_send_message];
END;
GO

CREATE PROCEDURE [dbo].[sp_send_message]
	@dialog_handle uniqueidentifier,
	@message_body nvarchar(max),
	@message_type nvarchar(128) = NULL
AS
BEGIN
	SET NOCOUNT ON;
	
	IF (@message_type IS NULL)
	BEGIN
		SET @message_type = N'DEFAULT';
	END;

	SEND ON CONVERSATION @dialog_handle MESSAGE TYPE @message_type (CAST(@message_body AS varbinary(max)));

    RETURN 0;
END
GO

-- =======================================
-- Create procedure to receive one message
-- =======================================
IF OBJECT_ID('dbo.sp_receive_message', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_message];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_message]
	@queue nvarchar(128),
	@timeout int = 1000
AS
BEGIN
	SET NOCOUNT ON;
	
	DECLARE @handle uniqueidentifier;
	DECLARE @message_type nvarchar(128);
	DECLARE @message_body nvarchar(max);

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (1)
		@handle_out = conversation_handle,
		@message_type_out = message_type_name,
		@message_body_out = CAST(message_body AS nvarchar(max))
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;';

	EXECUTE sp_executesql @sql,
		N'@handle_out uniqueidentifier OUTPUT, @message_type_out nvarchar(128) OUTPUT,
		  @message_body_out nvarchar(max) OUTPUT, @timeout int',
		  @handle_out       = @handle       OUTPUT,
		  @message_type_out = @message_type OUTPUT,
		  @message_body_out = @message_body OUTPUT,
		  @timeout          = @timeout;

	IF (@@ROWCOUNT = 0)
	BEGIN
		SELECT CAST('00000000-0000-0000-0000-000000000000' AS uniqueidentifier) AS [dialog_handle],
				N'' AS [message_type],
				N'' AS [message_body];
		RETURN 0;
	END

	IF (@message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/Error' OR
        @message_type = N'http://schemas.microsoft.com/SQL/ServiceBroker/EndDialog')
	BEGIN
		END CONVERSATION @handle;
	END

	SELECT @handle AS [dialog_handle], @message_type AS [message_type], @message_body AS [message_body];

    RETURN 0;
END
GO

-- =============================================
-- Create procedure to receive multiple messages
-- =============================================
IF OBJECT_ID('dbo.sp_receive_messages', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_receive_messages];
END;
GO

CREATE PROCEDURE [dbo].[sp_receive_messages]
	@queue nvarchar(128),
	@timeout int = 1000,
	@number_of_messages int = 10
AS
BEGIN
	SET NOCOUNT ON;

	DECLARE @sql nvarchar(1024) = N'WAITFOR (RECEIVE TOP (@number_of_messages)
		conversation_handle                 AS [dialog_handle],
		message_type_name                   AS [message_type],
		CAST(message_body AS nvarchar(max)) AS [message_body]
		FROM [dbo].[' + @queue + ']
	), TIMEOUT @timeout;';

	EXECUTE sp_executesql @sql, N'@number_of_messages int, @timeout int',
		  @number_of_messages = @number_of_messages, @timeout = @timeout;

	RETURN 0;
END