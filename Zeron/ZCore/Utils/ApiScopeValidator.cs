// Zeron - Scheduled Task Application for Windows OS
// Copyright (c) 2019 Jiowcl. All rights reserved.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Zeron.ZCore.Utils
{
    /// <summary>
    /// ApiScopeValidator - validates API access against configured scope rules.
    /// Scope format: "*" | "ApiName:*" | "ApiName:action" (comma-separated).
    /// Empty scopes config allows all (backward compatible).
    /// </summary>
    public static class ApiScopeValidator
    {
        // Scope separators.
        private static readonly char[] s_ScopeSeparators = [',', ';'];

        /// <summary>
        /// IsAllowed
        /// </summary>
        /// <param name="configuredScopes"></param>
        /// <param name="apiName"></param>
        /// <param name="requiredScope"></param>
        /// <returns>Returns bool.</returns>
        public static bool IsAllowed(string? configuredScopes, string? apiName, string? requiredScope = "*")
        {
            if (string.IsNullOrWhiteSpace(apiName))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(configuredScopes) || configuredScopes.Trim() == "*")
            {
                return true;
            }

            string action = string.IsNullOrWhiteSpace(requiredScope) ? "*" : requiredScope.Trim();

            foreach (string rawRule in configuredScopes.Split(s_ScopeSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (rawRule == "*")
                {
                    return true;
                }

                if (MatchesRule(rawRule, apiName, action))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// MatchesRule
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="apiName"></param>
        /// <param name="action"></param>
        /// <returns>Returns bool.</returns>
        private static bool MatchesRule(string rule, string apiName, string action)
        {
            int colonIndex = rule.IndexOf(':');

            if (colonIndex < 0)
            {
                return rule.Equals(apiName, StringComparison.OrdinalIgnoreCase);
            }

            string ruleApi = rule[..colonIndex].Trim();
            string ruleAction = rule[(colonIndex + 1)..].Trim();

            if (!ruleApi.Equals(apiName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return ruleAction == "*" || ruleAction.Equals(action, StringComparison.OrdinalIgnoreCase);
        }
    }
}
