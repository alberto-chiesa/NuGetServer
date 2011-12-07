using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using NuGetServer.Models;

namespace NuGetServer.Controllers
{
    [Authorize(Roles = AvailableRoles.Administrator)]
    [NoCache]
    public partial class UsersController : Controller
    {
        private const string ModelKey = "EditModel-0183544eb5b24c5384c174ae4eff5d9f";
        private const string ModelStateKey = "ModelErrors-d45ee532b2354ead8068bc536eb955f7";

        private void AddModelErrorForPreservation(string key, string error) {
            var list = TempData[ModelStateKey] as List<KeyValuePair<string, string>>;
            if (list == null)
                TempData[ModelStateKey] = list = new List<KeyValuePair<string, string>>();
            list.Add(new KeyValuePair<string, string>(key, error));
        }

        private void ReadModelStateAfterRedirect() {
            var list = TempData[ModelStateKey] as List<KeyValuePair<string, string>>;
            if (list != null) {
                foreach (var e in list)
                    ModelState.AddModelError(e.Key, e.Value);
            }
        }


        private readonly IUserRepository _repository;

        public UsersController(IUserRepository repository) {
            _repository = repository;
        }

        public virtual ActionResult Index() {
            return View(_repository.AllUsers);
        }

        public virtual ActionResult Create() {
            ReadModelStateAfterRedirect();
            var model = TempData[ModelKey] as EditOrCreateUserModel ?? MapUserToModel(null);
            return View(Views.EditOrCreate, model);
        }

        public virtual ActionResult Edit(string username) {
            ReadModelStateAfterRedirect();
            var model = TempData[ModelKey] as EditOrCreateUserModel;
            if (model == null) {
                var user = _repository.TryGetUser(username);
                if (user == null)
                    return new HttpNotFoundResult("User not found");
                model = MapUserToModel(user);
            }

            return View(Views.EditOrCreate, model);
        }

        [HttpPost]
        public virtual ActionResult Edit(EditOrCreateUserModel model) {
            using (var ts = new TransactionScope()) {
                var user = _repository.TryGetUser(model.Username);
                if (user == null) {
                    TempData[ModelKey] = model;
                    AddModelErrorForPreservation("Username", "The user does not exist exist");
                    return RedirectToAction(MVC.Users.Edit(model.Username));
                }

                if (model.ChangePassword) {
                    _repository.ChangePassword(model.Username, model.Password);
                }

                _repository.SetRoles(model.Username, model.Roles ?? new string[0]);

                ts.Complete();
            }

            return RedirectToAction(MVC.Users.Index());
        }

        [HttpPost]
        public virtual ActionResult Create(EditOrCreateUserModel model) {
            using (var ts = new TransactionScope()) {
                var user = _repository.TryGetUser(model.Username);
                if (user != null) {
                    TempData[ModelKey] = model;
                    AddModelErrorForPreservation("Username", "The user already exists");
                    return RedirectToAction(MVC.Users.Create());
                }

                _repository.CreateUser(model.Username, model.Password, model.Roles ?? new string[0]);

                ts.Complete();
            }

            return RedirectToAction(MVC.Users.Index());
        }

        private EditOrCreateUserModel MapUserToModel(User user) {
            return new EditOrCreateUserModel {
                       Username        = user != null ? user.Username : null,
                       Roles           = user != null ? user.Roles.ToArray() : new string[0],
                       IsCreate        = user == null,
                       ChangePassword  = user == null,
                       Password        = "",
                       PasswordConfirm = ""
                   };
        }

        [HttpPost]
        public virtual ActionResult Delete(string username) {
            using (var ts = new TransactionScope()) {
                _repository.DeleteUser(username);
                ts.Complete();
            }
            return RedirectToAction(MVC.Users.Index());
        }
    }
}
