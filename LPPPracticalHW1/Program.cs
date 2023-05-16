using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace LPPPracticalHW1
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            List<string> input;

            if (args.Length == 0)
            {
                input = ReadFromConsole();
                Console.WriteLine(CreateLPModel(input));
            }
            else if (args[0] == "Testing")
            {
                Test();
            }
            else
            {
                if (args[0].Length > 1)
                {
                    input = ReadFromFile(args[0]);
                    WriteToFile(CreateLPModel(input), args[1]);
                }
                else
                {
                    input = ReadFromFile(args[0]);
                    WriteToFile(CreateLPModel(input), "lp.txt");
                }

            }

            Console.WriteLine("FINISHED");
            Console.ReadKey();
        }

        /// <summary>
        /// Generates lp from given data.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static string CreateLPModel(List<string> edges)
        {
            var output = GetEdgeSet(edges);
            //Variables
            output += "var ResultSets{ (x, y, weight) in Edges}, binary;\n"; //Vytvoření proměných pro každou hranu, detekujících zda je hrana v množině.

            //PF
            output += "minimize obj: sum{ (x, y, weight) in Edges} ResultSets[x, y, weight] * weight;\n"; //Vybereme množinu s nejmenší váhou.

            //Constaits
            output += "s.t. c1 {(v, x, w1) in Edges, (x, y, w2) in Edges, (y, z, w3) in Edges: v == z}:" //Pro cyklus délky 3, vybereme alespoň jednu hranu.
                       + " ResultSets[v, x, w1] + ResultSets[x, y, w2] + ResultSets[y, z, w3] >= 1;\n";
            output += "s.t. c2 {(r, v, w1) in Edges, (v, x, w2) in Edges, (x, y, w3) in Edges, (y, z, w4) in Edges: r == z}:" //Stejné jako předchozí, jenom délky 4.
                       + " ResultSets[r, v, w1] + ResultSets[v, x, w2] + ResultSets[x, y, w3] + ResultSets[y, z, w4] >= 1;\n";

            output += "solve;\n";

            //Output
            output += "printf \"#OUTPUT: %d\\n\", sum{(x,y,weight) in Edges: ResultSets[x,y,weight]>0} weight;\n";
            output += "printf{(x,y,weight) in Edges : ResultSets[x,y,weight]>0} \"Edge %d --> %d (%d)\\n\", x, y, weight;\n";
            output += "printf \"#OUTPUT END\\n\";\n";
            output += "end;\n";

            return output;
        }

        /// <summary>
        /// Converts set of edges to string representation.
        /// </summary>
        /// <returns></returns>
        static string GetEdgeSet(List<string> edges) 
        {
            var output = "set Edges := {";
            for (int i = 0; i < edges.Count() - 1; i++)
            {
                var edge = ParseEdge(edges[i]);
                output += $"({edge[0].Trim()},{edge[1].Trim()},{edge[2].Trim()}), ";
            }
            var edgeEnd = ParseEdge(edges[edges.Count() - 1]);
            output += $"({edgeEnd[0].Trim()},{edgeEnd[1].Trim()},{edgeEnd[2].Trim()}) }};\n";
            return output;
        }

        static string[] ParseEdge(string line)
        {
            var separators = new string[] { "-->", " (", ")" };
            return line.Split(separators, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Parses the graph header into 3 parts. GRAPH NODECOUNT EDGECOUNT.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        static string[] ParseGraphHeader(string line)
        {
            var graphSettings = line.Split(' ');
            if (graphSettings[0] != "WEIGHTED" || graphSettings[1] != "DIGRAPH")
                return null;

            var edgeCount = graphSettings[3].Split(':');
            graphSettings[3] = edgeCount[0];
            return graphSettings;
        }

        /// <summary>
        /// Reading graph info from console, first u need to specify graph header.
        /// </summary>
        /// <param name="nodeCount"></param>
        /// <returns></returns>
        static List<string> ReadFromConsole()
        {
            var input = new List<string>();
            var line = Console.ReadLine();

            var graphHeader = ParseGraphHeader(line);
            var edgesCount = Convert.ToInt32(graphHeader[3]);

            for (int i = 0; i < edgesCount; i++)
            {
                input.Add(Console.ReadLine());
            }

            return input;
        }

        /// <summary>
        /// Reading graph info from file.
        /// </summary>
        /// <param name="filename">Path to file with data.</param>
        /// <param name="nodeCount">Count of graph nodes.</param>
        /// <returns></returns>
        static List<string> ReadFromFile(string filename)
        {
            var input = new List<string>();
            using (StreamReader sr = new StreamReader(filename))
            {
                var graphHeader = ParseGraphHeader(sr.ReadLine());

                while (!sr.EndOfStream)
                {
                    input.Add(sr.ReadLine());
                }
            }
            return input;
        }

        /// <summary>
        /// Writes generated lp to file.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="filename"></param>
        static void WriteToFile(string input, string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename))
            {
                sw.Write(input);
            }
        }

        /// <summary>
        /// Generates lp for every test in TESTS folder a runs it with glpsol.exe
        /// </summary>
        static void Test()
        {
            var finalSummary = new List<string>();
            DirectoryInfo d = new DirectoryInfo(@"..\..\Tests"); //Assuming Test is your Folder

            FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files

            foreach (FileInfo file in Files)
            {
                var filePath = file.FullName;
                var watch = new Stopwatch();

                List<string> input;

                Console.WriteLine($"Testing: {file.FullName}");

                watch.Start();
                input = ReadFromFile(filePath);
                WriteToFile(CreateLPModel(input), "lp.txt");


                // Use ProcessStartInfo class
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = false;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.FileName = @"C:\Users\mkotv\source\repos\LPPractical\winglpk-4.65\glpk-4.65\w64\glpsol.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.Arguments = $"-m {Path.GetFullPath("lp.txt")}";

                string output = "";
                try
                {
                    // Start the process with the info we specified.
                    // Call WaitForExit and then the using statement will close.
                    using (Process exeProcess = Process.Start(startInfo))
                    {
                        output = exeProcess.StandardOutput.ReadToEnd();
                        exeProcess.WaitForExit();
                    }
                }
                catch
                {
                    // Log error.
                }

                watch.Stop();
                if (output != "")
                {
                    string timeLine = "";
                    string myTime = watch.Elapsed.Minutes.ToString() + ":"
                                    + watch.Elapsed.Seconds.ToString() + "."
                                    + watch.Elapsed.Milliseconds.ToString();

                    var result = new List<string>();
                    bool add = false;

                    var lines = output.Split('\n');
                    foreach (string line in lines)
                    {
                        if (line.Contains("Time used"))
                            timeLine = line;
                        else if (line.Contains("#OUTPUT:"))
                        {
                            add = true;
                            result.Add(line);
                        }
                        else if (add)
                        {
                            result.Add(line);
                        }
                        else if (line.Contains("#OUTPUT END"))
                        {
                            result.Add(line);
                            break;
                        }
                    }
                    Console.WriteLine($"My time : {myTime}");
                    Console.WriteLine(timeLine);
                    foreach (string line in result)
                    {
                        Console.WriteLine(line);
                    }

                    finalSummary.Add($"{filePath}\n{result[0]}\n{myTime}");
                }
            }


            foreach (var summary in finalSummary)
            {
                Console.WriteLine(summary);
            }
        }
    }
}
