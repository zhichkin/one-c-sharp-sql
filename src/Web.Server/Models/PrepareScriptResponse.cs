using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace OneCSharp.Web.Server
{
    public sealed class PrepareScriptResponse
    {
        public string Script { get; set; }
        public List<ParseErrorDescription> Errors { get; } = new List<ParseErrorDescription>();
    }
    public sealed class ParseErrorDescription
    {
        public int Line { get; set; }
        public string Description { get; set; }
    }
}