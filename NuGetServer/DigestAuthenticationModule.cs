using System;
using System.Collections;
using System.Collections.Specialized;
using System.Configuration;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web;

namespace NuGetServer {
    /// <summary>
    /// This class does not really perform any authentication, but I leave it here until NuGet might support digest authentication.
    /// </summary>
    public class DigestAuthenticationModule : IHttpModule {
        public void Dispose() {
        }

        public void Init(HttpApplication application) {
            application.AuthenticateRequest += OnAuthenticateRequest;
            application.EndRequest          += OnEndRequest;
        }

        public void OnAuthenticateRequest(object source, EventArgs eventArgs) {
            HttpApplication app = (HttpApplication)source;

            // get Authorization header; check if not empty
            string authorization = app.Request.Headers["Authorization"];
            if (string.IsNullOrEmpty(authorization)) {
                AccessDenied(app);
                return;
            }

            // is it digest scheme ?
            authorization = authorization.Trim();
            if (authorization.IndexOf("Digest", 0) != 0) {
                AccessDenied(app);
                return;
            }

            // get Header parts
            // write them to the ListDictionary object
            ListDictionary dictAuthHeaderContents = GetHeaderParts(authorization);


            // check the user against the Database (by roles)
            // if everything is ok - get the password
            // this approach is the main difference 
            // from the Basic scheme
            // get user groups - 
            // necessary for the GenericPrincipal instance
            string username = (string)dictAuthHeaderContents["username"];
            string password = "";
            string[] groups;
            if (!AuthenticateAgentDigest(app, username, out password, out groups)) {
                AccessDenied(app);
                return;
            }


            // see Step #5 of the Digest algorithm
            // check against Digest Scheme
            string realm = ConfigurationSettings.AppSettings["Auth_Realm"];

            // a)
            // A1 = unq(username-value) ":" unq(realm-value) ":" passwd
            string a1 = String.Format("{0}:{1}:{2}", (string)dictAuthHeaderContents["username"], realm, password);

            // b)
            // HA1 = MD5(A1)
            string ha1 = CvtHex(a1);

            // c)
            // A2 = HTTP Method ":" digest-uri-value
            string a2 = String.Format("{0}:{1}", app.Request.HttpMethod, (string)dictAuthHeaderContents["uri"]);

            // d)
            // HA2 = MD5(A2)
            string ha2 = CvtHex(a2);

            // e)
            // GENRESPONSE = 
            // HA1 ":" nonce ":" nc ":" cnonce ":" qop ":" HA2
            string genResponse;
            if (dictAuthHeaderContents["qop"] != null) {
                genResponse = String.Format("{0}:{1}:{2}:{3}:{4}:{5}", ha1, (string)dictAuthHeaderContents["nonce"], (string)dictAuthHeaderContents["nc"], (string)dictAuthHeaderContents["cnonce"], (string)dictAuthHeaderContents["qop"], ha2);
            }
            else {
                genResponse = String.Format("{0}:{1}:{2}", ha1, (string)dictAuthHeaderContents["nonce"], ha2);
            }

            string hgenResponse = CvtHex(genResponse);

            // Check the nonce
            bool isNonceStale = !IsValidNonce((string)dictAuthHeaderContents["nonce"]);
            app.Context.Items["staleNonce"] = isNonceStale;

            // Check HGENRESPONSE 
            // against the response of the Authorization header
            // Check the nonce
            // if everything is ok - 
            // create GenericPrincipal instance, which contains
            // users groups
            string testResponse = dictAuthHeaderContents["response"].ToString();
            if ((testResponse == hgenResponse) && (!isNonceStale)) {
                app.Context.User = new GenericPrincipal(new GenericIdentity(username, "HTTPDigest.Components.AuthDigest"), groups);
            }
            else {
                AccessDenied(app);
                return;
            }
        }

