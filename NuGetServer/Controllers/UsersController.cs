using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace NuGetServer.Controllers
{
    [Authorize(Roles = AvailableRoles.Administrator)]
    public partial class UsersController : Controller
    {
        private readonly IUserRepository _repository;

        public UsersController(IUserRepository repository) {
            _repository = repository;
        }

        public virtual ActionResult Index()
        {
            return View(_repository.AllUsers);
        }

    }
}
