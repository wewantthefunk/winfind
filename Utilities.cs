using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace winfind {
    internal class Utilities {
        internal void TouchFile(string filePath) {
            TouchFile(filePath, string.Empty);
        }
        internal void TouchFile(string filePath, string data) {
            // Check if the file exists
            if (!File.Exists(filePath)) {
                // If the file does not exist, create it
                try {
                    using (FileStream fs = File.Create(filePath)) {
                        if (!string.IsNullOrEmpty(data)) {
                            fs.Write(Encoding.UTF8.GetBytes(data));
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine($"Failed to create file: {e.Message}");
                }
            }

            return;
        }

        internal void AppendToFile(string filePath, string data) {
            try {
                // Append text to the file
                File.AppendAllText(filePath, data);
            } catch (Exception e) {
                Console.WriteLine($"Failed to append data: {e.Message}");
            }
        }

        internal void DeleteFile(string filePath) {
            try {
                File.Delete(filePath);
            } catch (Exception e) {
                Console.WriteLine($"Failed to delete file: {e.Message}");
            }
        }

        internal List<string> LoadList(string filePath) {
            List<string> lines = new();
            try {
                // Read each line and add it to the list
                using StreamReader reader = new(filePath);
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    lines.Add(line);
                }
            } catch (Exception e) {
                Console.WriteLine($"Error reading the file: {e.Message}");
            }
            return lines;
        }

        internal List<string> ReadLinesFromFile(string filePath, int startLine, int numberOfLines) {
            List<string> result = new List<string>();

            try {
                using (StreamReader reader = new StreamReader(filePath)) {
                    int currentLine = 0;
                    while (!reader.EndOfStream && result.Count < numberOfLines) {
                        string? line = reader.ReadLine();

                        if (currentLine >= startLine) {
                            if (!string.IsNullOrEmpty(line))
                                result.Add(line);
                        }

                        currentLine++;
                    }
                }
            } catch (Exception e) {
                Console.WriteLine($"Error reading the file: {e.Message}");
            }

            return result;
        }
    }
}
