﻿using System.Threading.Tasks;
using Telegram.Bot.Types;
using PasswordManager.Bot.Models;
using PasswordManager.Bot.Commands.Abstractions;
using PasswordManager.Bot.Services.Abstractions;
using PasswordManager.Application.Services.Abstractions;

namespace PasswordManager.Bot.Commands {
	public class RemoveUserCommand : Abstractions.BotCommand, IMessageCommand {
		private readonly IUserService userService;

		public RemoveUserCommand(IBot bot, IUserService userService) : base(bot) {
			this.userService = userService;
		}

		async Task IMessageCommand.ExecuteAsync(Message message, BotUser user) {
			if(bot.IsAdmin(user)) {
				int spaceIndex;
				if ((spaceIndex = message.Text.IndexOf(' ')) != -1) {
					string userIdStr = message.Text[(spaceIndex+1)..];
					if(int.TryParse(userIdStr, out int userId)){
						if (bot.IsAdmin(user)) {
							await bot.Client.SendTextMessageAsync(user.Id,
								"You are trying to remove admin. I won't let you do this.");
							return;
						}

						if (await userService.DeleteUser(userId)) {
							await bot.Client.SendTextMessageAsync(user.Id,
								"User and their data have been removed.");
						} else {
							await bot.Client.SendTextMessageAsync(user.Id, "Invalid user id");
						}
					} else {
						await bot.Client.SendTextMessageAsync(user.Id, "Invalid user id");
					}
				} else {
					await bot.Client.SendTextMessageAsync(user.Id, "Use /adduser < user id >");
				}
			}
		}
	}
}
