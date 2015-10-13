﻿namespace PhotoContest.Models
{
    using System;
    using System.Collections.Generic;

    public class Contest
    {
        private const int DefaultParticipantsLimit = -1;
        private const bool DefaultContestState = true;

        private ICollection<User> participants;

        private ICollection<Picture> pictures;

        private ICollection<User> committee; 

        public Contest()
        {
            this.participants = new HashSet<User>();
            this.committee = new HashSet<User>();
            this.pictures = new HashSet<Picture>();
            this.IsActive = DefaultContestState;
            this.ParticipantsLimit = DefaultParticipantsLimit;
        }

        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int ParticipantsLimit { get; set; }

        public bool IsActive { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int RewardStrategyId { get; set; }

        public virtual RewardStrategy RewardStrategy { get; set; }

        public int VotingStrategyId { get; set; }

        public virtual VotingStrategy VotingStrategy { get; set; }

        public int ParticipationStrategyId { get; set; }

        public virtual ParticipationStrategy ParticipationStrategy { get; set; }

        public int DeadlineStrategyId { get; set; }

        public virtual DeadlineStrategy DeadlineStrategy { get; set; }
        
        public string OrganizatorId { get; set; }

        public virtual User Organizator { get; set; }

        public virtual ICollection<User> Participants
        {
            get
            {
                return this.participants;
            }
            set
            {
                this.participants = value;
            }
        }

        public virtual ICollection<User> Committee
        {
            get
            {
                return this.committee;
            }
            set
            {
                this.committee = value;
            }
        }

        public virtual ICollection<Picture> Pictures
        {
            get
            {
                return this.pictures;
            }
            set
            {
                this.pictures = value;
            }
        }
    }
}