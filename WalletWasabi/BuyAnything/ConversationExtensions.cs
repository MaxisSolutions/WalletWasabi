using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WalletWasabi.BuyAnything;

public static class ConversationExtensions
{
	public static bool IsCompleted(this Conversation conversation)
	{
		return conversation.OrderStatus == OrderStatus.Done;
	}

	public static bool IsUpdatable(this Conversation conversation) =>
		true;

	public static Conversation AddSystemChatLine(this Conversation conversation, string message, DataCarrier data,
		ConversationStatus newStatus) =>
		conversation with
		{
			ChatMessages = conversation.ChatMessages.AddReceivedMessage(message, data),
			ConversationStatus = newStatus
		};

	public static Conversation2 UpdateMetadata(this Conversation2 conversation, Func<ConversationMetaData2, ConversationMetaData2> updateMetadata)
	{
		return conversation with { MetaData = updateMetadata(conversation.MetaData) };
	}
}
