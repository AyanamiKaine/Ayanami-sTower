using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks; // Required for async operations

namespace AyanamisTower.StellaLearning.Util;
/// <summary>
/// Class to handle the smart URL opening logic
/// </summary>
public static class SmartUrlOpener
{
    // HttpClient is intended to be instantiated once and reused throughout the life of an application.
    // For simplicity in this example, we use 'using' per call, but in a larger app,
    // consider using IHttpClientFactory (ASP.NET Core) or a static instance.
    // Setting a default timeout for checks.
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Attempts to open the specified URL, checking for reachability first.
    /// If the URL is not reachable, it tries to open it via the Wayback Machine.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    public static void OpenUrlIntelligently(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Console.WriteLine("Error: URL cannot be empty.");
            return;
        }

        Console.WriteLine($"Attempting to open URL: {url}");

        // For now we dont use the fallback wayback machine implementation as its a bit bugged and sometimes uses it
        // even when i know it should worl.
        bool isReachable = true; // await IsUrlReachableAsync(url);

        if (isReachable)
        {
            Console.WriteLine("URL appears reachable. Opening directly in the default browser...");
            OpenUrlInDefaultBrowser(url);
        }
        else
        {
            Console.WriteLine("URL was not reachable or timed out. Constructing Wayback Machine fallback URL...");
            // Construct the Wayback Machine URL: https://web.archive.org/web/*/YOUR_URL
            string waybackUrl = $"https://web.archive.org/web/*/{url}";
            Console.WriteLine($"Attempting to open via Wayback Machine: {waybackUrl}");
            OpenUrlInDefaultBrowser(waybackUrl);
        }
    }

    /// <summary>
    /// Checks if a URL is reachable by sending an HTTP GET request.
    /// (Changed from HEAD to GET for better compatibility with servers that disallow HEAD).
    /// </summary>
    /// <param name="url">The URL to check.</param>
    /// <returns>True if the URL returns a success or redirect status code within the timeout, false otherwise.</returns>
    private static async Task<bool> IsUrlReachableAsync(string url)
    {
        // Basic URL validation
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult)
            || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
        {
            Console.WriteLine($"Invalid URL format provided: {url}");
            return false;
        }

        // Use a transient HttpClient here for simplicity. See notes in the original example.
        using var client = new HttpClient();
        client.Timeout = DefaultTimeout; // Apply the timeout from the class definition (e.g., TimeSpan.FromSeconds(10))

        try
        {
            // *** CHANGE HERE: Use HttpMethod.Get instead of HttpMethod.Head ***
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Optional: Add a User-Agent header to mimic a browser, some sites might require it.
            request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");

            Console.WriteLine($"Checking reachability for {url} using GET (Timeout: {DefaultTimeout.TotalSeconds}s)...");

            // Send the request, but we only care about the response *status*, not the full content completion.
            // The timeout should prevent waiting indefinitely for large pages.
            HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead); // Optimization: Stop after headers are read

            // Consider success (2xx) or redirects (3xx) as "reachable" for the purpose of opening in browser.
            bool isSuccess = response.IsSuccessStatusCode; // Status code 200-299
            bool isRedirect = (int)response.StatusCode >= 300 && (int)response.StatusCode < 400;

            if (isSuccess || isRedirect)
            {
                Console.WriteLine($"URL check successful. Status: {response.StatusCode} ({(int)response.StatusCode})");
                return true;
            }
            else
            {
                // Any other status code (4xx client errors, 5xx server errors) means it's not reachable in the desired way.
                Console.WriteLine($"URL check failed. Status: {response.StatusCode} ({(int)response.StatusCode})");
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            // Covers DNS resolution failures, connection refused, network unavailable etc.
            Console.WriteLine($"Network error checking URL '{url}': {ex.Message}");
            return false;
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // Catches HttpClient timeouts specifically
            Console.WriteLine($"Timeout occurred while checking URL '{url}'.");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            // Catches general cancellation, could also be a timeout if configured differently.
            Console.WriteLine($"Operation canceled while checking URL '{url}'. Might be a timeout. {ex.Message}");
            return false;
        }
        catch (Exception ex) // Catch-all for other unexpected issues
        {
            Console.WriteLine($"An unexpected error occurred while checking URL '{url}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Opens the given URL in the system's default web browser.
    /// </summary>
    /// <param name="url">The URL to open.</param>
    private static void OpenUrlInDefaultBrowser(string url)
    {
        try
        {
            // Using ProcessStartInfo is recommended for better control and compatibility
            var psi = new ProcessStartInfo(url)
            {
                // IMPORTANT: UseShellExecute must be true to use the OS's default application for the URL protocol (http/https).
                // If false, it would try to execute the URL string as a file path, which fails.
                UseShellExecute = true
            };
            Process.Start(psi);
            Console.WriteLine($"Successfully initiated opening '{url}' in the default browser.");
        }
        // Catch specific exception for "No application associated" on Windows
        catch (System.ComponentModel.Win32Exception noBrowser)
        {
            // Error code -2147467259 (0x80004005) typically means 'E_FAIL', often indicating no handler.
            // Other codes might exist depending on the exact OS/failure.
            if (noBrowser.ErrorCode == -2147467259)
            {
                Console.WriteLine($"Error: Could not open '{url}'. No default web browser seems to be configured or the system could not find it.");
            }
            else
            {
                Console.WriteLine($"A Win32 error occurred opening URL '{url}': {noBrowser.Message} (Code: {noBrowser.ErrorCode})");
            }
        }
        catch (Exception ex)
        {
            // Handles other potential errors like invalid URL format for Process.Start, permissions issues etc.
            Console.WriteLine($"An unexpected error occurred while trying to open the URL '{url}' in the browser: {ex.Message}");
        }
    }
}