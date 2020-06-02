Использование таблиц SQL Server в качестве очередей сообщений

Возможности.
1. Очереди сообщений типа 'FIFO', 'LIFO', 'HEAP', 'TIME', 'FILE'.
2. Режим конкурентного доступа к очередям 'S' (single) и 'M' (multiple).

API базы данных:
1. Хранимые процедуры:
- sp_create_queue @name, @type, @mode
- sp_delete_queue @name
- sp_produce_message @queue_name, @message_body, @message_type, @consume_time
- sp_consume_message @queue_name, @number_of_messages
2. Скалярные функции:
- fn_queue_exists @name returns int (0 = false, 1 = true)
- fn_is_name_valid @name returns bit (служебная функция)

Установка.
1. Запустить скрипт install-database.sql на SQL Server.
Будет создана база данных one-c-sharp-table-queues

Использование.
- Можно посмотреть как использовать в файле test-produce-consume-message.sql
- Можно воспользоваться обработкой 1С OneCSharpTableQueues.epf

Дополнительная информация.
https://infostart.ru/public/1214312/

Disclaimer.
- Технически данная версия поддерживает работу с очередями типа 'FIFO', 'LIFO', 'HEAP', 'TIME'.
  Тип очереди 'FILE' находится в разработке (для передачи больших файлов).
- Практически в обработке 1С реализована только работа с типом очереди 'FIFO' с режимом конкурентного доступа 'S'.
