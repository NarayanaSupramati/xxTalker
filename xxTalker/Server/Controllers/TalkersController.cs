using xxTalker.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace xxTalker.Server.Controllers
{

    [ApiController]
    [Route("api/Talkers")]
    public class TalkersController : ControllerBase
    {
        private readonly IDataAccessProvider _dataAccessProvider;

        public TalkersController(IDataAccessProvider dataAccessProvider)
        {
            _dataAccessProvider = dataAccessProvider;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Talker>>> GetTalkersAsync()
        {
            try
            {
                return await _dataAccessProvider.GetTalkersAsync();
            }
            catch (Exception e)
            {
                var errMsg = e.InnerException != null ? e.InnerException?.Message : e.Message;
                return Problem(errMsg);
            }
        }

        [HttpGet("{accountId}")]
        public async Task<ActionResult<Talker?>> GetTalkerAsync(string accountId)
        {
            try
            {
                return await _dataAccessProvider.GetTalkerAsync(accountId);
            }
            catch (Exception e)
            {
                var errMsg = e.InnerException != null ? e.InnerException?.Message : e.Message;
                return Problem(errMsg);
            }
        }

        [HttpDelete("{accountId}")]
        public async Task<IActionResult> DeleteTalkerAsync(string accountId)
        {
            return Unauthorized();
        }

    }
}
