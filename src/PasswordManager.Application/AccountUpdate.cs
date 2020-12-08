﻿using PasswordManager.Bot.Enums;

namespace PasswordManager.Bot {
	public class AccountUpdate {

		//TODO:
		//ISSUE: If user selects that he wants to update one thing (e.g. password)
		//and then after invintation message to send new password was sent
		//he selects that he want to update account name - 
		//new message with invintation to send name sends by bot
		//after sending new name TWO messages are deleting - selection and invintation to send name
		//BUT message with invintation to send password are not deleted
		//
		//SOLUTION:
		//instead of sending new message with invintation bot have
		//to edit message with selection to message with invintation
		//WITH "BACK" button that returns to selection message
		//and after users sends new data - bot have to delete only one message

		/// <summary> </summary>
		/// <param name="accountToUpdateId"></param>
		/// <param name="messageToDeleteId1">Message with selection what you want to update</param>
		/// <param name="messageToDeleteId2">Message with invitation to send new data like send new password</param>
		/// <param name="accountDataType"></param>
		public AccountUpdate(string accountToUpdateId, int messageToDeleteId1, int messageToDeleteId2, AccountDataType accountDataType) {
			AccountToUpdateId = accountToUpdateId;
			MessagetoDeleteId = new int[2];
			MessagetoDeleteId[0] = messageToDeleteId1;
			MessagetoDeleteId[1] = messageToDeleteId2;
			AccountDataType = accountDataType;
		}
		public string AccountToUpdateId;
		public int[] MessagetoDeleteId;
		public AccountDataType AccountDataType;
	}
}