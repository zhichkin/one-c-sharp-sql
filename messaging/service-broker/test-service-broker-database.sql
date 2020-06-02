USE [one-c-sharp-service-broker];
GO

-- ****************
-- * create queue *
-- ****************
--DECLARE @queue_name nvarchar(128) = N'test';
--EXEC [dbo].[sp_create_queue] @queue_name;
--SELECT @queue_name = [dbo].[fn_create_queue_name](@queue_name);
--SELECT @queue_name;
--SELECT [dbo].[fn_queue_exists](@queue_name); -- (0 = exists)
--SELECT * FROM sys.conversation_endpoints;

-- **************************************
-- * check if queue exists (0 = exists) *
-- **************************************
--DECLARE @queue nvarchar(128) = [dbo].[fn_create_queue_name](N'test');
--SELECT [dbo].[fn_queue_exists](@queue);

-- ****************
-- * send message *
-- ****************
--DECLARE @handle uniqueidentifier;
--DECLARE @queue nvarchar(128) = [dbo].[fn_create_queue_name](N'test');
--EXEC [dbo].[sp_get_local_dialog_handle] @handle OUTPUT, @queue;
--SELECT @handle;
--EXEC [dbo].[sp_send_message] @handle, N'test message тестовое сообщение';

-- **************************************************
-- * select messages to view - non-destructive read *
-- **************************************************
--SELECT
--	message_type_name AS [message_type],
--	DATALENGTH(message_body) AS [bytes],
--	CAST(message_body AS nvarchar(max)) AS [message_body]
--FROM [dbo].[275D1176-3D37-4797-9DEC-35BCAEF91E28/queue/test] WITH(NOLOCK);
--SELECT
--	N'Total bytes',
--	ISNULL(SUM(DATALENGTH(message_body)), 0) AS [total_bytes]
--FROM [dbo].[275D1176-3D37-4797-9DEC-35BCAEF91E28/queue/test] WITH(NOLOCK);

-- *******************************************************
-- * receive one or multiple messages - destructive read *
-- *******************************************************
--DECLARE @receive_queue nvarchar(128) = [dbo].[fn_create_queue_name](N'test');
--BEGIN TRANSACTION;
--EXEC [dbo].[sp_receive_message] @receive_queue;
----EXEC [dbo].[sp_receive_messages] @receive_queue, 1000, 10; -- receive multiple messages
--COMMIT TRANSACTION;
----ROLLBACK TRANSACTION; -- refuse delete message from queue

-- ****************
-- * delete queue *
-- ****************
--DECLARE @queue nvarchar(128) = [dbo].[fn_create_queue_name](N'test');
--EXEC [dbo].[sp_delete_queue] N'test';
--SELECT [dbo].[fn_queue_exists](@queue);
--SELECT * FROM sys.conversation_endpoints;