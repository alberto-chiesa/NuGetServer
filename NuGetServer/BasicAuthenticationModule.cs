using System;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using Castle.MicroKernel.Lifestyle;

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

		private void OnAuthenticateRequest(object sender, EventArgs e) {
			var application = (HttpApplication)sender;
			if (!application.Context.Request.IsAuthenticated) {
                DoBasicAuthentication(application);
			}
		}

        private void BasicAuthenticationFailed(HttpApplication context) {
            context.Response.StatusCode = 401;
            context.Response.StatusDescription = "Access Denied";
            context.Response.Write("401 Access Denied");
            context.CompleteRequest();
        }

		private IPrincipal Authenticate(string username, string password) {
            using (ApplicationBootstrapper.Container.BeginScope()) {
                var svc = ApplicationBootstrapper.Container.Resolve<IUserRepository>();
                return svc.AuthenticateUser(username, password);
            }
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

            if (IsBasicAuthUrl(application.Request.Path) && (application.Response.StatusCode == 401 || IsRedirectToLoginPage(application.Response))) {  // Messy: Forms authentication takes precedence here by default, so we have to override it.
                application.Response.ClearContent();
                application.Response.StatusCode = 401;
				application.Response.AddHeader("WWW-Authenticate","BASIC Realm=" + _realm);  // Use basic authentication for this page.
                application.Response.StatusDescription = "Access Denied";
                application.Response.Write("401 Access Denied");
                application.Response.RedirectLocation = null;
            }
		}

	    private bool IsRedirectToLoginPage(HttpResponse response) {
	        if (FormsAuthentication.IsEnabled && response.StatusCode == 302) {
                int queryIndex = response.RedirectLocation.IndexOf("?");
                string redirectLocation = (queryIndex >= 0 ? response.RedirectLocation.Substring(0, queryIndex) : response.RedirectLocation);
                return redirectLocation == FormsAuthentication.LoginUrl;
            }
            else {
                return false;
            }
	    }

	    private bool IsBasicAuthUrl(string path) {
            path = path.TrimStart(new[] { '/' }).ToLowerInvariant();
            return _basicAuthUrls.Any(p => path.StartsWith(p));
	    }
	}
}
