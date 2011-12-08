using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using NuGetServer.Models;

namespace NuGetServer.Controllers
{
    [Authorize]
    public partial class AccountController : Controller {
        private readonly IUserRepository _repository;

        public AccountController(IUserRepository repository) {
            _repository = repository;
        }

        public virtual ActionResult Settings() {
            var user = _repository.TryGetUser(User.Identity.Name);
            if (user == null)
                return new HttpStatusCodeResult(403); // The user does not exist. Shouldn't happen.
            return View(new AccountSettingsModel { Username = user.Username, Roles = user.Roles.ToList() });
        }

        [HttpPost]
        public virtual ActionResult Settings(AccountSettingsModel model) {
            using (var ts = new TransactionScope()) {
                _repository.ChangePassword(User.Identity.Name, model.Password);
                ts.Complete();
            }
            return RedirectToAction(MVC.Home.Index());
        }
    }
}
