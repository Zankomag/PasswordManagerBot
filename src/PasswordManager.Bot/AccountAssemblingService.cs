﻿using System;
using System.Collections.Generic;
using PasswordManager.Application.Encryption;
using PasswordManager.Bot.Abstractions;
using PasswordManager.Bot.Enums;
using PasswordManager.Bot.Extensions;
using PasswordManager.Bot.Models;
using PasswordManager.Core.Entities;

namespace PasswordManager.Bot {
	public class AccountAssemblingService : IAccountAssemblingService {
		//Assembling Accounts data is stored in memory
		//because storing it in database doesn't worth it
		private readonly Dictionary<int, AccountAssemblingModel> assemblingAccounts;

		public AccountAssemblingService() {
			assemblingAccounts = new Dictionary<int, AccountAssemblingModel>();
		}

		public void Cancel(int userId) => assemblingAccounts.Remove(userId);

		//TODO: Rename EncryptPassword to AddEncryptionKey

		// [2] /add AccountName \n Login => Ask for password? then for encryptionKey
		// [3] /add AccountName \n Login \n Password => Ask for encryptionKey
		// [4] /add AccountName \n Login \n Password \n EncryptionKey
		// [5] /add AccountName \n Link \n Login \n Password \n EncryptionKey
		// [6] /add AccountName \n Link \n Note \n Login \n Password \n EncryptionKey
		public AccountAssemblingStage Create(int userId, string[] args = null) {
			var accountAssemblingModel = new AccountAssemblingModel() {
				AccountAssemblingStage = AccountAssemblingStage.AddAccountName,
				UserId = userId,
			};

			if (args == null || args.Length == 0) {
				assemblingAccounts[userId] = accountAssemblingModel;
				return accountAssemblingModel.AccountAssemblingStage;
			}

			accountAssemblingModel.AccountName = args[0];
			switch (args.Length) {
				case 1:
					break;
				case 2:
					accountAssemblingModel.Login = args[1];
					accountAssemblingModel.AccountAssemblingStage = AccountAssemblingStage.AddPassword;
					break;
				case 3:
					accountAssemblingModel.Login = args[1];
					accountAssemblingModel.Password = args[2];
					accountAssemblingModel.AccountAssemblingStage = AccountAssemblingStage.EncryptPassword;
					break;
				case 4:
					accountAssemblingModel.Login = args[1];
					accountAssemblingModel.Password = args[2].Encrypt(args[3]);
					accountAssemblingModel.Encrypted = true;
					accountAssemblingModel.AccountAssemblingStage = AccountAssemblingStage.Release;
					break;
				case 5:
					accountAssemblingModel.Link = args[1].BuildLink();
					accountAssemblingModel.Login = args[2];
					accountAssemblingModel.Password = args[3].Encrypt(args[4]);
					accountAssemblingModel.Encrypted = true;
					accountAssemblingModel.AccountAssemblingStage = AccountAssemblingStage.Release;
					break;
				case 6:
					accountAssemblingModel.Link = args[1].BuildLink();
					accountAssemblingModel.Note = args[2];
					accountAssemblingModel.Login = args[3];
					accountAssemblingModel.Password = args[4].Encrypt(args[5]);
					accountAssemblingModel.Encrypted = true;
					accountAssemblingModel.AccountAssemblingStage = AccountAssemblingStage.Release;
					break;
				default:
					throw new ArgumentException("Too many arguments", nameof(args));
			}

			assemblingAccounts[userId] = accountAssemblingModel;
			return accountAssemblingModel.AccountAssemblingStage;
		}

		public AccountAssemblingStage GetCurrentStage(int userId) {
			if(assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssemblingModel)) {
				return accountAssemblingModel.AccountAssemblingStage;
			}
			return AccountAssemblingStage.None;
		}

		public AccountAssemblingStage GetNextStage(int userId) {
			if (assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssemblingModel)) {
				if (accountAssemblingModel.AccountAssemblingStage == AccountAssemblingStage.Release)
					throw new InvalidOperationException("Account is already assembled");
				return accountAssemblingModel.AccountAssemblingStage + 1;
			}
			return AccountAssemblingStage.None;
		}

		public Account Release(int userId) {
			if (assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssemblingModel)) {
				if(accountAssemblingModel.AccountAssemblingStage == AccountAssemblingStage.Release) {
					assemblingAccounts.Remove(userId);
					return new Account {
						AccountName = accountAssemblingModel.AccountName,
						UserId = accountAssemblingModel.UserId,
						Link = accountAssemblingModel.Link,
						Note = accountAssemblingModel.Note,
						Login = accountAssemblingModel.Login,
						Password = accountAssemblingModel.Password,
						Encrypted = accountAssemblingModel.Encrypted,
						OutdatedTime = new TimeSpan(0, 0, 0),
						PasswordUpdatedDate = DateTime.Now,
					};
				}
			}
			return null;
		}

		public AccountAssemblingStage Assemble(int userId, string property) {
			if (property == null)
				throw new ArgumentNullException(nameof(property));
			if (assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssemblingModel)) {
				switch (accountAssemblingModel.AccountAssemblingStage) {
					case AccountAssemblingStage.AddAccountName:
						accountAssemblingModel.AccountName = property;
						break;
					case AccountAssemblingStage.AddLink:
						accountAssemblingModel.Link = property;
						break;
					case AccountAssemblingStage.AddNote:
						accountAssemblingModel.Note = property;
						break;
					case AccountAssemblingStage.AddLogin:
						accountAssemblingModel.Login = property;
						break;
					case AccountAssemblingStage.AddPassword:
						accountAssemblingModel.Password = property;
						break;
					case AccountAssemblingStage.EncryptPassword:
						accountAssemblingModel.Password = property.Encrypt(property);
						break;
					case AccountAssemblingStage.Release:
						throw new InvalidOperationException("Account is already assembled.");
					default:
						throw new InvalidOperationException("Unexpected Account Assembling Stage");
				}
				return ++accountAssemblingModel.AccountAssemblingStage;
			}
			throw new InvalidOperationException(
				"AccountAssembling doesn't exist. Use Create(int userId, string[] args) to start inline assembling");
		}

		public AccountAssemblingStage SkipStage(int userId, AccountAssemblingStageSkip accountAssemblingStageSkip) {
			if (!assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssemblingModel)) {
				throw new InvalidOperationException("AccountAssembling doesn't exist.");
			}
			return accountAssemblingModel.AccountAssemblingStage = ((AccountAssemblingStage)accountAssemblingStageSkip) + 1;
		}

		//TODO:
		//Add enum for assembling property and 
		//return it to AddAccount command to switch
		//AddAccountCommand should have NextStep switch case along with above
		//So Assemble Account Service should bother about assembling steps
		//
		//TODO:
		//After user enters password (or accepts it via button)
		//It has option to enter EncryptionKey 
		//There is invintation to enter key message that holds 2 buttons:
		//first button allows to skip password encryption
		//second button shows Encryption key hint
	}
}
