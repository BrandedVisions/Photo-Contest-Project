﻿namespace PhotoContest.App.Controllers
{
    using System;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;

    using Microsoft.AspNet.Identity;

    using PhotoContest.App.Models.ViewModels;
    using PhotoContest.Data.Interfaces;
    using PhotoContest.Data.Strategies;
    using PhotoContest.Models;

    public class PicturesController : BaseController
    {
        public PicturesController(IPhotoContestData data)
            : base(data)
        {
        }

        public ActionResult Index()
        {
            var pictures =
                this.Data.Pictures.All()
                .Select(p => new PictureViewModel
                                {
                                    Url = p.Url,
                                    User = p.User.UserName
                                });

            return this.View(pictures);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload()
        {
            
            if (this.Request.Files.Count < 1)
            {
                this.Response.StatusCode = 400;
                return this.Json(new { ErrorMessage = "No file data" });
            }
            
            HttpPostedFileBase file = this.Request.Files[0];

            if (!file.ContentType.Contains("image"))
            {
                this.Response.StatusCode = 400;
                return this.Json(new { ErrorMessage = "The file is not a picture" });
            }
            
            if (file.ContentLength > 1000000)
            {
                this.Response.StatusCode = 400;
                return this.Json(new { ErrorMessage = "Picture size must be in range [1 - 1024 kb]" });
            }

            var userId = this.User.Identity.GetUserId();
            var userName = this.User.Identity.GetUserName();

            byte[] fileBuffer = new byte[file.ContentLength];
            file.InputStream.Read(fileBuffer, 0, (int)file.ContentLength);
            var base64 = Convert.ToBase64String(fileBuffer);

            Picture picture = new Picture
                                        {
                                            UserId = userId,
                                            Url = base64,
                                            ContestId = this.Data.Contests.All().FirstOrDefault().Id
                                        };
            this.Data.Pictures.Add(picture);
            this.Data.SaveChanges();

            var pictureView = new PictureViewModel { Url = picture.Url, User = userName };
            this.Response.StatusCode = 200;

            return PartialView("_PicturePartial", pictureView);
        }

        [HttpGet]
        [Authorize]
        public ActionResult Vote(int id)
        {
            // TODO change to HTTPPOST
            var user = this.Data.Users.Find(this.User.Identity.GetUserId());
            var picture = this.Data.Pictures.Find(id);
            if (!picture.Contest.IsActive)
            {
                throw new InvalidOperationException("The contest is closed.");
            }
            this.VotingStrategy = 
                StrategyFactory.GetVotingStrategy(picture.Contest.VotingStrategy.VotingStrategyType);

            this.VotingStrategy.Vote(this.Data, user, picture.Contest);

            if (picture.Votes.Any(v => v.UserId == user.Id))
            {
                throw new InvalidOperationException("You have already voted for this picture.");
            }
            var vote = new Vote { PictureId = picture.Id, UserId = user.Id };
            this.Data.Votes.Add(vote);
            this.Data.SaveChanges();
            
            return null;
        }
    }
}