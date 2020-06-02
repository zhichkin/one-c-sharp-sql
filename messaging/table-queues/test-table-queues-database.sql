USE [one-c-sharp-table-queues];
GO

DECLARE @count int;
DECLARE @name nvarchar(128) = 'test'; -- имя тестовой очереди
DECLARE @message_body nvarchar(max) = 'test тест'; -- тело тестового сообщения

-- Создание и удаление очередей
--EXEC [dbo].[sp_create_queue] @name;
--EXEC [dbo].[sp_delete_queue] @name;
--SELECT [dbo].[fn_queue_exists](@name);

-- Отправка сообщения в очередь
--EXEC [dbo].[sp_produce_message] @name, @message_body;

-- Получение сообщений из очереди
--BEGIN TRANSACTION;
--EXEC @count = [dbo].[sp_consume_message] @name, 1;
--SELECT @count;
--ROLLBACK TRANSACTION;
--COMMIT TRANSACTION;

-- Мониторинг сообщений в очередях
--SELECT [name], [type], [mode] FROM [one-c-sharp-table-queues].[dbo].[queues];

SELECT TOP (10)
	[consume_order],
	(CASE WHEN [message_type] = '' THEN 'default' ELSE [message_type] END) AS [message_type],
	CAST([message_body] AS nvarchar(max)) AS [message_body]
FROM
	[one-c-sharp-table-queues].[dbo].[test] WITH (NOLOCK);

