﻿using WalletWasabi.Gui;
using WalletWasabi.Wallets;

namespace WalletWasabi.Fluent.ViewModels
{
	public class HardwareWalletViewModel : WalletViewModel
	{
		internal HardwareWalletViewModel(UiConfig uiConfig, Wallet wallet) : base(uiConfig, wallet)
		{
		}
	}
}
