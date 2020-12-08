﻿using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using MultiUserLocalization;
using PasswordManager.Bot;
using System.Linq;
using System.Collections.Generic;
using PasswordManager.Bot.Enums;
using PasswordManager.Bot.Extensions;
using PasswordManager.Core.Entities;
using User = PasswordManager.Core.Entities.User;
using PasswordManager.Bot.Models;
using PasswordManager.Bot.Commands.Abstractions;
using PasswordManager.Application.Services.Abstractions;
using PasswordManager.Bot.Abstractions;

namespace PasswordManager.Bot.Commands {
	public class UpdateAccountCommand : Abstractions.BotCommand, ICallbackQueryCommand, IMessageCommand {
		private readonly IAccountService accountService;

		public UpdateAccountCommand(IBotService botService, IAccountService accountService) : base(botService) {
			this.accountService = accountService;
		}

		public async Task ExecuteAsync(CallbackQuery callbackQuery, BotUser user) {
			string accountId = callbackQuery.Data.Substring(2);
			if (callbackQuery.Data[1] == '0') {
				await .UpdateAccountAsync(
					callbackQuery.Message.Chat.Id,
					callbackQuery.Message.MessageId,
					accountId,
					Localization.GetMessage("ChooseWhatUpdate", user.Lang),
					user.Lang,
					callbackQuery.Message.Text.Count(x => x == '\n') % 2 == 0,
					callbackQuery.Message.Text);
			}
			else if(callbackQuery.Data[1] == 'E') {
				//Delete Link
				List<string> accountData = callbackQuery.Message.Text.Split('\n').Skip(2).ToList();
				accountData.RemoveAt(accountData.Count - 2);

				.DeleteAccountLink(user, accountId);
				await .UpdateAccountAsync(
					callbackQuery.Message.Chat.Id,
					callbackQuery.Message.MessageId,
					accountId,
					Localization.GetMessage("LinkDeleted", user.Lang),
					user.Lang,
					false,
					string.Join('\n', accountData));
			}
			else {
				await botService.Client.AnswerCallbackQueryAsync(callbackQuery.Id);
				.SetUserAction(user, UserAction.Update);
				AccountDataType accountDataType = (AccountDataType)(byte)callbackQuery.Data[1];
				InlineKeyboardMarkup inlineKeyboardMarkup = accountDataType == AccountDataType.Password ? 
					.GeneratePasswordButtonMarkup(user.Lang) : 
					null;
				Message sentMessage = await RequestUpdateData(callbackQuery.From.Id, accountDataType.ToString(), user.Lang, inlineKeyboardMarkup);
				.UpdatingAccounts[callbackQuery.From.Id] = new AccountUpdate(
					accountId,
					callbackQuery.Message.MessageId,
					sentMessage.MessageId,
					accountDataType);
			}
		}

		public async Task ExecuteAsync(Message message, BotUser user) {
			if (.UpdatingAccounts.ContainsKey(user.Id)) {
				AccountUpdate accountUpdate = .UpdatingAccounts[user.Id];
				if (!await .IsLengthExceededAsync(message.Text.Length, accountUpdate.AccountDataType.ToMaxAccountDataLength(), user.Id, user.Lang)) {
					await .UpdateAccountDataAsync(message.Text, accountUpdate.AccountToUpdateId, user.Id, user.Lang);
				}
			} else {
				.SetUserAction(user, UserAction.Search);
			}
		}

		private async Task<Message> RequestUpdateData(ChatId chatId, string dataKey, string langCode, InlineKeyboardMarkup inlineKeyboardMarkup = null) {
			return await botService.Client.SendTextMessageAsync(
				chatId,
				string.Format(Localization.GetMessage("UpdateAccData", langCode), Localization.GetMessage(dataKey, langCode)),
				replyMarkup: inlineKeyboardMarkup);
		}

