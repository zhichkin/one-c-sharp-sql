using Microsoft.Data.SqlClient;
using OneCSharp.Metadata.Services;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace OneCSharp.TSQL.Scripting
{
    public interface IQueryExecutor
    {
        /// <summary>
        /// Executes SQL script and returns result as JSON.
        /// </summary>
        string ExecuteJson(string sql);
    }
    public sealed class QueryExecutor: IQueryExecutor
    {
        private IMetadataService MetadataService { get; }
        public QueryExecutor(IMetadataService metadata)
        {
            MetadataService = metadata ?? throw new ArgumentNullException(nameof(metadata));
        }
        public string ExecuteJson(string sql)
        {
            string json;
            JsonWriterOptions options = new JsonWriterOptions { Indented = true };
            using (MemoryStream stream = new MemoryStream())
            {
                using (Utf8JsonWriter writer = new Utf8JsonWriter(stream, options))
                {
                    writer.WriteStartArray();
                    using (SqlConnection connection = new SqlConnection(MetadataService.ConnectionString))
                    using (SqlCommand command = new SqlCommand(sql, connection))
                    {
                        connection.Open();

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var schema = reader.GetColumnSchema();
                            while (reader.Read())
                            {
                                writer.WriteStartObject();
                                for (int c = 0; c < schema.Count; c++)
                                {
                                    object value = reader[c];
                                    string typeName = schema[c].DataTypeName;
                                    string columnName = schema[c].ColumnName;
                                    int valueSize = 0;
                                    if (schema[c].ColumnSize.HasValue)
                                    {
                                        valueSize = schema[c].ColumnSize.Value;
                                    }
                                    if (value == DBNull.Value)
                                    {
                                        writer.WriteNull(columnName);
                                    }
                                    else if (DbUtilities.IsString(typeName))
                                    {
                                        writer.WriteString(columnName, (string)value);
                                    }
                                    else if (DbUtilities.IsDateTime(typeName))
                                    {
                                        writer.WriteString(columnName, ((DateTime)value).ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture));
                                    }
                                    else if (DbUtilities.IsVersion(typeName))
                                    {
                                        writer.WriteString(columnName, $"0x{DbUtilities.ByteArrayToString((byte[])value)}");
                                    }
                                    else if (DbUtilities.IsBoolean(typeName, valueSize))
                                    {
                                        if (typeName == "bit")
                                        {
                                            writer.WriteBoolean(columnName, (bool)value);
                                        }
                                        else // binary(1)
                                        {
                                            writer.WriteBoolean(columnName, DbUtilities.GetInt32((byte[])value) == 0 ? false : true);
                                        }
                                    }
                                    else if (DbUtilities.IsNumber(typeName, valueSize))
                                    {
                                        if (typeName == "binary" || typeName == "varbinary") // binary(4) | varbinary(4)
                                        {
                                            writer.WriteNumber(columnName, DbUtilities.GetInt32((byte[])value));
                                        }
                                        else
                                        {
                                            writer.WriteNumber(columnName, (decimal)value);
                                        }
                                    }
                                    else if (DbUtilities.IsUUID(typeName, valueSize))
                                    {
                                        writer.WriteString(columnName, (new Guid((byte[])value)).ToString());
                                    }
                                    else if (DbUtilities.IsReference(typeName, valueSize))
                                    {
                                        byte[] reference = (byte[])value;
                                        int code = DbUtilities.GetInt32(reference[0..4]);
                                        Guid uuid = new Guid(reference[4..^0]);
                                        writer.WriteString(columnName, $"{{{code}:{uuid}}}");
                                    }
                                    else if (DbUtilities.IsBinary(typeName))
                                    {
                                        writer.WriteBase64String(columnName, (byte[])value);
                                    }
                                }
                                writer.WriteEndObject();
                            }
                        }
                    }
                    writer.WriteEndArray();
                }
                json = Encoding.UTF8.GetString(stream.ToArray());
            }
            return json;
        }
    }
}