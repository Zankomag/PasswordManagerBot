﻿using Dapper;
using System;
using System.Data;
using System.Data.SQLite;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using PasswordManager.Bot.Types;
using MultiUserLocalization;
using PasswordManager.Bot.Types.Enums;
using PasswordManager.Bot.Extensions;
using PasswordManager.Bot.Commands.Abstractions;
using PasswordManager.Core.Entities;
using User = PasswordManager.Core.Entities.User;
using UserAction = PasswordManager.Core.Entities.User.UserAction;

namespace PasswordManager.Bot.Commands {
	public class AddAccountCommand : IMessageCommand {

		public async Task ExecuteAsync(Message message, User user) {
			if (message.Text.StartsWith("/add") && message.Text.Length > 5) {
				PasswordManagerHandler.AssemblingAccounts.Remove(message.From.Id);
				await AssembleAccountAsync(message, user);
			}
			else if (message.Text == "/add") {
				PasswordManagerHandler.AssemblingAccounts[message.From.Id] = new Account() { UserId = message.From.Id };
				PasswordManagerHandler.SetUserAction(user, UserAction.Assemble);
				await BotHandler.Bot.SendTextMessageAsync(message.From.Id,
					"📝 " + Localization.GetMessage("AddAccount", user.Lang));
			}
			//then user is already assembling account, so CONTINUE assembling it
			else {
				Account account = PasswordManagerHandler.AssemblingAccounts[message.From.Id];
				string data = !message.Text.Contains('\n') ? message.Text :
					message.Text.Substring(0, message.Text.IndexOf('\n'));
				if (account.AccountName == null) {
					if (!await PasswordManagerHandler.IsLengthExceededAsync(message.Text.Length, MaxAccountDataLength.AccountName, message.From.Id, user.Lang)) {
						account.AccountName = data.Trim();
						PasswordManagerHandler.AssemblingAccounts[message.From.Id] = account;
						PasswordManagerHandler.SetUserAction(user, UserAction.Assemble);
						await AddLinkPrompt(message.Chat.Id, account.AccountName, user.Lang);
					}
					//TODO
					//ADD SKIPLINK CKECK
				} else if(/*!account.SkipLink &&*/ account.Link == null){
					if (!await PasswordManagerHandler.IsLengthExceededAsync(message.Text.Length,MaxAccountDataLength.Link, message.From.Id, user.Lang)) {
						account.Link = data.BuildLink();
						PasswordManagerHandler.AssemblingAccounts[message.From.Id] = account;
						PasswordManagerHandler.SetUserAction(user, UserAction.Assemble);
						if (account.Login == null) {
							await BotHandler.Bot.SendTextMessageAsync(message.From.Id,
								"📇 " + Localization.GetMessage("AddLogin", user.Lang));
						}
						else {
							await BotHandler.Bot.SendTextMessageAsync(message.From.Id,
							"🔐 " + String.Format(Localization.GetMessage("AddPassword", user.Lang), "/generator"),
						replyMarkup: PasswordManagerHandler.GeneratePasswordButtonMarkup(user.Lang));
						}
					}
				} else if (account.Login == null) {
					if (!await PasswordManagerHandler.IsLengthExceededAsync(message.Text.Length, MaxAccountDataLength.Login, message.From.Id, user.Lang)) {
						account.Login = data.Trim();
						PasswordManagerHandler.AssemblingAccounts[message.From.Id] = account;
						PasswordManagerHandler.SetUserAction(user, UserAction.Assemble);
						await BotHandler.Bot.SendTextMessageAsync(message.From.Id,
							"🔐 " + String.Format(Localization.GetMessage("AddPassword", user.Lang), "/generator"),
						replyMarkup: PasswordManagerHandler.GeneratePasswordButtonMarkup(user.Lang));
					}
				} else if (account.Password == null) {
					if (!await PasswordManagerHandler.IsLengthExceededAsync(message.Text.Length, MaxAccountDataLength.Password, message.From.Id, user.Lang)) {
						//TODO: ENCRYPT PASSWORD
						account.Password = data.Trim();
						await SaveToDBAsync(account, user);
						PasswordManagerHandler.AssemblingAccounts.Remove(message.From.Id);
					}
				}
			}
		}

