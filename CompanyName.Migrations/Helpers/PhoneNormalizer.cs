using System.Text;
using System.Text.RegularExpressions;

namespace CompanyName.Migrations.Helpers {
	public static class PhoneNormalizer {
		public static string Normalize(string phone) {
			// Phone conversion works idempotent. It means that conversion of already previously converted phone number produce identical result.
			Match match = Regex.Match(phone, @"([\(,\+,1,\s,-]*)(\d{3})([\),\s,-]*)(\d{3})(-*)(\d{4})(?!\d)([E,e,x,t,\.,\s]*)(\d*)");
			if (match.Success && string.Equals(match.Value, phone)) {
				var newPhone = new StringBuilder($"{match.Groups[2].Value}-{match.Groups[4].Value}-{match.Groups[6].Value}");
				string ext = match.Groups[8].Value;
				if (!string.IsNullOrEmpty(ext)) {
					newPhone.Append($" Ext. {ext}");
				}
				return newPhone.ToString();
			}
			return phone;
		}
	}
}