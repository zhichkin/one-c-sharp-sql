USE [master];
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- ============================
-- Create table queues database
-- ============================
IF (DB_ID('one-c-sharp-table-queues') IS NULL)
BEGIN
	CREATE DATABASE [one-c-sharp-table-queues];
END;
GO

USE [one-c-sharp-table-queues];
GO

-- ============================
-- Create queues register table
-- ============================
CREATE TABLE [dbo].[queues]
(
	[name] nvarchar(128) NOT NULL,
	[type] char(4)       NOT NULL, -- 'FIFO', 'LIFO', 'HEAP', 'TIME', 'FILE'
	[mode] char(1)       NOT NULL, -- 'S', 'M' concurrency access mode (single or multiple consumers)
);
GO
CREATE UNIQUE CLUSTERED INDEX [cux_queues_name] ON [dbo].[queues] ([name] ASC);
GO

-- ======================================
-- Create function to validate queue name
-- ======================================
IF OBJECT_ID('dbo.fn_is_name_valid', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_is_name_valid];
END;
GO

CREATE FUNCTION [dbo].[fn_is_name_valid]
(
	@name nvarchar(128)
)
RETURNS bit
AS
BEGIN
	IF (@name IS NULL OR @name = '') RETURN 0x00; -- false

	DECLARE @has_invalid_characters bit = CASE WHEN (@name NOT LIKE '%[^a-zа-я0-9_]%') THEN 0x00 ELSE 0x01 END;
	
	IF (@has_invalid_characters = 0x01) RETURN 0x00; -- false

	RETURN 0x01; -- true
END;
GO

-- ========================================
-- Create function to check if queue exists
-- ========================================
IF OBJECT_ID('dbo.fn_queue_exists', 'FN') IS NOT NULL
BEGIN
	DROP FUNCTION [dbo].[fn_queue_exists];
END;
GO

CREATE FUNCTION [dbo].[fn_queue_exists]
(
	@name nvarchar(128)
)
RETURNS int
AS
BEGIN
	IF ([dbo].[fn_is_name_valid](@name) = 0x00) RETURN 0; -- false

	IF (OBJECT_ID(N'dbo.' + @name, 'U') IS NULL) RETURN 0; -- false

	IF NOT EXISTS(SELECT 1 FROM [dbo].[queues] WHERE [name] = @name) RETURN 0; -- false

	RETURN 1; -- true
END
GO

-- ================================
-- Create procedure to create queue
-- ================================
IF OBJECT_ID('dbo.sp_create_queue', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_create_queue];
END;
GO

CREATE PROCEDURE [dbo].[sp_create_queue]
	@name nvarchar(128),
	@type char(4) = 'FIFO', -- queue type
	@mode char(1) = 'S' -- concurrency access mode (single or multiple consumers)
