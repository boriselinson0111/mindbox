using System;
using CompanyName.BuildingBlocks.MassTransitBusIntegration;

namespace CompanyName.Migrations.Configuration {
	// todo: move to config
	internal class RabbitMqOptions : IRabbitMqOptions {
		public bool InMemory => true;
		public string Host => "";
		public string Login => "";
		public string Password => "";

		public int ConnectionAttemptCount => 0;

		public TimeSpan Timeout => TimeSpan.FromSeconds(1);
	}
}