using OneCSharp.Metadata.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml;

namespace OneCSharp.Metadata.Services
{
    public sealed class XMLMetadataLoader
    {
        private sealed class LoadContext
        {
            internal LoadContext(InfoBase infoBase)
            {
                InfoBase = infoBase;
            }
            internal InfoBase InfoBase;
            internal BaseObject BaseObject;
            internal MetaObject MetaObject;
            internal MetaObject MetaObjectOwner;
            internal Property Property;
            internal string Table;
            internal Field Field;
            internal Dictionary<int, MetaObject> TypeCodes = new Dictionary<int, MetaObject>();
        }
        public void Load(string filePath, InfoBase infoBase)
        {
            LoadContext context = new LoadContext(infoBase);

            using (XmlReader reader = XmlReader.Create(filePath))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "InfoBase")
                        {
                            Read_InfoBase_Element(reader, context);
                        }
                        if (reader.Name == "Types")
                        {
                            context.TypeCodes.Clear();
                            context.InfoBase.BaseObjects.Clear();
                        }
                        if (reader.Name == "Type")
                        {
                            Read_Type_Element(reader, context);
                        }
                        if (reader.Name == "Namespaces")
                        {
                            // do nothing: see Types tag
                        }
                        if (reader.Name == "Namespace")
                        {
                            Read_Namespace_Element(reader, context);
                        }
                        else if (reader.Name == "Entities")
                        {
                            //context.MetaObject.MetaObjects.Clear();
                        }
                        else if (reader.Name == "Entity")
                        {
                            Read_Entity_Element(reader, context);
                        }
                        else if (reader.Name == "Properties")
                        {
                            //context.Entity.Properties.Clear();
                        }
                        else if (reader.Name == "Property")
                        {
                            Read_Property_Element(reader, context);
                        }
                        else if (reader.Name == "Tables")
                        {
                            context.MetaObject.Table = string.Empty;
                        }
                        else if (reader.Name == "Table")
                        {
                            Read_Table_Element(reader, context);
                        }
                        else if (reader.Name == "Fields")
                        {
                            //context.Table.Fields.Clear();
                        }
                        else if (reader.Name == "Field")
                        {
                            Read_Field_Element(reader, context);
                        }
                        else if (reader.Name == "Value")
                        {
                            Read_Value_Element(reader, context);
                        }
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "InfoBase")
                        {
                            Close_InfoBase_Element(context);
                        }
                        else if (reader.Name == "Type")
                        {
                            context.BaseObject = null;
                            context.MetaObject = null;
                            context.MetaObjectOwner = null;
                        }
                        else if (reader.Name == "Namespace")
                        {
                            context.BaseObject = null;
                            context.MetaObject = null;
                            context.MetaObjectOwner = null;
                        }
                        else if (reader.Name == "Entities")
                        {
                            context.MetaObjectOwner = null;
                        }
                        else if (reader.Name == "Entity")
                        {
                            context.MetaObject = null;
                        }
                        else if (reader.Name == "Property")
                        {
                            context.Property = null;
                        }
                        else if (reader.Name == "Table")
                        {
                            context.Table = string.Empty;
                        }
                        else if (reader.Name == "Field")
                        {
                            context.Field = null;
                        }
                    }
                }
            }
        }
        private void Read_InfoBase_Element(XmlReader reader, LoadContext context)
        {
            context.InfoBase.Alias = reader.GetAttribute("name");
            //context.InfoBase.Server = reader.GetAttribute("server");
            context.InfoBase.Name = reader.GetAttribute("database");
        }
        private void Close_InfoBase_Element(LoadContext context)
        {
            context.BaseObject = null;
            context.MetaObject = null;
            context.MetaObjectOwner = null;
            context.Property = null;
            context.Table = null;
            context.Field = null;
        }
        private void Read_Namespace_Element(XmlReader reader, LoadContext context)
        {
            string name = reader.GetAttribute("name");

            context.BaseObject = context.InfoBase.BaseObjects
                .Where((n) => n.Name == name)
                .FirstOrDefault();

            if (context.BaseObject != null) return;

            context.BaseObject = new BaseObject()
            {
                Name = name
            };
            context.InfoBase.BaseObjects.Add(context.BaseObject);
        }
        private void Read_Type_Element(XmlReader reader, LoadContext context)
        {
            string code = reader.GetAttribute("code");
            string name = reader.GetAttribute("name");

            string[] names = name.Split(".".ToCharArray());
            string _namespace = names[0];
            string _entity = names[1];

            context.BaseObject = context.InfoBase.BaseObjects
                .Where((n) => n.Name == _namespace)
                .FirstOrDefault();
            
            if (context.BaseObject == null)
            {
                context.BaseObject = new BaseObject()
                {
                    Name = _namespace
                };
                context.InfoBase.BaseObjects.Add(context.BaseObject);
            }

            context.MetaObject = new MetaObject()
            {
                TypeCode = int.Parse(code),
                Name = _entity,
                Schema = "dbo"
            };

            context.BaseObject.MetaObjects.Add(context.MetaObject);
            context.TypeCodes.Add(context.MetaObject.TypeCode, context.MetaObject);
        }
        private void Read_Entity_Element(XmlReader reader, LoadContext context)
        {
            string code = reader.GetAttribute("code");
            string name = reader.GetAttribute("name");
            string alias = reader.GetAttribute("alias");

            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(name)) return; // system table
            
            if (!string.IsNullOrEmpty(code)) // reference type
            {
                context.MetaObject = context.TypeCodes[int.Parse(code)];
                context.MetaObject.Alias = (alias == null ? string.Empty : alias);
                context.MetaObjectOwner = context.MetaObject;
                return;
            }

            // value type

            if (context.MetaObjectOwner == null) // independent type
            {
                context.MetaObject = new MetaObject()
                {
                    Name = name,
                    Alias = (alias == null ? string.Empty : alias)
                };
                context.BaseObject.MetaObjects.Add(context.MetaObject);
            }
            else // nested type
            {
                MetaObject entity = new MetaObject()
                {
                    Name = name,
                    Alias = (alias == null ? string.Empty : alias)
                };
                context.MetaObject = entity;
                context.MetaObjectOwner.MetaObjects.Add(entity);
            }
        }
        private void Read_Property_Element(XmlReader reader, LoadContext context)
        {
            if (context.MetaObject == null) return; // system table
            
            string name = reader.GetAttribute("name");
            string ordinal = reader.GetAttribute("ordinal");
            string typeCodes = reader.GetAttribute("type");
            string purpose = reader.GetAttribute("purpose");
            
            context.Property = new Property()
            {
                Name = name,
                Ordinal = int.Parse(ordinal)
            };
            context.MetaObject.Properties.Add(context.Property);

            SetPropertyPurpose(context, purpose);
            SetPropertyTypes(context, typeCodes);
        }
        private void SetPropertyPurpose(LoadContext context, string purpose)
        {
            context.Property.Purpose = (PropertyPurpose)Enum.Parse(typeof(PropertyPurpose), purpose);
        }
        private void SetPropertyTypes(LoadContext context, string typeCodes)
        {
            string[] types = typeCodes.Split(",".ToCharArray());

            foreach (string type in types)
            {
                if (type == "L")
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.Boolean);
                }
                else if (type == "N")
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.Decimal);
                }
                else if (type == "S")
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.String);
                }
                else if (type == "T")
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.DateTime);
                }
                else if (type == "B")
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.Binary);
                }
                else if (type == "GUID")
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.Guid);
                }
                else if (type == "IO") // вид движения накопления: 0 - приход, 1 - расход
                {
                    context.Property.PropertyTypes.Add((int)TypeCodes.Int32);
                }
                else
                {
                    int typeCode;
                    if (int.TryParse(type, out typeCode))
                    {
                        context.Property.PropertyTypes.Add(typeCode);
                    }
                    else
                    {
                        // wtf ? 8\
                    }
                }
            }
        }
        private void Read_Table_Element(XmlReader reader, LoadContext context)
        {
            string name = reader.GetAttribute("name");
            string purpose = reader.GetAttribute("purpose");
            if (purpose == "Main")
            {
                context.Table = name;
                context.MetaObject.Table = context.Table;
            }
        }
        private void Read_Field_Element(XmlReader reader, LoadContext context)
        {
            string name = reader.GetAttribute("name");
            string purpose = reader.GetAttribute("purpose");
            string _property = reader.GetAttribute("property");

            if (purpose == "Locator")
            {
                purpose = "Discriminator";
            }
            else if (purpose == "Number")
            {
                purpose = "Numeric";
            }

            context.Field = new Field()
            {
                Name = name,
                Purpose = (FieldPurpose)Enum.Parse(typeof(FieldPurpose), purpose)
            };
            //context.Table.Fields.Add(context.Field);

            //if (context.Table.Purpose != TablePurpose.Main) return;

            Property property = context.MetaObject.Properties
                .Where((p) => p.Name == _property)
                .FirstOrDefault();

            if (property == null) // system field, which has no mapping to any one property
            {
                property = new Property()
                {
                    Name = string.IsNullOrEmpty(_property) ? name : _property,
                    Purpose = PropertyPurpose.System
                };
                // ? надо бы разобраться какой тип данных назначать таким свойствам ...
                property.PropertyTypes.Add((int)TypeCodes.Binary);
                context.MetaObject.Properties.Add(property);
            }

            property.Fields.Add(context.Field);
        }
        private void Read_Value_Element(XmlReader reader, LoadContext context)
        {
            string order = reader.GetAttribute("order");
            string name = reader.GetAttribute("name");

            if (context.MetaObject == null) return;

            Property property = new Property()
            {
                Name = name,
                Ordinal = int.Parse(order),
                Purpose = PropertyPurpose.Property
            };
            property.PropertyTypes.Add(context.MetaObject.TypeCode);
            context.MetaObject.Properties.Add(property);
        }
        //public InfoBase GetMetadata(string ProgID, SQLConnectionDialogNotification settings, string tempDirectory)
        //{
        //    Type comType = Type.GetTypeFromProgID(ProgID, true); // V83.COMConnector
        //    dynamic connector = Activator.CreateInstance(comType);
        //    string connectionString = GetConnectionString(settings);
        //    dynamic session = connector.Connect(connectionString);

        //    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigurationExporter83.epf");
        //    string temp = Path.Combine(tempDirectory, "ConfigurationExporter83.epf");
        //    File.Copy(path, temp, true);
        //    dynamic processor = session.ExternalDataProcessors.Create(temp);
        //    string output = Path.Combine(tempDirectory, "configuration.xml");

        //    processor.Write(output);
        //    File.Delete(temp);

        //    Marshal.ReleaseComObject(processor); processor = null;
        //    Marshal.ReleaseComObject(session); session = null;
        //    Marshal.ReleaseComObject(connector); connector = null;

        //    InfoBase infoBase = new InfoBase();
        //    this.Load(output, infoBase);
        //    File.Delete(output);
        //    return infoBase;
        //}
        //private string GetConnectionString(SQLConnectionDialogNotification settings)
        //{
        //    return string.Format("Srvr=\"{0}\";Ref=\"{1}\";Usr=\"{2}\";Pwd=\"{3}\";",
        //        settings.Server,
        //        settings.Database,
        //        settings.UserName,
        //        settings.Password);
        //}
    }
}
//XMLMetadataAdapter adapter = new XMLMetadataAdapter();
//string progID = "V83.COMConnector";
//InfoBase infoBase = adapter.GetMetadata(progID, appInfo, temp);
//ImportSQLMetadata(infoBase, sqlInfo);