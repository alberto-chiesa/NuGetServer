using System.Collections.Generic;
using System.Linq;

namespace NuGetServer
{
    public class User {
        public string Username { get; set; }
        public IList<string> Roles { get; set; }

        public User(string username, IEnumerable<string> roles) {
            this.Username = username;
            this.Roles    = roles.ToList();
        }
    }
}
