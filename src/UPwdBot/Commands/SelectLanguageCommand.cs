﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Uten.Localization.MultiUser;

namespace UPwdBot.Commands {
	public class SelectLanguageCommand : IMessageCommand, ICallBackQueryCommand {
		private readonly InlineKeyboardMarkup inlineKeyboard;

		public SelectLanguageCommand() {
			//Set up Choosing Language Keyboard
			IList<string> icons = Localization.GetIcons();
			IList<string> langCodes = Localization.GetLangCodes();
			int langNumber = Localization.LanguageNumber;
			int colNumber = 5;
			int rowNumber= (langNumber % colNumber) == 0 ? langNumber / colNumber : langNumber / colNumber + 1;

			InlineKeyboardButton[][] buttons = new InlineKeyboardButton[rowNumber][];
			int currentLang = 0;
			for (int i = 0; i < rowNumber; i++) {
				buttons[i] = new InlineKeyboardButton[colNumber - (((i + 1) * colNumber) - langNumber)];

				for(int j = 0; j < buttons[i].Length; j++) {
					buttons[i][j] = InlineKeyboardButton
					.WithCallbackData(icons[currentLang], "L" + langCodes[currentLang]);
					currentLang++;
				}	
			}
			inlineKeyboard = new InlineKeyboardMarkup(buttons);
		}

		public async Task ExecuteAsync(Message message, Types.User user) {
			await BotHandler.Bot.SendTextMessageAsync(message.Chat.Id, Localization.GetMessage("ChooseLang", user.Lang),
				replyMarkup: inlineKeyboard);
		}

		public async Task ExecuteAsync(CallbackQuery callbackQuery, Types.User user) {
			string langCode = callbackQuery.Data.Substring(1);
			if (!Localization.ContainsLanguage(langCode))
				langCode = Localization.defaultLanguage;
			if (user == null) {
				PasswordManager.AddUser(callbackQuery.From.Id, langCode);
			} else {
				PasswordManager.SetUserLanguage(user, langCode);
			}
			
			await BotHandler.Bot.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId,
				Localization.GetMessage("LangIsSet", langCode) + "\n\n" +
				Localization.GetMessage("Help", langCode));
		}
	}
}
