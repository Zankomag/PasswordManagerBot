﻿using System.Threading.Tasks;
using Telegram.Bot.Types;
using PasswordManager.Bot.Types;
using MultiUserLocalization;
using System;
using System.Data;
using User = PasswordManager.Core.Entities.User;
using PasswordManager.Bot.Commands.Abstractions;

namespace PasswordManager.Bot.Commands {
	public class RemoveUserCommand : IMessageCommand {
		public async Task ExecuteAsync(Message message, User user) {
			if(user.Id == Bot.Instance.AdminId.Identifier)
			{
				try
				{
					string userIdStr = message.Text.Split(' ')[1];
					int userId = Convert.ToInt32(userIdStr);
					if(userId == Bot.Instance.AdminId.Identifier)
					{
						await Bot.Instance.Client.SendTextMessageAsync(Bot.Instance.AdminId, "You are trying to remove yourself. I won't let you do this.");
						return;
					}

					using (IDbConnection conn = new SQLiteConnection(Bot.Instance.connString))
					{
						conn.Execute("delete from Users where Id = @userId",
							new { userId});
					}
					await Bot.Instance.Client.SendTextMessageAsync(Bot.Instance.AdminId, "User and their data have been removed.");
				}
				catch(Exception ex)
				{
					await Bot.Instance.Client.SendTextMessageAsync(Bot.Instance.AdminId, "Error occured:\n\n" + ex.ToString());
				}
			}
		}
	}
}