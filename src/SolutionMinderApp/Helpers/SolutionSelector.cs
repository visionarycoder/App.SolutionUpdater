namespace vs.Architect.Client.SolutionMinder.ConsoleApp.Helpers;

public class SolutionSelector
{

    private readonly string recentSolutionsOption = Settings.Default.RecentSolutionsOption;
    private readonly string browseForFileOption = Settings.Default.BrowseOption;
    private readonly string exitOption = Settings.Default.ExitOption;
    private readonly int maxRecentSolutions = Settings.Default.MaxRecentSolutions;
    private readonly string solutionExtension = Settings.Default.SolutionExtension;

    private readonly List<string> recentSolutions = Settings.Default.RecentSolutions.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

    public (FileInfo? Selection, SelectionOption Option) SelectSolution()
    {

        while(true)
        {
            var options = new List<string>();
            if(recentSolutions.Count > 0)
            {
                options.Add(recentSolutionsOption);
            }
            options.Add(browseForFileOption);
            options.Add(exitOption);

            var choice = ShowMenu("Select an option:", options);
            switch(options[choice])
            {
                case var selectedOption when selectedOption == recentSolutionsOption && recentSolutions.Count > 0:
                {
                    var (selection, option) = SelectFromRecentSolutions();
                    if(option != SelectionOption.Exit && selection!.HasMatchingExtension(solutionExtension))
                        return (selection, SelectionOption.Select);
                    continue;
                }
                case var selectedOption when selectedOption == browseForFileOption:
                {
                    var (selection, option) = BrowseForFile(solutionExtension);
                    if(option != SelectionOption.Exit && selection!.HasMatchingExtension(solutionExtension))
                    {
                        if(!recentSolutions.Contains(selection!.FullName))
                        {
                            recentSolutions.Insert(0, selection.FullName);
                            if(recentSolutions.Count > maxRecentSolutions)
                                recentSolutions.RemoveAt(recentSolutions.Count - 1);
                            Settings.Default.RecentSolutions = string.Join("|", recentSolutions);
                            Settings.Default.Save();
                        }
                        return (selection, SelectionOption.Select);
                    }
                    break;
                }
                case var option when option == exitOption:
                {
                    return (null, SelectionOption.Exit);
                }
            }
            Console.WriteLine($"Invalid file.");
        }
    }

    private (FileInfo? Selection, SelectionOption Option) SelectFromRecentSolutions()
    {
        var options = new List<string>(recentSolutions) { exitOption };
        var choice = ShowMenu("Recent Solutions:", options);
        if(options[choice] == exitOption)
            return (null, SelectionOption.Exit);
        var selected = new FileInfo(options[choice]);
        return (Selection: selected, Option: SelectionOption.Select);
    }

    private (FileInfo? Selection, SelectionOption selectedOption) BrowseForFile(string fileExtension)
    {

        Console.Write($"Enter a starting folder: ({Environment.CurrentDirectory})");
        var filePattern = $"*{fileExtension}";
        var inputPath = Console.ReadLine();
        if(string.IsNullOrWhiteSpace(inputPath))
            inputPath = Environment.CurrentDirectory;
        var currentDir = new DirectoryInfo(inputPath);
        var options = new List<(string Displayed, DirectoryInfo? Dir, FileInfo? File, SelectionOption Option)>();

        while(true)
        {
            if(currentDir?.Exists != true)
            {
                Console.WriteLine("Invalid directory. Please try again.");
                Console.Write($"Enter the directory path to search for {fileExtension} file: ");
                inputPath = Console.ReadLine() ?? string.Empty;
                currentDir = new DirectoryInfo(inputPath);
                continue;
            }

            if(currentDir.Parent != null)
                options.Add((Displayed: "..", Dir: currentDir.Parent, File: null, Option: SelectionOption.Change));

            foreach(var di in currentDir.GetDirectories("*", SearchOption.TopDirectoryOnly))
                options.Add((Displayed: di.Name, Dir: di, File: null, Option: SelectionOption.Change));

            foreach(var fi in currentDir.GetFiles(filePattern, SearchOption.TopDirectoryOnly))
                options.Add((Displayed: fi.Name, Dir: fi.Directory!, File: fi, Option: SelectionOption.Select));

            options.Add((Displayed: exitOption, Dir: null, File: null, Option: SelectionOption.Exit));

            Console.WriteLine($"Current Directory: {currentDir.FullName}");
            var idx = ShowMenu("Options:", options.Select(i => i.Displayed).ToList());
            switch(options[idx])
            {
                case { Option: SelectionOption.Exit }:
                    return (options[idx].File, options[idx].Option);
                case { Option: SelectionOption.Select }:
                    if(options[idx].File is not null && options[idx].File!.HasMatchingExtension(fileExtension))
                        return (options[idx].File, options[idx].Option);
                    Console.WriteLine("Invalid file.");
                    currentDir = options[idx].Dir;
                    break;
                case { Option: SelectionOption.Change }:
                    currentDir = options[idx].Dir;
                    break;
            }
            options.Clear();
        }
    }

    private static int ShowMenu(string title, List<string> options)
    {
        while(true)
        {
            Console.WriteLine(title);
            var maxNumberWidth = options.Count.ToString().Length;

            for(var i = 0; i < options.Count; i++)
                Console.WriteLine($"{( i + 1 ).ToString().PadLeft(maxNumberWidth)}. {options[i]}");

            if(int.TryParse(Console.ReadLine(), out var selection) && selection > 0 && selection <= options.Count)
                return selection - 1;
            Console.WriteLine("Invalid selection.");
        }
    }

    public enum SelectionOption
    {
        Change,
        Select,
        Exit
    }

}