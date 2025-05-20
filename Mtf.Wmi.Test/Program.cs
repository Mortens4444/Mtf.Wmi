using Mtf.WmiHelper;

var hotfixIDs = Wmi.GetHotfixIDs("192.168.0.246", "user", "pass");
int num = 1;
foreach (var hotfix in hotfixIDs)
{
    Console.WriteLine($"{num++}. {hotfix}");
}

var processId = Wmi.StartApplication("calc", "192.168.0.246", "user", password: "pass");
Console.WriteLine($"Process started with id: {processId}");