﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using TentsNTrails.Models;

namespace TentsNTrails.Controllers
{

    public class ReviewController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private UserManager<User> manager;
        
        public ReviewController()
        {
            db = new ApplicationDbContext();
            manager = new UserManager<User>(new UserStore<User>(db));
        }

        [Authorize]
        // GET: Review
        // Gets the reviews only for the current user
        public ActionResult Index()
        {
            //var currentUser = manager.FindById(User.Identity.GetUserId());
            String userID = User.Identity.GetUserId();
            //var reviews = db.Reviews.Include(r => r.Location);
            // Get only reviews that have comments
            var reviewList = db.Reviews.Include(r => r.Location).Where(r => r.User.Id == userID).ToList();
            List<ReviewIndexViewModel> viewModelList = new List<ReviewIndexViewModel>();

            // build ViewModel List
            foreach (Review r in reviewList)
            {
                ReviewIndexViewModel viewModel = new ReviewIndexViewModel();
                viewModelList.Add(viewModel);
                viewModel.Review = r;

                // randomly grab zero or one images
                var images = db.LocationImages.Where(l => l.LocationID == r.LocationID);
                var imageList = images.OrderBy(c => Guid.NewGuid()).Take(Math.Min(images.Count(), 1)).ToList();
                if (imageList.Count == 1) viewModel.PreviewImage = imageList.Single();
                else viewModel.PreviewImage = null;
            }

            if (reviewList.Count == 0)
            {
                ViewBag.HasReviews = false;
            }
            else
            {
                ViewBag.HasReviews = true;
            }

            return View(viewModelList);
        }

        /*
        [Authorize]
        // GET: Review
        // Gets the reviews only for the current user
        public ActionResult IndexOLD()
        {
            var currentUserName = manager.FindById(User.Identity.GetUserId()).UserName;
            var reviews = db.Reviews.Include(r => r.Location);
            // Get only reviews that have comments
            var reviewList = reviews.Where(r => r.User.UserName == currentUserName)
                                .Where(r => !r.Comment.Equals(""))
                                .Where(r => r.Comment != null).ToList();
            if (reviewList.Count == 0)
            {
                ViewBag.HasReviews = false;
            }
            else
            {
                ViewBag.HasReviews = true;
            }
            return View(reviewList);
        }
        */

