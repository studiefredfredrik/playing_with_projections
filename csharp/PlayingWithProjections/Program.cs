using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PlayingWithProjections
{
    public class Program
    {
        public static void Main()
        {
            // User input
            var directoryOfJsonFiles = "C:\\Users\\fredrik\\Documents\\playing_with_projections\\data";
            List<string> jsonFiles = Directory.EnumerateFiles(directoryOfJsonFiles).Where(x => x.Contains(".json")).ToList();
            ConsoleTools.WriteColor($"Found {jsonFiles.Count()} files:", ConsoleColor.Green);
            foreach (string file in jsonFiles)
            {
                int index = jsonFiles.IndexOf(file);
                ConsoleTools.WriteColor($"{index}. {file}", ConsoleColor.Gray);
            }
            ConsoleTools.WriteColor($"{jsonFiles.Count()}. All files", ConsoleColor.Gray);
            Console.WriteLine("Please choose a file (by number)");
            string inp = Console.ReadLine();
            int selection = int.Parse(inp);
            string[] selectedFile;
            if (selection == jsonFiles.Count()) selectedFile = new string[] { directoryOfJsonFiles };
            else selectedFile = new string[] { jsonFiles[selection] };
            

            ConsoleTools.WriteColor("\nAvailable projections are:", ConsoleColor.Green);
            int pIndex = 0;
            ConsoleTools.WriteColor($"{pIndex}. Count", ConsoleColor.Gray); pIndex++;
            ConsoleTools.WriteColor($"{pIndex}. HowManyPlayersHaveRegistered", ConsoleColor.Gray); pIndex++;
            ConsoleTools.WriteColor($"{pIndex}. HowManyPlayersHaveRegisteredEachMonth", ConsoleColor.Gray); pIndex++;
            ConsoleTools.WriteColor($"{pIndex}. TrendingGames", ConsoleColor.Gray); pIndex++;
            ConsoleTools.WriteColor($"{pIndex}. ActivePlayers", ConsoleColor.Gray); pIndex++;


            Console.WriteLine("Please choose a projection (by number)");
            int selectedProjection = int.Parse(Console.ReadLine());

            var events = GetEvents(selectedFile);

            // Actions
            if (selectedProjection == 0) Console.WriteLine("Number of events: {0}", EventCounter.Count(events));
            if (selectedProjection == 1) Console.WriteLine("Players registered: {0}", EventCounter.HowManyPlayersHaveRegistered(events));
            if (selectedProjection == 2)
            {
                Dictionary<string, int> dict = EventCounter.HowManyPlayersHaveRegisteredEachMonth(events);
                Console.WriteLine($"Month \t\t Number");
                foreach (KeyValuePair<string, int> kvp in dict)
                {
                    Console.WriteLine($"{kvp.Key, 0}  {kvp.Value, 20}");
                }
            }
            if (selectedProjection == 3)
            {
                IOrderedEnumerable<KeyValuePair<string, int>> dict = EventCounter.TrendingGames(events);
                Console.WriteLine($"Game ID \t\t Plays");
                foreach (KeyValuePair<string, int> kvp in dict)
                {
                    Console.WriteLine($"{kvp.Key,0}  {kvp.Value,20}");
                }
            }
            if (selectedProjection == 4)
            {
                List<string> activePlayerList = EventCounter.ActivePlayers(events);
                Console.WriteLine("Currently Active players");
                activePlayerList.ForEach(x => Console.WriteLine(x));
            }

            Console.ReadKey();
        }

        private static IEnumerable<Event> GetEvents(string[] args)
        {
            return FileNames(args)
                .Select(file => new FileEventReader().Read(file))
                .Select(events => new JsonEventParser().Parse(events))
                .Aggregate((accu, events) => accu.Union(events));
        }

        private static IEnumerable<string> FileNames(string[] args)
        {
            if (!args.Any()) throw new ArgumentException("Expected a file or directory as parameter");
            if (args[0].EndsWith(".json")) return new[] {args[0]};

            return Directory.GetFiles(args[0])
                .Where(file => file.EndsWith(".json"));
        }
    }

    public class EventCounter
    {
        public static int Count(IEnumerable<Event> events)
        {
            return events.Count();
        }

        public static int HowManyPlayersHaveRegistered(IEnumerable<Event> events)
        {
            return events.Count(e => e.type == "PlayerHasRegistered");
        }

        public static Dictionary<string, int> HowManyPlayersHaveRegisteredEachMonth(IEnumerable<Event> events)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();


            foreach (Event oEvent in events)
            {
                string key = oEvent.timestamp.Year.ToString() + "-" + oEvent.timestamp.Month.ToString();
                if (oEvent.type != "PlayerHasRegistered") continue;
                if (dict.ContainsKey(key)) dict[key]++;
                else dict[key] = 1;
            }

            return dict;
        }

        public static IOrderedEnumerable<KeyValuePair<string, int>> TrendingGames(IEnumerable<Event> events)
        {
            List<string> gameWasFinished = new List<string>();

            //Find all the game finished
            foreach (Event oEvent in events)
            {
                if (oEvent.type != "GameWasFinished") continue;
                string key = oEvent.payload["game_id"];
                gameWasFinished.Add(key);
            }

            Dictionary<string, int> dictOfQuizIdGameId = new Dictionary<string, int>();

            //Match the game finished with their quiz_id's

            foreach (Event aEvent in events)
            {
                if (aEvent.type != "GameWasOpened") continue;
                string key = aEvent.payload["quiz_id"];

                if (dictOfQuizIdGameId.ContainsKey(key))
                {
                    // Game wasnt finished, doesnt count
                    if (!gameWasFinished.Contains(aEvent.payload["game_id"])) continue;
                    dictOfQuizIdGameId[key]++;
                }
                else
                {
                    // Game wasnt finished, doesnt count
                    if (!gameWasFinished.Contains(aEvent.payload["game_id"])) continue;
                    dictOfQuizIdGameId[key] = 1;
                }

            }
            var sortedDict = from entry in dictOfQuizIdGameId orderby entry.Value descending select entry;

            return sortedDict;
        }

        public static List<string> ActivePlayers(IEnumerable<Event> events)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();

            var listOfValidIDs = events.Where(x => x.type == "GameWasFinished" && x.payload.ContainsKey("game_id")).Select(e => e.payload["game_id"]).ToList();


            foreach (Event oEvent in events)
            {
                if ((oEvent.timestamp.Year == 2016) && oEvent.timestamp.Month == 4)
                    if (oEvent.type != "PlayerJoinedGame") continue;

                if (!oEvent.payload.ContainsKey("game_id")) continue;
                if (!listOfValidIDs.Contains(oEvent.payload["game_id"])) continue;

                if (!oEvent.payload.ContainsKey("player_id")) continue;
                string key = oEvent.payload["player_id"];
                if (dict.ContainsKey(key)) dict[key]++;
                else dict[key] = 1;
            }

            return dict.Where(entry => entry.Value > 5).Select(c => c.Key).ToList();


        }
    }
}