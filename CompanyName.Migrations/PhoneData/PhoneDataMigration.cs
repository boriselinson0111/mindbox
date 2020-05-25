using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using CompanyName.Domain.Entities;
using CompanyName.Domain.Entities.Supplementary;
using CompanyName.Domain.Repository;
using CompanyName.Infrastructure.Persistence.EF.Repository;
using CompanyName.Infrastructure.Utils;
using CompanyName.Migrations.Helpers;
using CompanyName.Utilities;
using CompanyName.Utilities.Collections;
using Autofac;
using AnswerPool = CompanyName.Domain.Entities.Forms.Answers.AnswerPool;

namespace CompanyName.Migrations.PhoneData {
	[Description("2019-09-11 Move Phones (Boris Elinson) 372303955.mig")]
	public class PhoneDataMigration : Migration {
		private const int LoggingInterval = 10;
		private const int GroupSize = 384;

		private readonly SoftwareMigrationRepository _softwareMigrationRepository;
		private readonly IUnitOfWorkFactory _unitOfWorkFactory;
		private readonly AnswerRepository _answerRepository;
		private readonly ITemplateControlRepository _templateControlRepository;

		private readonly string _migrationName;

		public PhoneDataMigration() { }

		private PhoneDataMigration(IComponentContext context) {
			EnsureThat.IsNotNull(context, nameof(context));
			_softwareMigrationRepository = (SoftwareMigrationRepository)context.Resolve<ISoftwareMigrationRepository>();
			_unitOfWorkFactory = context.Resolve<IUnitOfWorkFactory>();
			_answerRepository = (AnswerRepository)context.Resolve<IAnswerRepository>();
			_templateControlRepository = context.Resolve<ITemplateControlRepository>();
			_migrationName = typeof(PhoneDataMigration).GetCustomAttribute<DescriptionAttribute>()
			                                           .Description;
		}

		public override void Up() {
			Run(container => {
				var mainPhoneDataMigration = new PhoneDataMigration(container);
				SoftwareMigration softwareMigration = mainPhoneDataMigration.GetMigration() ?? mainPhoneDataMigration.CreateEmptyMigration();
				LastMigratedInfo lastMigrated = LastMigratedInfo.FromJson(softwareMigration.LastMigrated);
				if (softwareMigration.EndDateUtc != null) {
					Console.WriteLine($"Phones have been converted {softwareMigration.EndDateUtc.Value.ToLocalTime()} ");
					return;
				}
				Guid[] phoneFaxControls = mainPhoneDataMigration.GetPhoneFaxControls();
				int[] answerPoolIds = mainPhoneDataMigration._answerRepository.Get(x => x.Id > lastMigrated.AnswerPoolId,
				                                                                   x => x.Id,
				                                                                   x => x.Id,
				                                                                   SortOrder.Ascending);

				IReadOnlyCollection<int>[] groups = answerPoolIds.Split(GroupSize).ToArray();
				int lastMigratedAnswerPoolId = 0;
				for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++) {
					var stopWatch = new Stopwatch();
					stopWatch.Start();
					int[] parallelAnswerPoolIds = groups[groupIndex].ToArray();
					Parallel.ForEach(Partitioner.Create(0, parallelAnswerPoolIds.Length),
					                 range => {
						                 using (ILifetimeScope innerScope = container.BeginLifetimeScope()) {
							                 for (int index = range.Item1; index < range.Item2; index++) {
								                 int answerPoolId = parallelAnswerPoolIds[index];
								                 new PhoneDataMigration(innerScope).MovePhones(softwareMigration.Id, answerPoolId, phoneFaxControls);
							                 }
						                 }
					                 });
					lastMigratedAnswerPoolId = parallelAnswerPoolIds[parallelAnswerPoolIds.Length - 1];
					if (groupIndex % LoggingInterval == 0) {
						mainPhoneDataMigration.UpdateMigration(softwareMigration, lastMigratedAnswerPoolId);
					}
					stopWatch.Stop();
					Console.WriteLine($"groupIndex={groupIndex}, groups.Length={groups.Length} RunTime {stopWatch.ElapsedMilliseconds}");
				}
				mainPhoneDataMigration.UpdateMigration(softwareMigration, lastMigratedAnswerPoolId);
				mainPhoneDataMigration.CompleteMigration(softwareMigration);
			});
		}

		public void MovePhones(int softwareMigrationId, int answerPoolId, Guid[] phoneFaxControls) {
			try {
				AnswerPool answerPool = _answerRepository.FindByAnswerPoolId(answerPoolId);
				foreach (Guid controlId in phoneFaxControls) {
					string phone = answerPool[controlId]?.ToString();
					if (phone != null) {
						string newPhone = PhoneNormalizer.Normalize(phone);
						answerPool = answerPool.UpdateQuestionnaire(controlId, 0, newPhone, lineId: null);
					}
				}
				_unitOfWorkFactory.Execute(() => _answerRepository.ForceUpdateSnapshot(answerPool));
			} catch (Exception ex) {
				Console.WriteLine($"failed to process answerPoolId = {answerPoolId}, ex = {ex}");
			}
		}

		private Guid[] GetPhoneFaxControls() {
			Expression<Func<TemplateControl, bool>> condition =
				x => (x.Name.Contains("Phone") ||
				      x.Name.Contains("Fax") ||
				      x.Name == "CompaniesRefusedApplicantCoverageTel" ||
				      x.Name == "ReferencesCompletedWorkTelNumber") &&
				     !x.Name.EndsWith("Descr") &&
				     x.Name != "Healthcare.CrisisInterventionTelephoneReferralServicesScopeOfOperation" &&
				     x.Name != "Operations.DoYouLeaseAnyCranesWithoutOperatorNamePhoneCompetentPerson" &&
				     x.Name != "ProjectDetails.ProjectGeneralContractorLicenseNumPhoneSiteVerif" &&
				     x.Name != "Safety.ApplicantParticipateInFormalSafetyInspectionProvideNamePhonePersonPerformsInspection" &&
				     x.Name != "Safety.CellPhonePolicy" &&
				     x.Name != "Agency.ProducerPhone" &&
				     x.Name != "Agency.ProducerFax" &&
				     x.Type == ControlType.TextBox &&
				     x.Attributes < 3;

			return _templateControlRepository.Get(condition,
			                                      x => x.Id,
			                                      x => x.Id);
		}

		private void UpdateMigration(SoftwareMigration softwareMigration, int answerPoolId) {
			softwareMigration.LastMigrated = new LastMigratedInfo {
				AnswerPoolId = answerPoolId
			}.ToJson();
			_unitOfWorkFactory.Execute(() => _softwareMigrationRepository.Update(softwareMigration));
		}

		private SoftwareMigration GetMigration() {
			return _softwareMigrationRepository.FirstOrDefault(x => x.MigrationName == _migrationName,
			                                                   x => x);
		}

		private void CompleteMigration(SoftwareMigration softwareMigration) {
			softwareMigration.EndDateUtc = DateTime.UtcNow;
			_unitOfWorkFactory.Execute(() => _softwareMigrationRepository.Update(softwareMigration));
		}

		private SoftwareMigration CreateEmptyMigration() {
			var softwareMigration = new SoftwareMigration {
				MigrationName = _migrationName,
				StartDateUtc = DateTime.UtcNow,
				MigrationObjects = "PhoneFaxControls",
				LastMigrated = new LastMigratedInfo().ToJson()
			};
			_unitOfWorkFactory.Execute(() => _softwareMigrationRepository.Insert(softwareMigration));
			return GetMigration();
		}
	}
}