        // GET: Review/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        [Authorize]
        // GET: Review/Create/5
        // Accessed from the location page for which the review is for.
        public ActionResult Create(int? LocationID)
        {
            if (LocationID != null)
            {
                if (getIdIfRated(LocationID) == -1)
                {
                    ViewBag.LocationID = LocationID;
                    Location loc = db.Locations.Find(LocationID);
                    if (loc == null)
                    {
                        return HttpNotFound();
                    }
                    ViewBag.Location = loc;
                }
                else
                {
                    var currentUserName = manager.FindById(User.Identity.GetUserId()).UserName;
                    var reviews = db.Reviews.Where(r => r.LocationID == LocationID);
                    var reviewList = reviews.Where(r => r.User.UserName == currentUserName).ToList();

                    return RedirectToAction("Edit/" + reviewList.ElementAt(0).ReviewID, "Review");
                }
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            
            return View();
        }

        // POST: Review/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "LocationID,Difficulty,Rating,Comment")] Review review)
        {
            if (ModelState.IsValid)
            {
                //review.LocationID = ViewBag.LocationID;
                review.ReviewDate = DateTime.Now;
                review.User = manager.FindById(User.Identity.GetUserId());
                db.Reviews.Add(review);
                db.SaveChanges();
                
                var LocationID = review.LocationID;
                return RedirectToAction("Details/" + LocationID, "Location");
            }

            //ViewBag.LocationID = new SelectList(db.Locations, "LocationID", "Label", review.LocationID);
            return View(review);
        }

        // POST: Review/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateShort([Bind(Include = "LocationID,Rating")] Review review, string redirectAction, string redirectController)
        {
            //System.Diagnostics.Debug.WriteLineIf(redirectAction != null, "/Review/CreateShort?redirectAction=" + redirectAction);
            //System.Diagnostics.Debug.WriteLineIf(redirectAction == null, "/Review/CreateShort?redirectAction=null");
            //System.Diagnostics.Debug.WriteLineIf(redirectController != null, "/Review/CreateShort?redirectController=" + redirectController);
            //System.Diagnostics.Debug.WriteLineIf(redirectController == null, "/Review/CreateShort?redirectController=null");

            // Get the reviewID if this user has already made a rating/review
            int reviewID = getIdIfRated(review.LocationID);

            if (reviewID == -1) // the user has not rated this location yet
            {
                // Save their rating for the first time.
                if (ModelState.IsValid)
                {
                    review.ReviewDate = DateTime.Now;
                    review.User = manager.FindById(User.Identity.GetUserId());
                    db.Reviews.Add(review);
                    db.SaveChanges();

                    var LocationID = review.LocationID;
                    return RedirectToAction(redirectAction ?? "Index", redirectController ?? "Location");
                }
            }
            else if (!hasReviewed(reviewID)) // the user has rated but not reviewed this location yet
            {
                // Update the rating for this user.
                if (ModelState.IsValid)
                {
                    Review thisReview = db.Reviews.Find(reviewID);
                    thisReview.Rating = review.Rating;
                    db.Entry(thisReview).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction(redirectAction ?? "Index", redirectController ?? "Location");
                }
            }
            else // the user has made a rating and has made a review too, so they should now edit their review.
            {
                //Take the user to the Edit Review page.
                return RedirectToAction("Edit/" + reviewID, "Review");
            }

            return View(review);
        }

        // POST: Review/CreateShortToDetails
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateShortToDetails([Bind(Include = "LocationID,Rating")] Review review)
        {
            // Get the reviewID if this user has already made a rating/review
            int reviewID = getIdIfRated(review.LocationID);

            if (reviewID == -1) // the user has not rated this location yet
            {
                // Save their rating for the first time.
                if (ModelState.IsValid)
                {
                    review.ReviewDate = DateTime.Now;
                    review.User = manager.FindById(User.Identity.GetUserId());
                    db.Reviews.Add(review);
                    db.SaveChanges();

                    var LocationID = review.LocationID;
                    return RedirectToAction("Details", "Location", new { id = review.LocationID });
                }
            }
            else if (!hasReviewed(reviewID)) // the user has rated but not reviewed this location yet
            {
                // Update the rating for this user.
                if (ModelState.IsValid)
                {
                    Review thisReview = db.Reviews.Find(reviewID);
                    thisReview.Rating = review.Rating;
                    db.Entry(thisReview).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Details", "Location", new { id = thisReview.LocationID });
                }
            }
            else // the user has made a rating and has made a review too, so they should now edit their review.
            {
                //Take the user to the Edit Review page.
                return RedirectToAction("Edit/" + reviewID, "Review");
            }

            return View(review);
        }

        // Helper method that returns the ReviewID if this user has made a rating for this location or -1 elsewise
        public int getIdIfRated(int? LocationID)
        {
            var currentUserName = manager.FindById(User.Identity.GetUserId()).UserName;
            var reviews = db.Reviews.Where(r => r.Location.LocationID == LocationID);
            var userReviews = reviews.Where(r => r.User.UserName == currentUserName).ToList();

            if (userReviews.Count > 0)
            {
                return userReviews.First().ReviewID;
            }
            else
            {
                return -1;
            }
        }

        // Helper method that returns true if this review has a comment (if it is a review rather than a rating) or false elsewise
        public bool hasReviewed(int ReviewID)
        {
            var review = db.Reviews.Where(r => r.ReviewID == ReviewID).ToList().First();
            return !(review.Comment == null || review.Comment.Equals(""));
        }

        // GET: Review/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            ViewBag.LocationID = new SelectList(db.Locations, "LocationID", "Label", review.LocationID);
            return View(review);
        }

        // POST: Review/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ReviewID,LocationID,ReviewDate,Difficulty,Rating,Comment")] Review review)
        {
            if (ModelState.IsValid)
            {
                db.Entry(review).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.LocationID = new SelectList(db.Locations, "LocationID", "Label", review.LocationID);
            return View(review);
        }

        // GET: Review/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Review review = db.Reviews.Find(id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // POST: Review/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Review review = db.Reviews.Find(id);
            db.Reviews.Remove(review);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
