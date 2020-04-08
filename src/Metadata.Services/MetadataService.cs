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
        void UploadMetadata(InfoBase infoBase);
        void RemoveMetadata(InfoBase infoBase);
        void InitializeMetadata(InfoBase infoBase);
        string MapSchemaIdentifier(string schemaName);
        string MapTableIdentifier(InfoBase infoBase, string objectName);
        IList<Field> MapColumnIdentifier(InfoBase infoBase, string tableName, string columnName);
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
        public void InitializeMetadata(InfoBase infoBase)
        {
            if (infoBase == null) throw new ArgumentNullException(nameof(infoBase));

            string metadataFilePath = BuildMetadataFilePath(infoBase);
            XMLLoader.Load(metadataFilePath, infoBase);

            string connectionString = BuildConnectionString(infoBase);
            SQLLoader.Load(connectionString, infoBase);

            string key = CreateCashKey(infoBase);
            if (!Cash.TryAdd(key, infoBase))
            {
                // TODO: do some warning ?
            }
        }
        public void UploadMetadata(InfoBase infoBase)
        {
            throw new NotImplementedException();
        }
        public void RemoveMetadata(InfoBase infoBase)
        {
            throw new NotImplementedException();
        }
        private string CreateCashKey(InfoBase infoBase)
        {
            return infoBase.Name + infoBase.Version;
        }
        private string BuildMetadataFilePath(InfoBase infoBase)
        {
            if (string.IsNullOrWhiteSpace(infoBase.Name)) throw new InvalidOperationException(nameof(infoBase));
            if (string.IsNullOrWhiteSpace(infoBase.Version)) throw new InvalidOperationException(nameof(infoBase));
            
            string metadataFilePath = Path.Combine(MetadataCatalogPath, infoBase.Name);
            if (!Directory.Exists(metadataFilePath))
            {
                _ = Directory.CreateDirectory(metadataFilePath);
            }
            
            metadataFilePath = Path.Combine(metadataFilePath, infoBase.Version) + ".xml";
            if (!File.Exists(metadataFilePath))
            {
                throw new FileNotFoundException(metadataFilePath);
            }

            return metadataFilePath;
        }
        private string BuildConnectionString(InfoBase infoBase)
        {
            SqlConnectionStringBuilder helper = new SqlConnectionStringBuilder()
            {
                DataSource = infoBase.Server,
                InitialCatalog = infoBase.Database,
                IntegratedSecurity = string.IsNullOrWhiteSpace(infoBase.UserName)
            };
            if (!helper.IntegratedSecurity)
            {
                helper.UserID = infoBase.UserName;
                helper.Password = infoBase.Password;
                helper.PersistSecurityInfo = false;
            }
            return helper.ToString();
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
        public string MapTableIdentifier(InfoBase infoBase, string objectName)
        {
            if (!objectName.Contains('+')) return objectName;

            if (infoBase == null) throw new ArgumentNullException(nameof(infoBase));
            string key = CreateCashKey(infoBase);
            if (!Cash.TryGetValue(key, out InfoBase cash)) return objectName;

            string tableName = objectName.TrimStart('[').TrimEnd(']');
            string[] identifiers = tableName.Split('+');

            BaseObject bo = cash.BaseObjects.Where(bo => bo.Name == identifiers[0]).FirstOrDefault();
            if (bo == null) return objectName;

            MetaObject mo = bo.MetaObjects.Where(mo => mo.Name == identifiers[1]).FirstOrDefault();
            if (mo == null) return objectName;

            if (identifiers.Length == 3)
            {
                mo = mo.MetaObjects.Where(mo => mo.Name == identifiers[2]).FirstOrDefault();
                if (mo == null) return objectName;
            }

            return mo.Table;
        }
        public IList<Field> MapColumnIdentifier(InfoBase infoBase, string objectName, string propertyName)
        {
            if (!objectName.Contains('+'))return null;

            if (infoBase == null) throw new ArgumentNullException(nameof(infoBase));
            string key = CreateCashKey(infoBase);
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