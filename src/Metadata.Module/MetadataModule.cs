using OneCSharp.Metadata.Model;
using OneCSharp.Metadata.Services;
using OneCSharp.MVVM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace OneCSharp.Metadata.Module
{
    public sealed class MetadataModule : IModule
    {
        private const string MODULE_NAME = "Metadata";
        private const string METADATA_SETTINGS_FILE_NAME = "MetadataServiceSettings.json";
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _providers = new Dictionary<Type, object>();
        private readonly Dictionary<Type, IController> _controllers = new Dictionary<Type, IController>();
        public MetadataModule() { }
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
        public IMetadataService Metadata { get; private set; } = new MetadataService();
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
        public string MetadataSettingsFile
        {
            get
            {
                return Path.Combine(ModuleCatalogPath, METADATA_SETTINGS_FILE_NAME);
            }
        }
        public void Initialize(IShell shell)
        {
            Shell = shell ?? throw new ArgumentNullException(nameof(shell));
            InitializeMetadataService();
            ConfigureControllers();
            ConfigureView();
        }
        private void ConfigureControllers()
        {
            _controllers.Add(typeof(MetadataModuleController), new MetadataModuleController(this));
        }
        private void ConfigureView()
        {
            IController controller = GetController<MetadataModuleController>();
            controller.BuildTreeNode(null, out TreeNodeViewModel mainNode);
            Shell.AddTreeNode(mainNode);
        }
        private void InitializeMetadataService()
        {
            MetadataServiceSettings settings;
            if (File.Exists(MetadataSettingsFile))
            {
                settings = LoadMetadataSettings();
            }
            else
            {
                settings = new MetadataServiceSettings();
                SaveMetadataSettings(settings);
            }
            if (string.IsNullOrWhiteSpace(settings.Catalog))
            {
                settings.Catalog = Path.Combine(ModuleCatalogPath, MODULE_NAME);
            }
            Metadata.Configure(settings);
        }

        public MetadataServiceSettings LoadMetadataSettings()
        {
            string settingsJson = File.ReadAllText(MetadataSettingsFile, Encoding.UTF8);
            return JsonSerializer.Deserialize<MetadataServiceSettings>(settingsJson);
        }
        public void SaveMetadataSettings(MetadataServiceSettings settings)
        {
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(new UnicodeRange(0, 0xFFFF));
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = encoder,
                WriteIndented = true
            };
            byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(settings.SettingsCopy(), options);
            File.WriteAllBytes(MetadataSettingsFile, bytes);
        }
    }
}