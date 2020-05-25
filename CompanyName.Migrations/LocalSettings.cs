using System;
using CompanyName.Configuration;

namespace CompanyName.Migrations {
	public class LocalSettings : CommonSettings {
		public override TimeSpan MarketPortalSlidingExpiration => throw new NotImplementedException();
	}
}