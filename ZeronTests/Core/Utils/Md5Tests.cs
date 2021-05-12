// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Zeron.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zeron.Core.Utils.Tests
{
    [TestClass()]
    public class Md5Tests
    {
        [TestMethod()]
        public void GenerateBase64Test()
        {
            string md5serial = Md5.GenerateBase64("zeron");

            Assert.AreEqual(md5serial, "b3z777naGB7+WBz7tMRQ/Q==");
        }
    }
}