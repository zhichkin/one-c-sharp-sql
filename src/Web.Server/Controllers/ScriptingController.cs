using Microsoft.AspNetCore.Mvc;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using OneCSharp.Scripting.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace OneCSharp.Web.Server
{
    [Route("script")]
    [ApiController]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class ScriptingController : ControllerBase
    {
        private const string APPLICATION_JSON = "application/json";
        private IScriptingService Scripting { get; }
        public ScriptingController(IScriptingService scripting)
        {
            Scripting = scripting;
        }
        [HttpGet] public ActionResult<string> Get() { return "Hello from 1C# ! =)"; }
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
                    response.Errors.Add(new ParseErrorDescription() { Line = error.Line, Description = error.Message });
                }
                if (errors.Count > 0)
                {
                    return BadRequest(response);
                }
                response.Script = sql;
            }
            catch (Exception ex)
            {
                response.Errors.Add(new ParseErrorDescription() { Line = 0, Description = ex.Message });
                return BadRequest(response);
            }
            return response;
        }
        // POST: script/execute
        [HttpPost("execute")] public IActionResult Post([FromBody] ExecuteScriptRequest request) // ExecuteScriptResponse
        {
            ExecuteScriptResponse response = new ExecuteScriptResponse();
            if (request == null || string.IsNullOrWhiteSpace(request.Script))
            {
                response.Result = string.Empty;
                response.Errors.Add(new ParseErrorDescription() { Line = 0, Description = "Script is empty" });
                return BadRequest(response);
            }
            string sql;
            string json;
            try
            {
                sql = Scripting.PrepareScript(request.Script, out IList<ParseError> errors);
                foreach (ParseError error in errors)
                {
                    response.Errors.Add(new ParseErrorDescription() { Line = error.Line, Description = error.Message });
                }
                if (errors.Count > 0)
                {
                    response.Result = request.Script;
                    return BadRequest(response);
                }
                json = Scripting.ExecuteScript(sql, out _);
            }
            catch (Exception ex)
            {
                response.Result = string.Empty;
                response.Errors.Add(new ParseErrorDescription() { Line = 0, Description = ex.Message });
                return BadRequest(response);
            }
            return Content(json, APPLICATION_JSON, Encoding.UTF8);
        }
    }
}