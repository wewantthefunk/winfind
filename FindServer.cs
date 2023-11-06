using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace winfind {
    internal class FindServer {

        public void Start() {
            bool keepGoing = true;

            var server = new TcpListener(IPAddress.Any, 5000);
            server.Start();
            Console.WriteLine("Server started on port 5000.");

            // Check for 'end.it' file
            if (File.Exists("end.it")) {
                Console.WriteLine("'end.it' file found. Stopping server...");
                keepGoing = false;
            }

            while (keepGoing) {
                // Accept a client connection
                var client = server.AcceptTcpClient();
                Console.WriteLine("Client connected.");

                using (var stream = client.GetStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                using (var writer = new StreamWriter(stream, Encoding.UTF8)) {
                    // Read request from client
                    string request = reader.ReadLine();
                    Console.WriteLine($"Received: {request}");

                    // Send a response
                    string response = $"Server received your message: {request}";
                    writer.WriteLine(response);
                    writer.Flush();

                    Console.WriteLine("Response sent.");
                }

                client.Close();
                Console.WriteLine("Client disconnected.");

                // Check for 'end.it' file
                if (File.Exists("end.it")) {
                    Console.WriteLine("'end.it' file found. Stopping server...");
                    keepGoing = false;
                }
            }

            server.Stop();
            Console.WriteLine("Server stopped.");
        }
    }
}
