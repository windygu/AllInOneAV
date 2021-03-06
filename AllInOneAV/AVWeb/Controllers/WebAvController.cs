﻿using AVWeb.Filter;
using DataBaseManager.JavDataBaseHelper;
using DataBaseManager.ScanDataBaseHelper;
using Microsoft.Ajax.Utilities;
using Model.JavModels;
using Model.ScanModels;
using Model.WebModel;
using Newtonsoft.Json;
using Service;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using Utils;

namespace AVWeb.Controllers
{
    public class WebAvController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        [Rights]
        public ActionResult Play()
        {
            return View();
        }

        [Rights]
        public ActionResult UploadSeeds()
        {
            return View();
        }

        [Rights]
        public ActionResult PlayAv(string filePath)
        {
            var host = "http://www.cainqs.com:8087/avapi/playav?filename=" + filePath;
            ViewData.Add("path", host);

            return View();
        }

        [Rights]
        public ActionResult GetAv(string search, bool onlyExist = false, string searchType = "all", int page = 1, int pageSize = 20)
        {
            var scanResult = new List<ScanResult>();

            scanResult = ScanDataBaseManager.GetMatchScanResult();

            if (onlyExist)
            {
                scanResult = scanResult.Where(x => !string.IsNullOrEmpty(x.Location)).ToList();
            }

            var toBePlay = new List<ScanResult>();
            List<ScanResult> namePlay = new List<ScanResult>();
            List<ScanResult> actressPlay = new List<ScanResult>();
            List<ScanResult> categoryPlay = new List<ScanResult>();
            List<ScanResult> prefixPlay = new List<ScanResult>();
            List<ScanResult> direPlay = new List<ScanResult>();

            if (searchType == "all" || searchType == "id")
            {
                foreach (var r in scanResult)
                {
                    if (r.AvId == search.ToUpper())
                    {
                        toBePlay.Add(r);
                    }
                }
            }

            if (searchType == "all" || searchType == "actress")
            {
                foreach (var r in scanResult)
                {
                    foreach (var ac in r.ActressList)
                    {
                        if (ac.Contains(search))
                        {
                            toBePlay.Add(r);
                        }
                    }
                }
            }

            if (searchType == "all" || searchType == "category")
            {
                foreach (var r in scanResult)
                {
                    foreach (var ca in r.CategoryList)
                    {
                        if (ca.Contains(search))
                        {
                            toBePlay.Add(r);
                        }
                    }
                }
            }

            if (searchType == "all" || searchType == "prefix")
            {
                foreach (var r in scanResult)
                {
                    if (r.Prefix.Contains(search.ToUpper()))
                    {
                        toBePlay.Add(r);
                    }
                }
            }

            if (searchType == "all" || searchType == "director")
            {
                foreach (var r in scanResult)
                {
                    if (r.Director.Contains(search))
                    {
                        toBePlay.Add(r);
                    }
                }
            }

            var pageContent = toBePlay.OrderByDescending(x=>x.ReleaseDate).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData.Add("avs", pageContent);
            ViewData.Add("search", search);
            ViewData.Add("count", (int)Math.Ceiling((decimal)toBePlay.Count / pageSize));
            ViewData.Add("size", pageSize);
            ViewData.Add("current", page);
            ViewData.Add("total", toBePlay.Count);

            return View();
        }

