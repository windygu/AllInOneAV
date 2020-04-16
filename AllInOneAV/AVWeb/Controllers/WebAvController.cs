﻿using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Microsoft.Ajax.Utilities;
using Model.JavModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Utils;

namespace AVWeb.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class WebAvController : Controller
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        // GET: WebAv
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult UploadSeeds()
        {
            return View();
        }

        public ActionResult GetUnmatched(bool includePlayed = true)
        {
            ViewData.Add("list", WebService.WebService.GetUnMatch(includePlayed).OrderByDescending(x => x.HasPlayed).ToList());

            return View();
        }

        public ActionResult PlayAv(string filePath)
        {
            var host = "http://www.cainqs.com:8087/avapi/playav?filename=" + filePath;
            ViewData.Add("path", host);

            return View();
        }

        public ActionResult GetAv(int page = 1, int pageSize = 20, string id = "", string category = "", string actress = "", string director = "", string company = "", string publisher = "", string releaseDate = "", string orderBy = " ReleaseDate ", string orderType = " DESC ")
        {
            List<AV> ret = new List<AV>();
            string orderStr = orderBy + orderType;
            string where = " 1 = 1 ";
            string pageStr = @" AND t.OnePage BETWEEN " + (((page - 1) * pageSize) + 1) + " AND " + page * pageSize; ;

            if (!string.IsNullOrEmpty(id))
            {
                where += string.Format(" AND ID = '{0}' ", id);
            }

            if (!string.IsNullOrEmpty(category))
            {
                where += string.Format(" AND Category LIKE '%{0}%' ", category);
            }

            if (!string.IsNullOrEmpty(actress))
            {
                where += string.Format(" AND Actress LIKE '%{0}%' ", actress);
            }

            if (!string.IsNullOrEmpty(director))
            {
                where += string.Format(" AND Director LIKE '%{0}%' ", director);
            }

            if (!string.IsNullOrEmpty(company))
            {
                where += string.Format(" AND Company LIKE '%{0}%' ", company);
            }

            if (!string.IsNullOrEmpty(publisher))
            {
                where += string.Format(" AND Publisher = '%{0}%' ", publisher);
            }

            if (!string.IsNullOrEmpty(releaseDate))
            {
                var date = DateTime.Parse(releaseDate);

                where += string.Format(" AND ReleaseDate = '{0}' ", date.ToString("yyyy-MM-dd") + " 00:00:00.000");
            }

            var items =  WebService.WebService.GetAv(orderBy, where, pageStr);

            ViewData.Add("avs", items);

            return View();
        }

        public ActionResult Av(int avId)
        {
            var av = JavDataBaseManager.GetAV(avId);
            var match = ScanDataBaseManager.GetMatchMapByAvId(avId);

            if (av == null)
            {
                av = new AV();
            }

            if (match == null)
            {
                match = new Model.ScanModels.MatchMap();
            }

            ViewData.Add("av", av);
            ViewData.Add("match", match);

            return View();
        }

        public ActionResult Actress()
        {
            return View();
        }

        public ActionResult Category()
        {
            return View();
        }

        public JsonResult GetComics(int page = 1, int pageSize = 50)
        {
            string message = "";
            bool success = false;
            FileInfo[] files = null;
            int totalCount = 0;
            int currentCount = 0;

            try
            {
                files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\ComicDownload\\").GetFiles();
                totalCount = files.Count();
                files = files.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
                currentCount = files.Count();
                success = true;
            }
            catch (Exception ee)
            {
                message = ee.ToString();
            }

            return Json(new { success = success, message = message, data = files.Select(x=>x.Name).ToList(), totalCount = totalCount, currentCount = currentCount, page = page, pageSize = pageSize }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetComic(string name)
        {
            string message = "文件未找到";
            bool success = false;
            FileInfo fi = null;
            string url = "";
            double size = 0;
            string sizeStr = "";

            var files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\ComicDownload\\").GetFiles();
            fi = files.FirstOrDefault(x => x.Name == name);

            if (fi != null)
            {
                success = true;
                message = "";
                url = "http://www.cainqs.com:8087/comicdownload/" + fi.Name;
                size = fi.Length;
                sizeStr = FileSize.GetAutoSizeString(fi.Length, 1);
            }

            return Json(new { success = success, message = message, url = url, size = size, sizeStr = sizeStr }, JsonRequestBehavior.AllowGet);
        }

        public String Comic()
        {
            var template = "<a href=\"{0}\" booksize=\"{1}\" bookdate=\"{2}\">{3}</a><br>";
            var html = "<html><head><title>Index list.</title></head><body>{0}</body></html>";

            var files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\ComicDownload\\").GetFiles();
            StringBuilder sb = new StringBuilder();

            foreach (var file in files)
            {
                sb.Append(string.Format(template, "http://www.cainqs.com:8087/comicdownload/" + HttpUtility.UrlEncode(file.Name, Encoding.UTF8), file.Length, file.CreationTimeUtc.ToFileTimeUtc(), file.Name));
            }


            return string.Format(html,sb.ToString());
        }
    }
}