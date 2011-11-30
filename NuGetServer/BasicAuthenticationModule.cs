using System;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Security.Principal;
using Castle.MicroKernel.Lifestyle;

namespace NuGetServer {
	public class BasicAuthenticationModule : IHttpModule
	{
		private static readonly string _realm = ConfigurationManager.AppSettings["Auth_Realm"];

		public void Init(HttpApplication context) {
			context.AuthenticateRequest +=OnAuthenticateRequest;
			context.EndRequest          +=OnEndRequest;
		}

		public void Dispose() {
		}

		private void OnAuthenticateRequest(object sender, EventArgs e) {
			var application = (HttpApplication)sender;
			if (!application.Context.Request.IsAuthenticated) {
				string authHeader = application.Request.ServerVariables["HTTP_AUTHORIZATION"];
				if(authHeader == null || !authHeader.StartsWith("basic", StringComparison.OrdinalIgnoreCase)) {
                    Send401(application);
                    return;
                }
				string[] credentials = Base64Decode(authHeader.Substring(6)).Split(':');
                if (credentials.Length != 2) {
                    Send401(application);
                    return;
                }

				IPrincipal principal = Authenticate(credentials[0], credentials[1]);
                if (principal == null) {
                    Send401(application);
                    return;
                }
                application.Context.User = principal;
			}
		}

        private void Send401(HttpApplication context) {
            // Access denied
            // Write to the browser
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
			if(application.Response.StatusCode == 401) {
				application.Response.AddHeader("WWW-Authenticate","BASIC Realm=" + _realm);
			}
		}
	}
}
