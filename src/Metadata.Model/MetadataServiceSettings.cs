using System.Collections.Generic;

namespace OneCSharp.Metadata.Model
{
    public sealed class MetadataServiceSettings
    {
        public string Catalog { get; set; } = string.Empty;
        public List<DatabaseServer> Servers { get; set;  } = new List<DatabaseServer>();
        public MetadataServiceSettings SettingsCopy() // This method is used to save settings to file
        {
            MetadataServiceSettings copy = new MetadataServiceSettings()
            {
                Catalog = this.Catalog
            };
            foreach (DatabaseServer server in Servers)
            {
                DatabaseServer serverCopy = new DatabaseServer()
                {
                    Name = server.Name,
                    Address = server.Address
                };
                foreach (InfoBase database in server.Databases)
                {
                    serverCopy.Databases.Add(new InfoBase()
                    {
                        Name = database.Name,
                        Alias = database.Alias
                    });
                }
                copy.Servers.Add(serverCopy);
            }
            return copy;
        }
    }
}