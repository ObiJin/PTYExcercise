using System;
using System.Collections.Generic;
using System.Linq;

namespace EjercicioCelerative
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello PYT Technologies!");
            Console.WriteLine("Here you have the resolution to exercises \"1.d\"(1) and \"2.3\"(2)");
            Console.WriteLine("Select 1 or 2 to execute, or anything else to exit.");
            var selection = Console.ReadLine();

            switch (selection)
            {
                case "1":
                    var exercise = new GetInformation();
                    Console.WriteLine("Getting all records for CustomerID = 1...");
                    var information = exercise.GetXInformation(new Filter() { EntryID = null, CustomerID = "1" });

                    information.ForEach(x => { Console.WriteLine($"CustomerID: {x.CustomerID} EntryID: {x.EntryID} Information: {x.Information}"); });
                    break;
                case "2":
                    var maze = Maze.Randomize(25, 4, 100, true);
                    var mouses = maze.PredictMousesOut(5);
                    Console.WriteLine($"{mouses} mouses got out.");
                    break;
                default:
                    Console.WriteLine("No valid seleccion.");
                    break;
            }

            Console.Write("END");
            Console.ReadKey();
        }
    }

    #region GetXInformation
    class GetInformation
    {
        public List<XInformationOutput> GetXInformation(IXInfoFilter filter)
        {
            return DBService.Instance.GetTable("MXM", "Customer").Where(x => (filter.EntryID == null || x.EntryID == filter.EntryID)
                                                                            && (filter.CustomerID == null || x.CustomerID == filter.CustomerID)
                                                                        ).ToList();
        }
    }

    class DBService
    {
        private static DBService _instance;

        private DBService() { }

        public static DBService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DBService();
                }
                return _instance;
            }
        }

        public List<XInformationOutput> GetTable(string serverName, string tableName)
        {
            var ret = new List<XInformationOutput>() {
                        new XInformationOutput() { EntryID = "1", CustomerID = "1", Information = "Information0"},
                        new XInformationOutput() { EntryID = "2", CustomerID = "1", Information = "Information1"},
                        new XInformationOutput() { EntryID = "3", CustomerID = "2", Information = "Information2"},
                        new XInformationOutput() { EntryID = "4", CustomerID = "2", Information = "Information3"},
                        new XInformationOutput() { EntryID = "5", CustomerID = "3", Information = "Information4"},
                    };

            return ret;
        }
    }

    class XInformationOutput
    {
        public string EntryID { get; set; }
        public string CustomerID { get; set; }
        public string Information { get; set; }
    }

    interface IXInfoFilter
    {
        string EntryID { get; set; }
        string CustomerID { get; set; }

    }

    class Filter : IXInfoFilter
    {
        public string EntryID { get; set; }
        public string CustomerID { get; set; }
    }
    #endregion

    #region Maze
    class Maze
    {
        public List<Cell> Cells;
        private int MousesOut = 0;
        public Maze()
        {
            this.Cells = new List<Cell>();
        }

        public static Maze Randomize(int nodeCount, int branching, int seed, bool randomWeights)
        {
            var rnd = new Random(seed);
            var map = new Maze();
            bool exit = false, hasExit = false;

            for (int i = 0; i < nodeCount; i++)
            {
                if (!exit && !hasExit)
                {
                    exit = true;
                    hasExit = true;
                }

                var newNode = Cell.GetRandom(rnd, exit);
                map.Cells.Add(newNode);

                exit = false;
            }

            foreach (var cell in map.Cells)
                cell.ConnectClosestNodes(map.Cells, branching, rnd, randomWeights);

            foreach (var cell in map.Cells)
            {
                System.Diagnostics.Debug.WriteLine($"{cell}");
                foreach (var cnn in cell.Passages)
                {
                    System.Diagnostics.Debug.WriteLine($"{cnn}");
                }
            }
            return map;
        }

        public int PredictMousesOut(double timeLimit)
        {
            this.Cells.ForEach(c => AstarSearch(c, timeLimit));

            return this.MousesOut;
        }

        private void AstarSearch(Cell start, double timeLimit)
        {
            var exitNode = this.Cells.Where(c => c.IsExit).Single();
            var searchQueue = new List<Cell>();
            searchQueue.Add(exitNode);
            do
            {
                searchQueue = searchQueue.OrderBy(x => x.DistanceTo(start)).ToList();
                var currentCell = searchQueue.First();
                searchQueue.Remove(currentCell);
                foreach (var passage in currentCell.Passages.OrderBy(x => x.TimePenalty))
                {
                    var nextCell = passage.ConnectedCells.Where(c => c != currentCell).Single();

                    if (nextCell.Visited)
                        continue;

                    if (nextCell.TotalTimeToExit == null ||
                            (currentCell.TotalTimeToExit + passage.TimePenalty < nextCell.TotalTimeToExit
                            && currentCell.TotalTimeToExit + passage.TimePenalty < timeLimit)
                        )
                    {
                        nextCell.TotalTimeToExit = currentCell.TotalTimeToExit + passage.TimePenalty;
                        if (!searchQueue.Contains(nextCell))
                            searchQueue.Add(nextCell);
                    }
                }
                currentCell.Visited = true;
                if (currentCell == start)
                {
                    this.MousesOut++;
                    return;
                }
            } while (searchQueue.Any());
        }
    }

    class Cell
    {
        public int X;
        public int Y;
        public bool IsExit { get; set; }
        public List<Passage> Passages { get; set; }
        public bool Visited;
        public double? TotalTimeToExit { get; set; }

        public Cell(bool isExit, int x, int y)
        {
            this.X = x;
            this.Y = y;
            this.IsExit = isExit;
            this.Passages = new List<Passage>();
            Visited = false;
        }

        internal static Cell GetRandom(Random rnd, bool isExit)
        {
            return new Cell(isExit, rnd.Next(), rnd.Next());
        }

        internal void ConnectClosestNodes(List<Cell> cells, int branching, Random rnd, bool randomWeight)
        {
            var connections = new List<Passage>();
            foreach (var cell in cells)
            {
                if (cell == this)
                    continue;

                var dist = Math.Sqrt(Math.Pow(X - cell.X, 2) + Math.Pow(Y - cell.Y, 2));
                connections.Add(new Passage(timePenalty: randomWeight ? rnd.NextDouble() : dist,
                                            connectedCells: new List<Cell>() { this, cell })
                                );
            }
            connections = connections.OrderBy(x => x.TimePenalty).ToList();
            var count = 0;
            foreach (var cnn in connections)
            {
                cnn.ConnectedCells.ForEach(c => c.Passages.Add(cnn));
                count+= cnn.ConnectedCells.Count;
                if (count == branching)
                    return;
            }
        }

        public double DistanceTo(Cell cell)
        {
            var a = cell.X - this.X;
            var b = cell.Y - this.Y;

            return Math.Sqrt(a * a + b * b);
        }
    }

    class Passage
    {
        public Passage(double timePenalty, List<Cell> connectedCells)
        {
            this.TimePenalty = timePenalty;
            this.ConnectedCells = connectedCells;
        }
        public double TimePenalty;
        public List<Cell> ConnectedCells;
    }
    #endregion
}