        /*
        ######################################################
        #
        #   OnEndRequest
        # 
        #   set server response 
        #   WWW-Authenticate header (digest scheme)
        #   build header string according to scheme
        #   lift up modal window
        # 
        ######################################################
        */
        public void OnEndRequest(object source, EventArgs eventArgs) {
            HttpApplication app = (HttpApplication) source;
            if (app.Response.StatusCode == 401) {
                // from config.
                string lRealm = ConfigurationManager.AppSettings["HTTPDigest.Components.AuthDigest_Realm"];
                string lOpaque = ConfigurationManager.AppSettings["HTTPDigest.Components.AuthDigest_Opaque"];
                string lAlgorithm = ConfigurationManager.AppSettings["HTTPDigest.Components.AuthDigest_Algorithm"];
                string lQop = ConfigurationManager.AppSettings["HTTPDigest.Components.AuthDigest_Qop"];

                // generate
                string lNonce = GenerateNonce();

                bool isNonceStale = false;
                object staleObj = app.Context.Items["staleNonce"];
                if (staleObj != null)
                isNonceStale = (bool)staleObj;

                // Show Digest modal window
                // build WWW-Authenticate server response header
                string authHeader = string.Format(@"Digest realm=""{0}"", nonce=""{1}"", opaque=""{2}"", stale={3}, algorithm={4}, qop=""{5}""", lRealm, lNonce, lOpaque, isNonceStale ? "true" : "false", lAlgorithm, lQop);

                app.Response.AppendHeader("WWW-Authenticate", authHeader.ToString());

                // Set response state to 401
                app.Response.StatusCode = 401;
            }
        }

        protected virtual string GenerateNonce() {
            // Create a unique nonce - 
            // the simpliest version
            // Now + 3 minutes, encoded base64
            // The nonce validity check 
            // will be performed also against the time
            // More strong example of nonce - 
            // use additionally ETag and unique key, which is
            // known by the server
            DateTime nonceTime = DateTime.Now + TimeSpan.FromMinutes(3);
            string expireStr = nonceTime.ToString("G");

            Encoding enc = new ASCIIEncoding();
            byte[] expireBytes = enc.GetBytes(expireStr);
            string nonce = Convert.ToBase64String(expireBytes);

            // base64 adds "=" 
            // sign, which is forbidden by the server
            // cut it
            nonce = nonce.TrimEnd('=');
            return nonce;
        }


        protected virtual bool IsValidNonce(string nonce) {
            // Check nonce validity
            // decode from base64 and check
            // This implementation uses a simple version - 
            // thats why the check is simple also -
            // check against the time

            DateTime expireTime;
            int numPadChars = nonce.Length % 4;
            if (numPadChars > 0)
            numPadChars = 4 - numPadChars;
            string newNonce = 
            nonce.PadRight(nonce.Length + numPadChars, '=');

            try {
                byte[] decodedBytes = Convert.FromBase64String(newNonce);
                string preNonce = new ASCIIEncoding().GetString(decodedBytes);
                expireTime = DateTime.Parse(preNonce);
            }
            catch (FormatException) {
                return false;
            }
            return (expireTime >= DateTime.Now);
        }

        private string CvtHex(string sToConvert) {
            // Hashing
            Encoding enc = new ASCIIEncoding();
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bToConvert = 
            md5.ComputeHash(enc.GetBytes(sToConvert));
            string sConverted = "";
            for (int i = 0 ; i < 16 ; i++)
            sConverted += 
            String.Format("{0:x02}", bToConvert[i]);
            return sConverted;
        }

        private ListDictionary GetHeaderParts(string authorization) {
            // A method, that converts 
            // HTTP header string with all its contents
            // to the ListDictionary object
            ListDictionary dict = new ListDictionary();
            string[] parts = authorization.Substring(7).Split(new char[] {','});
            foreach (string part in parts) {
                string[] subParts = part.Split(new char[] {'='}, 2);
                string key = subParts[0].Trim(new char[] {' ', '\"'});
                string val = subParts[1].Trim(new char[] {' ', '\"'});
                dict.Add(key, val);
            }
            return dict;
        }

        private void AccessDenied(HttpApplication app) {
            // Access denied
            // Write to the browser
            app.Response.StatusCode = 401;
            app.Response.StatusDescription = "Access Denied";
            app.Response.Write("401 Access Denied");
            app.CompleteRequest();
        }

        protected virtual bool AuthenticateAgentDigest(HttpApplication app, string username, out string password, out string[] groups) {
            password = "";
            groups = null;
            int lagentID = 0;
            string lpageURL = "";

            password = "pass";
            groups = new[] { "user" };

            return true;
        }
    }
}