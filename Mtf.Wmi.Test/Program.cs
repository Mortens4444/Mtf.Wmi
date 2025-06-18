using Microsoft.Win32;
using Mtf.Extensions.Services;
using Mtf.Wmi.Test;
using Mtf.WmiHelper;
using System.Runtime.InteropServices;

const string VideoServerUsersRegistryPath = "SOFTWARE\\Graffiti\\Video Server\\Users";

GetVideoServerUsersFromRegistry("192.168.0.60", "Sziltech", "sziltech");

var hotfixIDs = Wmi.GetHotfixIDs("192.168.0.246", "user", "pass");
int num = 1;
foreach (var hotfix in hotfixIDs)
{
    Console.WriteLine($"{num++}. {hotfix}");
}

var processId = Wmi.StartApplication("calc", "192.168.0.246", "user", password: "pass");
Console.WriteLine($"Process started with id: {processId}");

static List<VideoServerUser> GetVideoServerUsersFromRegistry(string ipAddress, string osUsername, string osPassword)
{
    var usersCount = (uint)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, "NumUsers", RegistryValueKind.DWord);

    var result = new List<VideoServerUser>();
    for (var i = 0; i < usersCount; i++)
    {
        var user = new VideoServerUser
        {
            Id = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"ID-{i}"),
            FullName = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Full Name-{i}"),
            Username = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Login Name-{i}"),
            PasswordHash = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Password-{i}")
        };
        result.Add(user);

        var charset = "<> :_-|\\/*.,abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZÁÉŐÚÁŰÖÜÓÍáéűőúóüöí0123456789'\"+!%=()[]äđĐÄ€ł´Ł˝¨¨¸÷ß×¤*;}{@&#"; //
        var expected = user.PasswordHash.ToUpperInvariant();
        foreach (var separator in GenerateCombinations(charset, 4))
        {
            var candidate = user.Username + separator + user.Username;
            var hash = GetPasswordHash(candidate);

            if (hash != user.PasswordHash)
            {
                Console.WriteLine($"no match: {candidate}");
            }
            else
            {
                Console.WriteLine($"Found: {candidate}");
            }
        }
    }
    return result;
}

static string GetPasswordHash(string input)
{
    using (var md5 = System.Security.Cryptography.MD5.Create())
    {
        var bytes = System.Text.Encoding.ASCII.GetBytes(input);
        var hash = md5.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
    }
}
static IEnumerable<string> GenerateCombinations(string charset, int maxLength)
{
    return GenerateCombinationsRecursive(charset, "", maxLength);
}

static IEnumerable<string> GenerateCombinationsRecursive(string charset, string prefix, int maxLength)
{
    if (prefix.Length > 0)
        yield return prefix;

    if (prefix.Length >= maxLength)
        yield break;

    foreach (var c in charset)
    {
        foreach (var combo in GenerateCombinationsRecursive(charset, prefix + c, maxLength))
        {
            yield return combo;
        }
    }
}