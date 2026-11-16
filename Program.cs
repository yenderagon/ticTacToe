// Biblioteka Avalonia służy do tworzenia okienek (działa na Windows, macOS i Linuksie).
using Avalonia;
using Avalonia.Controls;                       // kontrolki: Window, Button, TextBlock, StackPanel
using Avalonia.Controls.ApplicationLifetimes;  // obsługa cyklu życia aplikacji okienkowej
using Avalonia.Controls.Primitives;            // UniformGrid – siatka o równych komórkach
using Avalonia.Layout;                         // wyrównanie elementów (HorizontalAlignment itd.)
using Avalonia.Media;                          // kolory (Brushes) i czcionki (FontWeight)
using Avalonia.Themes.Fluent;                  // gotowy wygląd kontrolek (motyw Fluent)

// Punkt wejścia programu.
class Program
{
    // [STAThread] jest wymagany przez okienkowe aplikacje – interfejs musi działać w jednym wątku.
    [STAThread]
    static void Main(string[] args)
    {
        OpenGameWindow(args);
    }

    // Otwiera osobne okno z planszą gry w kółko i krzyżyk.
    // Metoda działa, dopóki użytkownik nie zamknie okna.
    static void OpenGameWindow(string[] args)
    {
        AppBuilder.Configure<App>()                  // wskazujemy klasę aplikacji (App, niżej)
            .UsePlatformDetect()                     // automatycznie wybiera system (macOS/Windows/Linux)
            .StartWithClassicDesktopLifetime(args);  // uruchamia aplikację i czeka na zamknięcie okna
    }
}

// Klasa aplikacji – konfiguruje wygląd i decyduje, które okno pokazać na starcie.
class App : Application
{
    // Wywoływane raz przy starcie: ustawiamy motyw graficzny.
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());                                // bez motywu kontrolki byłyby niewidoczne
        RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;   // ciemna wersja motywu
    }

    // Wywoływane, gdy framework jest gotowy – tu tworzymy i ustawiamy główne okno.
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new GameWindow();   // zamknięcie tego okna kończy program
        }
        base.OnFrameworkInitializationCompleted();
    }
}

// Okno gry: zawiera planszę, napis ze stanem gry i przycisk „Nowa gra” oraz całą logikę rozgrywki.
class GameWindow : Window
{
    // Stan planszy: 9 pól numerowanych tak:
    //   0 | 1 | 2
    //   3 | 4 | 5
    //   6 | 7 | 8
    // Każde pole to ' ' (puste), 'O' albo 'X'.
    private readonly char[] board = new char[9];

    // Przyciski odpowiadające polom planszy (ten sam indeks co w tablicy board).
    private readonly Button[] cells = new Button[9];

    // Napis nad planszą: czyj ruch, kto wygrał albo remis.
    private readonly TextBlock status = new TextBlock();

    // Znak gracza, który teraz wykonuje ruch ('O' lub 'X').
    private char currentPlayer;

    // true, gdy gra się skończyła – wtedy kliknięcia w pola są ignorowane.
    private bool gameOver;

    // Wszystkie 8 układów trzech pól, które dają wygraną (indeksy z tablicy board).
    private static readonly int[][] winningLines =
    [
        [0, 1, 2], [3, 4, 5], [6, 7, 8], // wiersze
        [0, 3, 6], [1, 4, 7], [2, 5, 8], // kolumny
        [0, 4, 8], [2, 4, 6],            // przekątne
    ];

