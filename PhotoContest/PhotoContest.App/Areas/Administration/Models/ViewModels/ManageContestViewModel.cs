﻿namespace PhotoContest.App.Areas.Administration.Models.ViewModels
{
    using AutoMapper;

    using PhotoContest.App.Infrastructure.Mapping;
    using PhotoContest.Models;
    using PhotoContest.Models.Enums;

    public class ManageContestViewModel : IMapFrom<Contest>, IHaveCustomMappings
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Organizator { get; set; }

        public bool IsActive { get; set; }

        public bool IsOpenForSubmissions { get; set; }

        public bool CanParticipate { get; set; }

        public bool CanManage { get; set; }

        public ParticipationStrategyType ParticipationStrategyType { get; set; }

        public VotingStrategyType VotingStrategyType { get; set; }

        public DeadlineStrategyType DeadlineStrategyType { get; set; }

        public void CreateMappings(IConfiguration configuration)
        {
            configuration.CreateMap<Contest, ManageContestViewModel>()
               .ForMember(c => c.Organizator, opt => opt.MapFrom(c => c.Organizator.UserName))
               .ForMember(c => c.ParticipationStrategyType, opt => opt.MapFrom(c => c.ParticipationStrategy.ParticipationStrategyType))
               .ForMember(c => c.VotingStrategyType, opt => opt.MapFrom(c => c.VotingStrategy.VotingStrategyType))
               .ForMember(c => c.DeadlineStrategyType, opt => opt.MapFrom(c => c.DeadlineStrategy.DeadlineStrategyType))
               .ReverseMap();
        }
    }
}