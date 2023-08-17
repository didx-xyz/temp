﻿namespace Yoma.Core.Domain.Core.Models
{
    public class ScheduleJobOptions
    {
        public const string Section = "ScheduleJob";

        public string SeedSkillsSchedule { get; set; }

        public int SeedSkillsBatchSize { get; set; }

        public string SeedJobTitlesSchedule { get; set; }

        public int SeedJobTitlesBatchSize { get; set; }

        public string OpportunityExpirationSchedule { get; set; }

        public string OpportunityExpirationNotificationSchedule { get; set; }

        public int OpportunityExpirationNotificationIntervalInDays { get; set; }

        public int OpportunityExpirationBatchSize { get; set; }
    }
}
