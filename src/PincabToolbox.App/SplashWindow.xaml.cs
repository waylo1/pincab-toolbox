using System.Windows;

namespace PincabToolbox.App;

/// <summary>
/// Écran de démarrage (logo MC Automation / Pincab Toolbox).
///
/// <para>
/// Historique — à lire avant de toucher à ce fichier. Le logo de démarrage était à l'origine un item
/// MSBuild <c>&lt;SplashScreen&gt;</c> (17/08), retiré le 18/08 : ce mécanisme fait générer par WPF
/// un <c>Main()</c> qui ouvre une fenêtre native (layered + topmost + sans bordure, en P/Invoke
/// user32/gdi32) AVANT que l'Application et le Dispatcher ne tournent, ce qui a coïncidé avec un
/// blocage dur du « Contrôle intelligent des applications » de Windows 11 sur l'exe non signé.
/// Le commentaire d'avertissement dans <c>PincabToolbox.App.csproj</c> autorise explicitement la
/// seule autre voie : « une fenêtre WPF ordinaire ouverte depuis l'app une fois le runtime démarré ».
/// C'est exactement ce que fait cette classe — aucun P/Invoke, aucun code avant
/// <c>App.OnStartup</c>, aucune transparence (donc aucune fenêtre layered).
/// </para>
///
/// <para>
/// La signature de code, l'autre voie citée par ce commentaire, est définitivement exclue
/// (contrainte permanente, TRANSMISSION.md) — cette implémentation est donc la seule possible.
/// </para>
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }
}
