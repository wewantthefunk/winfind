// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.Reflection;
using winfind;

Utilities utilities= new Utilities();

utilities.TouchFile("config.txt");

utilities.TouchFile("files.idx");
utilities.TouchFile("files.lst");

string INDEX_FLAG = "--index";
string SERVER_FLAG = "--server";
string START_IN_FLAG = "--s";

if (args.Length == 0) {
    Console.WriteLine("specify either '" + INDEX_FLAG + "' to re-index drive, or the file or directory to search for");
    
    return;
}

// Get the full path to the executable file
string executablePath = Assembly.GetExecutingAssembly().Location;

// Get the directory that contains the executable
string? executableDirectory = Path.GetDirectoryName(executablePath);

if (executableDirectory == null) {
    Console.WriteLine("unable to find executable directory for winfind");
    return;
}

string currentDirectory = Environment.CurrentDirectory;

Environment.CurrentDirectory = executableDirectory;

if (args.Contains(SERVER_FLAG)) {
    FindServer server = new();
    server.Start();
    Environment.CurrentDirectory = currentDirectory;
    return;
}

int[] i;

if (args.Contains(INDEX_FLAG)) {
    WinIndex index = new();

    string startIn = "C:\\";

    if (args.Contains(START_IN_FLAG)) {
        int ii = -1;
        int count1 = 0;
        foreach (string s in args) {
            if (s == "-s") {
                ii = count1 + 1;
                break;
            }

            count1++;
        }

        if (ii > -1 && ii < args.Length)
            startIn = args[ii];
    }

    index.PerformIndex(startIn);

    return;
}

var watch = Stopwatch.StartNew();

bool startsWith = true;

string target = args[0];

List<string> found = new List<string>();

if (target.StartsWith("\"")) {
    target = target[1..];
}

if (target.EndsWith("\"")) {
    target = target[..^1];
}

target = target.ToLower();

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

Environment.CurrentDirectory = currentDirectory;