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

        public WinFind(string searchFor) {
            _searchFor = searchFor;
            _utilities= new Utilities();
            _files = new List<string>();
            _quickSearch = new List<string>();
            _configFile = "config.txt";

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
                    if (filename.StartsWith(_searchFor)) {
                        results.Add(file);
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
            string target = _searchFor;
            int min = 0;
            int max = _files.Count - 1;
            List<int> result = new List<int>();
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

            Console.WriteLine("total comparisons: " + Convert.ToString(count));

            return result.ToArray();
        }
    }
}
