using Microsoft.AspNetCore.Mvc;
using OneCSharp.Metadata.Services;
using System;

namespace OneCSharp.Web.Server
{
    [Route("metadata")]
    [ApiController]
    public class MetadataController : ControllerBase
    {
        private IMetadataService MetadataService { get; }
        public MetadataController(IMetadataService service)
        {
            MetadataService = service;
        }
        [HttpGet("use")]
        public ActionResult Get()
        {
            string serverName = (MetadataService.CurrentServer == null) ? "not defined" : MetadataService.CurrentServer.Name;
            string databaseName = (MetadataService.CurrentDatabase == null) ? "not defined" : MetadataService.CurrentDatabase.Name;
            string response = $"Server: {serverName}\nDatabase: {databaseName}";
            return Content(response);
        }
        // POST: metadata/use/{server}/{database}
        [HttpPost("use/{server}/{database}")]
        public ActionResult Post([FromRoute] string server, string database)
        {
            if (string.IsNullOrWhiteSpace(server)) return NotFound();
            if (string.IsNullOrWhiteSpace(database)) return NotFound();

            try
            {
                MetadataService.UseServer(server);
                MetadataService.UseDatabase(database);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
            return Ok();
        }
    }
}