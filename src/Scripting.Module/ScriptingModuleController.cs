using OneCSharp.MVVM;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OneCSharp.Scripting.Module
{
    public sealed class ScriptingModuleController : IController
    {
        #region "Private fields"
        private const string SCRIPT_TAB_TITLE = "SCRIPT";
        private const string MODULE_NAME = "Scripting";
        private const string MODULE_TOOLTIP = "Scripting module";
        private const string NODES_NAME = "Nodes";
        private const string NODES_TOOLTIP = "Scripting nodes";

        private const string MODULE_ICON_PATH = "pack://application:,,,/OneCSharp.Scripting.Module;component/images/JsonScript.png";
        //private const string WEB_SERVER_PATH = "pack://application:,,,/OneCSharp.Integrator.Module;component/images/WebServer.png";
        //private const string ADD_NODE_PATH = "pack://application:,,,/OneCSharp.Integrator.Module;component/images/AddLocalServer.png";

        private readonly BitmapImage MODULE_ICON = new BitmapImage(new Uri(MODULE_ICON_PATH));
        //private readonly BitmapImage WEB_SERVER_ICON = new BitmapImage(new Uri(WEB_SERVER_PATH));
        //private readonly BitmapImage ADD_NODE_ICON = new BitmapImage(new Uri(ADD_NODE_PATH));
        #endregion
        private IModule Module { get; set; }
        public ScriptingModuleController(IModule module)
        {
            Module = module;
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
                NodePayload = null
            };
            treeNode.ContextMenuItems.Add(new MenuItemViewModel()
            {
                MenuItemHeader = "About...",
                MenuItemIcon = MODULE_ICON,
                MenuItemCommand = new RelayCommand(ShowAboutWindow),
                MenuItemPayload = null
            });

            ConfigureNodes(treeNode);
        }
        private void ShowAboutWindow(object parameter)
        {
            MessageBox.Show("1C# Scripting module © 2020"
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
            //var controller = Module.GetController<ContractsController>();
            //controller.AttachTreeNodes(mainNode);

            //TreeNodeViewModel nodes = new TreeNodeViewModel()
            //{
            //    IsExpanded = true,
            //    NodeIcon = WEB_SERVER_ICON,
            //    NodeText = NODES_NAME,
            //    NodeToolTip = NODES_TOOLTIP,
            //    NodePayload = null
            //};
            //nodes.ContextMenuItems.Add(new MenuItemViewModel()
            //{
            //    MenuItemHeader = "Add node",
            //    MenuItemIcon = ADD_NODE_ICON,
            //    MenuItemCommand = new RelayCommand(CreateIntegrationNode),
            //    MenuItemPayload = nodes
            //});
            //mainNode.TreeNodes.Add(nodes);
        }
        //private TreeNodeViewModel CreateTreeNode(TreeNodeViewModel parent, IntegrationNode model)
        //{
        //    //TreeNodeViewModel treeNode = new TreeNodeViewModel()
        //    //{
        //    //    IsExpanded = true,
        //    //    NodeIcon = WEB_SERVER_ICON,
        //    //    NodeText = $"{model.Name} ({(string.IsNullOrWhiteSpace(model.Address) ? "{address}" : model.Address)})",
        //    //    NodeToolTip = $"{(string.IsNullOrWhiteSpace(model.Server) ? "{server}" : $"{model.Server}")} : {(string.IsNullOrWhiteSpace(model.Database) ? "{database}" : $"{model.Database}")}",
        //    //    NodePayload = model
        //    //};
        //    //treeNode.ContextMenuItems.Add(new MenuItemViewModel()
        //    //{
        //    //    MenuItemHeader = "Create node...",
        //    //    MenuItemIcon = ADD_NODE_ICON,
        //    //    MenuItemCommand = new RelayCommand(CreateIntegrationNode),
        //    //    MenuItemPayload = treeNode
        //    //});
        //    //treeNode.ContextMenuItems.Add(new MenuItemViewModel()
        //    //{
        //    //    MenuItemHeader = "Update node...",
        //    //    MenuItemIcon = ADD_NODE_ICON,
        //    //    MenuItemCommand = new RelayCommand(UpdateIntegrationNode),
        //    //    MenuItemPayload = treeNode
        //    //});
        //    //parent.TreeNodes.Add(treeNode);
        //    //return treeNode;
        //    return null;
        //}
        private void CreateIntegrationNode(object parameter)
        {
            //TreeNodeViewModel treeNode = parameter as TreeNodeViewModel;
            //if (treeNode == null) return;

            //ScriptConcept script = new ScriptConcept();
            //LanguageConcept language = new LanguageConcept()
            //{
            //    Parent = script,
            //    Assembly = Assembly.GetExecutingAssembly()
            //};
            //script.Languages.Add(language);
            //script.Statements.Add(new CreateIntegrationNodeConcept()
            //{
            //    Parent = script
            //});
            //CodeEditor editor = new CodeEditor()
            //{
            //    DataContext = SyntaxTreeController.Current.CreateSyntaxNode(null, script)
            //};
            //Module.Shell.AddTabItem(SCRIPT_TAB_TITLE, editor);

            //_ = CreateTreeNode(treeNode, node);
        }
        private void UpdateIntegrationNode(object parameter)
        {
            //if (!(parameter is TreeNodeViewModel treeNode)) return;
            //if (!(treeNode.NodePayload is IntegrationNode node)) return;
            //ScriptConcept script = new ScriptConcept();
            //LanguageConcept language = new LanguageConcept()
            //{
            //    Parent = script,
            //    Assembly = Assembly.GetExecutingAssembly()
            //};
            //script.Languages.Add(language);
            //script.Statements.Add(new CreateIntegrationNodeConcept()
            //{
            //    Parent = script,
            //    Identifier = node.Name
            //});
            //CodeEditor editor = new CodeEditor()
            //{
            //    DataContext = SyntaxTreeController.Current.CreateSyntaxNode(null, script)
            //};
            //Module.Shell.AddTabItem(SCRIPT_TAB_TITLE, editor);
        }
    }
}