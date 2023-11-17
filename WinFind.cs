using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace winfind {
    internal class WinFind {
        private string _searchFor;
        private string _configFile;
        private Utilities _utilities;
        private List<string> _files;
        private List<string> _quickSearch;
        private List<int> _found;
        private bool _startsWith;

        private readonly object lockObject;

        public WinFind(string searchFor, bool startsWith) {
            _searchFor = searchFor;
            _utilities= new Utilities();
            _files = new List<string>();
            _quickSearch = new List<string>();
            _configFile = "config.txt";
            lockObject = new object();
            _found= new List<int>();
            _startsWith = startsWith;

            if (!TryServer())
                _files = _utilities.LoadList(Environment.CurrentDirectory + "\\files.idx");

            Console.WriteLine("total files to search: " + Convert.ToString(_files.Count));

            ReadConfigFile();

            Console.WriteLine("total quick search directories: " + Convert.ToString(_quickSearch.Count));
        }

        private void ReadConfigFile() {
            List<string> entries = _utilities.ReadLinesFromFile(_configFile, 0, 999);

            bool addToQuick = false;

            foreach (string entry in entries) {
                if (string.IsNullOrEmpty(entry)) {
                    continue;
                } else if (entry == "quick:") {
                    addToQuick = true;
                } else if (entry.EndsWith(":")) {
                    addToQuick = false;
                } else {
                    if (addToQuick) {
                        _quickSearch.Add(entry.Trim().ToLower());
                    }
                }
            }
        }

        public List<string> GetQuickSearchResults() {
            List<string> results = new();

            foreach (string entry in _quickSearch) {
                results.AddRange(TraverseDirectories(entry));
            }

            return results;
        }

        private List<string> TraverseDirectories(string currentDirectory) {
            List<string> results = new();

            // Attempt to retrieve the list of directories
            string[] subDirectories;
            try {
                subDirectories = Directory.GetDirectories(currentDirectory);
            } catch (UnauthorizedAccessException e) {
                // Handle access denied exceptions gracefully
                Console.WriteLine($"Access denied to {currentDirectory}: {e.Message}");
                return results;
            } catch (Exception e) {
                // Handle other exceptions if needed
                Console.WriteLine($"Unable to enumerate directories in {currentDirectory}: {e.Message}");
                return results;
            }

            // Recursively call this method for each subdirectory
            foreach (string subDirectory in subDirectories) {
                TraverseDirectories(subDirectory);
            }

            // Optionally print files in the current directory
            try {
                string[] files = Directory.GetFiles(currentDirectory);
                if (!currentDirectory.EndsWith("\\"))
                    currentDirectory += "\\";

                foreach (string file in files) {
                    string filename = file.Replace(currentDirectory, string.Empty);
                    if (_startsWith) {
                        if (filename.StartsWith(_searchFor)) {
                            results.Add(file);
                        }
                    } else {
                        if (filename.Contains(_searchFor)) {
                            results.Add(file);
                        }
                    }
                }
            } catch (Exception e) {
                Console.WriteLine($"Unable to enumerate files in {currentDirectory}: {e.Message}");
            }

            return results;
        }

        private bool TryServer() {
            return false;

            /*
            bool result = true;
            try {
                // Attempt to connect to the server
                using (var client = new TcpClient("127.0.0.1", 5000))
                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
                    // Send a message to the server
                    string message = "Hello, Server!";
                    writer.WriteLine(message);
                    writer.Flush();
                    Console.WriteLine($"Sent: {message}");

                    // Read the server's response
                    string? response = reader.ReadLine();
                    Console.WriteLine($"Received: {response}");
                }

                Console.WriteLine("Client terminated.");
            } catch (SocketException e) {
                Console.WriteLine($"Could not connect to the server: {e.Message}");
                result = false;
            } catch (Exception e) {
                Console.WriteLine($"An error occurred: {e.Message}");
                result = false;
            }

            return result;*/
        }

        public int[] ParallelSearch() {
            var foundIndices = _files.AsParallel()
                                  .Select((record, index) => new { record, index })
                                  .Where(item => item.record.Trim().ToLower().Contains(_searchFor))
                                  .Select(item => item.index)
                                  .ToList();

            return foundIndices.ToArray();
        }

        public int[] ParallelSearch2() {
            int threadCount = Convert.ToInt32(Math.Ceiling(_files.Count *  0.00002f));
            int min = 0;
            int max = _files.Count;
            _found.Clear();

            Thread[] threads = new Thread[threadCount];

            int chunk = _files.Count / threadCount;

            for (int x = 0; x < threads.Length; x++) {
                int offset = x > 0 ? 1 : 0;
                min = (x * chunk) + offset;
                max = (x + 1) * chunk;
                if (max + chunk > _files.Count)
                    max = _files.Count;

                threads[x] = new Thread(() => LocalSearch2(min, max));
                threads[x].Start();
                threads[x].Join();
            }

            return _found.ToArray(); 
        }

        private void LocalSearch2(int min, int max) {
            int[] results = LocalSearch(min, max);
            Console.WriteLine(results.Length);
             for (int x = 0; x < results.Length; x++) {
                lock(lockObject) {
                    _found.Add(results[x]);
                }
            }
        }

        public int[] BinarySearch() {
            if (_files.Count == 0) {
                return ReadFromServer();
            }

            return LocalSearch();
        }

        private int[] ReadFromServer() {
            return new int[] { -1 };
        }

        private int[] LocalSearch() {
            return LocalSearch(0, _files.Count - 1);
        }

        private int[] LocalSearch(int min, int max) {
            string target = _searchFor;
            List<int> result = new();
            int count = 0;

            while (min <= max) {
                int middle = (min + max) / 2;
                string middleValue = _files[middle].Trim().ToLower().Replace(Environment.NewLine, string.Empty);

                // Check if middleValue starts with target
                if (middleValue.StartsWith(target, true, null)) {
                    result.Add(middle);
                    break; // Target found
                }

                count++;

                int comparison = string.Compare(middleValue, target, true);

                if (comparison < 0) {
                    min = middle + 1; // Search the right half
                } else {
                    max = middle - 1; // Search the left half
                }
            }

            if (result.Count > 0) {
                int idx = result[0] - 1; 

                while (idx >= 0) {
                    if (_files[idx].StartsWith(target, StringComparison.OrdinalIgnoreCase)) {
                        result.Add(idx);
                        idx--;
                        count++;
                    } else {
                        idx = -1;
                    }
                }

                idx = result[0] + 1;

                while (idx < _files.Count) {
                    if (_files[idx].StartsWith(target, StringComparison.OrdinalIgnoreCase)) {
                        result.Add(idx);
                        idx++;
                        count++;
                    } else {
                        idx = _files.Count;
                    }
                }
            }

            return result.ToArray();
        }
    }
}
