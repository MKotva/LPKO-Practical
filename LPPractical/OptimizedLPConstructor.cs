namespace LPPractical
{
    public class OptimizedLPConstructor
    {
        private struct Edge
        {
            public int IdFrom;
            public int IdTo;
        }

        private int _nodeCount;
        private List<int> _dominators;
        private List<Edge> _dominated;
        private int[,] _adjencyMatrix;

        public OptimizedLPConstructor(int nodeCount)
        {
            _nodeCount = nodeCount;
            _dominators = new List<int>();
            _dominated = new List<Edge>();

            _adjencyMatrix = new int[nodeCount, nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (i != j)
                        _adjencyMatrix[i, j] = 1;
                }
            }
        }

        /// <summary>
        /// Creates lp from given data.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public string CreateLPModel(List<string> edges)
        {
            InitMatrix(edges);
            SetDominations();

            string output = GetDominationsSet();
            output += GetIndependentSet();
            output += GetEdgeSet();

            //Variables
            output += "var Results{Independent} binary;\n";
            output += "var Matrix{Independent,Independent} binary;\n\n"; //Matrix pro všechny hrany mezi Independent vrcholy.

            //PF
            output += "minimize Colors: sum{x in Independent} Results[x];\n";

            //Constraits
            output += "s.t. c1 {x in Independent}: sum{y in Independent} Matrix[x,y] = 1;\n" + //Pro libovolný vrchol i existuje právě jeden vrchol se kterým má hranu. 
                      "s.t. c2 {x in Independent, y in Independent: x > y }: Matrix[x,y] = 0;\n" +
                      "s.t. c3 {(x,y) in Edges, z in Independent}: Matrix[x,y] = 0;\n" +
                      "s.t. c4 {(x,y) in Edges, z in Independent}: Matrix[x,z] + Matrix[y,z] <= Results[z];\n\n"; //Pokud je i a j dominováno k tak mají stejnou barvu.

            output += "solve;\n\n";

            //Output handle
            output += "printf \"#OUTPUT: %d\\n\", Colors.val;\n" +
                      "printf {x in Independent, y in Independent: Matrix[x,y] == 1} \"v_%d : %d\\n\", x, y;\n" +
                      "printf {(x, y) in Dominated, z in Independent: Matrix[x,z] == 1} \"v_%d : %d\\n\", y, z;\n" +
                      "printf \"#OUTPUT END\\n\";\n\n";

            //Exit
            output += "end;";
            return output;
        }

        /// <summary>
        /// Initializes matrix with given edges(they are setted to zero, non-edges are setted to 1)
        /// </summary>
        /// <param name="edges"></param>
        void InitMatrix(List<string> edges)
        {
            foreach (string edge in edges)
            {
                var fromTo = ParseEdge(edge);
                var from = Convert.ToInt32(fromTo[0]);
                var to = Convert.ToInt32(fromTo[1]);

                _adjencyMatrix[from, to] = 0;
                _adjencyMatrix[to, from] = 0;
            }
        }

        string[] ParseEdge(string line)
        {
            return line.Split(" -- ");
        }

        /// <summary>
        /// Converts dominated to string representation.
        /// </summary>
        /// <returns></returns>
        string GetDominationsSet()
        {
            string line = "set Dominated := { ";
            for (int i = 0; i < _dominated.Count - 1; i++)
            {
                line += $"({_dominated[i].IdFrom}, {_dominated[i].IdTo}), ";
            }
            line += $"({_dominated[_dominators.Count - 1].IdFrom}, {_dominated[_dominators.Count - 1].IdTo})"
                    + " };\n";
            return line;
        }


        /// <summary>
        /// Converts set of idependent verticies to string representation.
        /// </summary>
        /// <returns></returns>
        string GetIndependentSet()
        {
            string line = "set Independent := { ";
            for (int i = 0; i < _nodeCount - 1; i++)
            {
                if (_dominators.Contains(i))
                    continue;
                line += $"{i}, ";
            }
            line += $"{_nodeCount - 1}" + "};\n";
            return line;
        }

        /// <summary>
        /// Converts set of edges to string representation.
        /// </summary>
        /// <returns></returns>
        string GetEdgeSet()
        {
            string line = "set Edges := { ";
            for (int i = 0; i < _nodeCount; i++)
            {
                for (int j = i + 1; j < _nodeCount; j++)
                {
                    if (_adjencyMatrix[i, j] == 1 && !_dominators.Contains(i) && !_dominators.Contains(j))
                        line += $"({i}, {j}), ";
                }
            }
            line = line.Remove(line.Length - 2, 2);
            line += "};\n";
            return line;
        }

        /// <summary>
        /// Searches for dominating vecticies.
        /// </summary>
        void SetDominations()
        {
            for (int i = 0; i < _nodeCount; i++)
            {
                for (int j = 0; j < _nodeCount; j++)
                {
                    if (IsDominating(i, j))
                    {
                        _dominators.Add(i);
                        _dominated.Add(new Edge() { IdFrom = j, IdTo = i });
                    }
                }
            }
        }
        bool IsDominating(int from, int to)
        {
            if (from == to || _dominators.Contains(from))
            {
                return false;
            }
            for (int i = 0; i < _nodeCount; i++)
            {
                if (_adjencyMatrix[to, i] - _adjencyMatrix[from, i] < 0)
                    return false;
            }
            return true;
        }
    }
}