    // Konstruktor: buduje wygląd okna i rozpoczyna pierwszą grę.
    public GameWindow()
    {
        // Ustawienia samego okna.
        Title = "Kółko i krzyżyk";
        Width = 380;
        Height = 480;
        CanResize = false;                                          // stały rozmiar okna
        WindowStartupLocation = WindowStartupLocation.CenterScreen; // okno na środku ekranu

        // Siatka 3x3 – każde dodane dziecko trafia do kolejnej komórki (od lewej do prawej, wierszami).
        var grid = new UniformGrid { Rows = 3, Columns = 3, Width = 300, Height = 300 };

        // Tworzymy 9 przycisków – po jednym na każde pole planszy.
        for (int i = 0; i < 9; i++)
        {
            // Kopia licznika dla lambdy: bez niej każdy przycisk "widziałby" końcową wartość i (9).
            int index = i;

            var cell = new Button
            {
                Margin = new Thickness(4),                               // odstęp między polami
                HorizontalAlignment = HorizontalAlignment.Stretch,       // przycisk wypełnia całą komórkę
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center, // znak O/X na środku pola
                VerticalContentAlignment = VerticalAlignment.Center,
                FontSize = 48,
                FontWeight = FontWeight.Bold,
            };

            // Po kliknięciu pola wykonujemy ruch na tym polu.
            cell.Click += (_, _) => MakeMove(index);

            cells[i] = cell;          // zapamiętujemy przycisk, żeby później zmieniać jego treść i kolor
            grid.Children.Add(cell);  // dodajemy przycisk do siatki
        }

        // Wygląd napisu ze stanem gry.
        status.FontSize = 22;
        status.HorizontalAlignment = HorizontalAlignment.Center;

        // Przycisk rozpoczynający grę od nowa.
        var newGameButton = new Button
        {
            Content = "Nowa gra",
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        newGameButton.Click += (_, _) => NewGame();

        // Układamy elementy jeden pod drugim: napis, plansza, przycisk.
        Content = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(20),
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { status, grid, newGameButton },
        };

        // Przygotowujemy pustą planszę do pierwszej gry.
        NewGame();
    }

    // Czyści planszę i ustawia stan początkowy gry.
    private void NewGame()
    {
        for (int i = 0; i < 9; i++)
        {
            board[i] = ' ';                                  // pole puste w logice gry
            cells[i].Content = "";                           // pole puste na ekranie
            cells[i].ClearValue(Button.BackgroundProperty);  // usuwa zielone podświetlenie po poprzedniej wygranej
        }
        currentPlayer = 'O'; // zawsze zaczyna kółko
        gameOver = false;
        status.Text = "Ruch: O";
    }

    // Obsługuje kliknięcie pola o podanym indeksie (0–8).
    private void MakeMove(int index)
    {
        // Ignorujemy kliknięcie, jeśli gra się skończyła albo pole jest już zajęte.
        if (gameOver || board[index] != ' ')
            return;

        // Stawiamy znak bieżącego gracza – w logice gry i na ekranie.
        board[index] = currentPlayer;
        cells[index].Content = currentPlayer.ToString();
        cells[index].Foreground = currentPlayer == 'O' ? Brushes.RoyalBlue : Brushes.Crimson; // O niebieskie, X czerwone

        // Sprawdzamy, czy ten ruch dał wygraną.
        int[]? line = FindWinningLine();
        if (line != null)
        {
            gameOver = true;
            status.Text = $"Wygrywa {currentPlayer}!";
            foreach (int i in line)
                cells[i].Background = Brushes.LightGreen; // podświetlamy zwycięską linię
            return;
        }

        // Brak wygranej i brak pustych pól = remis.
        if (!board.Contains(' '))
        {
            gameOver = true;
            status.Text = "Remis!";
            return;
        }

        // Gra toczy się dalej – oddajemy ruch drugiemu graczowi.
        currentPlayer = currentPlayer == 'O' ? 'X' : 'O'; // zmiana znaku po każdym ruchu
        status.Text = $"Ruch: {currentPlayer}";
    }

    // Zwraca trzy indeksy zwycięskiej linii albo null, jeśli nikt jeszcze nie wygrał.
    private int[]? FindWinningLine()
    {
        foreach (var line in winningLines)
        {
            // Linia jest wygrana, gdy pierwsze pole nie jest puste i wszystkie trzy mają ten sam znak.
            char a = board[line[0]];
            if (a != ' ' && a == board[line[1]] && a == board[line[2]])
                return line;
        }
        return null;
    }
}
