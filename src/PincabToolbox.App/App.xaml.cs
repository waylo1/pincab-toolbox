using System;
using System.Windows;
using System.Windows.Threading;

namespace PincabToolbox.App;

public partial class App : Application
{
    /// <summary>Durée d'affichage du logo de démarrage. Assez court pour ne jamais gêner.</summary>
    private static readonly TimeSpan SplashDuration = TimeSpan.FromMilliseconds(1600);

    private SplashWindow? _splash;
    private DispatcherTimer? _splashTimer;

    /// <summary>
    /// Affiche le logo de démarrage, puis laisse WPF ouvrir <c>MainWindow</c> normalement
    /// (<c>StartupUri</c>, inchangé). Voir <see cref="SplashWindow"/> pour l'historique : ce logo
    /// ne doit JAMAIS revenir sous forme d'item MSBuild <c>&lt;SplashScreen&gt;</c>.
    /// </summary>
    protected override void OnStartup(StartupEventArgs e)
    {
        // Pendant que le splash est la seule fenêtre ouverte, sa fermeture ne doit pas pouvoir
        // arrêter l'application. On neutralise donc l'arrêt automatique le temps du démarrage,
        // et on rétablit le comportement WPF par défaut dès que le splash est fermé.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        try
        {
            _splash = new SplashWindow();
            _splash.Show();
        }
        catch
        {
            // Un logo de démarrage ne vaut pas un crash au lancement : si l'image ou la fenêtre
            // échoue pour une raison quelconque, l'application continue sans splash.
            _splash = null;
        }

        // base.OnStartup ne fait que lever l'évènement Startup ; c'est APRÈS son retour que WPF
        // crée la fenêtre de StartupUri. Le timer ci-dessous ne peut donc se déclencher qu'une
        // fois MainWindow réellement ouverte, jamais avant.
        base.OnStartup(e);

        if (_splash is null)
        {
            RestoreNormalShutdown();
            return;
        }

        _splashTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher)
        {
            Interval = SplashDuration,
        };
        _splashTimer.Tick += (_, _) => CloseSplash();
        _splashTimer.Start();
    }

    private void CloseSplash()
    {
        if (_splashTimer is not null)
        {
            _splashTimer.Stop();
            _splashTimer = null;
        }

        if (_splash is not null)
        {
            var splash = _splash;
            _splash = null;
            try
            {
                splash.Close();
            }
            catch
            {
                // Fermeture best-effort : jamais bloquante pour l'utilisateur.
            }
        }

        // Le splash étant la première fenêtre créée, WPF lui a affecté Application.MainWindow.
        // On rend ce rôle à la vraie fenêtre principale et on lui donne le focus.
        foreach (Window window in Windows)
        {
            if (window is MainWindow main)
            {
                MainWindow = main;
                try
                {
                    main.Activate();
                }
                catch
                {
                    // Activation best-effort.
                }
                break;
            }
        }

        RestoreNormalShutdown();
    }

    /// <summary>
    /// Rétablit le comportement d'arrêt par défaut de WPF (<c>OnLastWindowClose</c>). Volontairement
    /// pas <c>OnMainWindowClose</c> : si MainWindow était introuvable, l'application ne pourrait
    /// plus jamais se fermer.
    /// </summary>
    private void RestoreNormalShutdown() => ShutdownMode = ShutdownMode.OnLastWindowClose;
}
