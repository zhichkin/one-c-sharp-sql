using Microsoft.Data.SqlClient;
using OneCSharp.Core.Model;
using OneCSharp.SQL.Model;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OneCSharp.SQL.Services
{
    public sealed class SQLHelper
    {
        private readonly Regex rxFld = new Regex("_Fld\\d+$"); // Example: "СуммаНДС (_Fld123)"

        private sealed class SqlFieldInfo
        {
            public SqlFieldInfo() { }
            public int ORDINAL_POSITION;
            public string COLUMN_NAME;
            public string DATA_TYPE;
            public int CHARACTER_MAXIMUM_LENGTH;
            public byte NUMERIC_PRECISION;
            public int NUMERIC_SCALE;
            public bool IS_NULLABLE;
            public bool IsFound;
        }
        private sealed class ClusteredIndexInfo
        {
            public ClusteredIndexInfo() { }
            public string NAME;
            public bool IS_UNIQUE;
            public bool IS_PRIMARY_KEY;
            public List<ClusteredIndexColumnInfo> COLUMNS = new List<ClusteredIndexColumnInfo>();
            public bool HasNullableColumns
            {
                get
                {
                    bool result = false;
                    foreach (ClusteredIndexColumnInfo item in COLUMNS)
                    {
                        if (item.IS_NULLABLE)
                        {
                            return true;
                        }
                    }
                    return result;
                }
            }
            public ClusteredIndexColumnInfo GetColumnByName(string name)
            {
                ClusteredIndexColumnInfo info = null;
                for (int i = 0; i < COLUMNS.Count; i++)
                {
                    if (COLUMNS[i].NAME == name) return COLUMNS[i];
                }
                return info;
            }
        }
        private sealed class ClusteredIndexColumnInfo
        {
            public ClusteredIndexColumnInfo() { }
            public byte KEY_ORDINAL;
            public string NAME;
            public bool IS_NULLABLE;
        }
        private List<SqlFieldInfo> GetSqlFields(string tableName)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    ORDINAL_POSITION, COLUMN_NAME, DATA_TYPE,");
            sb.AppendLine(@"    ISNULL(CHARACTER_MAXIMUM_LENGTH, 0) AS CHARACTER_MAXIMUM_LENGTH,");
            sb.AppendLine(@"    ISNULL(NUMERIC_PRECISION, 0) AS NUMERIC_PRECISION,");
            sb.AppendLine(@"    ISNULL(NUMERIC_SCALE, 0) AS NUMERIC_SCALE,");
            sb.AppendLine(@"    CASE WHEN IS_NULLABLE = 'NO' THEN CAST(0x00 AS bit) ELSE CAST(0x01 AS bit) END AS IS_NULLABLE");
            sb.AppendLine(@"FROM");
            sb.AppendLine(@"    INFORMATION_SCHEMA.COLUMNS");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    TABLE_NAME = N'{0}'");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"    ORDINAL_POSITION ASC;");

            string sql = string.Format(sb.ToString(), tableName);

            List<SqlFieldInfo> list = new List<SqlFieldInfo>();
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            SqlFieldInfo item = new SqlFieldInfo()
                            {
                                ORDINAL_POSITION = reader.GetInt32(0),
                                COLUMN_NAME = reader.GetString(1),
                                DATA_TYPE = reader.GetString(2),
                                CHARACTER_MAXIMUM_LENGTH = reader.GetInt32(3),
                                NUMERIC_PRECISION = reader.GetByte(4),
                                NUMERIC_SCALE = reader.GetInt32(5),
                                IS_NULLABLE = reader.GetBoolean(6)
                            };
                            list.Add(item);
                        }
                    }
                }
            }
            return list;
        }
        private ClusteredIndexInfo GetClusteredIndexInfo(string tableName)
        {
            ClusteredIndexInfo info = null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT");
            sb.AppendLine(@"    i.name,");
            sb.AppendLine(@"    i.is_unique,");
            sb.AppendLine(@"    i.is_primary_key,");
            sb.AppendLine(@"    c.key_ordinal,");
            sb.AppendLine(@"    f.name,");
            sb.AppendLine(@"    f.is_nullable");
            sb.AppendLine(@"FROM sys.indexes AS i");
            sb.AppendLine(@"INNER JOIN sys.tables AS t ON t.object_id = i.object_id");
            sb.AppendLine(@"INNER JOIN sys.index_columns AS c ON c.object_id = t.object_id AND c.index_id = i.index_id");
            sb.AppendLine(@"INNER JOIN sys.columns AS f ON f.object_id = t.object_id AND f.column_id = c.column_id");
            sb.AppendLine(@"WHERE");
            sb.AppendLine(@"    t.object_id = OBJECT_ID(@table) AND i.type = 1 -- CLUSTERED");
            sb.AppendLine(@"ORDER BY");
            sb.AppendLine(@"c.key_ordinal ASC;");
            string sql = sb.ToString();

            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();

                command.Parameters.AddWithValue("table", tableName);
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        info = new ClusteredIndexInfo()
                        {
                            NAME = reader.GetString(0),
                            IS_UNIQUE = reader.GetBoolean(1),
                            IS_PRIMARY_KEY = reader.GetBoolean(2)
                        };
                        info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                        {
                            KEY_ORDINAL = reader.GetByte(3),
                            NAME = reader.GetString(4),
                            IS_NULLABLE = reader.GetBoolean(5)
                        });
                        while (reader.Read())
                        {
                            info.COLUMNS.Add(new ClusteredIndexColumnInfo()
                            {
                                KEY_ORDINAL = reader.GetByte(3),
                                NAME = reader.GetString(4),
                                IS_NULLABLE = reader.GetBoolean(5)
                            });
                        }
                    }
                }
            }
            return info;
        }


        public void UUID_1C_to_SQL(string UUID)
        {
            // TODO: convert one UUID to another
            //61ac5c5b-6053-4846-bfee-1de510c2baf8 // 1C
            //E51DEEBF-C210-F8BA-4846-605361AC5C5B // SQL
        }
        public void UUID_SQL_to_1C(string UUID)
        {
            // TODO: convert one UUID to another
            //E51DEEBF-C210-F8BA-4846-605361AC5C5B // SQL
            //61ac5c5b-6053-4846-bfee-1de510c2baf8 // 1C
        }

        public string ConnectionString { get; set; }
        public void Load(Database infoBase)
        {
            foreach (Namespace ns in infoBase.Namespaces)
            {
                List<Task> tasks = new List<Task>();
                foreach (var item in ns.DataTypes)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        GetSQLMetadata((MetaObject)item);
                    }));
                }
                Task all = Task.WhenAll(tasks);
                _ = all.Wait(Timeout.Infinite);
            }
        }
        public async Task LoadAsync(Database infoBase)
        {
            foreach (Namespace ns in infoBase.Namespaces)
            {
                List<Task> tasks = new List<Task>();
                foreach (var item in ns.DataTypes)
                {
                    tasks.Add(Task.Run(() =>
                    {
                        GetSQLMetadata((MetaObject)item);
                    }));
                }
                await Task.WhenAll(tasks);
            }
        }
        private void GetSQLMetadata(MetaObject metaObject)
        {
            ReadSQLMetadata(metaObject);
        }
        private void ReadSQLMetadata(MetaObject metaObject)
        {
            List<SqlFieldInfo> sql_fields = GetSqlFields(metaObject.Name);
            if (sql_fields.Count == 0) return;

            ClusteredIndexInfo indexInfo = this.GetClusteredIndexInfo(metaObject.Name);
            if (indexInfo == null) { /* TODO: handle situation somehow*/ }

            foreach (var property in metaObject.Properties)
            {
                if (property.ValueType is ListType listType)
                {
                    ReadSQLMetadata((MetaObject)listType.Type);
                    continue;
                }

                string search;
                Match match = rxFld.Match(property.Name);
                if (match.Success)
                {
                    property.Name = property.Name.Replace(match.Value, string.Empty);
                    search = match.Value;
                }
                else if (property.Name == DBToken.OwnerID)
                {
                    property.Name = "Владелец";
                    search = $"_{DBToken.OwnerID}";
                }
                else
                {
                    continue;
                }
                var fields = sql_fields.Where(f => f.COLUMN_NAME.StartsWith(search));
                foreach (var field in fields)
                {
                    field.IsFound = true;
                    AddMetaObjectField(property, field, indexInfo);
                }
            }

            int position = 0;
            var nonFounds = sql_fields.Where(f => f.IsFound == false);
            foreach (var field in nonFounds) // СтандартныеРеквизиты - их нет в файле DBNames !
            {
                AddProperty(metaObject, field, indexInfo, position);
                position++;
            }
        }
        private void AddMetaObjectField(Property property, SqlFieldInfo info, ClusteredIndexInfo indexInfo)
        {
            Field field = new Field()
            {
                Owner = property,
                Name = info.COLUMN_NAME
            };
            property.Fields.Add(field);
            
            field.TypeName = info.DATA_TYPE;
            field.Length = info.CHARACTER_MAXIMUM_LENGTH;
            field.Precision = info.NUMERIC_PRECISION;
            field.Scale = info.NUMERIC_SCALE;
            field.IsNullable = info.IS_NULLABLE;

            DefineMetaObjectFieldPurpose(field);

            if (indexInfo != null)
            {
                ClusteredIndexColumnInfo columnInfo = indexInfo.GetColumnByName(info.COLUMN_NAME);
                if (columnInfo != null)
                {
                    field.IsPrimaryKey = true;
                    field.KeyOrdinal = columnInfo.KEY_ORDINAL;
                }
            }
        }
        private void AddProperty(MetaObject metaObject, SqlFieldInfo info, ClusteredIndexInfo indexInfo, int position)
        {
            Property property = new Property
            {
                Owner = metaObject,
                Name = info.COLUMN_NAME
            };
            metaObject.Properties.Insert(position, property);
            AddMetaObjectField(property, info, indexInfo);
            DefineSystemPropertyType(property);
        }
        private void DefineSystemPropertyType(Property property)
        {
            string name = property.Name.TrimStart('_'); // TODO: _Date_Time

            if (name == DBToken.IDRRef)
            {
                property.Name = "Ссылка";
                property.ValueType = SimpleType.UniqueIdentifier; //Types.Add(new TypeInfo() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.RecorderRRef) // TODO: определять при чтении метаданных из Config
            {
                property.Name = "Регистратор_Cсылка";
                property.ValueType = SimpleType.UniqueIdentifier; //.Types.Add(new TypeInfo() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.RecorderTRef) // TODO: определять при чтении метаданных из Config
            {
                property.Name = "Регистратор_Тип";
                property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            if (name == DBToken.EnumOrder)
            {
                property.Name = "Порядок";
                property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            else if (name == DBToken.Version)
            {
                if (property.Fields.Count > 0)
                {
                    property.Fields[0].Purpose = FieldPurpose.Version;
                }
                property.Name = "Version";
                property.ValueType = SimpleType.Binary; //.Types.Add(new TypeInfo() { Name = "Version", TypeCode = -7 });
                return;
            }
            else if (name == DBToken.Marked)
            {
                property.Name = "ПометкаУдаления";
                property.ValueType = SimpleType.Boolean; //.Types.Add(new TypeInfo() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == "Date_Time") // DBToken.DateTime
            {
                property.Name = "Дата";
                property.ValueType = SimpleType.DateTime; //.Types.Add(new TypeInfo() { Name = "DateTime", TypeCode = -3 });
                return;
            }
            else if (name == DBToken.NumberPrefix)
            {
                property.Name = "МоментВремени";
                property.ValueType = SimpleType.DateTime; //.Types.Add(new TypeInfo() { Name = "DateTime", TypeCode = -3 });
                return;
            }
            else if (name == DBToken.Number)
            {
                property.Name = "Номер";
                if (property.Fields.Count > 0)
                {
                    if (property.Fields[0].TypeName.Contains("char"))
                    {
                        property.ValueType = SimpleType.String; //.Types.Add(new TypeInfo() { Name = "String", TypeCode = -2 });
                    }
                    else
                    {
                        property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                    }
                }
                else
                {
                    property.ValueType = SimpleType.String; //.Types.Add(new TypeInfo() { Name = "String", TypeCode = -2 });
                }
                return;
            }
            else if (name == DBToken.Posted)
            {
                property.Name = "Проведён";
                property.ValueType = SimpleType.Boolean; //.Types.Add(new TypeInfo() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.PredefinedID)
            {
                property.Name = "ИдентификаторПредопределённого";
                property.ValueType = SimpleType.UniqueIdentifier; //.Types.Add(new TypeInfo() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.Description)
            {
                property.Name = "Наименование";
                property.ValueType = SimpleType.String; //.Types.Add(new TypeInfo() { Name = "String", TypeCode = -2 });
                return;
            }
            else if (name == DBToken.Code)
            {
                property.Name = "Код";
                if (property.Fields.Count > 0)
                {
                    if (property.Fields[0].TypeName.Contains("char"))
                    {
                        property.ValueType = SimpleType.String; //.Types.Add(new TypeInfo() { Name = "String", TypeCode = -2 });
                    }
                    else
                    {
                        property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                    }
                }
                else
                {
                    property.ValueType = SimpleType.String; //.Types.Add(new TypeInfo() { Name = "String", TypeCode = -2 });
                }
                return;
            }
            else if (name == DBToken.Folder)
            {
                property.Name = "ЭтоГруппа";
                property.ValueType = SimpleType.Boolean; //.Types.Add(new TypeInfo() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.KeyField)
            {
                property.Name = "KeyField";
                property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            else if (name.Contains(DBToken.LineNo))
            {
                property.Name = "НомерСтроки";
                property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                return;
            }
            else if (name == DBToken.ParentIDRRef) // adjacency list - иерархический справочник
            {
                property.Name = "Родитель";
                property.ValueType = property.Owner;
                return;
            }
            else if (name.Contains(DBToken.OwnerID))
            {
                property.Name = "Владелец";
                return;
            }
            else if (name.Contains(DBToken.IDRRef)) // табличная часть
            {
                property.Name = "Ссылка";
                property.ValueType = SimpleType.UniqueIdentifier; //.Types.Add(new TypeInfo() { Name = "UUID", TypeCode = -6 });
                return;
            }
            else if (name == DBToken.Period)
            {
                property.Name = "Период";
                property.ValueType = SimpleType.DateTime; //.Types.Add(new TypeInfo() { Name = "DateTime", TypeCode = -3 });
                return;
            }
            else if (name == DBToken.Active)
            {
                property.Name = "Активность";
                property.ValueType = SimpleType.Boolean; //.Types.Add(new TypeInfo() { Name = "Boolean", TypeCode = -1 });
                return;
            }
            else if (name == DBToken.RecordKind) // Перечисление: Приход | Расход
            {
                property.Name = "ВидДвижения";
                property.ValueType = SimpleType.Numeric; //.Types.Add(new TypeInfo() { Name = "Numeric", TypeCode = -4 });
                return;
            }
        }
        private void DefineMetaObjectFieldPurpose(Field field)
        {
            if (string.IsNullOrEmpty(field.Name))
            {
                field.Purpose = FieldPurpose.Value;
                return;
            }

            if (field.TypeName == "image" || field.TypeName == "varbinary")
            {
                field.Purpose = FieldPurpose.Binary;
                return;
            }

            if (char.IsDigit(field.Name[field.Name.Length - 1]))
            {
                field.Purpose = FieldPurpose.Value;
                return;
            }

            if (field.Name.EndsWith(DBToken.RRRef))
            {
                field.Purpose = FieldPurpose.Object;
                return;
            }
            else if (field.Name.EndsWith(DBToken.RTRef))
            {
                field.Purpose = FieldPurpose.TypeCode;
                return;
            }
            else if (field.Name.EndsWith(DBToken.TYPE))
            {
                field.Purpose = FieldPurpose.Discriminator;
                return;
            }
            else if (field.Name.EndsWith(DBToken.RRef))
            {
                field.Purpose = FieldPurpose.Object;
                return;
            }
            else if (field.Name.EndsWith(DBToken.TRef))
            {
                field.Purpose = FieldPurpose.TypeCode;
                return;
            }
            else if (field.Name.EndsWith(DBToken.S))
            {
                field.Purpose = FieldPurpose.String;
                return;
            }
            else if (field.Name.EndsWith(DBToken.N))
            {
                field.Purpose = FieldPurpose.Numeric;
                return;
            }
            else if (field.Name.EndsWith(DBToken.L))
            {
                field.Purpose = FieldPurpose.Boolean;
                return;
            }
            else if (field.Name.EndsWith(DBToken.T))
            {
                field.Purpose = FieldPurpose.DateTime;
                return;
            }

            field.Purpose = FieldPurpose.Value;
        }
    }
}