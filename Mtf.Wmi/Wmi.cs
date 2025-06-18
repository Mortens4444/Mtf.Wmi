using Microsoft.Win32;
using Mtf.Extensions;
using Mtf.WmiHelper.Models;
using Mtf.WmiHelper.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Management;
using System.Security;

namespace Mtf.WmiHelper
{
    public static class Wmi
    {
        private const string DefaultNamespace = "CIMv2";
        private const string Localhost = "localhost";
        private const string Ms40e = "MS_40E";
        private const string Root = "root";
        private static readonly char[] separator = new[] { ',' };

        public static uint StartApplication(string processPath, string computerName = Localhost, string username = null, string password = null, string authority = "ntlmdomain:DOMAIN", string @namespace = DefaultNamespace)
        {
            using (var securePassword = password.GetSecureString())
            {
                return StartApplication(processPath, computerName, username, securePassword, authority, @namespace);
            }
        }

        public static uint StartApplication(string processPath, string computerName = Localhost, string username = null, SecureString securePassword = null, string authority = "ntlmdomain:DOMAIN", string @namespace = DefaultNamespace)
        {
            var scope = GetManagementScope(computerName, @namespace);

            if (!LocalDeviceIdentifier.IsLocalMachine(computerName))
            {
                scope.Options = new ConnectionOptions
                {
                    Username = username,
                    SecurePassword = securePassword,
                    Authority = authority,
                    Locale = Ms40e,
                    Timeout = TimeSpan.MaxValue,
                    EnablePrivileges = true,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    Impersonation = ImpersonationLevel.Impersonate
                };
            }
            scope.Connect();
            uint processId;
            using (var processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), null))
            {
                object[] methodArgs = { processPath, null, null, null };
                var result = processClass.InvokeMethod("Create", methodArgs);
                processId = (uint)methodArgs[3];
            }
            return processId;
        }

        public static ReadOnlyCollection<string> GetHotfixIDs(string computerName, string username, string password, string authority = "ntlmdomain:DOMAIN", string @namespace = DefaultNamespace)
        {
            using (var securePassword = password.GetSecureString())
            {
                return GetHotfixIDs(computerName, username, securePassword, authority);
            }
        }

        public static ReadOnlyCollection<string> GetHotfixIDs(string computerName, string username, SecureString securePassword, string authority = "ntlmdomain:DOMAIN", string @namespace = DefaultNamespace)
        {
            var result = new List<string>();

            var options = new ConnectionOptions
            {
                Username = username,
                SecurePassword = securePassword,
                Authority = authority,
                EnablePrivileges = true,
                Authentication = AuthenticationLevel.PacketPrivacy,
                Impersonation = ImpersonationLevel.Impersonate
            };
            var scope = GetManagementScope(computerName, @namespace, options);
            scope.Connect();

            var query = new ObjectQuery("SELECT HotFixID FROM Win32_QuickFixEngineering");
            using (var searcher = new ManagementObjectSearcher(scope, query))
            {
                foreach (var queryObj in searcher.Get().Cast<ManagementObject>())
                {
                    if (queryObj["HotFixID"] != null)
                    {
                        result.Add(queryObj["HotFixID"].ToString());
                    }
                }
            }

            return new ReadOnlyCollection<string>(result);
        }

        private static ManagementScope GetManagementScope(string computerName, string @namespace, ConnectionOptions options = null)
        {
            var scopePath = GetScopePath(computerName, @namespace);
            return options == null ? new ManagementScope(scopePath) : new ManagementScope(scopePath, options);
        }

        private static string GetScopePath(string computerName, string @namespace)
        {
            return (computerName?.StartsWith("\\\\", StringComparison.Ordinal) ?? false) ?
                $"{computerName}\\{Root}\\{@namespace}" :
                $"\\\\{computerName ?? Localhost}\\{Root}\\{@namespace}";
        }

        public static WmiReaderResult GetObjects(string queryString, string @namespace = DefaultNamespace)
        {
            return GetObjects(queryString, @namespace, null, ImpersonationLevel.Impersonate, AuthenticationLevel.Default);
        }

        /// <summary>
        /// Execute WMI query on a computer.
        /// </summary>
        /// <param name="queryString">"Example: SELECT * FROM Win32_BaseBoard"</param>
        /// <param name="namespace">Example: CIMv2</param>
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
        public static WmiReaderResult GetObjects(string queryString, string @namespace = DefaultNamespace, string computerName = Localhost,
            ImpersonationLevel impersonationLevel = 0, AuthenticationLevel authenticationLevel = 0, bool enablePrivileges = false,
            string username = null, SecureString securePassword = null, string password = null, string authority = null, ManagementNamedValueCollection context = null)
        {
            var scope = GetManagementScope(computerName, @namespace);

            if (!LocalDeviceIdentifier.IsLocalMachine(computerName))
            {
                scope.Options = new ConnectionOptions
                {
                    Username = username,
                    Password = password,
                    SecurePassword = securePassword,
                    Authority = authority,
                    Context = context,
                    Locale = Ms40e,
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
                var shares = objectSearcher.Get().Cast<ManagementObject>().ToList();
                if (columnNames == Constants.Star && shares.Count != 0)
                {
                    columnNames = String.Join(",", shares.First().Properties.Cast<PropertyData>().Select(p => p.Name));
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
            if (String.IsNullOrWhiteSpace(query) || !query.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
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

            return String.Join(",", columnNames);
        }

        public static object ReadRegistry(string ipAddress, string username, string password, RegistryHive registryHive, string key, string value, RegistryValueKind kind)
        {
            using (var regKey = GetRegistryKey(registryHive, key))
            {
                var raw = regKey.GetValue(value);
                if (raw == null)
                {
                    return null;
                }

                switch (kind)
                {
                    case RegistryValueKind.DWord:
                    case RegistryValueKind.QWord:
                        return Convert.ToUInt32(raw);
                    case RegistryValueKind.String:
                    case RegistryValueKind.ExpandString:
                    case RegistryValueKind.MultiString:
                        return Convert.ToString(raw);
                    case RegistryValueKind.Binary:
                    case RegistryValueKind.Unknown:
                        return raw;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static object ReadRemoteRegistry(string ipAddress, string username, string password, RegistryHive registryHive, string key, string value, RegistryValueKind kind = RegistryValueKind.String)
        {
            var options = new ConnectionOptions
            {
                Username = username,
                Password = password
            };

            var scope = GetManagementScope(ipAddress, "default", options);
            if (!scope.IsConnected)
            {
                scope.Connect();
            }

            var registry = new ManagementClass(scope, new ManagementPath("StdRegProv"), null);

            string methodName, resultKey;
            switch (kind)
            {
                case RegistryValueKind.DWord:
                case RegistryValueKind.QWord:
                case RegistryValueKind.Binary:
                    methodName = kind == RegistryValueKind.DWord ? "GetDWORDValue" :
                                    kind == RegistryValueKind.QWord ? "GetQWORDValue" : "GetBinaryValue";
                    resultKey = "uValue";
                    break;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.MultiString:
                    methodName = kind == RegistryValueKind.String ? "GetStringValue" :
                                    kind == RegistryValueKind.ExpandString ? "GetExpandedStringValue" : "GetMultiStringValue";
                    resultKey = "sValue";
                    break;
                case RegistryValueKind.Unknown:
                    methodName = "GetBinaryValue";
                    resultKey = "uValue";
                    break;
                default:
                    throw new NotImplementedException();
            }

            var inParams = registry.GetMethodParameters(methodName);
            inParams["hDefKey"] = unchecked((uint)registryHive);
            inParams["sSubKeyName"] = key;
            inParams["sValueName"] = value;

            var outParams = registry.InvokeMethod(methodName, inParams, null);
            uint result = Convert.ToUInt32(outParams["ReturnValue"]);

            if (result != 0)
            {
                throw new Win32Exception((int)result);
            }

            return outParams[resultKey];
        }

        public static RegistryKey GetRegistryKey(RegistryHive registryHive, string name, bool writable = false)
        {
            RegistryKey key;

            switch (registryHive)
            {
                case RegistryHive.ClassesRoot:
                    key = Registry.ClassesRoot;
                    break;
                case RegistryHive.CurrentConfig:
                    key = Registry.CurrentConfig;
                    break;
                case RegistryHive.CurrentUser:
                    key = Registry.CurrentUser;
                    break;
                case RegistryHive.LocalMachine:
                    key = Registry.LocalMachine;
                    break;
                case RegistryHive.PerformanceData:
                    key = Registry.PerformanceData;
                    break;
                case RegistryHive.Users:
                    key = Registry.Users;
                    break;
#if NETFRAMEWORK
                case RegistryHive.DynData:
                    key = Registry.DynData;
                    break;
#endif
                default:
                    throw new NotSupportedException();
            }

            return key.OpenSubKey(name, writable);
        }
    }
}
