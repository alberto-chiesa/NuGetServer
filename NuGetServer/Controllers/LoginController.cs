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
    public class LoginController : Controller
    {
        private IUserRepository _userRepository;

        public LoginController(IUserRepository userRepository) {
            _userRepository = userRepository;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View(new LoginModel());
        }

        [HttpPost]
        public ActionResult Index(LoginModel model) {
            IPrincipal principal = null;
            if (!string.IsNullOrEmpty(model.Username) && !string.IsNullOrEmpty(model.Password))
                principal = _userRepository.AuthenticateUser(model.Username, model.Password);

            if (principal != null) {
                FormsAuthentication.RedirectFromLoginPage(principal.Identity.Name, false);
                return View(model);
            }
            else {
                return View(model);
            }
        }
    }
}