        [Rights]
        public ActionResult Av(int avId)
        {
            var av = JavDataBaseManager.GetAV(avId);
            var match = ScanDataBaseManager.GetMatchScanResult(avId);

            if (av == null)
            {
                av = new AV();
            }

            if (match == null)
            {
                match = new ScanResult();
            }

            ViewData.Add("av", av);
            ViewData.Add("match", match);

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

        [Rights]
        public ActionResult ShowMag(ShowMagType type = ShowMagType.All, int jobId = 0)
        {
            var data = ScanDataBaseManager.GetAllMagByJob(jobId).Where(x => !string.IsNullOrEmpty(x.AvId)).GroupBy(x => x.AvId).ToDictionary(x => x.Key, x=>x.ToList());
            Dictionary<ShowMagKey, List<RemoteScanMag>> ret = new Dictionary<ShowMagKey, List<RemoteScanMag>>();

            foreach (var d in data)
            {
                if (d.Value.Count > 0)
                {
                    var key = new ShowMagKey();
                    key.Key = d.Key;
                    key.Type |= ShowMagType.All;

                    //没有已存在文件
                    if (d.Value.FirstOrDefault().SearchStatus == 1)
                    {
                        d.Value.ForEach(x => x.ClassStr = "card bg-primary");
                        key.Type |= ShowMagType.OnlyNotExist;
                    }

                    //有已存在文件
                    if (d.Value.FirstOrDefault().SearchStatus == 2)
                    {
                        d.Value.ForEach(x => x.ClassStr = "card bg-success");
                        key.Type |= ShowMagType.OnlyExist;
                    }

                    if (d.Value.Exists(x => x.MagSize > 0))
                    {
                        key.Type |= ShowMagType.HasMagSize;
                    }
                    else
                    {
                        key.Type |= ShowMagType.HasNoMagSize;
                    }

                    if (!string.IsNullOrEmpty(d.Value.FirstOrDefault().MatchFile))
                    {
                        d.Value.ForEach(x => x.MatchFileSize = new FileInfo(x.MatchFile).Length);

                        if (d.Value.Max(x => x.MagSize >= x.MatchFileSize))
                        {
                            key.Type |= ShowMagType.GreaterThenExist;
                        }
                    }
                    else
                    {
                        if (d.Value.Max(x => x.MagSize > 0))
                        {
                            key.Type |= ShowMagType.GreaterThenNotExist;
                        }
                    }

                    if (d.Value.Exists(x => x.MagTitle.Contains(d.Key) || x.MagTitle.Contains(d.Key.Replace("-", ""))))
                    {
                        ret.Add(key, d.Value);
                    }
                }
            }

            ret = ret.Where(x => x.Key.Type.HasFlag(type)).ToDictionary(x => x.Key, x => x.Value);

            ViewData.Add("jobId", jobId);
            ViewData.Add("data", ret);        

            return View();
        }

        public ActionResult ShareFile(string name)
        {
            List<FileInfo> f = new List<FileInfo>();
            List<DirectoryInfo> d = new List<DirectoryInfo>();

            try
            {
                var dirs = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Share\\" + name + "\\").GetDirectories();
                var files = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Share\\" + name + "\\").GetFiles();

                foreach (var file in files)
                {
                    f.Add(file);
                }

                foreach (var dir in dirs)
                {
                    d.Add(dir);
                }

                ApplicationLog.Debug(name);

                ViewData.Add("name", name.ToUpper());
            }
            catch (Exception ee)
            {
                ApplicationLog.Error(ee.ToString());
            }

            ViewData.Add("file", f);
            ViewData.Add("dir", d);

            return View();
        }

        public ActionResult ShareList()
        {
            var list = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "\\Share\\").GetDirectories();

            return View(list);
        }

        [Rights]
        public ActionResult ScanJav()
        {
            #region 按页面
            List<ScanMap> page = new List<ScanMap>();

            page.Add(new ScanMap()
            {
                Title = "新话题",
                Url = "http://www.javlibrary.com/cn/vl_update.php?mode="
            });

            page.Add(new ScanMap()
            {
                Title = "新加入",
                Url = "http://www.javlibrary.com/cn/vl_newentries.php?mode="
            });
            page.Add(new ScanMap()
            {
                Title = "最想要",
                Url = "http://www.javlibrary.com/cn/vl_mostwanted.php?mode="
            });
            page.Add(new ScanMap()
            {
                Title = "高评价",
                Url = "http://www.javlibrary.com/cn/vl_bestrated.php?mode="
            });
            page.Add(new ScanMap()
            {
                Title = "新发行",
                Url = "http://www.javlibrary.com/cn/vl_newrelease.php?mode="
            });

            ViewData.Add("page", page);
            #endregion

            #region 按演员
            var actress = JavDataBaseManager.GetActress();
            ViewData.Add("actress", actress);
            #endregion

            #region 按类型
            var cate = JavDataBaseManager.GetCategories();
            ViewData.Add("cate", cate);
            #endregion

            #region 按收藏
            var faviModel = ScanDataBaseManager.GetFaviScan();

            var favi = faviModel.GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x.ToList());

            ViewData.Add("favi", favi);
            #endregion

