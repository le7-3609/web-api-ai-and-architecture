using Azure;
using Entities;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class Gemini : IGemini
    {
        IConfiguration _config;
        ILogger<Gemini> logger;
        private readonly IOptions<GeminiSettings> _geminiSettings;

        public Gemini(IConfiguration config, ILogger<Gemini> logger, IOptions<GeminiSettings> geminiSettings)
        {
            this._config = config;
            this.logger = logger;
            _geminiSettings = geminiSettings;
        }

        public async Task<string?> RunGeminiForUserProduct(string userRequest, string category)
        {
            var request = BuildRequest(
                role: "Strict JSON Transformer",
                task: "Generate a technical prompt value based on user request and product category context.",
                contextTitle: "Category",
                contextValue: category,
                userRequest: userRequest);

            return await RunGeminiAsync(request);
        }

        public async Task<string?> RunGeminiForFillCategory(string userRequest, string mainCategory)
        {
            var request = BuildRequest(
                role: "Category Expansion Transformer",
                task: "Generate technical expansion instructions for user category customization, using the main category only as context.",
                contextTitle: "Main Category",
                contextValue: mainCategory,
                userRequest: userRequest);

            return await RunGeminiAsync(request);
        }

        public async Task<string?> RunGeminiForFillBasicSite(string userRequest)
        {
            var request = BuildRequest(
                role: "Website Architecture Transformer",
                task: "Generate a clear technical definition for a base website request.",
                contextTitle: "Context",
                contextValue: "Basic Site",
                userRequest: userRequest);

            return await RunGeminiAsync(request);
        }

        private async Task<string?> RunGeminiAsync(string request)
        {
            var apiKey = ResolveApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                logger.LogWarning("Gemini API key is missing");
                return null;
            }

            var client = new Client(apiKey: apiKey);

            try
            {
                var response = await client.Models.GenerateContentAsync(
                    model: "gemini-3-flash-preview",
                    contents: request,
                    config: new GenerateContentConfig
                    {
                        Temperature = 0.2f,
                        ResponseMimeType = "application/json"
                    });

                string? rawResponse = response.Candidates?[0].Content?.Parts?[0].Text;
                if (string.IsNullOrWhiteSpace(rawResponse))
                {
                    logger.LogWarning("Gemini returned an empty response");
                    return null;
                }

                return ExtractJson(rawResponse);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Gemini request failed");
                return null;
            }
        }

        private static string BuildRequest(string role, string task, string contextTitle, string contextValue, string userRequest)
        {
            return $$"""
                Role: {{role}}
                Task: {{task}}
                Constraint: Return only a valid JSON object with no markdown or extra text.

                Input:
                - {{contextTitle}}: {{contextValue}}
                - User Request: {{userRequest}}

                Schema:
                {
                  "technical_value": "string"
                }
                """;
        }

        private string? ResolveApiKey()
        {
            if (!string.IsNullOrWhiteSpace(_geminiSettings.Value.ApiKey))
            {
                return _geminiSettings.Value.ApiKey;
            }

            return _config["GEMINI_API_KEY"];
        }

        private static string? ExtractJson(string rawResponse)
        {
            string cleanJson = rawResponse.Trim();
            if (cleanJson.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("```", "", StringComparison.OrdinalIgnoreCase)
                    .Trim();
            }
            else if (cleanJson.StartsWith("```", StringComparison.OrdinalIgnoreCase))
            {
                cleanJson = cleanJson.Replace("```", "", StringComparison.OrdinalIgnoreCase).Trim();
            }

            int start = cleanJson.IndexOf('{');
            int end = cleanJson.LastIndexOf('}');
            if (start < 0 || end < 0 || end < start)
            {
                return null;
            }

            return cleanJson.Substring(start, end - start + 1);
        }
    }
}
