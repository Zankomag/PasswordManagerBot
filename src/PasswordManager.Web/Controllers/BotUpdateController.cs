﻿using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;
using PasswordManager.Bot.Abstractions;

namespace PasswordManager.Web.Controllers
{
    [Route("api/bots")]
    [ApiController]
    public class BotUpdateController : ControllerBase {

		private readonly IBotHandler botHandler;
		private readonly IBotService botService;

		public BotUpdateController(IBotHandler botHandler, IBotService botService) {
			this.botHandler = botHandler;
			this.botService = botService;
		}

		[HttpPost("{token}")]
		public IActionResult Post([FromBody]Update update, string token) {
			if (botService.IsTokenCorrect(token)) {
				botHandler.HandleUpdate(update);
			}
			return Ok();
		}
	}
}