AS
BEGIN
	SET NOCOUNT ON;

	IF ([dbo].[fn_is_name_valid](@name) = 0x00) THROW 50001, N'Bad queue name format.', 1;

	IF (NOT @type IN ('FIFO', 'LIFO', 'HEAP', 'TIME', 'FILE')) THROW 50002, N'Invalid queue type.', 1;

	IF (NOT @mode IN ('S', 'M')) THROW 50003, N'Invalid concurrency access mode.', 1;

	SET XACT_ABORT ON;

	BEGIN TRY
		BEGIN TRANSACTION;

		INSERT [dbo].[queues] ([name], [type], [mode]) SELECT @name, @type, @mode;

		IF (@type IN ('FIFO', 'LIFO'))
		BEGIN
			EXEC(N'CREATE TABLE [dbo].[' + @name + N']
			(
				[consume_order] bigint         NOT NULL IDENTITY(0,1),
				[message_type]  nvarchar(128)  NOT NULL,
				[message_body]  varbinary(max) NOT NULL
			);
			CREATE UNIQUE CLUSTERED INDEX [cux_' + @name + N'] ON [dbo].[' + @name + N'] (consume_order ASC);');
		END;
		ELSE IF (@type = 'HEAP')
		BEGIN
			EXEC(N'CREATE TABLE [dbo].[' + @name + N']
			(
				[message_type] nvarchar(128)  NOT NULL,
				[message_body] varbinary(max) NOT NULL
			);');
		END;
		ELSE IF (@type = 'TIME')
		BEGIN
			EXEC(N'CREATE TABLE [dbo].[' + @name + N']
			(
				[consume_time] datetime       NOT NULL,
				[message_type] nvarchar(128)  NOT NULL,
				[message_body] varbinary(max) NOT NULL
			);
			CREATE CLUSTERED INDEX [cux_' + @name + N'] ON [dbo].[' + @name + N'] (consume_time ASC);');
		END;
		ELSE IF (@type = 'FILE')
		BEGIN
			EXEC(N'CREATE TABLE [dbo].[' + @name + N']
			(
				[file_name] nvarchar(256)  NOT NULL,
				[file_type] nvarchar(128)  NOT NULL,
				[file_body] varbinary(max) NOT NULL
			);
			CREATE UNIQUE CLUSTERED INDEX [cux_' + @name + N'] ON [dbo].[' + @name + N'] (file_name ASC);');
		END;

		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION;
		THROW;
	END CATCH

	RETURN 0;
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

CREATE PROCEDURE [dbo].[sp_delete_queue]
	@name nvarchar(128)
AS
BEGIN
	SET NOCOUNT ON;

	IF ([dbo].[fn_is_name_valid](@name) = 0x00) THROW 50001, N'Bad queue name format.', 1;

	SET XACT_ABORT ON;

	BEGIN TRY
		BEGIN TRANSACTION;

		DELETE [dbo].[queues] WHERE [name] = @name;

		IF OBJECT_ID(N'dbo.' + @name, 'U') IS NOT NULL
		BEGIN
			EXEC(N'DROP TABLE [dbo].[' + @name + N'];');
		END;

		COMMIT TRANSACTION;
	END TRY
	BEGIN CATCH
		IF (@@TRANCOUNT > 0) ROLLBACK TRANSACTION;
		THROW;
	END CATCH

	RETURN 0;
END;
GO

-- ====================================
-- Create procedure to produce messages
-- ====================================
IF OBJECT_ID('dbo.sp_produce_message', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_produce_message];
END;
GO

CREATE PROCEDURE [dbo].[sp_produce_message]
	@queue_name   nvarchar(128),
	@message_body nvarchar(max), -- use varchar(max) for BASE64 to reduce memory space !!!
	@message_type nvarchar(128) = N'',
	@consume_time datetime = '2020-01-01'
AS
BEGIN
	SET NOCOUNT ON;

	IF ([dbo].[fn_is_name_valid](@queue_name) = 0x00) THROW 50001, N'Bad queue name format.', 1;

	IF (@message_body IS NULL OR @message_body = '') THROW 50002, N'Invalid parameter value: @message_body.', 1;

	IF (@message_type IS NULL) SET @message_type = N'';

	IF (@consume_time IS NULL) SET @consume_time = '2020-01-01';

	DECLARE @type char(4);
	DECLARE @mode char(1);
	SELECT @type = [type], @mode = [mode] FROM [dbo].[queues] WHERE [name] = @queue_name;

	IF (@type IS NULL) THROW 50003, N'Queue is not found.', 1;

	DECLARE @sql nvarchar(1024);
	DECLARE @message_body_value varbinary(max) = CAST(@message_body AS varbinary(max));

	IF (@type = 'TIME')
	BEGIN
		SET @sql = N'INSERT [dbo].[' + @queue_name + N']
				([consume_time], [message_type], [message_body])
			VALUES
				(@consume_time, @message_type, @message_body);';
		EXECUTE sp_executesql @sql, N'@consume_time datetime, @message_type nvarchar(128), @message_body varbinary(max)',
				@consume_time = @consume_time, @message_type = @message_type, @message_body = @message_body_value;
	END;
	ELSE
	BEGIN
		SET @sql = N'INSERT [dbo].[' + @queue_name + N'] ([message_type], [message_body]) VALUES (@message_type, @message_body);';
		EXECUTE sp_executesql @sql, N'@message_type nvarchar(128), @message_body varbinary(max)',
				@message_type = @message_type, @message_body = @message_body_value;
	END;

	RETURN 0;
END;
GO

-- ====================================
-- Create procedure to consume messages
-- ====================================
IF OBJECT_ID('dbo.sp_consume_message', 'P') IS NOT NULL
BEGIN
	DROP PROCEDURE [dbo].[sp_consume_message];
END;
GO

CREATE PROCEDURE [dbo].[sp_consume_message]
	@queue_name nvarchar(128),
	@number_of_messages int = 1
AS
BEGIN
	SET NOCOUNT ON;

	IF ([dbo].[fn_is_name_valid](@queue_name) = 0x00) THROW 50001, N'Bad queue name format.', 1;

	IF (@number_of_messages <= 0) THROW 50002, N'Invalid parameter value: @number_of_messages.', 1;

	DECLARE @type char(4);
	DECLARE @mode char(1);
	SELECT @type = [type], @mode = [mode] FROM [dbo].[queues] WHERE [name] = @queue_name;

	IF (@type IS NULL) THROW 50003, N'Queue is not found.', 1;

	DECLARE @sql nvarchar(1024);

	IF (@type IN ('FIFO', 'LIFO'))
	BEGIN
		SET @sql = N'WITH [cte] AS
			(
				SELECT TOP (@number_of_messages)
					[consume_order],
					[message_type],
					[message_body]
				FROM
					[dbo].[' + @queue_name + N'] WITH (rowlock' + IIF(@mode = 'S', N'', N', readpast') + N')
				ORDER BY
					[consume_order] ' + IIF(@type = 'FIFO', N'ASC', N'DESC') + N'
			)
			DELETE [cte] OUTPUT
				deleted.[message_type]                        AS [message_type],
				CAST(deleted.[message_body] AS nvarchar(max)) AS [message_body];';
	END;
	ELSE IF (@type = 'HEAP')
	BEGIN
		SET @sql = N'DELETE TOP (@number_of_messages)
				[dbo].[' + @queue_name + N'] WITH (rowlock, readpast)
			OUTPUT
				deleted.[message_type]                        AS [message_type],
				CAST(deleted.[message_body] AS nvarchar(max)) AS [message_body];';
	END;
	ELSE IF (@type = 'TIME')
	BEGIN
		SET @sql = N'WITH [cte] AS
			(
				SELECT TOP (@number_of_messages)
					[consume_time],
					[message_type],
					[message_body]
				FROM
					[dbo].[' + @queue_name + N'] WITH (rowlock' + IIF(@mode = 'S', N'', ', readpast') + N')
				WHERE
					[consume_time] < GETUTCDATE()
				ORDER BY
					[consume_time] ASC
			)
			DELETE [cte] OUTPUT
				deleted.[message_type]                        AS [message_type],
				CAST(deleted.[message_body] AS nvarchar(max)) AS [message_body];';
	END; 

	EXECUTE sp_executesql @sql, N'@number_of_messages int', @number_of_messages = @number_of_messages;

    RETURN @@ROWCOUNT;
END;
GO