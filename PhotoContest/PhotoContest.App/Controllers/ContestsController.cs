﻿namespace PhotoContest.App.Controllers
{
    #region
    using System;
    using System.Net;
    using System.Web;
    using PhotoContest.Models;

    using System.Web.Mvc;

    using Microsoft.AspNet.Identity;

    using PhotoContest.Data.Interfaces;
    using PhotoContest.Data.Strategies;

    using System.Collections.Generic;
    using System.Linq;

    using AutoMapper;
    using AutoMapper.QueryableExtensions;

    using PhotoContest.Models.Enums;

    using PhotoContest.Infrastructure.Interfaces;

    using PhotoContest.Infrastructure.Models.BindingModels.Contest;
    using PhotoContest.Infrastructure.Models.BindingModels.Reward;
    using PhotoContest.Infrastructure.Models.ViewModels.Contest;
    using PhotoContest.Infrastructure.Models.ViewModels.Reward;
    using PhotoContest.Infrastructure.Models.ViewModels.Strategy;

    using PhotoContest.App.Services;

    #endregion

    public class ContestsController : BaseController
    {
        private const string GoogleDrivePicturesBaseLink = "http://docs.google.com/uc?export=open&id=";

        private PicturesService _picturesService;

        private IContestsService _service;

        public ContestsController(IPhotoContestData data, IContestsService service)
            : base(data)
        {
            this._picturesService = new PicturesService();
            this._service = service;
        }

        [HttpGet]
        public ActionResult Contests()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult AllContests()
        {
            var viewModel = _service.GetAllContests(this.User.Identity.GetUserId());

            return this.PartialView("_AllContestsPartial", viewModel);
        }

        [HttpGet]
        public ActionResult ActiveContests()
        {
            var viewModel = this._service.GetActiveContests(this.User.Identity.GetUserId());

            return this.PartialView("_ActiveContestsPartial", viewModel);
        }

        [HttpGet]
        public ActionResult InactiveContests()
        {
            var viewModel = this._service.GetInactiveContests();

            return this.PartialView("_InactiveContestsPartial", viewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult MyContests()
        {
            var viewModel = this._service.GetMyContests(this.User.Identity.GetUserId());

            return this.PartialView("_MyContestsPartial", viewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult GetRewardPartial()
        {
            return PartialView("~/Views/Rewards/_AddPartial.cshtml", new AddRewardViewModel());
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddRewards(int id, CreateRewardsBindingModel model)
        {
            if (model == null)
            {
                this.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                return this.Json(new {ErrorMessage = "Missing data"});
            }

            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                return this.HttpNotFound("Contest not found");
            }

            if (contest.Status != ContestStatus.Active)
            {
                this.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return this.Json(new {ErrorMessage = "Cannot add reward to inactive contest."});
            }

            for (int i = 0; i < model.Name.Length; i++)
            {
                if (model.Place[i] < 1 || (contest.TopNPlaces != null && model.Place[i] > contest.TopNPlaces))
                {
                    this.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return this.Json(new { ErrorMessage = "Reward for unknown place" });
                }

                contest.Rewards.Add(new Reward()
                {
                    ContestId = contest.Id,
                    Name = model.Name[i],
                    Description = model.Description[i],
                    Place = model.Place[i],
                    ImageUrl = model.ImageUrl[i]
                });
            }

            this.Data.SaveChanges();

            return this.RedirectToAction("PreviewContest", new {id = id});
        }

        [Authorize]
        [HttpGet]
        public ActionResult NewContest()
        {
            var viewModel = new CreateContestViewModel();

            viewModel.RewardStrategies = this.Data.RewardStrategies.All()
                .Select(r => new StrategyViewModel()
                {
                    Id = r.Id, Description = r.Description, Name = r.Name,
                })
                .ToList();

            viewModel.ParticipationStrategies = this.Data.ParticipationStrategies.All()
                .Select(p => new StrategyViewModel()
                {
                    Id = p.Id, Description = p.Description, Name = p.Name,
                })
                .ToList();

            viewModel.VotingStrategies = this.Data.VotingStrategies.All()
                .Select(v => new StrategyViewModel()
                {
                    Id = v.Id, Description = v.Description, Name = v.Name,
                })
                .ToList();

            viewModel.DeadlineStrategies = this.Data.DeadlineStrategies.All()
                .Select(dl => new StrategyViewModel()
                {
                    Id = dl.Id, Description = dl.Description, Name = dl.Name,
                })
                .ToList();

            return this.View("NewContestForm", viewModel);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateContest(CreateContestBindingModel model)
        {
            if (model == null)
            {
                this.Response.StatusCode = 400;
                return this.Content("Missing Data");
            }

            if (!this.ModelState.IsValid)
            {
                this.Response.StatusCode = 400;
                return this.Json(this.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            }

            if (this.Data.RewardStrategies.Find(model.RewardStrategyId) == null)
            {
                throw new ArgumentException("Not existing reward strategy");
            }

            if (this.Data.ParticipationStrategies.Find(model.ParticipationStrategyId) == null)
            {
                throw new ArgumentException("Not existing participation strategy");
            }

            if (this.Data.VotingStrategies.Find(model.VotingStrategyId) == null)
            {
                throw new ArgumentException("Not existing voting strategy");
            }

            if (this.Data.DeadlineStrategies.Find(model.DeadlineStrategyId) == null)
            {
                throw new ArgumentException("Not existing deadline strategy");
            }

            var loggedUserId = this.User.Identity.GetUserId();

            var contest = new Contest
            {
                Title = model.Title,
                Description = model.Description,
                Status = ContestStatus.Active,
                RewardStrategyId = model.RewardStrategyId,
                VotingStrategyId = model.VotingStrategyId,
                ParticipationStrategyId = model.ParticipationStrategyId,
                DeadlineStrategyId = model.DeadlineStrategyId,
                ParticipantsLimit = model.ParticipantsLimit,
                TopNPlaces = model.TopNPlaces,
                SubmissionDeadline = model.SubmissionDeadline,
                IsOpenForSubmissions = true,
                StartDate = DateTime.Now,
                OrganizatorId = loggedUserId,
            };

            this.Data.Contests.Add(contest);
            this.Data.SaveChanges();

            return this.RedirectToAction("PreviewContest", new { id = contest.Id });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Join(int id)
        {
            var user = this.Data.Users.Find(this.User.Identity.GetUserId());
            var contest = this.Data.Contests.Find(id);

            var messages = new List<string>();

            try
            {
                if (contest.OrganizatorId == user.Id)
                {
                    throw new InvalidOperationException("You cannot join contest created by you");
                }

                var deadlineStrategy =
                    StrategyFactory.GetDeadlineStrategy(contest.DeadlineStrategy.DeadlineStrategyType);

                deadlineStrategy.CheckDeadline(this.Data, contest, user);

                var participationStrategy =
                    StrategyFactory.GetParticipationStrategy(contest.ParticipationStrategy.ParticipationStrategyType);

                participationStrategy.CheckPermission(this.Data, user, contest);

                if (contest.Participants.Contains(user))
                {
                    throw new ArgumentException("You already participate in this contest");
                }

                if (contest.Committee.Contains(user))
                {
                    throw new ArgumentException("You cannot participate in this contest, you are in the committee");
                }

                contest.Participants.Add(user);
                this.Data.Contests.Update(contest);
                this.Data.SaveChanges();
            }
            catch (ArgumentException e)
            {
                messages.Add(e.Message);
            }
            catch (InvalidOperationException e)
            {
                messages.Add(e.Message);
            }

            this.TempData["Messages"] = messages;

            if (messages.Count > 0)
            {
                this.Response.StatusCode = 400;
                return this.Content(string.Join("\n", messages));
            }

            return this.RedirectToAction("PreviewContest", new { id = contest.Id });
        }

        [Authorize]
        [HttpGet]
        public ActionResult JoinCommittee(int id)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest.Status != ContestStatus.Active)
            {
                this.Response.StatusCode = 400;
                return this.Content("The contest is not active");
            }

            var user = this.Data.Users.Find(this.User.Identity.GetUserId());

            var invitation = contest.Organizator.SendedInvitations.FirstOrDefault(i => i.ContestId == contest.Id && i.InvitedId == user.Id && i.Type == InvitationType.Committee);

            if (invitation == null)
            {
                this.Response.StatusCode = 400;
                return this.Content("You don't have an invitation");
            }

            if (invitation.Status != InvitationStatus.Neutral)
            {
                this.Response.StatusCode = 400;
                return this.Content("You already have responded to the invitation");
            }

            invitation.Status = InvitationStatus.Accepted;
            contest.Committee.Add(user);

            this.Data.SaveChanges();

            return new HttpStatusCodeResult(200);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(int id)
        {
            if (this.Request.Files.Count < 1)
            {
                this.Response.StatusCode = 400;
                return this.Json(new { ErrorMessage = "No file data" });
            }

            var files = new List<HttpPostedFileBase>();

            for (int i = 0; i < this.Request.Files.Count; i++)
            {
                var result = this._picturesService.ValidateImageData(this.Request.Files[i]);

                if (result != null)
                {
                    this.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return this.Json(new { ErrorMessage = result });
                }

                files.Add(this.Request.Files[i]);
            }

            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                throw new HttpException(404, "Contest with such id does not exists");
            }

            var user = this.Data.Users.Find(this.User.Identity.GetUserId());

            try
            {
                if (contest.Committee.Contains(user) || !contest.Participants.Contains(user)
                    || contest.OrganizatorId == user.Id)
                {
                    throw new InvalidOperationException("You are either organizator of this contest or in the committee or you don't not participate in it");
                }

                var deadlineStrategy = StrategyFactory.GetDeadlineStrategy(contest.DeadlineStrategy.DeadlineStrategyType);

                deadlineStrategy.CheckDeadline(this.Data, contest, user);

                foreach (var file in files)
                {
                    var result = this._picturesService.UploadImageToGoogleDrive(file, file.FileName, file.ContentType);

                    if (result[0] != "success")
                    {
                        this.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                        return this.Content(result[1]);
                    }

                    Picture picture = new Picture
                    {
                        UserId = user.Id,
                        Url = GoogleDrivePicturesBaseLink + result[1],
                        GoogleFileId = result[1],
                        ContestId = contest.Id
                    };

                    this.Data.Pictures.Add(picture);
                }

                this.Data.SaveChanges();

                this.Response.StatusCode = 200;

                return RedirectToAction("PreviewContest", new { id = contest.Id });
            }
            catch (InvalidOperationException e)
            {
                this.TempData["message"] = e.Message;
            }

            this.Response.StatusCode = 400;
            return this.Json(new { ErrorMessage = this.TempData["message"] });
        }

        [HttpGet]
        public ActionResult PreviewContest(int id)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                return this.HttpNotFound("The selected contest no longer exists");
            }

            if (contest.Status != ContestStatus.Active)
            {
                var contestWinners =
                    this.Data.ContestWinners.All()
                    .Where(c => c.ContestId == contest.Id)
                    .ProjectTo<ContestWinnerViewModel>()
                    .ToList();

                return this.View("PreviewInactiveContest", contestWinners);
            }

            var contestViewModel = Mapper.Map<PreviewContestViewModel>(contest);

            if (this.User.Identity.GetUserId() != null)
            {
                var user = this.Data.Users.Find(this.User.Identity.GetUserId());

                if (contest.OrganizatorId == user.Id)
                {
                    contestViewModel.CanManage = true;
                }
                else
                {
                    if (!contest.Committee.Contains(user) && contest.Participants.Contains(user))
                    {
                        contestViewModel.CanUpload = true;
                    }

                    if (!contest.Committee.Contains(user) && !contest.Participants.Contains(user))
                    {
                        contestViewModel.CanParticipate = true;
                    }
                }

            }

            return this.View(contestViewModel);
        }

        [Authorize]
        [HttpGet]
        public ActionResult ManageContest(int id)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                return this.HttpNotFound("The selected contest does not exist");
            }

            var loggedUserId = this.User.Identity.GetUserId();

            if (contest.OrganizatorId != loggedUserId)
            {
                this.Response.StatusCode = 401;
                return this.Content("Logged user is not the contest organizator");
            }

            var contestBindingModel = Mapper.Map<UpdateContestBindingModel>(contest);

            return this.View("ManageContestForm", contestBindingModel);
        }

        [Authorize]
        [HttpPatch]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateContest(UpdateContestBindingModel model)
        {
            if (model == null)
            {
                this.Response.StatusCode = 400;
                return this.Json(new { ErrorMessage = "Missing Data" } );
            }

            if (!this.ModelState.IsValid)
            {
                this.Response.StatusCode = 400;
                return this.Json(this.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            }

            var contest = this.Data.Contests.Find(model.Id);

            if (contest == null)
            {
                return this.HttpNotFound("The selected contest does not exist");
            }

            var loggedUserId = this.User.Identity.GetUserId();

            if (contest.OrganizatorId != loggedUserId)
            {
                this.Response.StatusCode = 401;
                return this.Json(new { ErrorMessage = "Logged user is not the contest organizator" });
            }

            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                contest.Title = model.Title;
            }

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                contest.Description = model.Description;
            }

            if (model.EndDate != default(DateTime))
            {
                contest.EndDate = model.EndDate;
            }

            this.Data.Contests.Update(contest);
            this.Data.SaveChanges();

            return this.RedirectToAction("PreviewContest", new { id = contest.Id });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult InviteUser(string username, int contestId, InvitationType type)
        {
            var contest = this.Data.Contests.Find(contestId);

            if (contest == null)
            {
                this.Response.StatusCode = 404;
                return this.Content(string.Format("Contest with id {0} not found", contestId));
            }

            var loggedUser = this.Data.Users.Find(this.User.Identity.GetUserId());

            if (contest.OrganizatorId != loggedUser.Id)
            {
                this.Response.StatusCode = 400;
                return this.Content("Only the contest organizator can invite users.");
            }

            if (type == InvitationType.Committee
                && contest.VotingStrategy.VotingStrategyType != VotingStrategyType.Closed)
            {
                this.Response.StatusCode = 400;
                return this.Content("The contest voting strategy type is not 'CLOSED'.");
            }

            if (type == InvitationType.ClosedContest
                && contest.ParticipationStrategy.ParticipationStrategyType != ParticipationStrategyType.Closed)
            {
                this.Response.StatusCode = 400;
                return this.Content("The contest participation strategy type is not 'CLOSED'.");
            }

            if (!contest.IsOpenForSubmissions)
            {
                this.Response.StatusCode = 400;
                return this.Content("The contest is closed for submissions/registrations.");
            }

            var userToInvite = this.Data.Users.All().FirstOrDefault(u => u.UserName == username);

            if (userToInvite == null)
            {
                this.Response.StatusCode = 404;
                return this.Content(string.Format("User with username {0} not found", username));
            }

            if (userToInvite.UserName == loggedUser.UserName)
            {
                this.Response.StatusCode = 400;
                return this.Content("Users cannot invite themselves.");
            }

            if (userToInvite.PendingInvitations.Any(i => i.ContestId == contestId && i.Type == type))
            {
                this.Response.StatusCode = 400;
                return this.Content(string.Format("User is already invited to contest with id {0}", contest.Id));
            }

            var invitation = new Invitation
            {
                ContestId = contestId,
                InviterId = loggedUser.Id,
                InvitedId = userToInvite.Id,
                DateOfInvitation = DateTime.Now,
                Type = type,
                Status = InvitationStatus.Neutral
            };

            if (type == InvitationType.ClosedContest)
            {
                contest.InvitedUsers.Add(userToInvite);
            }

            userToInvite.PendingInvitations.Add(invitation);
            loggedUser.SendedInvitations.Add(invitation);

            this.Data.SaveChanges();

            this.HubContext.Clients.User(username).notificationReceived(invitation.Id);

            this.Response.StatusCode = 200;

            return this.Content(string.Format("User with username {0} successfully invited", username));
        }

        [Authorize]
        [HttpPost]
        public ActionResult FinalizeContest(int id)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                this.Response.StatusCode = 404;
                return this.Content(string.Format("Contest with id {0} not found", id));
            }

            if (contest.Status != ContestStatus.Active)
            {
                this.Response.StatusCode = 404;
                return this.Content(string.Format("Contest with id {0} is not active", id));
            }

            var loggedUser = this.Data.Users.Find(this.User.Identity.GetUserId());

            if (contest.OrganizatorId != loggedUser.Id)
            {
                this.Response.StatusCode = 400;
                return this.Content("Only the contest organizator can finalize it.");
            }

            contest.IsOpenForSubmissions = false;
            contest.Status = ContestStatus.Finalized;
            contest.EndDate = DateTime.Now;

            this.Data.SaveChanges();

            var rewardStrategy =
                    StrategyFactory.GetRewardStrategy(contest.RewardStrategy.RewardStrategyType);

            rewardStrategy.ApplyReward(this.Data, contest);

            return new HttpStatusCodeResult(200);
        }

        [Authorize]
        [HttpPost]
        public ActionResult DismissContest(int id)
        {
            var contest = this.Data.Contests.Find(id);

            if (contest == null)
            {
                this.Response.StatusCode = 404;
                return this.Content(string.Format("Contest with id {0} not found", id));
            }

            if (contest.Status != ContestStatus.Active)
            {
                this.Response.StatusCode = 404;
                return this.Content(string.Format("Contest with id {0} is not active", id));
            }

            var loggedUser = this.Data.Users.Find(this.User.Identity.GetUserId());

            if (contest.OrganizatorId != loggedUser.Id)
            {
                this.Response.StatusCode = 400;
                return this.Content("Only the contest organizator can dismiss it.");
            }

            contest.IsOpenForSubmissions = false;
            contest.Status = ContestStatus.Dismissed;
            contest.EndDate = DateTime.Now;

            this.Data.SaveChanges();

            return new HttpStatusCodeResult(200);
        }
    }
}