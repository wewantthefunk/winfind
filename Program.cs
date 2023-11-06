// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Reflection;
using winfind;

Utilities utilities= new Utilities();

utilities.TouchFile("config.txt");

utilities.TouchFile("files.idx");
utilities.TouchFile("files.lst");

if (args.Length == 0) {
    Console.WriteLine("specify either '-index' to re-index drive, or the file or directory to search for");
    
    return;
}

// Get the full path to the executable file
string executablePath = Assembly.GetExecutingAssembly().Location;

// Get the directory that contains the executable
string executableDirectory = Path.GetDirectoryName(executablePath);

string currentDirectory = Environment.CurrentDirectory;

Environment.CurrentDirectory = executableDirectory;

if (args.Contains("-server")) {
    FindServer server = new FindServer();
    server.Start();
    Environment.CurrentDirectory = currentDirectory;
    return;
}

Stopwatch stopwatch = new Stopwatch();

// Start the stopwatch
stopwatch.Start();

if (!args.Contains("-index")) {

    string target = args[0];

    if (target.StartsWith("\"")) {
        target = target.Substring(1);
    }

    if (target.EndsWith("\"")) {
        target = target.Substring(0, target.Length - 1);
    }

    target = target.ToLower();

    WinFind winFind = new(target);

    int[] i = winFind.BinarySearch();

    if (i.Length == 0) {
        Console.WriteLine(args[0] + " not found");
        Environment.CurrentDirectory = currentDirectory;
        return;
    }

    Array.Sort(i);

    List<string> found = utilities.ReadLinesFromFile("files.lst", i[0], i.Length);

    Console.WriteLine("Total files found: " + Convert.ToString(found.Count));

    int count = 0;
    foreach(string s in found) {
        Console.WriteLine(s);
        count++;
    }

    Console.WriteLine("Total files found: " + Convert.ToString(found.Count));

    Environment.CurrentDirectory = currentDirectory;
    return;
}

WinIndex index = new();

string startIn = "C:\\";

if (args.Contains("-s")) {
    int i = -1;
    int count = 0;
    foreach(string s in args) {
        if (s == "-s") {
            i = count + 1;
            break;
        }

        count++;
    }

    if (i > -1 && i < args.Length)
        startIn = args[i];

}

index.PerformIndex(startIn);

Environment.CurrentDirectory = currentDirectory;