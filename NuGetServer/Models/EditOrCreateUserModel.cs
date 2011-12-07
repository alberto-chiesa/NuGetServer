using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NuGetServer.Models {
    public class EditOrCreateUserModel {
        public bool IsCreate { get; set; }
        public string Username { get; set; }
        public string[] Roles { get; set; }
        public bool ChangePassword { get; set; }
        public string Password { get; set; }
        public string PasswordConfirm { get; set; }
    }
}