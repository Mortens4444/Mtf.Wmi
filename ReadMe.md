# Mtf.Wmi Library Documentation

## Overview

The `Mtf.Wmi` library provides tools for executing WMI (Windows Management Instrumentation) queries on both local and remote computers, utilizing the `Wmi` class for querying system information. It includes capabilities for impersonation, secure password handling, and flexible query parameters. This document covers setup, class details, method descriptions, and usage examples for effective system management and monitoring in .NET applications.

## Installation

To install the `Mtf.Wmi` package, follow these steps:

1. **Add Package**:
   Open the terminal in your project directory and run:

   ```bash
   dotnet add package Mtf.Wmi
   ```

2. **Include the Namespace**:
   Add the `Mtf.WmiHelper` namespace at the beginning of your code file:

   ```csharp
   using Mtf.WmiHelper;
   ```

## Class: Wmi

The `Wmi` class provides methods for WMI queries that can return data from various namespaces on Windows-based systems. It supports querying both local and remote systems, with options for handling authentication, impersonation, and secure passwords.

### Methods

#### `WmiGetObjects`

**`WmiReaderResult WmiGetObjects(string queryString, string nameSpace)`**

Executes a basic WMI query on the local computer using default authentication settings.

- **Parameters**:
  - `queryString`: A WMI query string, e.g., `SELECT * FROM Win32_BaseBoard`.
  - `nameSpace`: The WMI namespace to query, e.g., `cimv2`.

- **Returns**: A `WmiReaderResult` containing the column names and query results.

**`WmiReaderResult WmiGetObjects(string queryString, string nameSpace, string computerName, ImpersonationLevel impersonationLevel, AuthenticationLevel authenticationLevel, bool enablePrivileges = false, string username = null, SecureString securePassword = null, string password = null, string authority = null, ManagementNamedValueCollection context = null)`**

Performs a WMI query on a specified computer with advanced options.

- **Parameters**:
  - `queryString`: WMI query string, e.g., `SELECT * FROM Win32_BaseBoard`.
  - `nameSpace`: WMI namespace, e.g., `cimv2`.
  - `computerName`: Target computer, e.g., `localhost`.
  - `impersonationLevel`, `authenticationLevel`, `enablePrivileges`: Security and privilege settings.
  - `username`, `securePassword`, `password`: Authentication credentials.
  - `authority`, `context`: Optional settings for WMI management context.

- **Returns**: A `WmiReaderResult` containing column names and data results from the query.

#### `GetCommaSeparatedColumnNames`

**`string GetCommaSeparatedColumnNames(string query)`**

Extracts column names from a WMI `SELECT` query string.

- **Parameters**:
  - `query`: The WMI query string, beginning with `SELECT`.

- **Returns**: A comma-separated string of column names extracted from the query.

### Example Usage

```csharp
using Mtf.WmiHelper;
using System.Security;
using System.Management;

public class WmiExample
{
    public void ExecuteQuery()
    {
        var securePassword = new SecureString();
        foreach (var ch in "password") securePassword.AppendChar(ch);

        // Basic query on local computer
        var result = Wmi.WmiGetObjects("SELECT * FROM Win32_BaseBoard", "cimv2");

        // Advanced query on a remote computer
        var remoteResult = Wmi.WmiGetObjects(
            "SELECT * FROM Win32_OperatingSystem",
            "cimv2",
            "RemoteComputerName",
            ImpersonationLevel.Impersonate,
            AuthenticationLevel.PacketPrivacy,
            enablePrivileges: true,
            username: "Administrator",
            securePassword: securePassword,
            context: new ManagementNamedValueCollection()
        );

        // Display results
        Console.WriteLine($"Columns: {result.ColumnNames}");
        foreach (var row in result.Rows)
        {
            Console.WriteLine(String.Join(", ", row));
        }
    }
}
```

### Notes

- **Error Handling**: Ensure exception handling for secure data transmission and network issues.
- **Security**: Use secure passwords (`SecureString`) where possible to enhance security when accessing remote systems.
- **Logging and Monitoring**: Consider integrating logging to capture WMI query performance and errors in production environments.

This documentation provides a solid foundation for managing and utilizing WMI queries within .NET applications, enabling efficient and secure system information retrieval across networked computers.