		private async Task AssembleAccountAsync(Message message, User user) {
			string[] accountData = message.Text.Remove(0, 5).Split('\n', StringSplitOptions.RemoveEmptyEntries);
			if (await PasswordManagerHandler.IsLengthExceededAsync(accountData[0].Length, MaxAccountDataLength.AccountName, message.From.Id, user.Lang)) {
				return;
			}
			Account account = new Account {
				AccountName = accountData[0].Trim(),
				UserId = message.From.Id
			};

			if (accountData.Length > 3) {
				if (!await PasswordManagerHandler.IsLengthExceededAsync(accountData[1].Length, MaxAccountDataLength.Link,		message.From.Id, user.Lang) &&
					!await PasswordManagerHandler.IsLengthExceededAsync(accountData[2].Length, MaxAccountDataLength.Login,		message.From.Id, user.Lang) &&
					!await PasswordManagerHandler.IsLengthExceededAsync(accountData[3].Length, MaxAccountDataLength.Password,	message.From.Id, user.Lang)) {

					account.Link = accountData[1].BuildLink();
					account.Login = accountData[2].Trim();
					//TODO: ENCRYPT PASSWORD
					account.Password = accountData[3].Trim();
					await SaveToDBAsync(account, user);
				}
			} else if(accountData.Length == 3) {
				if (!await PasswordManagerHandler.IsLengthExceededAsync(accountData[2].Length, MaxAccountDataLength.Password,	message.From.Id, user.Lang) &&
					!await PasswordManagerHandler.IsLengthExceededAsync(accountData[1].Length, MaxAccountDataLength.Login,		message.From.Id, user.Lang)) {
					
					account.Login = accountData[1].Trim();
					//TODO: ENCRYPT PASSWORD
					account.Password = accountData[2].Trim();
					await SaveToDBAsync(account, user);
				}
			} else if(accountData.Length == 2) {
				if (!await PasswordManagerHandler.IsLengthExceededAsync(accountData[1].Length, MaxAccountDataLength.Login, message.From.Id, user.Lang)) {
					account.Login = accountData[1].Trim();
					PasswordManagerHandler.AssemblingAccounts[message.From.Id] = account;
					PasswordManagerHandler.SetUserAction(user, UserAction.Assemble);
					await AddLinkPrompt(message.Chat.Id, account.AccountName, user.Lang);
				}
			} else {
				PasswordManagerHandler.AssemblingAccounts[message.From.Id] = account;
				PasswordManagerHandler.SetUserAction(user, UserAction.Assemble);
				await AddLinkPrompt(message.Chat.Id, account.AccountName, user.Lang);
			}

		}

		private static async Task SaveToDBAsync(Account account, User user, int messageToEditId = 0) {
			using (IDbConnection conn = new SQLiteConnection(Bot.Instance.connString)) {
				account.Id = (long)conn.ExecuteScalar("Insert into Account (UserId, AccountName, Link, Login, Password) " +
					"values (@UserId, @AccountName, @Link, @Login, @Password);" +
					"select last_insert_rowid()",
					account);
			}

			PasswordManagerHandler.SetUserAction(user, UserAction.Search);

			await PasswordManagerHandler.ShowAccount(account.UserId, account, user.Lang, messageToEditId: messageToEditId,
				extraMessage: "✅ " + String.Format(Localization.GetMessage("AccountAdded", user.Lang), account.AccountName));
		}

		private static async Task AddLinkPrompt(ChatId chatId, string accountName, string langCode) {
			var inlineKeyBoard = new InlineKeyboardMarkup(
				new InlineKeyboardButton[][] {
							new InlineKeyboardButton[] {
								InlineKeyboardButton.WithCallbackData("🔗 " + accountName.AutoLink(), CallbackCommandCode.AutoLink.ToStringCode())
							},
							new InlineKeyboardButton[] {
								InlineKeyboardButton.WithCallbackData(
									"⏩ " + Localization.GetMessage("Skip",langCode), CallbackCommandCode.SkipLink.ToStringCode())
							}
				}
			);
			await BotHandler.Bot.SendTextMessageAsync(chatId,
				"🔗 " + String.Format(Localization.GetMessage("AddLink", langCode), "/help"),
				replyMarkup: inlineKeyBoard);
		}

		public static async Task UpdateCallBackMessageAsync(ChatId chatId, int messageId, Account account, User user) {

			if (account.AccountName == null) {
				await BotHandler.Bot.EditMessageTextAsync(chatId, messageId,
					"📝 " + Localization.GetMessage("AddAccount", user.Lang));
			}
			//TODO
			//ADD SKIPLINK CKECK
			else if (/*!account.SkipLink &&*/ account.Link == null) {
				await AddLinkPrompt(chatId, account.AccountName, user.Lang);
			}
			else if (account.Login == null) {
				await BotHandler.Bot.EditMessageTextAsync(chatId, messageId,
					"📇 " + Localization.GetMessage("AddLogin", user.Lang));
			}
			else if (account.Password == null) {
				await BotHandler.Bot.EditMessageTextAsync(chatId, messageId,
					"🔐 " + String.Format(Localization.GetMessage("AddPassword", user.Lang), "/generator"),
					replyMarkup: PasswordManagerHandler.GeneratePasswordButtonMarkup(user.Lang));
			}
			else {
				PasswordManagerHandler.AssemblingAccounts.Remove(account.UserId);
				await SaveToDBAsync(account, user, messageId);
			}
		}

	}
}
