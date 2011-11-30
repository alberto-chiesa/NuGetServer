using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NuGetServer.Controllers
{
    public class UsersController : Controller
    {
        private readonly IUserRepository _repository;

        public UsersController(IUserRepository repository) {
            _repository = repository;
        }


        public ActionResult Index()
        {
            return View(_repository.AllUsers);
        }

    }
}
