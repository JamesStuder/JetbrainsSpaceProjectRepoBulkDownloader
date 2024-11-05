using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using LibGit2Sharp;

namespace Space.BulkRepoDownloader
{
internal class Program
    {
        private static async Task Main(string[] args)
        {
            // Prompt the user for required information
            Console.Write("Enter your JetBrains Space organization URL (e.g., https://your-org.jetbrains.space): ");
            string? spaceUrl = Console.ReadLine();
            
            Console.Write("Enter your project Key: ");
            string? projectId = Console.ReadLine();
            
            Console.Write("Enter your Bearer Token: ");
            string? bearerToken = Console.ReadLine();

            Console.Write("Enter the directory where you want to clone the repositories: ");
            string? cloneDirectory = Console.ReadLine();
            
            Console.Write("Email for pull: ");
            string? emailForPull = Console.ReadLine();

            // Retrieve repository names and clone or pull each repository
            var repoNames = await GetRepositoryNamesAsync(spaceUrl, projectId, bearerToken);
            
            foreach (string repoName in repoNames)
            {
                string? cloneUrl = await GetCloneUrlAsync(spaceUrl, projectId, repoName, bearerToken);
                if (string.IsNullOrEmpty(cloneUrl))
                {
                    Console.WriteLine($"Failed to get clone URL for repository: {repoName}");
                    continue;
                }

                string repoPath = System.IO.Path.Combine(cloneDirectory!, repoName);

                Console.WriteLine($"Processing repository: {repoName}");

                if (System.IO.Directory.Exists(repoPath))
                {
                    Console.WriteLine($"Repository {repoName} already exists. Pulling latest changes...");
                    PullRepository(repoPath, bearerToken, emailForPull);
                }
                else
                {
                    Console.WriteLine($"Cloning repository {repoName} from {cloneUrl}...");
                    CloneRepository(cloneUrl, repoPath, bearerToken);
                }
            }
        }

        private static async Task<List<string>> GetRepositoryNamesAsync(string? spaceUrl, string? projectId, string? bearerToken)
        {
            var repoNames = new List<string>();
            using HttpClient client = new ();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            string url = $"{spaceUrl}/api/http/projects/key:{projectId}?$fields=repos(name)";
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JsonDocument jsonDoc = JsonDocument.Parse(jsonResponse);
                if (jsonDoc.RootElement.TryGetProperty("repos", out JsonElement reposElement))
                {
                    foreach (JsonElement repo in reposElement.EnumerateArray())
                    {
                        string? name = repo.GetProperty("name").GetString();
                        if (name != null) repoNames.Add(name);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Failed to retrieve repositories: {response.ReasonPhrase}");
            }

            return repoNames;
        }

        private static async Task<string?> GetCloneUrlAsync(string? spaceUrl, string? projectId, string repoName, string? bearerToken)
        {
            using HttpClient client = new ();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            string url = $"{spaceUrl}/api/http/projects/key:{projectId}/repositories/{repoName}/url";
            HttpResponseMessage response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                JsonDocument jsonDoc = JsonDocument.Parse(jsonResponse);
                if (jsonDoc.RootElement.TryGetProperty("httpUrl", out JsonElement cloneUrlElement))
                {
                    return cloneUrlElement.GetString();
                }
            }
            else
            {
                Console.WriteLine($"Failed to get clone URL for repository {repoName}: {response.ReasonPhrase}");
            }

            return null;
        }

        private static void CloneRepository(string? cloneUrl, string repoPath, string? bearerToken)
        {
            try
            {
                CloneOptions options = new ()
                {
                    FetchOptions = 
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "bearer", Password = bearerToken }
                    }
                };
                Repository.Clone(cloneUrl, repoPath, options);
                Console.WriteLine($"Cloned {cloneUrl} to {repoPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clone repository: {ex.Message}");
            }
        }

        private static void PullRepository(string repoPath, string? bearerToken, string? emailForPull)
        {
            try
            {
                using Repository repo = new (repoPath);
                PullOptions options = new()
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = "bearer", Password = bearerToken }
                    }
                };
                Commands.Pull(repo, new Signature("Automated Pull", emailForPull, DateTimeOffset.Now), options);
                Console.WriteLine($"Pulled latest changes for {repoPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to pull repository: {ex.Message}");
            }
        }
    }
}