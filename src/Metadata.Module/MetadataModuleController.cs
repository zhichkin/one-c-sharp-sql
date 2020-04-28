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
        private const string MODULE_TOOLTIP = "Metadata module";

        private const string MODULE_ICON_PATH = "pack://application:,,,/OneCSharp.Metadata.Module;component/images/JsonScript.png";

        private readonly BitmapImage MODULE_ICON = new BitmapImage(new Uri(MODULE_ICON_PATH));
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
                MenuItemHeader = "Attach metadata",
                MenuItemIcon = MODULE_ICON,
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
                NodeIcon = MODULE_ICON,
                NodeText = server.Name,
                NodeToolTip = server.Address,
                NodePayload = server
            };
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "Attach database",
                MenuItemIcon = MODULE_ICON,
                MenuItemCommand = new RelayCommand(AttachDatabase),
                MenuItemPayload = treeNode
            });
        }
        private void CreateDatabaseNode(InfoBase database, out TreeNodeViewModel treeNode)
        {
            treeNode = new TreeNodeViewModel()
            {
                NodeIcon = MODULE_ICON,
                NodeText = database.Name,
                NodeToolTip = database.Alias,
                NodePayload = database
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