using Microsoft.Win32;
using OneCSharp.Metadata.Model;
using OneCSharp.MVVM;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OneCSharp.Metadata.Module
{
    public sealed class MetadataModuleController : IController
    {
        #region "Private fields"

        private const string MODULE_NAME = "Metadata";
        private const string MODULE_TOOLTIP = "Databases metadata";

        private const string MODULE_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Metadata.png";
        private const string QUESTION_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/QuestionMark.png";

        private const string ATTACH_SERVER_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/AddLocalServer.png";
        private const string SERVER_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/DataServer.png";
        private const string ATTACH_DATABASE_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/ConnectToDatabase.png";
        private const string DATABASE_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Database.png";

        private const string ENUM_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Перечисление.png";
        private const string EAV_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/ПланВидовХарактеристик.png";
        private const string CATALOG_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Справочник.png";
        private const string DOCUMENT_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Документ.png";
        private const string NESTED_OBJECT_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/NestedTable.png";
        private const string INFOREG_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/РегистрСведений.png";
        private const string ACCUMREG_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/РегистрНакопления.png";
        private const string ACCOUNTREG_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/РегистрБухгалтерии.png";
        private const string EXCHANGE_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/ПланОбмена.png";

        private const string PROPERTY_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Реквизит.png";
        private const string MEASURE_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Ресурс.png";
        private const string DIMENSION_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/Измерение.png";
        private const string SYSTEM_PROPERTY_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/СтандартныйРеквизит.png";

        private readonly BitmapImage MODULE_ICON = new BitmapImage(new Uri(MODULE_ICON_PATH));
        private readonly BitmapImage QUESTION_ICON = new BitmapImage(new Uri(QUESTION_ICON_PATH));

        private readonly BitmapImage ATTACH_SERVER_ICON = new BitmapImage(new Uri(ATTACH_SERVER_ICON_PATH));
        private readonly BitmapImage SERVER_ICON = new BitmapImage(new Uri(SERVER_ICON_PATH));
        private readonly BitmapImage ATTACH_DATABASE_ICON = new BitmapImage(new Uri(ATTACH_DATABASE_ICON_PATH));
        private readonly BitmapImage DATABASE_ICON = new BitmapImage(new Uri(DATABASE_ICON_PATH));

        private readonly BitmapImage ENUM_ICON = new BitmapImage(new Uri(ENUM_ICON_PATH));
        private readonly BitmapImage EAV_ICON = new BitmapImage(new Uri(EAV_ICON_PATH));
        private readonly BitmapImage CATALOG_ICON = new BitmapImage(new Uri(CATALOG_ICON_PATH));
        private readonly BitmapImage DOCUMENT_ICON = new BitmapImage(new Uri(DOCUMENT_ICON_PATH));
        private readonly BitmapImage NESTED_OBJECT_ICON = new BitmapImage(new Uri(NESTED_OBJECT_ICON_PATH));
        private readonly BitmapImage INFOREG_ICON = new BitmapImage(new Uri(INFOREG_ICON_PATH));
        private readonly BitmapImage ACCUMREG_ICON = new BitmapImage(new Uri(ACCUMREG_ICON_PATH));
        private readonly BitmapImage ACCOUNTREG_ICON = new BitmapImage(new Uri(ACCOUNTREG_ICON_PATH));
        private readonly BitmapImage EXCHANGE_ICON = new BitmapImage(new Uri(EXCHANGE_ICON_PATH));

        private readonly BitmapImage PROPERTY_ICON = new BitmapImage(new Uri(PROPERTY_ICON_PATH));
        private readonly BitmapImage MEASURE_ICON = new BitmapImage(new Uri(MEASURE_ICON_PATH));
        private readonly BitmapImage DIMENSION_ICON = new BitmapImage(new Uri(DIMENSION_ICON_PATH));
        private readonly BitmapImage SYSTEM_PROPERTY_ICON = new BitmapImage(new Uri(SYSTEM_PROPERTY_ICON_PATH));

        private BitmapImage GetIconByName(string name)
        {
            if (name == "Перечисление") { return ENUM_ICON; }
            else if (name == "ПланВидовХарактеристик") { return EAV_ICON; }
            else if (name == "Справочник") { return CATALOG_ICON; }
            else if (name == "Документ") { return DOCUMENT_ICON; }
            else if (name == "РегистрСведений") { return INFOREG_ICON; }
            else if (name == "РегистрНакопления") { return ACCUMREG_ICON; }
            else if (name == "РегистрБухгалтерии") { return ACCOUNTREG_ICON; }
            else if (name == "ПланОбмена") { return EXCHANGE_ICON; }
            return QUESTION_ICON;
        }
        private BitmapImage GetPropertyIcon(PropertyPurpose purpose)
        {
            if (purpose == PropertyPurpose.Property) { return PROPERTY_ICON; }
            else if (purpose == PropertyPurpose.Measure) { return MEASURE_ICON; }
            else if (purpose == PropertyPurpose.Dimension) { return DIMENSION_ICON; }
            else if (purpose == PropertyPurpose.System || purpose == PropertyPurpose.Hierarchy) { return SYSTEM_PROPERTY_ICON; }
            return QUESTION_ICON;
        }

        #endregion
        private MetadataModule Module { get; set; }
        public MetadataModuleController(IModule module)
        {
            Module = (MetadataModule)module;
        }
        public void AttachTreeNodes(TreeNodeViewModel parentNode)
        {
            throw new NotImplementedException();
        }
        public void BuildTreeNode(object model, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                IsExpanded = true,
                NodeIcon = MODULE_ICON,
                NodeText = MODULE_NAME,
                NodeToolTip = MODULE_TOOLTIP,
                NodePayload = Module
            };
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Attach server",
                MenuItemIcon = ATTACH_SERVER_ICON,
                MenuItemCommand = new RelayCommand(AttachServer),
                MenuItemPayload = treeNode
            });
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "About...",
                MenuItemIcon = MODULE_ICON,
                MenuItemCommand = new RelayCommand(ShowAboutWindow),
                MenuItemPayload = treeNode
            });

            ConfigureNodes(treeNode);
        }
        private void ShowAboutWindow(object parameter)
        {
            MessageBox.Show("1C# Metadata module © 2016"
                + Environment.NewLine
                + Environment.NewLine + "Created by Zhichkin"
                + Environment.NewLine + "dima_zhichkin@mail.ru"
                + Environment.NewLine + "https://github.com/zhichkin/",
                "1C#",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        private void ConfigureNodes(TreeNodeViewModel mainNode)
        {
            foreach (DatabaseServer server in Module.Metadata.Settings.Servers)
            {
                CreateServerNode(server, out TreeNodeViewModel serverNode);
                mainNode.TreeNodes.Add(serverNode);
                
                foreach (InfoBase database in server.Databases)
                {
                    CreateDatabaseNode(database, out TreeNodeViewModel databaseNode);
                    serverNode.TreeNodes.Add(databaseNode);
                }
            }
        }
        private void CreateServerNode(DatabaseServer server, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                NodeIcon = SERVER_ICON,
                NodeText = server.Name,
                NodeToolTip = server.Address,
                NodePayload = server
            };
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Attach database",
                MenuItemIcon = ATTACH_DATABASE_ICON,
                MenuItemCommand = new RelayCommand(AttachDatabase),
                MenuItemPayload = treeNode
            });
        }
        private void CreateDatabaseNode(InfoBase database, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                NodeIcon = DATABASE_ICON,
                NodeText = database.Name,
                NodeToolTip = database.Alias,
                NodePayload = database
            };
            foreach (BaseObject baseObject in database.BaseObjects)
            {
                CreateBaseObjectNode(baseObject, out TreeNodeViewModel node);
                treeNode.TreeNodes.Add(node);
            }
        }
        private void CreateBaseObjectNode(BaseObject baseObject, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                NodeIcon = GetIconByName(baseObject.Name),
                NodeText = baseObject.Name,
                NodeToolTip = $"{baseObject.Name} ({baseObject.MetaObjects.Count})",
                NodePayload = baseObject
            };
            foreach (MetaObject metaObject in baseObject.MetaObjects)
            {
                CreateMetaObjectNode(metaObject, treeNode.NodeIcon, out TreeNodeViewModel node);
                treeNode.TreeNodes.Add(node);
            }
        }
        private void CreateMetaObjectNode(MetaObject metaObject, BitmapImage icon, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                NodeIcon = icon,
                NodeText = metaObject.Name,
                NodeToolTip = metaObject.Table,
                NodePayload = metaObject
            };
            foreach (Property property in metaObject.Properties)
            {
                CreatePropertyNode(property, out TreeNodeViewModel node);
                treeNode.TreeNodes.Add(node);
            }
            foreach (MetaObject nestedObject in metaObject.MetaObjects)
            {
                CreateMetaObjectNode(nestedObject, NESTED_OBJECT_ICON, out TreeNodeViewModel node);
                treeNode.TreeNodes.Add(node);
            }
        }
        private void CreatePropertyNode(Property property, out TreeNodeViewModel treeNode)
        {
            string toolTip = string.Empty;
            string newLine = string.Empty;
            foreach (Field field in property.Fields)
            {
                toolTip += $"{newLine}{field.Name} ({field.TypeName}{(field.IsNullable ? string.Empty : "NOT")} NULL)";
                newLine = Environment.NewLine;
            }
            treeNode = new TreeNodeViewModel()
            {
                NodeIcon = GetPropertyIcon(property.Purpose),
                NodeText = property.Name,
                NodeToolTip = toolTip,
                NodePayload = property
            };
        }

        private void AttachServer(object parameter)
        {
            if (!(parameter is TreeNodeViewModel parentNode)) return;

            // ask for catalog name
            InputStringDialog dialog = new InputStringDialog()
            {
                Title = "Server name"
            };
            _ = dialog.ShowDialog();
            if (dialog.Result == null) return;
            
            string serverName = (string)dialog.Result;
            DatabaseServer server = Module.Metadata.Settings.Servers.Where(s => s.Name == serverName).FirstOrDefault();
            if (server != null)
            {
                MessageBox.Show($"Server \"{serverName}\" is already attached !", "1C#", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else
            {
                server = new DatabaseServer() { Name = serverName };
            }

            string catalogPath = Path.Combine(Module.ModuleCatalogPath, MODULE_NAME, serverName);
            if (!Directory.Exists(catalogPath))
            {
                _ = Directory.CreateDirectory(catalogPath);
            }

            Module.Metadata.Settings.Servers.Add(server);
            Module.SaveMetadataSettings(Module.Metadata.Settings);

            CreateServerNode(server, out TreeNodeViewModel serverNode);
            serverNode.IsExpanded = true;
            parentNode.IsExpanded = true;
            parentNode.TreeNodes.Add(serverNode);
        }
        private void AttachDatabase(object parameter)
        {
            if (!(parameter is TreeNodeViewModel parentNode)) return;

            InputStringDialog input = new InputStringDialog()
            {
                Title = "Database name"
            };
            _ = input.ShowDialog();
            if (input.Result == null) return;

            string serverName = parentNode.NodeText;
            string databaseName = (string)input.Result;
            string metadataFilePath = Path.Combine(Module.ModuleCatalogPath, MODULE_NAME, serverName, $"{databaseName}.xml");

            DatabaseServer server = Module.Metadata.Settings.Servers.Where(s => s.Name == serverName).FirstOrDefault();
            InfoBase database = server.Databases.Where(d => d.Name == databaseName).FirstOrDefault();
            if (database != null)
            {
                MessageBox.Show($"Database \"{databaseName}\" is already attached !", "1C#", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            OpenFileDialog dialog = new OpenFileDialog()
            {
                Filter = "XML files (*.xml)|*.xml",
                Multiselect = false,
                InitialDirectory = Module.ModuleCatalogPath
            };
            if (dialog.ShowDialog() == false) return;
            try
            {
                File.Copy(dialog.FileName, metadataFilePath, true);
                Module.Metadata.UseServer(serverName);
                Module.Metadata.UseDatabase(databaseName);
                Module.SaveMetadataSettings(Module.Metadata.Settings);
            }
            catch (Exception ex)
            {
                //TODO: File.Delete(metadataFilePath); ???
                MessageBox.Show(ex.Message, "1C#", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CreateDatabaseNode(Module.Metadata.CurrentDatabase, out TreeNodeViewModel databaseNode);
            databaseNode.IsExpanded = true;
            parentNode.IsExpanded = true;
            parentNode.TreeNodes.Add(databaseNode);
        }
    }
}