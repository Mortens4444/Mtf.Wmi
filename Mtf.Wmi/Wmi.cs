using Mtf.WmiHelper.Models;
using Mtf.WmiHelper.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security;

namespace Mtf.WmiHelper
{
    public static class Wmi
    {
        private static readonly char[] separator = new[] { ',' };

        public static WmiReaderResult GetObjects(string queryString, string nameSpace)
        {
            return GetObjects(queryString, nameSpace, null, ImpersonationLevel.Impersonate, AuthenticationLevel.Default);
        }

        /// <summary>
        /// Execute WMI query on a computer.
        /// </summary>
        /// <param name="queryString">"Example: SELECT * FROM Win32_BaseBoard"</param>
        /// <param name="namespace">Example: cimv2</param>
        /// <param name="computerName">Example: localhost</param>
        /// <param name="impersonationLevel">Example: ImpersonationLevel.Impersonate</param>
        /// <param name="authenticationLevel">Example: AuthenticationLevel.Default</param>
        /// <param name="enablePrivileges">True to enable privileges</param>
        /// <param name="username">Example: Administrator</param>
        /// <param name="securePassword">Example: SecureString securePassword = new SecureString(); password.ToCharArray().ToList().ForEach(securePassword.AppendChar); securePassword.MakeReadOnly();</param>
        /// <param name="password">Example: Password</param>
        /// <param name="authority">Example: Authority</param>
        /// <param name="context">Example: var context = new ManagementNamedValueCollection(); context.Add("Key1", "Value1"); context.Add("Key2", "Value2");</param>
        /// <returns>Result of the query</returns>
        public static WmiReaderResult GetObjects(string queryString, string @namespace, string computerName,
            ImpersonationLevel impersonationLevel, AuthenticationLevel authenticationLevel, bool enablePrivileges = false,
            string username = null, SecureString securePassword = null, string password = null, string authority = null, ManagementNamedValueCollection context = null)
        {
            var managementScope = $"\\\\{computerName ?? "localhost"}\\root\\{@namespace}";
            var scope = new ManagementScope(managementScope);

            if (!LocalDeviceIdentifier.IsLocalMachine(computerName))
            {
                scope.Options = new ConnectionOptions
                {
                    Username = username,
                    Password = password,
                    SecurePassword = securePassword,
                    Authority = authority,
                    Context = context,
                    Locale = "MS_40E",
                    Impersonation = impersonationLevel,
                    Authentication = authenticationLevel,
                    EnablePrivileges = enablePrivileges,
                    Timeout = TimeSpan.MaxValue,
                };
            }

            using (var objectSearcher = new ManagementObjectSearcher(scope, new ObjectQuery(queryString)))
            {
                var columnNames = GetCommaSeparatedColumnNames(queryString);
                var results = new List<IEnumerable<object>>();
                var shares = objectSearcher.Get().Cast<ManagementObject>();
                if (columnNames == Constants.Star && shares.Any())
                {
                    columnNames = string.Join(",", shares.First().Properties.Cast<PropertyData>().Select(p => p.Name));
                }

                foreach (var share in shares)
                {
                    var item = share.Properties.Cast<PropertyData>().Select(property => property.Value);
                    results.Add(item);
                }
                return new WmiReaderResult(columnNames, results);
            }
        }

        public static string GetCommaSeparatedColumnNames(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || !query.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Not supported query: {query}", nameof(query));
            }

            var selectLength = "SELECT ".Length;
            var fromIndex = query.IndexOf(" FROM", selectLength, StringComparison.OrdinalIgnoreCase);
            if (fromIndex < 0)
            {
                return Constants.Star;
            }

            var columnNames = query.Substring(selectLength, fromIndex - selectLength)
                .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                .Select(name => name.Trim());

            return string.Join(",", columnNames);
        }
    }
}
