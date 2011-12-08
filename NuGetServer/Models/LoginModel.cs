using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NuGetServer.Models {
    public class LoginModel {
        public string ReturnUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
        public bool LoginFailed { get; set; }
    }
}