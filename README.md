# Jetbrains Space Project Repo Bulk Downloader
Bulk download repositories from JetBrains Space project.  This will use clone if the project doesn't exist locally and pull if it already exists.

# Steps:
1. Get your Jetbrains Space Url
2. Generate a permanent user token in Space
3. Get Project Key.  This is not the ID of the project.  When you set up the project you are asked to input a name and a key.  We want the key that you set up at that time.
4. Clone project
5. Build project
6. Run project
   1. Answer questions:
      1. Input space url
      2. Input project key
      3. Input token
      4. Input root location to place the repositories
      5. Input the email address to use for pulls