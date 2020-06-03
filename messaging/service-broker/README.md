# Использование SQL Server Service Broker
# (локальные очереди - на одном сервере)

**Возможности.**
1. Создание / удаление очередей.
2. Отправка сообщений (только по одному за раз).
3. Получение одного или нескольких сообщений за один раз.
3. Ожидание поступления сообщений в очередь в спящем режиме.

**Установка.**
1. Выполнить скрипт **install-service-broker-database.sql** на сервере SQL Server.

Будет создана база данных **one-c-sharp-service-broker**

**Использование.**
- Используйте обработку 1С OneCSharpServiceBroker.epf
- Примеры кода SQL в файле test-service-broker-database.sql

**Тестирование производительности.**

Аналогична варианту использования таблиц SQL Server в качестве очередей.

Подробности: https://github.com/zhichkin/one-c-sharp-sql/tree/master/messaging/table-queues

**Дополнительная информация.**

Возможна доработка варианта, когда очереди находятся на разных серверах SQL Server. При таком варианте использования отправка сообщений осуществляется локально на одном сервере, а Service Broker берёт на себя маршрутизацию и доставку такого сообщения в очередь, расположенную на другом инстансе SQL Server.

<a href="https://youtu.be/NGlvyD4CmiQ" target="_blank"><img src="https://img.youtube.com/vi/NGlvyD4CmiQ/mqdefault.jpg" alt="ALT-SQL Server Service Broker (presentation)" width="300" height="180" border="10" /></a>