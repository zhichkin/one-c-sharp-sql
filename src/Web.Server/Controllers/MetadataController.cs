using Microsoft.AspNetCore.Mvc;
using OneCSharp.TSQL.Scripting;

namespace OneCSharp.Web.Server
{
    [Route("metadata")]
    [ApiController]
    public class MetadataController : ControllerBase
    {
        private IScriptingService Scripting { get; }
        public MetadataController(IScriptingService scripting)
        {
            Scripting = scripting;
        }
        // POST: metadata/use
        [HttpPost("use")] public ActionResult<ExecuteScriptResponse> Post([FromBody] ExecuteScriptRequest request)
        {
            ExecuteScriptResponse response = new ExecuteScriptResponse();
            if (request == null)
            {
                response.Result = string.Empty;
                response.Errors.Add(new ParseErrorDescription()
                {
                    Line = 0,
                    Description = "Script is empty"
                });
                return BadRequest();
            }
            return response;
        }
    }
}