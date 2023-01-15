using Microsoft.AspNetCore.Mvc;

namespace ApplicationAccess.Hosting.Rest.ReaderWriter.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        protected ReaderWriterNode<String, String, String, String> readerWriterNode;

        public TestController(ReaderWriterNode<String, String, String, String> readerWriterNode)
        {
            this.readerWriterNode = readerWriterNode;
        }

        [HttpGet]
        [Route("Users")]
        public IEnumerable<String> Get()
        {
            return readerWriterNode.Users;
        }
    }
}
