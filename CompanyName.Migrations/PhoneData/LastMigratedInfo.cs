using Newtonsoft.Json;

namespace CompanyName.Migrations.PhoneData {
	public class LastMigratedInfo {
		public int AnswerPoolId { get; set; }
		public string ToJson() => JsonConvert.SerializeObject(this);
		public static LastMigratedInfo FromJson(string data) => JsonConvert.DeserializeObject<LastMigratedInfo>(data);
	}
}