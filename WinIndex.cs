namespace winfind {
    internal class WinIndex {

        private readonly Utilities _utilities;
        private readonly string _configFile;
        private readonly string _indexFile;
        private readonly string _listFile;
        private List<string> _excluded;
        private List<string> _quickLookup;
        private FileEntry _files;

        public WinIndex() { 
            _utilities= new Utilities();
            _configFile = "config.txt";
            _indexFile = "files.idx";
            _listFile= "files.lst";
            _files = new FileEntry();
            _excluded = new List<string>(); 
            _quickLookup = new List<string>();

            ReadConfigFile();
        }

        private void ReadConfigFile() {
            List<string> entries = _utilities.ReadLinesFromFile(_configFile, 0, 999);

            bool addToExclude = false;
            bool addToQuick = false;

            foreach (string entry in entries) {
                if (string.IsNullOrEmpty(entry)) {
                    continue;
                } else if (entry == "exclude:") {
                    addToExclude = true;
                    addToQuick = false;
                } else if (entry == "quick:") {
                    addToExclude = false;
                    addToQuick = true;
                } else {
                    if (addToExclude) {
                        _excluded.Add(entry.Trim().ToLower());
                    } else if (addToQuick) { 
                        _quickLookup.Add(entry.Trim().ToLower());
                    }
                }
            }
        }

        public void PerformIndex(string startIn) {
            try {
                // Start the recursive search from the root directory
                TraverseDirectories(startIn);
                WriteEntries();
            } catch (Exception e) {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
        }

        private void WriteEntries() {
            _utilities.DeleteFile(_listFile);
            _utilities.DeleteFile(_indexFile);
            int count = 0;
            foreach (KeyValuePair<string, List<string>> entry in _files) {
                string key = entry.Key;
                foreach(string value in entry.Value) {
                    _utilities.AppendToFile(_listFile, value + Environment.NewLine);
                    _utilities.AppendToFile(_indexFile, key + Environment.NewLine);
                    count++;
                }
            }
        }

        private void TraverseDirectories(string currentDirectory) {
            foreach (string exclude in _excluded) {
                if (currentDirectory.ToLower().StartsWith(exclude)) {
                    return;
                }
            }
            // Print the current directory
            Console.WriteLine($"Indexing Directory: {currentDirectory}");

            // Attempt to retrieve the list of directories
            string[] subDirectories;
            try {
                subDirectories = Directory.GetDirectories(currentDirectory);
            } catch (UnauthorizedAccessException e) {
                // Handle access denied exceptions gracefully
                Console.WriteLine($"Access denied to {currentDirectory}: {e.Message}");
                return;
            } catch (Exception e) {
                // Handle other exceptions if needed
                Console.WriteLine($"Unable to enumerate directories in {currentDirectory}: {e.Message}");
                return;
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
                    AddToDictionary(filename, file);
                }
            } catch (Exception e) {
                Console.WriteLine($"Unable to enumerate files in {currentDirectory}: {e.Message}");
            }
        }

        private void AddToDictionary(string key, string value) {
            // If the key is not present in the dictionary, add an empty list as the value
            if (!_files.ContainsKey(key)) {
                _files[key] = new List<string>();
            }

            // Add the new value to the list corresponding to this key
            _files[key].Add(value);
        }
    }
}
