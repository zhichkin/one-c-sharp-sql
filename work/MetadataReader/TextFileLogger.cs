using System.IO;

namespace OneCSharp.SQL.Services
{
    public interface ILogger
    {
        string CatalogPath { get; }
        void WriteEntry(string entry);
    }
    public sealed class TextFileLogger : ILogger
    {
        private readonly string _logPath;
        public TextFileLogger(string logPath) { _logPath = logPath; }
        public void WriteEntry(string entry)
        {
            using (StreamWriter writer = new StreamWriter(_logPath, true))
            {
                writer.WriteLine(entry);
                writer.Close();
            }
        }
        public string CatalogPath
        {
            get { return Path.GetDirectoryName(_logPath); }
        }
    }
}
