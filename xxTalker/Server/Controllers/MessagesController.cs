using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using System.Text;
using System.Text.Json;
using xxTalker.Shared;
using xxTalker.Shared.Models;
using Microsoft.AspNetCore.SignalR;
using xxTalker.Server.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using xxTalker.Server.Filters;
using Microsoft.AspNetCore.Cors;

namespace xxTalker.Server.Controllers
{

    [ApiController]
    [Route("api/Messages")]
    public class MessagesController : ControllerBase
    {

        private readonly IDataAccessProvider _dataAccessProvider;
        private readonly IHubContext<TalkerHub> _hubContext;
        private readonly IConfiguration _config;

        public MessagesController(IDataAccessProvider dataAccessProvider, IHubContext<TalkerHub> hubContext, IConfiguration config)
        {
            _dataAccessProvider = dataAccessProvider;
            _hubContext = hubContext;
            _config = config;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TalkerMessage>>> GetMessagesAsync()
        {
            try
            {
                return await _dataAccessProvider.GetMessagesAsync();
            }
            catch (Exception e)
            {
                var errMsg = e.InnerException != null ? e.InnerException?.Message : e.Message;
                return Problem(errMsg);
            }
        }

        [HttpGet("{messageId}")]
        public async Task<ActionResult<TalkerMessage?>> GetMessageAsync(int messageId)
        {
            try
            {
                return await _dataAccessProvider.GetMessageAsync(messageId);
            }
            catch (Exception e)
            {
                var errMsg = e.InnerException != null ? e.InnerException?.Message : e.Message;
                return Problem(errMsg);
            }
        }

        [HttpPost]
        [EnableCors]
        [ValidateReferrer]
        [RequestLimit("AddMessageAsync", NoOfRequest = 6, Seconds = 60)]
        public async Task<IActionResult> AddMessageAsync([FromBody] TalkerMessage message)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem(ModelState);
                }
                var messageErrors = await ValidationMessage(message);
                if (messageErrors.Any())
                {
                    var vpd = new ValidationProblemDetails(messageErrors);
                    return ValidationProblem(vpd);
                }
                //if (message.MessageType != MessageType.Info)
                //{
                await NotifyGroup(message.ReceiverAccount, message.ReceiverAccount);
                await NotifyGroup(message.ReceiverAccount);
                //}

