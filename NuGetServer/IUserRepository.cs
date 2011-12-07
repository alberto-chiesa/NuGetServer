using System.Collections.Generic;
using System.Security.Principal;
using NuGetServer.Models;

namespace NuGetServer {
    public interface IUserRepository {
        User AuthenticateUser(string username, string password);
        User TryGetUser(string username);

        void CreateUser(string username, string password, IEnumerable<string> roles);
        void ChangePassword(string username, string newPassword);
        void SetRoles(string username, IEnumerable<string> roles);
        void DeleteUser(string username);
        IEnumerable<User> AllUsers { get; }
    }
}