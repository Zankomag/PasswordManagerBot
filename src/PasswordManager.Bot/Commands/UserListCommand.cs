﻿using System.Threading.Tasks;
using Telegram.Bot.Types;
using PasswordManager.Bot;
using MultiUserLocalization;
using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using PasswordManager.Core.Entities;
using PasswordManager.Bot.Models;
using PasswordManager.Bot.Commands.Abstractions;
using PasswordManager.Application.Services.Abstractions;
using PasswordManager.Bot.Abstractions;
using User = PasswordManager.Core.Entities.User;

namespace PasswordManager.Bot.Commands {
	public class UserListCommand: Abstractions.BotCommand, IMessageCommand {
		private readonly IUserService userService;

		public UserListCommand(IBotService botService, IUserService userService) : base(botService) {
			this.userService = userService;
		}

		public async Task ExecuteAsync(Message message, BotUser user) {
			if(botService.IsAdmin(user)) {
				try {
					IList<User> users = await userService.GetAllBasicInfoAsync();
					string response = string.Empty;
					//TODO: use string builder
					for(int i = 0; i < users.Count; i++) {
						response += $"{(i + 1)}. [{ users[i].Id}](tg://user?id={users[i].Id}): {users[i].Accounts.Count}\n\n";
					}
					//TODO:
					//fix @UPwdBot, get bot nickname from bot and save in in bot class
					await botService.SendMessageToAllAdmins("All @UPwdBot users:\nUser: Number of accounts\n" + response,
						Telegram.Bot.Types.Enums.ParseMode.Markdown);
				}
				catch(Exception ex) {
					await botService.SendMessageToAllAdmins("Error occured:\n\n" + ex.ToString());
					//Log Exception
				}
			}
		}
	}
}
