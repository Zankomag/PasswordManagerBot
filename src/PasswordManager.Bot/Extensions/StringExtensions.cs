﻿using System.Text;
using System;
using PasswordManager.Bot.Commands.Enums;

namespace PasswordManager.Bot.Extensions {
	public static class StringExtensions {

		///<returns>https://value</returns>
		public static string BuildLink(this string value) {
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("string is null, whitespace or empty");
			return (value.StartsWith("https://") || value.StartsWith("http://"))
				? value.Trim() : "https://" + value.Trim();
		}


		/// ///<returns>first_word_in_string.com</returns>
		public static string AutoLink(this string value) {
			if (string.IsNullOrWhiteSpace(value))
				throw new ArgumentException("string is null, whitespace or empty");
			int spaceIndex;
			string autoLink = (spaceIndex = value.IndexOf(' ')) == -1
				? value.ToLower() : value.Substring(0, spaceIndex).ToLower();
			StringBuilder autoLinkBuilder = new StringBuilder(autoLink);
			if (!autoLink.Contains('.')) {
				autoLinkBuilder.Append(".com");
				return autoLinkBuilder.ToString();
			}
			return autoLink;
		}

		public static string ToZeroOneString(this bool param) {
			return param ? "1" : "0";
		}

		/// <summary>
		/// This function retunrs reverse bool because it will be handled as new setting
		/// which must be opposite to last setting
		/// </summary>
		/// <param name="param"></param>
		/// <returns></returns>
		public static string ToReverseZeroOneString(this bool param) {
			return param ? "0" : "1";
		}

			public static string ToEmojiString(this bool param, bool addSpace = false) {
			string result =  param ? "✅" : "✖️";
			if (addSpace)
				result += " ";
			return result;
		}

		public static string ToStringCode(this CallbackQueryCommandCode callbackCommandCode) 
			=> ((char)callbackCommandCode).ToString();

		/// <param name="setUpPasswordGeneratorCommandCode"></param>
		/// <returns><see cref="CallbackQueryCommandCode.SetUpPasswordGenerator"/> + setUpPasswordGeneratorCommandCode</returns>
		public static string ToStringCode(this SetUpPasswordGeneratorCommandCode setUpPasswordGeneratorCommandCode) {
			StringBuilder commandBuilder 
				= new StringBuilder(
					((char)CallbackQueryCommandCode.SetUpPasswordGenerator)
						.ToString());
			return commandBuilder
				.Append((char)setUpPasswordGeneratorCommandCode)
				.ToString();
		}

		/// <param name="addAccountCommandCode"></param>
		/// <returns><see cref="CallbackQueryCommandCode.AddAccount"/> + addAccountCommandCode</returns>
		public static string ToStringCode(this AddAccountCommandCode addAccountCommandCode) {
			StringBuilder commandBuilder
				= new StringBuilder(
					((char)CallbackQueryCommandCode.AddAccount)
						.ToString());
			return (commandBuilder.Append((char)addAccountCommandCode).ToString());
		}

		/// <returns>null if there is no command in message</returns>
		public static string GetTextCommand(this string messageText) {
			//TODO:
			//remove '/' from returned message when using new commands without /
			if (messageText == null)
				throw new ArgumentNullException(nameof(messageText));
			//Command that starts with '/' may contain args and must be separated from them
			if (messageText.StartsWith('/')) {
				string commandString = messageText.ToLower();
				int cIndex = commandString.IndexOfAny(new char[] { ' ', '\n' });
				return cIndex != -1 ? commandString.Substring(0, cIndex) : commandString;
			}
			return null;
		}

		//TODO:
		//unit test
		/// <param name="commandText">command with args</param>
		/// <returns>All commands args separated by new line, except command itself</returns>
		public static string[] GetCommandArgsByNewLine(this string commandText) {
			if (commandText == null)
				throw new ArgumentNullException(nameof(commandText));
			if (commandText[0] != '/')
				throw new ArgumentException("command must start with '/'", nameof(commandText));
			int indexOfSpace = commandText.IndexOf(' ');
			int indexOfNewLine = commandText.IndexOf('\n');
			int firstArgStartIndex; //Equals to length of /command + space|\n after it
			//Assign to firstArgStartIndex lowest index + 1 if it's not -1
			if (indexOfSpace == -1) {
				if (indexOfNewLine == -1)
					return null;
				firstArgStartIndex = indexOfNewLine + 1;
			} else {
				firstArgStartIndex = indexOfNewLine == -1 
					? indexOfSpace + 1
					: indexOfNewLine < indexOfSpace
					? indexOfNewLine : indexOfSpace;
			}
			return commandText.Remove(0, firstArgStartIndex)
				.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
