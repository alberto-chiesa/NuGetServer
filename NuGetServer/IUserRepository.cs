using System.Collections.Generic;
using System.Security.Principal;

namespace NuGetServer {
    public interface IUserRepository {
        IPrincipal AuthenticateUser(string username, string password);

        void CreateUser(string username, string password, IEnumerable<string> roles);
        void ChangePassword(string username, string newPassword);
        void SetRoles(string username, IEnumerable<string> roles);
        void DeleteUser(string username);
        IEnumerable<IPrincipal> AllUsers { get; }
    }
}