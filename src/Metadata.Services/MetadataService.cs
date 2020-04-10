using Microsoft.Data.SqlClient;
using OneCSharp.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OneCSharp.Metadata.Services
{
    public interface IMetadataService
    {
        InfoBase CurrentDatabase { get; }
        void UseServer(string serverAddress);
        void UseDatabase(string databaseName);
        string MapSchemaIdentifier(string schemaName);
        string MapTableIdentifier(string databaseName, string tableIdentifier);
        IList<Field> MapColumnIdentifier(InfoBase infoBase, string tableName, string columnName);
        MetaObject GetMetaObject(string databaseName, string tableIdentifier);
        Property GetProperty(string databaseName, string tableIdentifier, string columnIdentifier);
    }
    public sealed class MetadataService : IMetadataService
    {
        private XMLMetadataLoader XMLLoader { get; } = new XMLMetadataLoader();
        private SQLMetadataLoader SQLLoader { get; } = new SQLMetadataLoader();
        private Dictionary<string, InfoBase> Cash { get; } = new Dictionary<string, InfoBase>();
        private string MetadataCatalogPath
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string assemblyCatalogPath = Path.GetDirectoryName(assembly.Location);
                string metadataCatalogPath = Path.Combine(assemblyCatalogPath, "Metadata");
                if (!Directory.Exists(metadataCatalogPath))
                {
                    _ = Directory.CreateDirectory(metadataCatalogPath);
                }
                return metadataCatalogPath;
            }
        }
        public string ConnectionString { get; private set; }
        public InfoBase CurrentDatabase { get; private set; }
        public void UseServer(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) throw new ArgumentNullException(nameof(address));

            SqlConnectionStringBuilder csb;
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                csb = new SqlConnectionStringBuilder()
                {
                    IntegratedSecurity = true
                };
            }
            else
            {
                csb = new SqlConnectionStringBuilder(ConnectionString);
            }
            csb.InitialCatalog = address;
            ConnectionString = csb.ToString();
        }
        public void UseDatabase(string identifier)
        {
            if (string.IsNullOrWhiteSpace(ConnectionString)) throw new InvalidOperationException(nameof(ConnectionString));
            if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentNullException(nameof(identifier));

            InfoBase infobase;
            string key = CreateCashKey(identifier);

            if (Cash.TryGetValue(key, out infobase))
            {
                CurrentDatabase = infobase;
            }
            else
            {
                SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(ConnectionString)
                {
                    InitialCatalog = identifier
                };
                ConnectionString = csb.ToString();

                InitializeMetadata(identifier, out infobase);
                Cash.Add(key, infobase);
                CurrentDatabase = infobase;
            }
        }
        private string CreateCashKey(string databaseIdentifier)
        {
            return databaseIdentifier;
        }
        private string BuildMetadataFilePath(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) throw new InvalidOperationException(nameof(identifier));

            string metadataFilePath = Path.Combine(MetadataCatalogPath, identifier + ".xml");
            if (!File.Exists(metadataFilePath))
            {
                throw new FileNotFoundException(metadataFilePath);
            }
            return metadataFilePath;
        }
        private void InitializeMetadata(string identifier, out InfoBase infobase)
        {
            if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentNullException(nameof(identifier));

            infobase = new InfoBase() { Database = identifier };
            string metadataFilePath = BuildMetadataFilePath(identifier);
            XMLLoader.Load(metadataFilePath, infobase);
            SQLLoader.Load(ConnectionString, infobase);
        }

        public MetaObject GetMetaObject(string databaseName, string tableIdentifier) // $"[Документ+ПоступлениеТоваровУслуг+Товары]"
        {
            if (!tableIdentifier.Contains('+')) return null; // this is not 1C format, but schema object (table)

            string key;
            if (string.IsNullOrEmpty(databaseName))
            {
                key = CreateCashKey(CurrentDatabase.Database);
            }
            else
            {
                key = CreateCashKey(databaseName);
            }
            if (!Cash.TryGetValue(key, out InfoBase cash)) return null;

            string tableName = tableIdentifier.TrimStart('[').TrimEnd(']');
            string[] identifiers = tableName.Split('+');

            BaseObject bo = cash.BaseObjects.Where(bo => bo.Name == identifiers[0]).FirstOrDefault();
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
        public IList<Field> MapColumnIdentifier(InfoBase infoBase, string objectName, string propertyName)
        {
            if (!objectName.Contains('+'))return null;

            if (infoBase == null) throw new ArgumentNullException(nameof(infoBase));
            string key = CreateCashKey(infoBase.Database);
            if (!Cash.TryGetValue(key, out InfoBase cash)) return null;

            string tableName = objectName.TrimStart('[').TrimEnd(']');
            string[] identifiers = tableName.Split('+');

            BaseObject bo = cash.BaseObjects.Where(bo => bo.Name == identifiers[0]).FirstOrDefault();
            if (bo == null) return null;
            MetaObject mo = bo.MetaObjects.Where(mo => mo.Name == identifiers[1]).FirstOrDefault();
            if (mo == null) return null;
            if (identifiers.Length == 3)
            {
                mo = mo.MetaObjects.Where(mo => mo.Name == identifiers[2]).FirstOrDefault();
                if (mo == null) return null;
            }
            Property property = mo.Properties.Where(p => p.Name == propertyName).FirstOrDefault();
            if (property == null) return null;
            if (property.Fields.Count == 0) return null;

            return property.Fields;
        }
    }
}