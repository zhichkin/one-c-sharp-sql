using Microsoft.Data.SqlClient;
using OneCSharp.Core.Model;
using OneCSharp.SQL.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace OneCSharp.SQL.Services
{
    internal sealed class DBNameEntry
    {
        internal string Token = string.Empty; // type of meta object
        internal MetaObject MetaObject = new MetaObject();
        internal List<DBName> DBNames = new List<DBName>();
    }
    internal sealed class DBName
    {
        internal string Token;
        internal int TypeCode;
        internal bool IsMainTable;
    }

    internal delegate void SpecialParser(StreamReader reader, string line, MetaObject table);

    public interface IMetadataReader
    {
        bool CheckServerConnection(Server server);
        List<Database> GetDatabases(Server server);
        Task ReadMetadataAsync(Database infoBase, IProgress<string> progress);
    }
    public sealed class MetadataReader : IMetadataReader
    {
        private readonly object syncRoot = new object();
        private readonly ILogger _logger;
        private readonly Dictionary<string, DBNameEntry> _DBNames = new Dictionary<string, DBNameEntry>();
        private readonly Dictionary<string, MetaObject> _internal_UUID = new Dictionary<string, MetaObject>();
        public MetadataReader()
        {
            _logger = null; //new TextFileLogger();

            _SpecialParsers.Add("cf4abea7-37b2-11d4-940f-008048da11f9", ParseMetaObjectProperties); // Catalogs properties collection
            _SpecialParsers.Add("932159f9-95b2-4e76-a8dd-8849fe5c5ded", ParseNestedObjects); // Catalogs nested objects collection
            _SpecialParsers.Add("45e46cbc-3e24-4165-8b7b-cc98a6f80211", ParseMetaObjectProperties); // Documents properties collection
            _SpecialParsers.Add("21c53e09-8950-4b5e-a6a0-1054f1bbc274", ParseNestedObjects); // Documents nested objects collection

            _SpecialParsers.Add("13134203-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция измерений регистра сведений
            _SpecialParsers.Add("13134202-f60b-11d5-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция ресурсов регистра сведений
            _SpecialParsers.Add("a2207540-1400-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра сведений

            _SpecialParsers.Add("b64d9a43-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция измерений регистра накопления
            _SpecialParsers.Add("b64d9a41-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция ресурсов регистра накопления
            _SpecialParsers.Add("b64d9a42-1642-11d6-a3c7-0050bae0a776", ParseMetaObjectProperties); // Коллекция реквизитов регистра накопления
        }
        internal string ConnectionString { get; set; }
        public bool CheckServerConnection(Server server)
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder()
            {
                DataSource = server.Address,
                IntegratedSecurity = true,
                PersistSecurityInfo = false
            };

            bool result = false;
            {
                SqlConnection connection = new SqlConnection(csb.ToString());
                try
                {
                    connection.Open();
                    result = (connection.State == ConnectionState.Open);
                }
                catch
                {
                    // TODO: handle or log the error
                }
                finally
                {
                    if (connection != null) connection.Dispose();
                }
            }
            return result;
        }
        private void WriteBinaryDataToFile(Stream binaryData, string fileName)
        {
            string filePath = Path.Combine(_logger.CatalogPath, fileName);
            using (FileStream output = File.Create(filePath))
            {
                binaryData.CopyTo(output);
            }
        }
        public List<Database> GetDatabases(Server server)
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder()
            {
                DataSource = server.Address,
                IntegratedSecurity = true,
                PersistSecurityInfo = false
            };
            ConnectionString = csb.ConnectionString;

            List<Database> list = new List<Database>();

            { // limited scope for variables declared in it - using statement does like that - used here to get control over catch block
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT database_id, name FROM sys.databases WHERE name NOT IN ('master', 'model', 'msdb', 'tempdb', 'Resource', 'distribution', 'reportserver', 'reportservertempdb');";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        list.Add(new Database()
                        {
                            Name = reader.GetString(1),
                            Alias = string.Empty
                        });
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                    _ = error.Message;
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return list;
        }
        public void ReadMetadata(Database database)
        {
            _DBNames.Clear();
            _internal_UUID.Clear();

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder()
            {
                DataSource = database.Owner.Address,
                InitialCatalog = database.Name,
                IntegratedSecurity = true,
                PersistSecurityInfo = false
            };
            ConnectionString = csb.ConnectionString;

            ReadDBNames();
            if (_DBNames.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (var item in _DBNames)
                {
                    if (new Guid(item.Key) == Guid.Empty) continue; // system tables and settings
                    if (string.IsNullOrWhiteSpace(item.Value.Token)) continue; // unsupported meta-object type
                    if (item.Value.Token == DBToken.Const) continue; // not supported yet
                    tasks.Add(Task.Run(() =>
                    {
                        ProcessDBName(database, item.Key, item.Value, null);
                    }));
                }
                Task all = Task.WhenAll(tasks);
                _ = all.Wait(Timeout.Infinite);

                ResolvePropertiesReferenceValueTypes(database); // resolve internal identifiers into types

                // load SQL metadata
                SQLHelper SQL = new SQLHelper
                {
                    ConnectionString = ConnectionString
                };
                SQL.Load(database);
            }
        }
        public async Task ReadMetadataAsync(Database database, IProgress<string> progress)
        {
            _DBNames.Clear();
            _internal_UUID.Clear();

            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder()
            {
                DataSource = database.Owner.Address,
                InitialCatalog = database.Name,
                IntegratedSecurity = true,
                PersistSecurityInfo = false
            };
            ConnectionString = csb.ConnectionString;

            await Task.Run(() => { ReadDBNames(); }).ConfigureAwait(false);

            if (_DBNames.Count > 0)
            {
                List<Task> tasks = new List<Task>();
                foreach (var item in _DBNames)
                {
                    if (new Guid(item.Key) == Guid.Empty) continue; // system tables and settings
                    if (string.IsNullOrWhiteSpace(item.Value.Token)) continue; // unsupported meta-object type
                    if (item.Value.Token == DBToken.Const) continue; // not supported yet
                    tasks.Add(Task.Run(() =>
                    {
                        ProcessDBName(database, item.Key, item.Value, progress);
                    }));
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);

                ResolvePropertiesReferenceValueTypes(database); // resolve internal identifiers into types
                await ReadSQLMetadataAsync(database).ConfigureAwait(false);
            }
        }
        private void ProcessDBName(Database database, string fileName, DBNameEntry entry, IProgress<string> progress)
        {
            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null) return;
            
            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                _ = reader.ReadLine(); // skip the 1. line of the file

                ReadInternalIdentifier(reader, entry);

                ParseMetadataObject(reader, entry, database);

                if (progress != null && entry.MetaObject != null)
                {
                    progress.Report(entry.MetaObject.Name);
                }
            }   
        }
            
        # region " Read DBNames "
        internal void ReadDBNames()
        {
            SqlBytes binaryData = GetDBNamesFromDatabase();
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);

            if (_logger == null)
            {
                ParseDBNames(stream);
            }
            else
            {
                MemoryStream memory = new MemoryStream();
                stream.CopyTo(memory);
                memory.Seek(0, SeekOrigin.Begin);
                WriteBinaryDataToFile(memory, "DBNames.txt");
                memory.Seek(0, SeekOrigin.Begin);
                ParseDBNames(memory);
            }
        }
        private SqlBytes GetDBNamesFromDatabase()
        {
            SqlBytes binaryData = null;

            { // limited scope for variables declared in it - using statement does just the same - used here to get control over catch block
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT BinaryData FROM Params WHERE FileName = N'DBNames'";
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                    _ = error.Message;
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return binaryData;
        }
        private void ParseDBNames(Stream stream)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line = reader.ReadLine();
                if (line != null)
                {
                    int capacity = GetDBNamesCapacity(line); // count DBName entries
                    _ = reader.ReadLine();
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length < 36) continue;
                        ParseDBNameLine(line);
                    }
                }
            }
        }
        private int GetDBNamesCapacity(string line)
        {
            return int.Parse(line.Replace("{", string.Empty).Replace(",", string.Empty));
        }
        private void ParseDBNameLine(string line)
        {
            string FileName = line.Substring(1, 36);
            int tokenEnd = line.IndexOf('"', 39);
            int typeCode = line.IndexOf('}', tokenEnd + 2);

            DBName dbname = new DBName()
            {
                Token = line[39..tokenEnd],
                TypeCode = int.Parse(line[(tokenEnd + 2)..typeCode])
            };
            dbname.IsMainTable = IsMainTable(dbname.Token);

            if (_DBNames.TryGetValue(FileName, out DBNameEntry entry))
            {
                entry.DBNames.Add(dbname);
            }
            else
            {
                entry = new DBNameEntry();
                entry.DBNames.Add(dbname);
                _DBNames.Add(FileName, entry);
            }
            if (string.IsNullOrWhiteSpace(entry.Token) && dbname.IsMainTable)
            {
                entry.Token = dbname.Token;
            }
        }
        private bool IsMainTable(string token)
        {
            return token switch
            {
                DBToken.VT => true,
                DBToken.Enum => true,
                DBToken.Const => true,
                DBToken.InfoRg => true,
                DBToken.AccumRg => true,
                DBToken.Document => true,
                DBToken.Reference => true,
                _ => false,
            };
        }
        private bool IsReferenceType(string token)
        {
            return token switch
            {
                DBToken.Enum => true,
                DBToken.Document => true,
                DBToken.Reference => true,
                _ => false,
            };
        }
        #endregion

        #region " Read internal identifiers "
        private async Task ParseInternalIdentifiers(Database database)
        {
            List<Task> tasks = new List<Task>();
            foreach (var entry in _DBNames)
            {
                if (new Guid(entry.Key) == Guid.Empty) continue; // system tables and settings
                if (string.IsNullOrWhiteSpace(entry.Value.Token)) continue; // non-main tables does not have internal identifiers
                SqlBytes binaryData = ReadConfigFromDatabase(entry.Key);
                if (binaryData == null) continue;
                DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
                tasks.Add(Task.Run(() =>
                {
                    ParseInternalIdentifier(stream, entry.Value);
                }));
            }
            await Task.WhenAll(tasks);
        }
        private void ParseInternalIdentifier(Stream stream, DBNameEntry entry)
        {
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                _ = reader.ReadLine(); // skip the 1. line of the file
                string line = reader.ReadLine();
                try
                {
                    string[] items = line.Split(',');
                    string UUID = (entry.Token == DBToken.Enum ? items[1] : items[3]);
                    lock (syncRoot)
                    {
                        _internal_UUID.Add(UUID, entry.MetaObject);
                    }
                }
                catch (Exception error)
                {
                    _ = error.Message;
                    return;
                }
            }
        }
        private void ReadInternalIdentifier(StreamReader reader, DBNameEntry entry)
        {
            string line = reader.ReadLine();
            string[] items = line.Split(',');
            string UUID = (entry.Token == DBToken.Enum ? items[1] : items[3]);
            lock (syncRoot)
            {
                entry.MetaObject.UUID = new Guid(UUID);
                _internal_UUID.Add(UUID, entry.MetaObject);
            }
        }
        #endregion

        #region " Read Config "
        private readonly Regex rxUUID = new Regex("[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}"); // Example: eb3dfdc7-58b8-4b1f-b079-368c262364c9
        private readonly Regex rxSpecialUUID = new Regex("^{[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12},\\d+(?:})?,$"); // Example: {3daea016-69b7-4ed4-9453-127911372fe6,0}, | {cf4abea7-37b2-11d4-940f-008048da11f9,5,
        private readonly Regex rxOCSName = new Regex("^{\\d,\\d,[a-fA-F0-9]{8}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{4}-[a-fA-F0-9]{12}},\"\\w+\",$"); // Example: {1,0,33405315-3023-41e2-a11c-8965c269ce17},"Реквизит1", | {1,0,cab9f251-2656-4e97-950d-f2d4588ddc3a},"ТабличнаяЧасть1",
        private readonly Regex rxOCSType = new Regex("^{\"[#BSDN]\""); // Example: {"#",1aaea747-a4ba-4fb2-9473-075b1ced620c}, | {"B"}, | {"S",10,0}, | {"D","T"}, | {"N",10,0,1}
        private readonly Regex rxNestedProperties = new Regex("^{888744e1-b616-11d4-9436-004095e12fc7,\\d+[},]$"); // look rxSpecialUUID
        private readonly Dictionary<string, SpecialParser> _SpecialParsers = new Dictionary<string, SpecialParser>();
        private SqlBytes ReadConfigFromDatabase(string fileName)
        {
            SqlBytes binaryData = null;

            { // limited scope for variables declared in it - using statement does just the same - used here to get control over catch block
                SqlConnection connection = new SqlConnection(ConnectionString);
                SqlCommand command = connection.CreateCommand();
                SqlDataReader reader = null;
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT BinaryData FROM Config WHERE FileName = @FileName ORDER BY PartNo ASC";
                command.Parameters.AddWithValue("FileName", fileName);
                try
                {
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        binaryData = reader.GetSqlBytes(0);
                    }
                }
                catch (Exception error)
                {
                    // TODO: log error
                    _ = error.Message;
                }
                finally
                {
                    if (reader != null)
                    {
                        if (reader.HasRows) command.Cancel();
                        reader.Dispose();
                    }
                    if (command != null) command.Dispose();
                    if (connection != null) connection.Dispose();
                }
            } // end of limited scope

            return binaryData;
        }
        private void ParseMetadataObject(StreamReader reader, DBNameEntry entry, Database infoBase)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            _ = reader.ReadLine();
            line = reader.ReadLine();
            if (line == null) return;

            try
            {
                ParseMetaObjectNames(line, entry, entry.MetaObject);
            }
            catch (Exception error)
            {
                //TODO: log error !
            }
            lock (syncRoot)
            {
                SetMetaObjectNamespace(infoBase, entry);
            }
            if (entry.Token == DBToken.Reference)
            {
                ParseReferenceOwner(reader, entry.MetaObject);
            }

            int count = 0;
            string UUID = null;
            Match match = null;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxSpecialUUID.Match(line);
                if (!match.Success) continue;

                string[] lines = line.Split(',');
                UUID = lines[0].Replace("{", string.Empty);
                count = int.Parse(lines[1].Replace("}", string.Empty));
                if (count == 0) continue;

                if (_SpecialParsers.ContainsKey(UUID))
                {
                    _SpecialParsers[UUID](reader, line, entry.MetaObject);
                }
            }
        }
        private void ParseMetaObjectNames(string line, DBNameEntry entry, MetaObject table)
        {
            string[] lines = line.Split(',');
            string FileName = lines[2].Replace("}", string.Empty);
            
            table.Alias = lines[3].Replace("\"", string.Empty);
            
            DBName dbname = entry.DBNames.Where(i => i.IsMainTable).FirstOrDefault();
            if (dbname != null)
            {
                table.TypeCode = dbname.TypeCode;
                table.Name = CreateMetaObjectName(table, dbname);
            }
        }
        private string CreateMetaObjectName(MetaObject table, DBName dbname)
        {
            return $"_{dbname.Token}{dbname.TypeCode}";
        }
        private void SetMetaObjectNamespace(Database infoBase, DBNameEntry entry)
        {
            if (entry.MetaObject.Owner != null) return;

            Namespace ns = infoBase.Namespaces.Where(n => n.Name == entry.Token).FirstOrDefault();
            if (ns == null)
            {
                if (string.IsNullOrEmpty(entry.Token))
                {
                    ns = infoBase.Namespaces.Where(n => n.Name == "Unknown").FirstOrDefault();
                    if (ns == null)
                    {
                        ns = new Namespace() { Name = "Unknown", Owner = infoBase };
                        infoBase.Namespaces.Add(ns);
                    }
                }
                else
                {
                    ns = new Namespace() { Name = entry.Token, Owner = infoBase };
                    infoBase.Namespaces.Add(ns);
                }
            }
            entry.MetaObject.Owner = ns;
            ns.DataTypes.Add(entry.MetaObject);
        }
        private void ParseReferenceOwner(StreamReader reader, MetaObject table)
        {
            int count = 0;
            string[] lines;

            _ = reader.ReadLine(); // строка описания - "Синоним" в терминах 1С
            _ = reader.ReadLine();
            string line = reader.ReadLine();
            if (line != null)
            {
                lines = line.Split(',');
                count = int.Parse(lines[1].Replace("}", string.Empty));
            }
            if (count == 0) return;

            Match match;
            MultipleType types = new MultipleType();
            for (int i = 0; i < count; i++)
            {
                _ = reader.ReadLine();
                line = reader.ReadLine();
                if (line == null) return;

                match = rxUUID.Match(line);
                if (match.Success)
                {
                    if (_DBNames.TryGetValue(match.Value, out DBNameEntry entry))
                    {
                        types.Types.Add(entry.MetaObject);
                    }
                }
                _ = reader.ReadLine();
            }

            if (types.Types.Count > 0)
            {
                Property property = new Property
                {
                    Owner = table,
                    Name = DBToken.OwnerID, // "Владелец" [_OwnerIDRRef] | [_OwnerID_TYPE] + [_OwnerID_RTRef] + [_OwnerID_RRRef]
                    ValueType = (types.Types.Count == 1) ? types.Types[0] : types
                };
                table.Properties.Add(property);
            }
        }
        private void ParseMetaObjectProperties(StreamReader reader, string line, MetaObject table)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1].Replace("}", string.Empty));

            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxOCSName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseProperty(reader, nextLine, table);
                        break;
                    }
                }
            }
        }
        private void ParseProperty(StreamReader reader, string line, MetaObject owner)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            Property property = new Property
            {
                Owner = owner,
                Name = objectName
            };
            owner.Properties.Add(property);

            if (_DBNames.TryGetValue(fileName, out DBNameEntry entry))
            {
                if (entry.DBNames.Count == 1)
                {
                    property.Name += CreateMetaObjectFieldName(entry.DBNames[0]);
                }
                else if (entry.DBNames.Count > 1)
                {
                    foreach (var dbn in entry.DBNames.Where(dbn => dbn.Token == DBToken.Fld))
                    {
                        property.Name += CreateMetaObjectFieldName(dbn);
                    }
                }
            }
            ParsePropertyTypes(reader, property);
        }
        private string CreateMetaObjectFieldName(DBName dbname)
        {
            return $"_{dbname.Token}{dbname.TypeCode}";
        }
        private void ParsePropertyTypes(StreamReader reader, Property property)
        {
            string line = reader.ReadLine();
            if (line == null) return;

            while (line != "{\"Pattern\",")
            {
                line = reader.ReadLine();
                if (line == null) return;
            }
            
            Match match;
            MultipleType types = new MultipleType();
            while ((line = reader.ReadLine()) != null)
            {
                match = rxOCSType.Match(line);
                if (!match.Success) break;

                string token = match.Value.Replace("{", string.Empty).Replace("\"", string.Empty);
                switch (token)
                {
                    case DBToken.S: { types.Types.Add(SimpleType.String); break; }
                    case DBToken.B: { types.Types.Add(SimpleType.Boolean); break; }
                    case DBToken.N: { types.Types.Add(SimpleType.Numeric); break; }
                    case DBToken.D: { types.Types.Add(SimpleType.DateTime); break; }
                    default:
                        {
                            string[] lines = line.Split(',');
                            string UUID = lines[1].Replace("}", string.Empty);

                            if (UUID == "e199ca70-93cf-46ce-a54b-6edc88c3a296")
                            {
                                types.Types.Add(SimpleType.Binary); // ХранилищеЗначения - varbinary(max)
                            }
                            else if (UUID == "fc01b5df-97fe-449b-83d4-218a090e681e")
                            {
                                types.Types.Add(SimpleType.UniqueIdentifier); // УникальныйИдентификатор - binary(16)
                            }
                            else if (_internal_UUID.TryGetValue(UUID, out MetaObject referenceType))
                            {
                                types.Types.Add(referenceType);
                            }
                            else // UUID is not loaded yet - leave it for second pass
                            {
                                types.Types.Add(new MetaObject() { UUID = new Guid(UUID) });
                            }
                            break;
                        }
                }
            }
            if (types.Types.Count == 1)
            {
                property.ValueType = types.Types[0];
            }
            else if (types.Types.Count > 1)
            {
                property.ValueType = types;
            }
        }
        private void ParseNestedObjects(StreamReader reader, string line, MetaObject owner)
        {
            string[] lines = line.Split(',');
            int count = int.Parse(lines[1]);
            Match match;
            string nextLine;
            for (int i = 0; i < count; i++)
            {
                while ((nextLine = reader.ReadLine()) != null)
                {
                    match = rxOCSName.Match(nextLine);
                    if (match.Success)
                    {
                        ParseNestedObject(reader, nextLine, owner);
                        break;
                    }
                }
            }
        }
        private void ParseNestedObject(StreamReader reader, string line, MetaObject owner)
        {
            string[] lines = line.Split(',');
            string fileName = lines[2].Replace("}", string.Empty);
            string objectName = lines[3].Replace("\"", string.Empty);

            MetaObject nested = new MetaObject()
            {
                Owner = owner,
                Alias = objectName
            };

            Property property = new Property()
            {
                Owner = owner,
                Name = objectName,
                ValueType = new ListType() { Type = nested } // similar to List<T> where T : ComplexType
            };
            owner.Properties.Add(property);

            if (_DBNames.TryGetValue(fileName, out DBNameEntry entry))
            {
                DBName dbname = entry.DBNames.Where(i => i.IsMainTable).FirstOrDefault();
                if (dbname != null)
                {
                    nested.TypeCode = dbname.TypeCode;
                    nested.Name = $"{owner.Name}_{dbname.Token}{dbname.TypeCode}"; ;
                }
            }
            ParseNestedObjectProperties(reader, nested);
        }
        private void ParseNestedObjectProperties(StreamReader reader, MetaObject table)
        {
            string line;
            Match match;
            while ((line = reader.ReadLine()) != null)
            {
                match = rxNestedProperties.Match(line);
                if (match.Success)
                {
                    ParseMetaObjectProperties(reader, line, table);
                    break;
                }
            }
        }
        #endregion
        private void ResolvePropertiesReferenceValueTypes(Database infoBase)
        {
            foreach (var ns in infoBase.Namespaces)
            {
                foreach (var entity in ns.DataTypes)
                {
                    ResolvePropertiesValueType((MetaObject)entity);
                }
            }
        }
        private void ResolvePropertiesValueType(MetaObject metaObject)
        {
            foreach (var property in metaObject.Properties)
            {
                if (property.ValueType is MetaObject valueType)
                {
                    if (valueType.TypeCode == 0)
                    {
                        if (_internal_UUID.TryGetValue(valueType.UUID.ToString(), out MetaObject resolvedType))
                        {
                            property.ValueType = resolvedType;
                        }
                    }
                }
                else if (property.ValueType is ListType listType)
                {
                    ResolvePropertiesValueType((MetaObject)listType.Type); // process nested meta-object
                }
                else if (property.ValueType is MultipleType multipleType)
                {
                    for (int i = 0; i < multipleType.Types.Count; i++)
                    {
                        var dataType = multipleType.Types[i];
                        if (dataType is MetaObject referenceType)
                        {
                            if (referenceType.TypeCode == 0)
                            {
                                if (_internal_UUID.TryGetValue(dataType.UUID.ToString(), out MetaObject resolvedType))
                                {
                                    multipleType.Types[i] = resolvedType;
                                }
                            }
                        }
                    }
                }
            }
        }


        public async Task ReadSQLMetadataAsync(Database infoBase)
        {
            SQLHelper SQL = new SQLHelper();
            SQL.ConnectionString = ConnectionString;
            await SQL.LoadAsync(infoBase);
        }
    

        internal void SaveMetaObjectToFile(MetaObject table)
        {
            if (_logger == null) return;

            var kv = _DBNames
                .Where(i => i.Value.MetaObject == table)
                .FirstOrDefault();
            string fileName = kv.Key;

            if (new Guid(fileName) == Guid.Empty) return;

            SqlBytes binaryData = ReadConfigFromDatabase(fileName);
            if (binaryData == null) return;

            DeflateStream stream = new DeflateStream(binaryData.Stream, CompressionMode.Decompress);
            WriteBinaryDataToFile(stream, $"{fileName}.txt");
        }
    }
}