using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Thesis {
    static class DebugUiHandler {
        // Based on: https://github.com/define-private-public/CSharpNetworking/blob/master/02.HttpListener/HttpServer.cs (license: Unlicense)

        static HttpListener listener;

        public static void Run() {
            JsonOutputHelper.ExportRunListJsonFile();
            HostUiServer();
        }

        static void HostUiServer() {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(AppConfig.UiHostUrl);
            listener.Start();
            Console.WriteLine("UI running on URL {0}", AppConfig.UiHostUrl);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }

        static async Task HandleIncomingConnections() {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer) {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown")) {
                    Console.WriteLine("Shutdown requested");
                    runServer = false;
                }

                // Get original request path
                string originalRequestFilePath;
                if (req.Url.AbsolutePath.StartsWith("/output/")) {
                    // Create symlink to output folder
                    originalRequestFilePath = Path.Combine(DevConfig.OutputFolder, "." + req.Url.AbsolutePath[7..]);
                } else {
                    originalRequestFilePath = Path.Combine(DevConfig.UiFolder, "." + req.Url.AbsolutePath);
                }
                string requestFilePath = originalRequestFilePath;

                // If path is an existing folder, try ~/index.html
                if (Directory.Exists(originalRequestFilePath)) {
                    requestFilePath = Path.Combine(originalRequestFilePath, "index.html");
                } else {
                    // If path is a non-existing folder, try ~.html
                    if (Path.GetExtension(originalRequestFilePath) == "") {
                        if (originalRequestFilePath.EndsWith("/")) requestFilePath = originalRequestFilePath[0..^1] + ".html";
                        else requestFilePath += ".html";
                    }
                }

                // Check if file exists
                int statusCode;
                if (File.Exists(requestFilePath)) {
                    statusCode = 200;
                } else {
                    requestFilePath = Path.Combine(DevConfig.UiFolder, "./404.html");
                    statusCode = 404;
                }

                // Read file (multiple tries if file is busy)
                string dataStr = null;
                for (int i = 0; i < 10; i++) {
                    try {
                        dataStr = File.ReadAllText(requestFilePath);
                        break;
                    } catch {
                        Thread.Sleep(100);
                    }
                }
                if (dataStr == null) {
                    // File remains busy, use 404
                    requestFilePath = Path.Combine(DevConfig.UiFolder, "./404.html");
                    statusCode = 404;
                    dataStr = File.ReadAllText(requestFilePath);
                }

                // Get file info
                string extension = Path.GetExtension(requestFilePath);
                string contentType = extension switch {
                    ".html" => "text/html",
                    ".js" => "text/javascript",
                    ".css" => "text/css",
                    ".json" => "application/json",
                    ".ttf" => "application/x-font-ttf",
                    ".woff2" => "application/font-woff2",
                    _ => throw new Exception(string.Format("Unknown file type: {0}", extension)),
                };

                // Log some request info
                //Console.WriteLine("{0} {1} {2}", req.HttpMethod, req.Url, statusCode);
                //Console.WriteLine(originalRequestFilePath);
                //Console.WriteLine(requestFilePath);
                //Console.WriteLine();

                // Write response info
                byte[] data = Encoding.UTF8.GetBytes(dataStr);
                resp.ContentType = contentType;
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;
                resp.StatusCode = statusCode;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data.AsMemory(0, data.Length));
                resp.Close();
            }
        }

    }
}