            return View();
        }

        [Rights]
        public ActionResult ScanJobList(int pageSize = 20)
        {
            var model = ScanDataBaseManager.GetScanJob(pageSize);

            return View(model);
        }

        [Rights]
        public JsonResult DeleteScanJob(int jobId)
        {
            var ret = 0;

            ret += ScanDataBaseManager.DeleteScanJob(jobId);
            ret += ScanDataBaseManager.DeleteRemoteMagScan(jobId);

            if (ret > 0)
            {
                return Json(new { success = "Success" }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = "Fail" }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetValidateCode()
        {
            ValidateCode vCode = new ValidateCode();
            string code = vCode.CreateValidateCode(5);
            Session["ValidateCode"] = code;
            byte[] bytes = vCode.CreateValidateGraphic(code);
            return File(bytes, @"image/jpeg");
        }

        public ActionResult Login()
        {
            return View();
        }

        public ActionResult Logout()
        {
            var uName = CookieTools.GetCookie("uName").Value;
 
            if (!string.IsNullOrEmpty(uName))
            {
                CacheTools.CacheRemove(uName);

                CookieTools.RemoveCookie(uName);
                CookieTools.RemoveCookie("token");
            }

            return View("Index");
        }

        public ActionResult NoRights()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string token = "")
        {
            string uName = Request.Form["userName"];
            string uPwd = Request.Form["userPassword"];
            string uValidate = Request.Form["validate"];
            string serviceCode = Session["ValidateCode"] as string;//服务器端验证码

            if (!string.IsNullOrEmpty(uName) && !string.IsNullOrEmpty(uPwd) && uValidate.Equals(serviceCode))
            {
                try
                {
                    if (ScanDataBaseManager.IsUser(uName, uPwd))
                    {
                        Guid guid = Guid.NewGuid();

                        CookieTools.AddCookie("token", guid.ToString(), "");
                        CookieTools.AddCookie("uName", uName, "");

                        CacheTools.CacheInsert(uName, guid.ToString(), DateTime.Now.AddDays(1));

                        TempData["LoginState"] = 1;

                        return Redirect("Index");
                    }
                }
                catch (Exception e)
                {

                }
            }

            return View("Login");
        }

        [Rights]
        [HttpPost]
        public JsonResult PostScanJob(string jobName, string scanParameter)
        {
            var jobId = ScanDataBaseManager.InsertScanJob(jobName, scanParameter);

            return Json(new { msg = "success", jobId = jobId });
        }

        [Rights]
        [HttpPost]
        public JsonResult Add115Task(string mag)
        {
            CookieContainer cc = new CookieContainer();
            bool ret = false;
            string msg = "";

            foreach (var t in JsonConvert.DeserializeObject<List<CookieItem>>(ScanDataBaseManager.GetOneOneFiveCookie().OneOneFiveCookie))
            {
                Cookie c = new Cookie(t.Name, t.Value, "/", "115.com");
                cc.Add(c);
            }

            var split = mag.Split(new string[] { "magnet:?" }, StringSplitOptions.None).Where(x => !string.IsNullOrEmpty(x));

            Dictionary<string, string> param = new Dictionary<string, string>();

            if (split.Count() <= 1)
            {
                param.Add("url", mag);
            }
            else
            {
                int index = 0;
                foreach (var s in split)
                {
                    param.Add(string.Format("url[{0}]", index), "magnet:?" + s);

                    index++;
                }
            }

            param.Add("sign", "");
            param.Add("uid" , "340200422");
            param.Add("time", DateTime.Now.ToFileTimeUtc() + "");

            var returnStr = "";

            if (split.Count() <= 1)
            {
                returnStr = HtmlManager.Post("https://115.com/web/lixian/?ct=lixian&ac=add_task_url", param, cc);
            }
            else
            {
                returnStr = HtmlManager.Post("https://115.com/web/lixian/?ct=lixian&ac=add_task_urls", param, cc);
            }

            if (!string.IsNullOrEmpty(returnStr))
            {
                var data = Newtonsoft.Json.Linq.JObject.Parse(returnStr);

                bool.TryParse(data.Property("state").Value.ToString(), out ret);

                if (ret == false)
                {
                    msg = data.Property("error_msg").Value.ToString();
                }
            }

            if (string.IsNullOrEmpty(msg))
            { 
                msg = "下载成功";
            }

            return Json(new { status = ret, msg = msg}, JsonRequestBehavior.AllowGet);
        }
    }
}