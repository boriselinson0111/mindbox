using CompanyName.Domain.Account;
using CompanyName.Domain.Migration;
using CompanyName.Security;

namespace CompanyName.Migrations.Configuration {
	public class EmptySecurityContext : ISecurityContext {
		public ICurrentUser User => null;
		public int? UserId => null;
		public IMarketWithSettings CurrentMarket => null;
		public int? CurrentMarketOrUserCompanyId => null;
		public int? CurrentMarketOrUserMarketId => null;
		public bool IsRequestFormsFiller => false;
		public bool IsAgencyUnderMarket => false;
	}
}