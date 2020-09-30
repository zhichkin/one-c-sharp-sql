# DaJet (c) QL (T-SQL Scripting)

DaJet QL в своей основе использует библиотеку Microsoft.SqlServer.TransactSql.ScriptDom, которая в том числе используется для редактора кода T-SQL в составе SQL Server Management Studio. Таким образом реализуется возможность использования практически всех возможностей синтаксиса SQL Server 2005-2016. Кроме этого это делает возможным расширять язык запросов DaJet QL.

Использование библиотеки реализовано в виде web api сервиса.
Основными функциональными классами библиотеки являются MetadataService и ScriptingService.

Документация по one-c-sharp-sql и примеры практического использования:

[DaJet QL - расширяемый язык запросов](https://infostart.ru/public/1226230/)

[JSON в запросах DaJet QL](https://infostart.ru/public/1228025/)

[Использование таблиц SQL Server в качестве очередей](https://github.com/zhichkin/one-c-sharp-sql/tree/master/sql/table-queues)
