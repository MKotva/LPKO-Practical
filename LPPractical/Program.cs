using LPPractical;
using System.Diagnostics;
using System.Net.NetworkInformation;

class Program
{
    static void Main(string[] args)
    {
        List<string> input;
        int nodeCount = 0;

        if (args.Length == 0)
        {
            input = ReadFromConsole(out nodeCount);
            var lpContructor = new OptimizedLPConstructor(nodeCount);
            Console.WriteLine(lpContructor.CreateLPModel(input));
        }
        else if (args[0] == "Testing")
        {
            Test();
        }
        else
        {
            if (args[0].Length > 1)
            {
                input = ReadFromFile(args[0], out nodeCount);
                var lpContructor = new OptimizedLPConstructor(nodeCount);
                WriteToFile(lpContructor.CreateLPModel(input), args[1]);
            }
            else
            {
                input = ReadFromFile(args[0], out nodeCount);
                var lpContructor = new OptimizedLPConstructor(nodeCount);
                WriteToFile(lpContructor.CreateLPModel(input), "lp.txt");
            }
        }
        Console.WriteLine("FINISHED");
        Console.ReadKey();
    }

    /// <summary>
    /// Reading graph info from console, first u need to specify graph header.
    /// </summary>
    /// <param name="nodeCount"></param>
    /// <returns></returns>
    static List<string> ReadFromConsole(out int nodeCount)
    {
        var input = new List<string>();
        var line = Console.ReadLine();

        var graphHeader = ParseGraphHeader(line);
        var edgesCount = Convert.ToInt32(graphHeader[2]);

        for (int i = 0;  i < edgesCount; i++) 
        {
            input.Add(Console.ReadLine());
        }
        nodeCount = Convert.ToInt32(graphHeader[1]);
        return input;
    }


    /// <summary>
    /// Reading graph info from file.
    /// </summary>
    /// <param name="filename">Path to file with data.</param>
    /// <param name="nodeCount">Count of graph nodes.</param>
    /// <returns></returns>
    static List<string> ReadFromFile(string filename, out int nodeCount)
    {
        var input = new List<string>();
        using (StreamReader sr = new StreamReader(filename))
        {
            var graphHeader = ParseGraphHeader(sr.ReadLine());
            nodeCount = Convert.ToInt32(graphHeader[1]);

            while (!sr.EndOfStream)
            {
                input.Add(sr.ReadLine());
            }
        }
        return input;
    }

    /// <summary>
    /// Parses the graph header into 3 parts. GRAPH NODECOUNT EDGECOUNT.
    /// </summary>
    /// <param name="line"></param>
    /// <returns></returns>
    static string[] ParseGraphHeader(string line)
    {
        var graphSettings = line.Split(' ');
        if (graphSettings[0] != "GRAPH")
            return null;

        var edgeCount = graphSettings[2].Split(':');
        graphSettings[2] = edgeCount[0];
        return graphSettings;
    }

    /// <summary>
    /// Writes generated lp to file.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="filename"></param>
    static void WriteToFile(string input, string filename)
    {
        using(StreamWriter sw = new StreamWriter(filename)) 
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
        DirectoryInfo d = new DirectoryInfo(@"..\..\..\Tests"); //Assuming Test is your Folder

        FileInfo[] Files = d.GetFiles("*.txt"); //Getting Text files

        foreach (FileInfo file in Files)
        {
            var filePath = file.FullName;
            var watch = new Stopwatch();

            List<string> input;
            int nodeCount = 0;

            Console.WriteLine($"Testing: {file.FullName}");

            watch.Start();
            input = ReadFromFile(filePath, out nodeCount);
            var lpContructor = new OptimizedLPConstructor(nodeCount);
            WriteToFile(lpContructor.CreateLPModel(input), "lp.txt");


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
            if ( output != "" )
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
                    else if(line.Contains("#OUTPUT"))
                    {
                        add = true;
                        result.Add(line);
                    }
                    else if(add)
                    {
                        result.Add(line);
                    }
                    else if(line.Contains("#OUTPUT END"))
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


        foreach(var summary in finalSummary)
        {
            Console.WriteLine(summary);
        }
    }
}
