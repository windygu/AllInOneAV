﻿using DataBaseManager.JavDataBaseHelper;
using Model.Common;
using Model.JavModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenerateReport
{
    class Program
    {
        static void Main(string[] args)
        {
            Service.ReportService.GenerateReport();
        }
    }
}
