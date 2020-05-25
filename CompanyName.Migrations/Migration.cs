using System;
using CompanyName.Business;
using CompanyName.Infrastructure;
using CompanyName.Infrastructure.MessageQueue;
using CompanyName.Infrastructure.Modules;
using CompanyName.Infrastructure.Persistence.EF.Modules;
using CompanyName.Infrastructure.Settings;
using CompanyName.Migrations.Configuration;
using CompanyName.Program.Settings;
using CompanyName.QuoteParameters;
using CompanyName.Security;
using CompanyName.Web.Configuration;
using Autofac;
using AutoMapper;

namespace CompanyName.Migrations {
	public abstract class Migration : IMigration {
		public abstract void Up();
		public virtual void Down() { }

		protected void Run(Action<IContainer> action) {
			var builder = new ContainerBuilder();
			builder.RegisterAppulateModules();
			builder.RegisterModule<PersistenceRegistrationModule>();
			builder.RegisterModule<MessageQueueRegistrationModule>();
			builder.RegisterModule<ProgramSubsystemModule>();

			builder.Register(ctx => AutoMapperConfiguration.Create());
			builder.Register(ctx => ctx.Resolve<MapperConfiguration>().CreateMapper()).As<IMapper>().InstancePerLifetimeScope();

			builder.Register(c => {
				var sharedConfig = new SharedConfig(
					connectionString: c.Resolve<IDatabaseSettings>().GetDatabaseConnectionString(),
					storageFolder: c.Resolve<IStorageSettings>().StorageFolder,
					rabbitMqOptions: new RabbitMqOptions());
				return new Startup(sharedConfig);
			}).AsSelf().SingleInstance();

			builder.RegisterType<EmptySecurityContext>().As<ISecurityContext>();
			using (IContainer container = builder.Build()) {
				Factory.InitComponentsRegistry(new ComponentsRegistry(container));
				Settings.Initialize(new LocalSettings());
				container.Resolve<Startup>().Init();
				action(container);
			}
		}
	}
}