using Microsoft.AspNetCore.Mvc;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.TSQL.Scripting;
using System;
using System.Collections.Generic;

namespace OneCSharp.Web.Server
{
    [Route("script")]
    [ApiController]
    public class ScriptingController : ControllerBase
    {
        private IScriptingService Scripting { get; }
        public ScriptingController(IScriptingService scripting)
        {
            Scripting = scripting;
        }
        [HttpGet] public ActionResult<string> Get()
        {
            return "Hello from 1C# ! =)";
        }
        // POST: script/translate
        [HttpPost("translate")] public ActionResult<PrepareScriptResponse> Post([FromBody] PrepareScriptRequest request)
        {
            PrepareScriptResponse response = new PrepareScriptResponse();
            if (request == null || string.IsNullOrWhiteSpace(request.Script))
            {
                response.Script = string.Empty;
                response.Errors.Add(new ParseErrorDescription()
                {
                    Line = 0,
                    Description = "Script is empty"
                });
                return BadRequest(response);
            }

            try
            {
                string sql = Scripting.PrepareScript(request.Script, out IList<ParseError> errors);

                foreach (ParseError error in errors)
                {
                    response.Errors.Add(new ParseErrorDescription()
                    {
                        Line = error.Line,
                        Description = error.Message
                    });
                }
                if (errors.Count > 0)
                {
                    return BadRequest(response);
                }

                response.Script = sql;
            }
            catch (Exception ex)
            {
                response.Errors.Add(new ParseErrorDescription()
                {
                    Line = 0,
                    Description = ex.Message
                });
                return BadRequest(response);
            }

            return response;
        }
        // POST: script/execute
        [HttpPost("execute")] public ActionResult<ExecuteScriptResponse> Post([FromBody] ExecuteScriptRequest request)
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