A simple NuGet server which hosts an authenticated read-only feed. The feed is authenticated using BASIC authentication, which works with the package manager. There is an interface for administrating users.

Installation Instructions
----
1. Download the release (or get the source code, build and publish it).
2. Create an IIS web site that points to the folder where you put the download.
3. Login using the account admin/abcd to create the user accounts you want. Do not delete the last admin account.
4. Put any package you want to be in your feed inside the ~/Packages folder
5. (Optional) Edit any of the views to fit your purpose better.
