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

    var watch = Stopwatch.StartNew();

    bool startsWith = true;

    string target = args[0];

    List<string> found = new List<string>();

    if (target.StartsWith("\"")) {
        target = target.Substring(1);
    }

    if (target.EndsWith("\"")) {
        target = target.Substring(0, target.Length - 1);
    }

    target = target.ToLower();

    int[] i;

    if (target.StartsWith("*")) {
        target = target.Substring(1);
        startsWith = false;
    }

    WinFind winFind = new(target, startsWith);

    if (startsWith) {
        i = winFind.BinarySearch();
    } else {

        i = winFind.ParallelSearch();
    }

    Array.Sort(i);

    List<string> list;

    int c = 0;

    if (i.Length > 100) {
        Console.WriteLine(i.Length.ToString() + " records found. Only showing the first 100. Perhaps you need a more granular search");
    }

    if (i.Length > 0) {
        while (c < 100 && c < i.Length) {
            list = utilities.ReadLinesFromFile("files.lst", i[c], 1);
            if (list.Count > 0) {
                found.Add(list[0]);
            }
            c++;
        }
    }

    found.AddRange(winFind.GetQuickSearchResults());

    Console.WriteLine("Total files found: " + Convert.ToString(found.Count));

    int count = 0;
    foreach(string s in found) {
        Console.WriteLine(s);
        count++;
    }

    Console.WriteLine("Total files found: " + Convert.ToString(found.Count));

    Environment.CurrentDirectory = currentDirectory;

    watch.Stop();

    Console.WriteLine($"Total search time: {watch.ElapsedMilliseconds} ms");

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