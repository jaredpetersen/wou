﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity.Spatial;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using System.Xml.Linq;

namespace TentsNTrails.Models
{
    /**
     * Location stores all Location-related data in a model.  For now I decided to try out
     * using the DbGeography datatype as it is designed to handle geo-coordinates.
     * 
     * See the following link for helpful information on how and why I chose this datatype:
     * http://weblog.west-wind.com/posts/2012/Jun/21/Basic-Spatial-Data-with-SQL-Server-and-Entity-Framework-50
     */
    public class Location
    {
        // for difficulty rating below
        public enum DifficultyRatings
        {
            [Display(Name = "Easy")]
            Easy,
            [Display(Name = "Medium")]
            Medium,
            [Display(Name = "Hard")]
            Hard,
            [Display(Name = "Varies")]
            Varies,
            [Display(Name = "NA")]
            NA
        }

        [Key]
        public int LocationID { get; set; }

        [Display(Name = "Name")]
        [Required]
        public String Label { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180,180)]
        public double Longitude { get; set; }

        // date created
        [DataType(DataType.DateTime)]
        [Display(Name = "Created")]
        [Editable(false)]
        public DateTime? DateCreated { get; set; }

        // date modified
        [DataType(DataType.DateTime)]
        [Display(Name = "Modified")]
        [Editable(false)]
        public DateTime? DateModified { get; set; }

        // difficulty rating
        [DisplayFormat(NullDisplayText = "No Rating")]
        public DifficultyRatings Difficulty { get; set; }

        // description of the location
        [DataType(DataType.MultilineText)]
        [StringLength(250)]
        public string Description { get; set; }

        // Location recreations
        [Display(Name = "Recreation Tags")]
        public virtual ICollection<LocationRecreation> RecOptions { get; set; }
        public virtual ICollection<Recreation> Recreations { get; set; }
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<LocationFlag> LocationFlags { get; set; }
        public virtual ICollection<LocationImage> Images { get; set; }
        public virtual ICollection<LocationVideo> Videos { get; set; }
        public virtual String State { get; set; }


        // calculate the overall rating of this Location, based off of its Reviews.
        public virtual double Rating() {
            if (Reviews != null && Reviews.Count != 0)
            {
                double up = 0;
                for (int i = 0; i < Reviews.Count; i++)
                {
                    up = Reviews.Count(item => item.Rating);
                }
                return up / Reviews.Count;
            }
            else
            {
                return 0;
            }
        }

        // get the number of upvotes for this Location from its associated Reviews.
        public virtual int UpVotes()
        {
            if (Reviews != null && Reviews.Count != 0)
            {
                int up = 0;
                for (int i = 0; i < Reviews.Count; i++)
                {
                    up = Reviews.Count(item => item.Rating);
                }
                return up;
            }
            else
            {
                return 0;
            }
        }

        // get the number of downvotes for this Location from its associated Reviews.
        public virtual int DownVotes()
        {
            if (Reviews != null && Reviews.Count != 0)
            {
                int up = 0;
                for (int i = 0; i < Reviews.Count; i++)
                {
                    up = Reviews.Count(item => item.Rating);
                }
                return Reviews.Count - up;
            }
            else
            {
                return 0;
            }
        }


        // Handles newlines in a string for html markup by replacing each with a <br> tag.
        public string GetDescriptionMarkup()
        {
            if (Description != null)
            {
                return Description.Replace(Environment.NewLine, "<br />");
            }
            else
            {
                return "";
            }
        }

        private int DescriptionPreviewLength = 50;
        // used for review index page, to give only a preview of the comment
        public string GetDescriptionPreview()
        {
            if (Description == null) return "";
            if (Description.Length < DescriptionPreviewLength) return Description.Replace(Environment.NewLine, " ");
            else return Description.Substring(0, DescriptionPreviewLength - 3).Replace(Environment.NewLine, " ") + "...";
        }

        static string baseUri = "http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false";
        public void RetrieveFormatedAddress()
        {
            string lat = Latitude + "";
            string lng = Longitude + "";
            string requestUri = string.Format(baseUri, lat, lng);

            using (WebClient wc = new WebClient())
            {
                string result = wc.DownloadString(requestUri);
                var xmlElm = XElement.Parse(result);
                var status = (from elm in xmlElm.Descendants()
                              where
                                  elm.Name == "status"
                              select elm).FirstOrDefault();
                if (status.Value.ToLower() == "ok")
                {
                    var res = xmlElm.Descendants("address_component")
                        .Where(x => (string)x.Element("type") == "administrative_area_level_1"
                        )
                        .Select(x => (string)x.Element("short_name")).FirstOrDefault();

                    State = res.ToString();
                }
            }
        }
    }
}