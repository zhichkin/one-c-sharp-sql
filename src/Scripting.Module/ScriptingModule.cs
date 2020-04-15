using OneCSharp.MVVM;
using System;
using System.Collections.Generic;
using System.IO;

namespace OneCSharp.Scripting.Module
{
    public sealed class ScriptingModule : IModule
    {
        private const string MODULE_NAME = "Scripting";
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _providers = new Dictionary<Type, object>();
        private readonly Dictionary<Type, IController> _controllers = new Dictionary<Type, IController>();
        public ScriptingModule() { }
        public T GetService<T>()
        {
            return (T)_services[typeof(T)];
        }
        public T GetProvider<T>()
        {
            return (T)_providers[typeof(T)];
        }
        public T GetController<T>()
        {
            return (T)_controllers[typeof(T)];
        }
        public IController GetController(Type type)
        {
            return _controllers[type];
        }
        public IShell Shell { get; private set; }
        public string ModuleCatalogPath
        {
            get
            {
                string moduleCatalog = Path.Combine(Shell.ModulesCatalogPath, MODULE_NAME);
                if (!Directory.Exists(moduleCatalog))
                {
                    _ = Directory.CreateDirectory(moduleCatalog);
                }
                return moduleCatalog;
            }
        }
        public void Initialize(IShell shell)
        {
            Shell = shell ?? throw new ArgumentNullException(nameof(shell));
            ConfigureControllers();
            ConfigureView();
        }
        private void ConfigureControllers()
        {
            _controllers.Add(typeof(ScriptingModuleController), new ScriptingModuleController(this));
        }
        private void ConfigureView()
        {
            IController controller = GetController<ScriptingModuleController>();
            controller.BuildTreeNode(null, out TreeNodeViewModel mainNode);
            Shell.AddTreeNode(mainNode);
        }
    }
}