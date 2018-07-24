using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace docs.host
{
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private const string Host = "docs.microsoft.com/";

        [HttpGet("{*path}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Document>> GetAsync(string path)
        {
            return NotFound();
        }

        private Tuple<string, string, string, string> ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return Tuple.Create<string, string, string, string>($"{Host}index", "live", "en-us", null);
            }

            return null;
        }
    }
}
