﻿using System;
using System.Collections.Generic;
using PasswordManager.Bot.Abstractions;
using PasswordManager.Bot.Enums;
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

		public AccountAssemblingStage Create(int userId) {
			AccountAssemblingStage nextAccountAssemblingStage = AccountAssemblingStage.AddAccountName;
			assemblingAccounts[userId] = new AccountAssemblingModel() {
				AccountAssemblingStage = nextAccountAssemblingStage,
				UserId = userId,
			};
			return nextAccountAssemblingStage;
		}
		//TODO:
		//Add EncryptionKey in the last arg line in inline assembling
		public AccountAssemblingStage Create(int userId, string[] args) => throw new NotImplementedException();

		public AccountAssemblingStage GetCurrentStage(int userId) {
			if(assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssembleModel)) {
				return accountAssembleModel.AccountAssemblingStage;
			}
			return AccountAssemblingStage.None;
		}

		//TODO:
		//Delete this method if it's not used
		public AccountAssemblingStage GetNextStage(int userId) {
			if (assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssembleModel)) {
				if ((int)accountAssembleModel.AccountAssemblingStage > (int)AccountAssemblingStage.Release)
					throw new InvalidOperationException();
				return accountAssembleModel.AccountAssemblingStage + 1;
			}
			return AccountAssemblingStage.None;
		}

		public Account Release(int userId) {
			if (assemblingAccounts.TryGetValue(userId, out AccountAssemblingModel accountAssembleModel)) {
				if(accountAssembleModel.AccountAssemblingStage == AccountAssemblingStage.Release) {
					assemblingAccounts.Remove(userId);
					return new Account {
						AccountName = accountAssembleModel.AccountName,
						UserId = accountAssembleModel.UserId,
						Link = accountAssembleModel.Link,
						Note = accountAssembleModel.Note,
						Login = accountAssembleModel.Login,
						Password = accountAssembleModel.Password,
						Encrypted = accountAssembleModel.Encrypted,
						OutdatedTime = new TimeSpan(0, 0, 0),
						PasswordUpdatedDate = DateTime.Now,
					};
				}
			}
			return null;
		}

		public AccountAssemblingStage Assemble(int userId, string property) {

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
