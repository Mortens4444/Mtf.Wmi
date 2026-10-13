using Microsoft.Win32;
using Mtf.Wmi.Test;
using Mtf.WmiHelper;

const bool localRegistry = true;
const string VideoServerUsersRegistryPath = localRegistry ? "SOFTWARE\\WOW6432Node\\Graffiti\\Video Server\\Users" : "SOFTWARE\\Graffiti\\Video Server\\Users";

var videoServerUsers = GetVideoServerUsersFromRegistry("192.168.0.60", "Sziltech", "sziltech");
RecoverPasswords(videoServerUsers);

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
    
    var usersCount = localRegistry ?
        Convert.ToUInt32(Wmi.ReadRegistry(RegistryHive.LocalMachine, VideoServerUsersRegistryPath, "NumUsers", RegistryValueKind.DWord)) :
        Convert.ToUInt32(Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, "NumUsers", RegistryValueKind.DWord));

    var result = new List<VideoServerUser>();
    for (var i = 0; i < usersCount; i++)
    {
        try
        {
            var videoServer = localRegistry ?
                new VideoServerUser
                {
                    Id = (string)Wmi.ReadRegistry(RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"ID-{i}", RegistryValueKind.String),
                    FullName = (string)Wmi.ReadRegistry(RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Full Name-{i}", RegistryValueKind.String),
                    Username = (string)Wmi.ReadRegistry(RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Login Name-{i}", RegistryValueKind.String),
                    PasswordHash = (string)Wmi.ReadRegistry(RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Password-{i}", RegistryValueKind.String)
                } :
                new VideoServerUser
                {
                    Id = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"ID-{i}"),
                    FullName = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Full Name-{i}"),
                    Username = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Login Name-{i}"),
                    PasswordHash = (string)Wmi.ReadRemoteRegistry(ipAddress, osUsername, osPassword, RegistryHive.LocalMachine, VideoServerUsersRegistryPath, $"Password-{i}")
                };

            result.Add(videoServer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Skipping user {i}: {ex.Message}");
        }
    }
    return result;
}

static void RecoverPasswords(IEnumerable<VideoServerUser> users)
{
    foreach (var user in users)
    {
        if (user.PasswordHash == null)
        {
            continue;
        }
        RecoverPassword(user);
    }
}

static void RecoverPassword(VideoServerUser user)
{
    var charset = "<> :_-|\\/*.,abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZÁÉŐÚÁŰÖÜÓÍáéűőúóüöí0123456789'\"+!%=()[]äđĐÄ€ł´Ł˝¨¨¸÷ß×¤*;}{@&#"; //
    var expected = user.PasswordHash.ToUpperInvariant();
    using var md5 = System.Security.Cryptography.MD5.Create();
    foreach (var separator in GenerateCombinations(charset, 4))
    {
        var candidate = user.Username + separator + user.Username;
        var hash = GetPasswordHash(md5, candidate);

        if (hash == expected)
        {
            Console.WriteLine($"Found: {candidate}");
            break;
        }
    }
}

static string GetPasswordHash(System.Security.Cryptography.MD5 md5, string input)
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(input);
    var hash = md5.ComputeHash(bytes);
    return BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
}
static IEnumerable<string> GenerateCombinations(string charset, int maxLength)
{
    yield return String.Empty;
    foreach (var combo in GenerateCombinationsRecursive(charset, "", maxLength))
    {
        yield return combo;
    }
}

static IEnumerable<string> GenerateCombinationsRecursive(string charset, string prefix, int maxLength)
{
    if (prefix.Length > 0)
    {
        yield return prefix;
    }

    if (prefix.Length >= maxLength)
    {
        yield break;
    }

    foreach (var c in charset)
    {
        foreach (var combo in GenerateCombinationsRecursive(charset, prefix + c, maxLength))
        {
            yield return combo;
        }
    }
}