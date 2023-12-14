using System.Collections.Generic;
using System.Collections.ObjectModel;
using WalletWasabi.BuyAnything;
using WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows.ShopinBit;

namespace WalletWasabi.Fluent.ViewModels.Wallets.Buy.Workflows;

/// <summary>
/// ShopinBit Step #2: Select Country
/// </summary>
public class CountryStep : WorkflowStep2<Country>
{
	public CountryStep(Conversation2 conversation) : base(conversation)
	{
		// TODO
		Countries = new();
	}

	public ObservableCollection<Country> Countries { get; }

	protected override IEnumerable<string> BotMessages(Conversation2 conversation)
	{
		// Assistant greeting, min order limit
		yield return $"Hello, I am your chosen {GetAssistantName(conversation)}. At present, we focus on requests where the value of the goods or services is at least $1,000 USD";

		// Ask for Location
		yield return "To start, please indicate your country. If your order involves shipping, provide the destination country. For non-shipping orders, please specify your nationality.";
	}

	protected override Conversation2 PutValue(Conversation2 conversation, Country value) =>
		conversation.UpdateMetadata(x => x with { Country = value });

	protected override Country? RetrieveValue(Conversation2 conversation) =>
		conversation.MetaData.Country;

	protected override string StringValue(Country value) =>
		value.Name;

	private string GetAssistantName(Conversation2 conversation) =>
		conversation.MetaData.Product?.GetDescription() ?? "Assistant";
}
