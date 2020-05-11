# one-c-sharp-sql (T-SQL Scripting)
Данный репозиторий является развитием идей [проекта 1C#](https://github.com/zhichkin/one-c-sharp)

one-c-sharp-sql в своей основе использует библиотеку Microsoft.SqlServer.TransactSql.ScriptDom, которая в том числе используется для редактора кода T-SQL в составе SQL Server Management Studio. Таким образом реализуется возможность использования практически всех возможностей синтаксиса SQL Server 2005-2016.

Использование библиотеки реализовано в виде web api сервиса.
Основными функциональными классами библиотеки являются MetadataService и ScriptingService.

Документация по one-c-sharp-sql и примеры практического использования:

[Язык запросов 1C#](https://infostart.ru/public/1226230/)

[JSON в запросах 1C#](https://infostart.ru/public/1228025/)
