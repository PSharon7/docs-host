using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace docs.host
{
    [Route("api/Documents")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        [HttpGet("{url}/{branch}/{locale}/{version}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<Document>> GetAsync(string url, string branch, string locale, string version)
        {
            var document = await Reader.QueryDocument(url, branch, locale, version);
            if (document is null)
                return NotFound();

            return document;
        }
    }
}
