using Microsoft.Data.SqlClient;
using OneCSharp.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OneCSharp.Metadata.Services
{
    public interface IMetadataService
    {
        DatabaseServer CurrentServer { get; }
        InfoBase CurrentDatabase { get; }
        string ConnectionString { get; }
        void Configure(MetadataServiceSettings settings);
        void UseServer(string serverName);
        void UseDatabase(string databaseName);

        string MapSchemaIdentifier(string schemaName);
        string MapTableIdentifier(string databaseName, string tableIdentifier);
        MetaObject GetMetaObject(IList<string> tableIdentifiers);
        MetaObject GetMetaObject(string databaseName, string tableIdentifier);
        Property GetProperty(string databaseName, string tableIdentifier, string columnIdentifier);
    }
    public sealed class MetadataService : IMetadataService
    {
        private const string ERROR_SERVICE_IS_NOT_CONFIGURED = "Metadata service is not configured properly!";
        private const string ERROR_SERVER_IS_NOT_DEFINED = "Current database server is not defined!";
        private XMLMetadataLoader XMLLoader { get; } = new XMLMetadataLoader();
        private SQLMetadataLoader SQLLoader { get; } = new SQLMetadataLoader();
        private MetadataServiceSettings Settings { get; set; }
        public DatabaseServer CurrentServer { get; private set; }
        public InfoBase CurrentDatabase { get; private set; }
        public string ConnectionString { get; private set; }
        private string ServerCatalogPath(string serverName)
        {
            return Path.Combine(Settings.Catalog, serverName);
        }
        private string MetadataFilePath(string serverName, string databaseName)
        {
            return Path.Combine(ServerCatalogPath(serverName), databaseName + ".xml");
        }
        public void Configure(MetadataServiceSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.Catalog)) throw new ArgumentNullException(nameof(settings.Catalog));
            if (!Directory.Exists(settings.Catalog)) throw new DirectoryNotFoundException(settings.Catalog);

            Settings = settings;

            if (settings.Servers.Count == 0) return;

            int s = 0;
            int i = 0;
            InfoBase database;
            DatabaseServer server;
            string serverCatalogPath;
            string metadataFilePath;
            while (s < settings.Servers.Count)
            {
                server = settings.Servers[s];
                serverCatalogPath = ServerCatalogPath(server.Name);

                if (server == null || string.IsNullOrWhiteSpace(server.Name) || !Directory.Exists(serverCatalogPath))
                {
                    settings.Servers.RemoveAt(s);
                    continue;
                }
                s++;

                if (server.Databases.Count == 0) continue;

                while (i < server.Databases.Count)
                {
                    database = server.Databases[i];
                    metadataFilePath = MetadataFilePath(server.Name, database.Name);

                    if (database == null || string.IsNullOrWhiteSpace(database.Name) || !File.Exists(metadataFilePath))
                    {
                        server.Databases.RemoveAt(i);
                        continue;
                    }
                    i++;

                    InitializeMetadata(database, metadataFilePath);
                }
            }
        }
        private void InitializeMetadata(InfoBase infobase, string metadataFilePath)
        {
            XMLLoader.Load(metadataFilePath, infobase);
            //SQLLoader.Load(ConnectionString, infobase); // TODO: optimize loading of SQL metadata time !
        }
        public void UseServer(string serverName)
        {
            if (string.IsNullOrWhiteSpace(serverName)) throw new ArgumentNullException(nameof(serverName));
            if (Settings == null) throw new InvalidOperationException(ERROR_SERVICE_IS_NOT_CONFIGURED);

            string catalogPath = ServerCatalogPath(serverName);
            if (!Directory.Exists(catalogPath)) throw new DirectoryNotFoundException(catalogPath);

            DatabaseServer server = Settings.Servers.Where(s => s.Name == serverName).FirstOrDefault();
            if (server == null)
            {
                server = new DatabaseServer() { Name = serverName };
                Settings.Servers.Add(server);
            }
            
            SqlConnectionStringBuilder csb;
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                csb = new SqlConnectionStringBuilder() { IntegratedSecurity = true };
            }
            else
            {
                csb = new SqlConnectionStringBuilder(ConnectionString);
            }
            csb.DataSource = string.IsNullOrWhiteSpace(server.Address) ? server.Name : server.Address;
            ConnectionString = csb.ToString();

            CurrentServer = server;
        }
        public void UseDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName)) throw new ArgumentNullException(nameof(databaseName));
            if (Settings == null) throw new InvalidOperationException(ERROR_SERVICE_IS_NOT_CONFIGURED);
            if (CurrentServer == null) throw new InvalidOperationException(ERROR_SERVER_IS_NOT_DEFINED);

            string metadataFilePath = MetadataFilePath(CurrentServer.Name, databaseName);
            if (!File.Exists(metadataFilePath)) throw new DirectoryNotFoundException(metadataFilePath);

            InfoBase database = CurrentServer.Databases.Where(db => db.Name == databaseName).FirstOrDefault();
            if (database == null)
            {
                database = new InfoBase() { Name = databaseName };
                InitializeMetadata(database, metadataFilePath);
                CurrentServer.Databases.Add(database);
            }
            
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString)
            {
                InitialCatalog = database.Name
            };
            ConnectionString = csb.ToString();

            CurrentDatabase = database;
        }
        
        private bool IsSpecialSchema(string schemaName)
        {
            return (schemaName == "Перечисление"
                || schemaName == "Справочник"
                || schemaName == "Документ"
                || schemaName == "ПланВидовХарактеристик"
                || schemaName == "ПланСчетов"
                || schemaName == "ПланОбмена"
                || schemaName == "РегистрСведений"
                || schemaName == "РегистрНакопления"
                || schemaName == "РегистрБухгалтерии");
        }
        public MetaObject GetMetaObject(IList<string> tableIdentifiers)
        {
            if (tableIdentifiers == null || tableIdentifiers.Count != 4) { return null; }

            string databaseName = null;
            string serverIdentifier = tableIdentifiers[0];
            string databaseIdentifier = tableIdentifiers[1];
            string schemaIdentifier = tableIdentifiers[2];
            string tableIdentifier = tableIdentifiers[3];

            if (serverIdentifier != null)
            {
                if (tableIdentifier.Contains('+')) // [server].[database].Документ.[ПоступлениеТоваровУслуг+Товары]
                {
                    databaseName = tableIdentifiers[1];
                    tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = string.Empty; // dbo
                }
                else
                {
                    if (IsSpecialSchema(databaseIdentifier)) // [database].Документ.ПоступлениеТоваровУслуг.Товары
                    {
                        databaseName = tableIdentifiers[0];
                        tableIdentifiers[3] = $"{databaseIdentifier}+{schemaIdentifier}+{tableIdentifier}";
                        tableIdentifiers[2] = string.Empty; // dbo
                        tableIdentifiers[1] = serverIdentifier;
                        tableIdentifiers[0] = null;
                    }
                    else if (IsSpecialSchema(schemaIdentifier)) // [server].[database].Документ.ПоступлениеТоваровУслуг
                    {
                        databaseName = tableIdentifiers[1];
                        tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                        tableIdentifiers[2] = string.Empty; // dbo
                    }
                }
            }
            else if (databaseIdentifier != null)
            {
                if (IsSpecialSchema(databaseIdentifier)) // Документ.ПоступлениеТоваровУслуг.Товары
                {
                    databaseName = tableIdentifiers[1];
                    tableIdentifiers[3] = $"{databaseIdentifier}+{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = null;
                    tableIdentifiers[1] = null;
                }
                else if (IsSpecialSchema(schemaIdentifier)) // [database].Документ.ПоступлениеТоваровУслуг
                {
                    databaseName = tableIdentifiers[1];
                    tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = string.Empty; // dbo
                }
            }
            else if (schemaIdentifier != null)
            {
                if (IsSpecialSchema(schemaIdentifier)) // Документ.ПоступлениеТоваровУслуг
                {
                    tableIdentifiers[3] = $"{schemaIdentifier}+{tableIdentifier}";
                    tableIdentifiers[2] = null;
                }
            }
            else // ПоступлениеТоваровУслуг or some normal table
            {
                return null;
            }

            return GetMetaObject(databaseName, tableIdentifiers[3]);
        }
        public MetaObject GetMetaObject(string databaseName, string tableIdentifier) // $"[Документ+ПоступлениеТоваровУслуг+Товары]"
        {
            if (!tableIdentifier.Contains('+')) return null; // this is not special format, but schema object (table)

            InfoBase database;
            if (string.IsNullOrEmpty(databaseName))
            {
                database = CurrentDatabase;
            }
            else
            {
                database = CurrentServer.Databases.Where(db => db.Name == databaseName).FirstOrDefault();
            }
            if (database == null) return null;

            string tableName = tableIdentifier.TrimStart('[').TrimEnd(']');
            string[] identifiers = tableName.Split('+');

            BaseObject bo = database.BaseObjects.Where(bo => bo.Name == identifiers[0]).FirstOrDefault();
            if (bo == null) return null;

            MetaObject @object = bo.MetaObjects.Where(mo => mo.Name == identifiers[1]).FirstOrDefault();
            if (@object == null) return null;

            if (identifiers.Length == 3)
            {
                @object = @object.MetaObjects.Where(mo => mo.Name == identifiers[2]).FirstOrDefault();
                if (@object == null) return null;
            }

            return @object;
        }
        public Property GetProperty(string databaseName, string tableIdentifier, string columnIdentifier)
        {
            MetaObject @object = GetMetaObject(databaseName, tableIdentifier);
            if (@object == null) return null;

            Property @property = @object.Properties.Where(p => p.Name == columnIdentifier).FirstOrDefault();
            if (@property == null) return null;
            if (@property.Fields.Count == 0) return null;

            return @property;
        }

        public string MapSchemaIdentifier(string schemaName)
        {
            if (schemaName == "Перечисление"
                || schemaName == "Справочник"
                || schemaName == "Документ"
                || schemaName == "ПланВидовХарактеристик"
                || schemaName == "ПланСчетов"
                || schemaName == "ПланОбмена"
                || schemaName == "РегистрСведений"
                || schemaName == "РегистрНакопления"
                || schemaName == "РегистрБухгалтерии")
            {
                return string.Empty; // default schema name = dbo
            }
            return schemaName;
        }
        public string MapTableIdentifier(string databaseName, string tableIdentifier)
        {
            MetaObject @object = GetMetaObject(databaseName, tableIdentifier);
            if (@object == null)
            {
                return tableIdentifier;
            }
            return @object.Table;
        }
    }
}