		//Moved from 
		private async Task UpdateAccountDataAsync(string data, string accountId, int userId, string langCode) {
			if (UpdatingAccounts.ContainsKey(userId)) {
				AccountUpdate accountUpdate = UpdatingAccounts[userId];
				if (accountUpdate.AccountDataType == AccountDataType.Password) {
					//TODO: ENCRYPT
					data = data.Trim();
				} else if (accountUpdate.AccountDataType == AccountDataType.Link) {
					data = data.BuildLink();
				} else {
					data = data.Trim();
				}
				using (IDbConnection conn = new SQLiteConnection(botService.connString)) {
					conn.Execute($"update Account set {accountUpdate.AccountDataType.ToString()} = @data where Id = @accountId and UserId = @userId",
						new { data, accountId, userId });
				}
				UpdatingAccounts.Remove(userId);
				SetUserAction(userId, UserAction.Search);
				await ShowAccountById(userId, accountId, langCode, extraMessage: Localization.GetMessage("AccountUpdated", langCode));
				await BotHandlerService.TryDeleteMessageAsync(userId, accountUpdate.MessagetoDeleteId[0]);
				await BotHandlerService.TryDeleteMessageAsync(userId, accountUpdate.MessagetoDeleteId[1]);
			}
		}

		//Moved from 
		private async Task UpdateAccountAsync(
			ChatId chatId, int messageId, string accountId, string message,
			string langCode, bool containsDeleteLinkButton, string messageText) {

			string updateCommandCode = CallbackCommandCode.UpdateAccount.ToStringCode();

			InlineKeyboardButton[] accNameButton =
					new InlineKeyboardButton[] {
						InlineKeyboardButton.WithCallbackData(
							"📝 " + Localization.GetMessage("AccountName", langCode),
							updateCommandCode + 'N' + accountId)};
			InlineKeyboardButton[] linkButton =
				new InlineKeyboardButton[] {
						InlineKeyboardButton.WithCallbackData(
							"🔗 " + Localization.GetMessage("Link", langCode),
							updateCommandCode + 'R' + accountId) };
			InlineKeyboardButton[] loginButton =
				new InlineKeyboardButton[] {
						InlineKeyboardButton.WithCallbackData(
							"📇 " + Localization.GetMessage("Login", langCode),
							updateCommandCode + 'L' + accountId) };
			InlineKeyboardButton[] passwordButton =
				new InlineKeyboardButton[] {
						InlineKeyboardButton.WithCallbackData(
							"🔐 " + Localization.GetMessage("Password", langCode),
							updateCommandCode + 'P' + accountId) };
			InlineKeyboardButton[] backButton =
				new InlineKeyboardButton[] {
						InlineKeyboardButton.WithCallbackData(
							"⏪ " + Localization.GetMessage("Back", langCode),
							CallbackCommandCode.ShowAccount.ToStringCode() + accountId) };

			var keyboardMarkup = new InlineKeyboardMarkup(containsDeleteLinkButton ?
				new InlineKeyboardButton[][] {
						accNameButton,
						linkButton,
						new InlineKeyboardButton[] {
							InlineKeyboardButton.WithCallbackData(
								"🗑 " + Localization.GetMessage("DeleteLink", langCode),
								updateCommandCode + 'E' + accountId) },
						loginButton,
						passwordButton,
						backButton
				} :
				new InlineKeyboardButton[][] {
						accNameButton,
						linkButton,
						loginButton,
						passwordButton,
						backButton});

			await botService.Client.EditMessageTextAsync(chatId,
				messageId,
				message + "\n\n" +
				((messageText.Count(x => x == '\n') > 3) ?
					messageText.Substring(messageText.IndexOf('\n') + 2) :
					messageText),
				replyMarkup: keyboardMarkup,
				disableWebPagePreview: true);
		}

		//Moved from 
		private void DeleteAccountLink(User user, string accountId) {
			using (IDbConnection conn = new SQLiteConnection(botService.connString)) {
				if (user != null) {
					conn.Execute("update Accounts set Link = NULL where Id = @accountId and UserId = @Id",
						new { accountId, user.Id });
				}
			}
		}

	}
}