                await _dataAccessProvider.AddMessageAsync(message);
                return Ok();
            }
            catch (Exception e)
            {
                var errMsg = e.InnerException != null ? e.InnerException?.Message : e.Message;
                return Problem(errMsg);
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMessageAsync([FromBody] TalkerMessage message)
        {
            return Unauthorized();
        }

        private async Task NotifyGroup(string receiverAccount, string groupName = "Home")
        {
            await _hubContext.Clients.Group(groupName).SendAsync("NewAccountMessage", $"{receiverAccount}");
        }

        private async Task<Dictionary<string, string[]>> ValidationMessage(TalkerMessage message)
        {
            var errorList = new Dictionary<string, string[]>();

            message.MessageId = 0;
            message.MessageDate = DateTime.UtcNow;
            message.RefMessageNum = null;
            if (message.Rating < 0) message.Rating = 0;
            if (message.Rating > 5) message.Rating = 5;

            if (message.Rating > 0 && (string.IsNullOrEmpty(message.SenderAccount) || message.ReceiverAccount == message.SenderAccount))
            {
                errorList.Add("Rating", new[] { "Self-rating" });
            }

            if (message.MessageType != MessageType.Rating && message.Rating > 0)
            {
                errorList.Add("MessageType", new[] { "Wrong" });
            }

            if (message.Rating == 0 && message.MessageType == MessageType.Rating)
            {
                if (errorList.ContainsKey("Rating"))
                {
                    var msg = errorList.GetValueOrDefault("Rating");
                    msg[msg.Length] = "Not set";
                }
                else
                {
                    errorList.Add("Rating", new[] { "Not set" });
                }
            }

            if (string.IsNullOrEmpty(message.Message) && message.MessageType != MessageType.Info)
            {
                errorList.Add("Message", new[] { "Not set" });
            }

            var account = string.IsNullOrEmpty(message.SenderAccount) || message.SenderAccount == message.ReceiverAccount ? message.ReceiverAccount : message.SenderAccount;
            if (!CheckSign(account, message.Signature, message.Message))
            {
                errorList.Add("Signature", new[] { "Not verified" });
            }

            if (message.MessageType == MessageType.Info && (!string.IsNullOrEmpty(message.SenderAccount) && message.SenderAccount != message.ReceiverAccount))
            {
                errorList.Add("SenderAccount", new[] { "Not empty" });
            }

            if (message.ReceiverRole != null)
            {
                var isValidRole = CheckRole(message.ReceiverRole);
                if (!isValidRole)
                {
                    errorList.Add("ReceiverRole", new[] { "Invalid" });
                }
            }

            if (message.SenderRole != null)
            {
                var isValidRole = CheckRole(message.SenderRole);
                if (!isValidRole)
                {
                    errorList.Add("SenderRole", new[] { "Invalid" });
                }
            }

            var messageNum = await GetMessageNum(message.ReceiverAccount);
            if (messageNum != message.MessageNum)
            {
                errorList.Add("MessageNum", new[] { "Does not match" });
            }

            if (!string.IsNullOrEmpty(message.ReceiverAccount) && !await GetAccountDataAsync(message.ReceiverAccount))
            {
                errorList.Add("ReceiverAccount", new[] { "Not found" });
            }

            if (!string.IsNullOrEmpty(message.SenderAccount) && !await GetAccountDataAsync(message.SenderAccount))
            {
                errorList.Add("SenderAccount", new[] { "Not found" });
            }

            //spam & dupl
            var lastMsg = await _dataAccessProvider.GetLastSenderMessageAsync(message);
            if (lastMsg != null) {
                var msgInterval = _config.GetValue<int>("MessageInterval");
                var timeAgo = DateTime.UtcNow.Subtract(lastMsg.MessageDate);
                if (timeAgo < TimeSpan.FromSeconds(msgInterval))
                {
                    if (errorList.ContainsKey("Message"))
                    {
                        var msg = errorList.GetValueOrDefault("Message");
                        msg[msg.Length] = $"Outside the {msgInterval}-second interval";
                    }
                    else
                    {
                        errorList.Add("Message", new[] { $"Outside the {msgInterval}-second interval" });
                    }
                }
                if (lastMsg.Message == message.Message)
                {
                    if (errorList.ContainsKey("Message"))
                    {
                        var msg = errorList.GetValueOrDefault("Message");
                        msg[msg.Length] = "Duplicate";
                    }
                    else
                    {
                        errorList.Add("Message", new[] { "Duplicate" });
                    }
                }
            }

            return errorList;
        }

        private async Task<int> GetMessageNum(string accountId)
        {
            var talker = await _dataAccessProvider.GetTalkerAsync(accountId);
            if (talker != null)
            {
                return talker.Messages.Count;
            }
            return 0;
        }

        private bool CheckRole(string role)
        {
            var roles = role.Split(",").Select(r => r.Trim());
            return roles.Contains("Validator") || roles.Contains("Nominator") || roles.Contains("Council") || roles.Contains("Holder");
        }

        private bool CheckSign(string account, string signature, string? message)
        {
            var bSignature = Common.StringToByteArray(signature);
            var bPublicKey = Common.GetPublicKeyFromAddr(account);
            var bMessage = Encoding.UTF8.GetBytes(string.Format("<Bytes>{0}</Bytes>", message));

            return Schnorrkel.Sr25519v091.Verify(bSignature, bPublicKey, bMessage);
        }

        private async Task<bool> GetAccountDataAsync(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
            {
                return false;
            }
            try
            {
                var postBody = new
                {
                    operationName = "GetAccountByPK",
                    variables = new { accountId = accountId },
                    query = "fragment identity on identity {\n  blurb\n  display\n  discord\n  displayParent: display_parent\n  email\n  judgements\n  legal\n  riot\n  twitter\n  verified\n  web\n  __typename\n}\n\nfragment roles_fragment on account {\n  techcommit\n  special\n  nominator\n  council\n  validator\n  __typename\n}\n\nfragment account on account {\n  id: account_id\n  controllerAddress: controller_address\n  active\n  whenCreated: when_created\n  whenKilled: when_killed\n  blockHeight: block_height\n  identity {\n    ...identity\n    __typename\n  }\n  nonce\n  timestamp\n  ...roles_fragment\n  lockedBalance: locked_balance\n  reservedBalance: reserved_balance\n  totalBalance: total_balance\n  bondedBalance: bonded_balance\n  councilBalance: council_balance\n  democracyBalance: democracy_balance\n  transferrableBalance: transferrable_balance\n  unbondingBalance: unbonding_balance\n  vestingBalance: vesting_balance\n  __typename\n}\n\nquery GetAccountByPK($accountId: String!) {\n  account: account_by_pk(account_id: $accountId) {\n    ...account\n    __typename\n  }\n}"
                };
                using var response = await new HttpClient().PostAsJsonAsync("https://xxexplorer-prod.hasura.app/v1/graphql", postBody);
                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }
                JsonElement jData = await response.Content.ReadFromJsonAsync<JsonElement>();
                JsonElement acc = jData.GetProperty("data").GetProperty("account");
                if (acc.ValueKind == JsonValueKind.Null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

    }
}
