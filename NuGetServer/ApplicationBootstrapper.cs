using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Linq;
using System.ServiceModel.Activation;
using System.Web;
using System.Web.Hosting;
using System.Web.Routing;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Ninject;
using NuGet.Server;
using NuGet.Server.DataServices;
using NuGet.Server.Infrastructure;

[assembly: WebActivator.PreApplicationStartMethod(typeof(NuGetServer.ApplicationBootstrapper), "Start")]


namespace NuGetServer {
    public static class ApplicationBootstrapper {
        public static IWindsorContainer Container { get; private set; }

        public static void Start() {
            MapRoutes(RouteTable.Routes);

            var authService = new AuthenticationService(HostingEnvironment.MapPath("~/App_Data/Users"));

            Container = new WindsorContainer();
            Container.Register(
                Component.For<IAuthenticationService>().Instance(authService)
            );

            authService.CreateAdminAccountIfNoUsersExist();
        }

        private static void MapRoutes(RouteCollection routes) {
            // The default route is http://{root}/nuget/Packages
            var factory = new DataServiceHostFactory();
            var serviceRoute = new ServiceRoute("nuget", factory, typeof(Packages));
            serviceRoute.Defaults = new RouteValueDictionary { { "serviceType", "odata" } };
            serviceRoute.Constraints = new RouteValueDictionary { { "serviceType", "odata" } };
            routes.Add("nuget", serviceRoute);
        }

        private static PackageService CreatePackageService() {
            return NinjectBootstrapper.Kernel.Get<PackageService>();
        }
    }
}