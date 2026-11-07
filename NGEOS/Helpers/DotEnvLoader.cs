using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NGEOS.Helpers
{
    /// <summary>
    /// Lightweight, secure .env file loader for NGEOS.
    /// Reads key=value pairs from the root-level .env file and injects them into Environment variables
    /// without exposing secrets or overriding pre-existing system environment variables in production.
    /// </summary>
    public static class DotEnvLoader
    {
        public static void Load(string? preferredPath = null)
        {
            try
            {
                var candidates = new List<string>();

                if (!string.IsNullOrWhiteSpace(preferredPath))
                {
                    candidates.Add(Path.Combine(preferredPath, ".env"));
                }

                var currentDir = Directory.GetCurrentDirectory();
                candidates.Add(Path.Combine(currentDir, ".env"));
                candidates.Add(Path.GetFullPath(Path.Combine(currentDir, "..", ".env")));
                candidates.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"));
                candidates.Add(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", ".env")));

                var envFile = candidates.FirstOrDefault(File.Exists);
                if (envFile == null)
                {
                    return; // In production environments, physical .env is optional
                }

                foreach (var rawLine in File.ReadAllLines(envFile))
                {
                    var line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var delimiterIndex = line.IndexOf('=');
                    if (delimiterIndex <= 0)
                        continue;

                    var key = line.Substring(0, delimiterIndex).Trim();
                    var val = line.Substring(delimiterIndex + 1).Trim();

                    // Strip surrounding single or double quotes
                    if (val.Length >= 2 &&
                        ((val.StartsWith("\"") && val.EndsWith("\"")) ||
                         (val.StartsWith("'") && val.EndsWith("'"))))
                    {
                        val = val.Substring(1, val.Length - 2);
                    }

                    // System / hosting environment variables take precedence
                    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
                    {
                        Environment.SetEnvironmentVariable(key, val);
                    }
                }
            }
            catch (Exception)
            {
                // Silently handle if file read is not permitted; fallback to system env vars
            }
        }
    }
}
