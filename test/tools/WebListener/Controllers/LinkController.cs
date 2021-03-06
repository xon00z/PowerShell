using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using mvc.Models;

namespace mvc.Controllers
{
    public class LinkController : Controller
    {
        public JsonResult Index()
        {
            if (!Request.Query.TryGetValue("maxlinks", out StringValues maxLinksSV) || !Int32.TryParse(maxLinksSV.FirstOrDefault(), out int maxLinks) || maxLinks < 1)
            {
                maxLinks = 3;
            }

            if (!Request.Query.TryGetValue("linknumber", out StringValues linkNumberSV) || !Int32.TryParse(linkNumberSV.FirstOrDefault(), out int linkNumber) || linkNumber < 1)
            {
                linkNumber = 1;
            }

            string baseUri = Regex.Replace(UriHelper.GetDisplayUrl(Request), "\\?.*", String.Empty);

            string type = Request.Query.TryGetValue("type", out StringValues typeSV) ? typeSV.FirstOrDefault() : "default";

            var linkList = new List<String>();
            if (maxLinks > 1 && linkNumber > 1)
            {
                linkList.Add(GetLink(baseUri: baseUri, maxLinks: maxLinks, linkNumber: linkNumber - 1, type: type, rel: "prev"));
            }
            linkList.Add(GetLink(baseUri: baseUri, maxLinks: maxLinks, linkNumber: maxLinks, type: type, rel: "last"));
            linkList.Add(GetLink(baseUri: baseUri, maxLinks: maxLinks, linkNumber: 1, type: type, rel: "first"));
            linkList.Add(GetLink(baseUri: baseUri, maxLinks: maxLinks, linkNumber: linkNumber, type: type, rel: "self"));

            bool sendMultipleHeaders = false;
            bool skipNextLink = false;
            switch (type.ToUpper())
            {
                case "NOURL":
                    linkList.Add(Constants.NoUrlLinkHeader);
                    skipNextLink = true;
                    break;
                case "MALFORMED":
                    linkList.Add(Constants.MalformedUrlLinkHeader);
                    skipNextLink = true;
                    break;
                case "NOREL":
                    linkList.Add(Constants.NoRelLinkHeader);
                    skipNextLink = true;
                    break;
                case "MULTIPLE":
                    sendMultipleHeaders = true;
                    break;
                default:
                    break;
            }

            if (!skipNextLink && maxLinks > 1 && linkNumber < maxLinks)
            {
                linkList.Add(GetLink(baseUri: baseUri, maxLinks: maxLinks, linkNumber: linkNumber + 1, type: type, rel: "next"));
            }

            StringValues linkHeader;
            if (sendMultipleHeaders)
            {
                linkHeader = linkList.ToArray();
            }
            else
            {
                linkHeader = String.Join(",", linkList);
            }
            Response.Headers.Add("Link", linkHeader);

            // Generate /Get/ result and append linknumber, maxlinks, and type
            var getController = new GetController();
            getController.ControllerContext = this.ControllerContext;
            var result = getController.Index();
            var output = result.Value as Hashtable;
            output.Add("linknumber", linkNumber);
            output.Add("maxlinks", maxLinks);
            output.Add("type", type.FirstOrDefault());

            return result;
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private string GetLink(string baseUri, int maxLinks, int linkNumber, string type, string rel)
        {
            return String.Format(Constants.LinkUriTemplate, baseUri, maxLinks, linkNumber, type, rel);
        }
    }
}
