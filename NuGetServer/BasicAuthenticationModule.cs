using System;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using Castle.MicroKernel.Lifestyle;
using NuGetServer.Models;

namespace NuGetServer {
	public class BasicAuthenticationModule : IHttpModule
	{
		private static readonly string   _realm         = ConfigurationManager.AppSettings["BasicAuth_Realm"];
        private static readonly string[] _basicAuthUrls = (ConfigurationManager.AppSettings["BasicAuth_Urls"] ?? "").Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.ToLowerInvariant().TrimStart(new[] { '~', '/' })).ToArray();

		public void Init(HttpApplication context) {
			context.AuthenticateRequest +=OnAuthenticateRequest;
			context.EndRequest          +=OnEndRequest;
		}

		public void Dispose() {
		}

        private bool DoBasicAuthentication(HttpApplication application) {
			string authHeader = application.Request.ServerVariables["HTTP_AUTHORIZATION"];
			if(authHeader == null || !authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase)) {
                return false;
            }
			string[] credentials = Base64Decode(authHeader.Substring(6)).Split(':');
            if (credentials.Length != 2) {
                BasicAuthenticationFailed(application); // Bad authorization header
                return false;
            }

			IPrincipal principal = Authenticate(credentials[0], credentials[1]);
            if (principal == null) {
                BasicAuthenticationFailed(application); // Invalid credentials
                return false;
            }
            application.Context.User = principal;

            return true;
        }

        private bool DoFormsAuthentication(HttpApplication application) {
            var authCookie = application.Request.Cookies[FormsAuthentication.FormsCookieName];
            if (authCookie == null)
                return false;

            var ticket = FormsAuthentication.Decrypt(authCookie.Value);
            if (ticket == null)
                return false;

            var user = WithUserRepository(repo => repo.TryGetUser(ticket.Name));
            if (user == null)
                return false;

            application.Context.User = ToPrincipal(user);

            return true;
        }

	    private IPrincipal ToPrincipal(User user) {
            return user != null ? new GenericPrincipal(new GenericIdentity(user.Username), user.Roles.ToArray()) : null;
	    }

	    private void OnAuthenticateRequest(object sender, EventArgs e) {
			var application = (HttpApplication)sender;
            if (DoFormsAuthentication(application))
                return;
            DoBasicAuthentication(application);
		}

        private void BasicAuthenticationFailed(HttpApplication context) {
            context.Response.StatusCode = 401;
            context.Response.StatusDescription = "Access Denied";
            context.Response.Write("401 Access Denied");
            context.CompleteRequest();
        }

        private TResult WithUserRepository<TResult>(Func<IUserRepository, TResult> func) {
            using (ApplicationBootstrapper.Container.BeginScope()) {
                return func(ApplicationBootstrapper.Container.Resolve<IUserRepository>());
            }
        }

		private IPrincipal Authenticate(string username, string password) {
            return ToPrincipal(WithUserRepository(repo => repo.AuthenticateUser(username, password)));
		}

	    private string Base64Decode(string encodedData) {
			var encoder = new System.Text.UTF8Encoding();
			var utf8Decode = encoder.GetDecoder();
			byte[] todecodeByte = Convert.FromBase64String(encodedData);
			int charCount = utf8Decode.GetCharCount(todecodeByte, 0, todecodeByte.Length);
			char[] decodedChar = new char[charCount];
			utf8Decode.GetChars(todecodeByte, 0, todecodeByte.Length, decodedChar, 0);
			return new String(decodedChar);
		}

		private void OnEndRequest(object sender, EventArgs e) {
			var application = (HttpApplication)sender;

            if (application.Response.StatusCode == 401) {
                application.Response.ClearContent();
                application.Response.StatusCode = 401;
                application.Response.StatusDescription = "Access Denied";
                application.Response.Write("401 Access Denied");

                if (IsBasicAuthUrl(application.Request.Path)) {
				    application.Response.AddHeader("WWW-Authenticate","BASIC Realm=" + _realm);  // Use basic authentication for this page.
                }
                else {
                    FormsAuthentication.RedirectToLoginPage();
                }
            }
		}

	    private bool IsBasicAuthUrl(string path) {
            path = path.TrimStart(new[] { '/' }).ToLowerInvariant();
            return _basicAuthUrls.Any(p => path.StartsWith(p));
	    }
	}
}
