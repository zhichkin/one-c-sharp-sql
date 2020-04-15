using System.Collections.Generic;

namespace OneCSharp.Web.Server
{
    public sealed class ExecuteScriptResponse
    {
        public string Result { get; set; }
        public List<ParseErrorDescription> Errors { get; } = new List<ParseErrorDescription>();
    }
}