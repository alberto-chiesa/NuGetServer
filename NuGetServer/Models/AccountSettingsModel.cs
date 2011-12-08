using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NuGetServer.Models {
    public class AccountSettingsModel {
        public string Username { get; set; }
        public List<string> Roles { get; set; }
        public string Password { get; set; }
        public string PasswordConfirm { get; set; }
    }
}