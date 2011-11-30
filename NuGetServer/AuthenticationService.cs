using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Web;
using Microsoft.Isam.Esent.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace NuGetServer {
    public interface IAuthenticationService {
        IPrincipal AuthenticateUser(string username, string password);
        void CreateUser(string username, string password, IEnumerable<string> roles);
    }

    public static class Roles {
        public const string Administrator = "Administrator";
        public const string Writer        = "Writer";
        public const string Reader        = "Reader";
    }

    public class AuthenticationService : IAuthenticationService {
        private class UserData {
            public string Username { get; set; }
            public byte[] PasswordHash { get; set; }
            public byte[] PasswordSalt { get; set; }
            public string[] Roles { get; set; }
        }

        private readonly PersistentDictionary<string, string> _users;
        private readonly Random _rnd = new Random();

        public AuthenticationService(string dbDirectory) {
            _users = new PersistentDictionary<string, string>(dbDirectory);
        }

        private UserData TryGetUser(string username) {
            string data;
            if (_users.TryGetValue(username, out data))
                return JsonConvert.DeserializeObject<UserData>(data);
            return null;
        }

        private void SetUser(UserData user) {
            string data = JsonConvert.SerializeObject(user);
            _users.Add(user.Username, data);
        }

        public IPrincipal AuthenticateUser(string username, string password) {
            UserData user = TryGetUser(username);
            if (user != null && IsCorrectPassword(user, password)) {
                return new GenericPrincipal(new GenericIdentity(username), (string[])user.Roles.Clone());
            }
            else {
                return null;
            }
        }

        private static bool IsCorrectPassword(UserData user, string password) {
            byte[] hashedAttempt = HashPassword(password, user.PasswordSalt);
            if (hashedAttempt.Length != user.PasswordHash.Length)
                return false;   // Probably will never happen. The hash should always return the same length data (I'm pretty sure)

            for (int i = 0; i < hashedAttempt.Length; i++) {
                if (hashedAttempt[i] != user.PasswordHash[i])
                    return false;
            }
            return true;
        }

        private static byte[] HashPassword(string password, byte[] salt) {
            using (var sha = SHA256.Create()) {
                var encoded = Encoding.Unicode.GetBytes(password);
                var salted = new byte[salt.Length + encoded.Length];
                Array.Copy(salt, salted, encoded.Length);
                Array.Copy(encoded, 0, salted, salt.Length, encoded.Length);
                return sha.ComputeHash(salted);
            }
        }

        public void CreateUser(string username, string password, IEnumerable<string> roles) {
            if (TryGetUser(username) != null)
                throw new ArgumentException("User " + username + " does already exist.", "username");

            byte[] salt = new byte[32];
            _rnd.NextBytes(salt);

            SetUser(new UserData {
                Username = username,
                PasswordSalt = salt,
                PasswordHash = HashPassword(password, salt),
                Roles = roles.ToArray()
            });
        }

        public void CreateAdminAccountIfNoUsersExist() {
            if (_users.Count == 0) {
                CreateUser("admin", "abcd", new[] { Roles.Reader, Roles.Writer, Roles.Administrator });
            }
        }
    }
}