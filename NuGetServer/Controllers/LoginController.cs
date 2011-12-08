using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using NuGetServer.Models;

namespace NuGetServer.Controllers
{
    public partial class LoginController : Controller
    {
        private const string LoginModelKey = "LoginModel-f9ad4a199be4ceabc7649cb5a3d198b";

        private readonly IUserRepository _userRepository;

        public LoginController(IUserRepository userRepository) {
            _userRepository = userRepository;
        }

        [HttpGet]
        public virtual ActionResult Index(string returnUrl)
        {
            var model = TempData[LoginModelKey] as LoginModel ?? new LoginModel();
            model.ReturnUrl = returnUrl;
            return View(model);
        }

        [HttpPost]
        public virtual ActionResult DoLogin(LoginModel model, string returnUrl) {
            User user = null;
            bool loginFailed = false;

            if (!string.IsNullOrEmpty(model.Username) && !string.IsNullOrEmpty(model.Password)) {
                user = _userRepository.AuthenticateUser(model.Username, model.Password);
                loginFailed = (user == null);
            }

            if (user != null) {
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                return RedirectAfterLoginOrLogout(returnUrl);
            }
            else {
                TempData[LoginModelKey] = new LoginModel { Username = model.Username, Password = model.Password, RememberMe = model.RememberMe, LoginFailed = loginFailed };
                return RedirectToAction(MVC.Login.Index().AddRouteValues(new { returnUrl }));
            }
        }

        private ActionResult RedirectAfterLoginOrLogout(string returnUrl) {
            if (!string.IsNullOrEmpty(returnUrl))
                return Redirect(returnUrl);
            else if (!string.IsNullOrEmpty(FormsAuthentication.DefaultUrl))
                return Redirect(FormsAuthentication.DefaultUrl);
            else
                return Redirect("/");
        }

        public virtual ActionResult SignOut(string returnUrl) {
            FormsAuthentication.SignOut();
            return RedirectAfterLoginOrLogout(returnUrl);
        }
    }
}
