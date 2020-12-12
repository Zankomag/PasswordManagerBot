﻿
namespace PasswordManager.Bot.Commands.Enums {

	public enum UpdateAccountCommandCode {
		AccountName = 'A',
		Link = 'R',
		Note = 'N',
		Login = 'L',
		Password = 'P',
		ReencryptPassword = 'E' //Used for both Encrypt and Re-Encrypt actions
	}
}
