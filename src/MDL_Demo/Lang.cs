using System.Globalization;

namespace MDL_Demo;

// The app speaks English, Czech and German, like the mdl-demo console. It
// follows the language of Windows; About can switch it until the app closes,
// for trying the translations. Every text is here, its three versions side by
// side. A window reads them when it is built, so a switch rebuilds the window.
// The Details log stays in English: it is wslc's own output, mostly.
public static class Lang
{
    public static readonly string[] Names = ["English", "Čeština", "Deutsch"];
    static readonly string[] codes = ["en", "cs", "de"];

    // An index into Names; English when Windows speaks something else.
    public static int Current { get; set; } =
        Math.Max(0, Array.IndexOf(codes, CultureInfo.CurrentUICulture.TwoLetterISOLanguageName));

    static string T(string en, string cs, string de) => Current switch { 1 => cs, 2 => de, _ => en };

    static string T(string en, string cs, string de, params object[] args) => string.Format(T(en, cs, de), args);

    // Main window
    public static string Demos => T("Demos", "Dema", "Demos");
    public static string RefreshDemos => T("Refresh the demos", "Obnovit seznam dem", "Demos aktualisieren");
    public static string About => T("About", "O aplikaci", "Über");
    public static string Details => T("Details", "Podrobnosti", "Details");
    public static string OpenInBrowser => T("Open in browser", "Otevřít v prohlížeči", "Im Browser öffnen");
    public static string Start => T("Start", "Spustit", "Starten");
    public static string Stop => T("Stop", "Zastavit", "Stoppen");
    public static string DeleteMore => T("Delete…", "Smazat…", "Löschen…");
    public static string Running => T("Running", "Běží", "Läuft");
    public static string Stopped => T("Stopped", "Zastaveno", "Gestoppt");
    public static string OlderVersion => T("· older version", "· starší verze", "· ältere Version");
    public static string OlderVersionHint => T(
        "Created before the latest version was downloaded. New demos get the latest version.",
        "Vytvořeno před stažením nejnovější verze. Nová dema dostanou nejnovější verzi.",
        "Vor dem Herunterladen der neuesten Version erstellt. Neue Demos erhalten die neueste Version.");
    public static string CreateNew => T("+ Create", "+ Vytvořit", "+ Erstellen");

    // Dialog buttons
    public static string Ok => "OK";
    public static string Cancel => T("Cancel", "Zrušit", "Abbrechen");
    public static string Close => T("Close", "Zavřít", "Schließen");
    public static string Create => T("Create", "Vytvořit", "Erstellen");
    public static string Delete => T("Delete", "Smazat", "Löschen");
    public static string Remove => T("Remove", "Odstranit", "Entfernen");
    public static string Switch => T("Switch", "Přepnout", "Wechseln");

    // Status line and messages
    public static string CheckingWsl => T(
        "Checking WSL containers…", "Kontrolují se kontejnery WSL…", "WSL-Container werden geprüft…");
    public static string Refreshing => T("Refreshing…", "Obnovuje se…", "Wird aktualisiert…");
    public static string SomethingWentWrong => T(
        "Something went wrong", "Něco se nepovedlo", "Etwas ist schiefgegangen");
    public static string NoFreePort => T(
        "Could not find a free port for a new demo.",
        "Pro nové demo se nepodařilo najít volný port.",
        "Für eine neue Demo wurde kein freier Port gefunden.");
    public static string CannotCreatePortInUse(int port) => T(
        "The demo cannot be created because port {0} is used by another program on this computer. " +
        "Close that program and try again.",
        "Demo nelze vytvořit, protože port {0} používá jiný program na tomto počítači. " +
        "Zavřete ho a zkuste to znovu.",
        "Die Demo kann nicht erstellt werden, weil Port {0} von einem anderen Programm auf diesem Computer " +
        "verwendet wird. Schließen Sie dieses Programm und versuchen Sie es erneut.", port);
    public static string CannotStartPortInUse(string title, int port) => T(
        "\"{0}\" cannot start because port {1} is used by another program on this computer. " +
        "Close that program and try again.",
        "Demo „{0}“ nelze spustit, protože port {1} používá jiný program na tomto počítači. " +
        "Zavřete ho a zkuste to znovu.",
        "„{0}“ kann nicht gestartet werden, weil Port {1} von einem anderen Programm auf diesem Computer " +
        "verwendet wird. Schließen Sie dieses Programm und versuchen Sie es erneut.", title, port);
    public static string WaitingForDemo => T(
        "Waiting for the demo to start…", "Čeká se na spuštění dema…", "Warten auf den Start der Demo…");
    public static string NewDemo => T("New demo", "Nové demo", "Neue Demo");
    public static string Downloading => T(
        "Downloading the latest version, this can take a few minutes…",
        "Stahuje se nejnovější verze, může to trvat několik minut…",
        "Die neueste Version wird heruntergeladen, das kann einige Minuten dauern…");
    public static string Creating => T("Creating the demo…", "Vytváří se demo…", "Die Demo wird erstellt…");
    public static string Created(string? title) => T(
        "Created \"{0}\". Set up your demo site in the browser.",
        "Demo „{0}“ je vytvořeno. Nastavte si demo stránky v prohlížeči.",
        "„{0}“ wurde erstellt. Richten Sie Ihre Demo-Website im Browser ein.", title ?? "");
    public static string Starting(string title) => T(
        "Starting \"{0}\"…", "Spouští se „{0}“…", "„{0}“ wird gestartet…", title);
    public static string Started(string title) => T(
        "Started \"{0}\".", "Demo „{0}“ je spuštěno.", "„{0}“ wurde gestartet.", title);
    public static string Stopping(string title) => T(
        "Stopping \"{0}\"…", "Zastavuje se „{0}“…", "„{0}“ wird gestoppt…", title);
    public static string StoppedKept(string title) => T(
        "Stopped \"{0}\". Its site and data are kept.",
        "Demo „{0}“ je zastaveno. Jeho stránky a data zůstávají.",
        "„{0}“ wurde gestoppt. Website und Daten bleiben erhalten.", title);
    public static string DeleteQuestion(string title) => T(
        "Delete \"{0}\"?", "Smazat „{0}“?", "„{0}“ löschen?", title);
    public static string DeleteWarning => T(
        "This removes its site and all its data.",
        "Odstraní se jeho stránky a všechna data.",
        "Die Website und alle Daten werden entfernt.");
    public static string Deleting(string title) => T(
        "Deleting \"{0}\"…", "Maže se „{0}“…", "„{0}“ wird gelöscht…", title);
    public static string Deleted(string title) => T(
        "Deleted \"{0}\".", "Demo „{0}“ je smazáno.", "„{0}“ wurde gelöscht.", title);
    public static string FreeDiskSpaceMore => T(
        "Free disk space…", "Uvolnit místo na disku…", "Speicherplatz freigeben…");
    public static string FreeDiskSpaceHint => T(
        "Remove downloaded versions that no demo uses.",
        "Odstraní stažené verze, které žádné demo nepoužívá.",
        "Entfernt heruntergeladene Versionen, die keine Demo verwendet.");
    public static string FreeDiskSpaceQuestion => T(
        "Free disk space?", "Uvolnit místo na disku?", "Speicherplatz freigeben?");
    public static string FreeDiskSpaceText => T(
        "Removes downloaded versions that no demo uses. Your demos and the latest version are kept.",
        "Odstraní stažené verze, které žádné demo nepoužívá. Vaše dema a nejnovější verze zůstanou.",
        "Entfernt heruntergeladene Versionen, die keine Demo verwendet. Ihre Demos und die neueste Version " +
        "bleiben erhalten.");
    public static string FreeingDiskSpace => T(
        "Freeing disk space…", "Uvolňuje se místo na disku…", "Speicherplatz wird freigegeben…");
    public static string Done => T("Done.", "Hotovo.", "Fertig.");

    // New demo form; an underscore marks the Alt key of a field.
    public static string NameLabel => T("_Name", "_Název", "_Name");
    public static string NameHint => T(
        "Optional. Shown at the top of the demo and used as the Moodle site name.",
        "Nepovinné. Zobrazí se v záhlaví dema a použije se jako název stránek Moodle.",
        "Optional. Wird oben in der Demo angezeigt und als Name der Moodle-Website verwendet.");
    public static string DownloadFirst => T(
        "_Download the latest version first",
        "Nejprve _stáhnout nejnovější verzi",
        "Zuerst die neueste Version _herunterladen");
    public static string DownloadHint => T(
        "Recommended. Uncheck to use the version already on this computer.",
        "Doporučeno. Zrušte zaškrtnutí, chcete-li použít verzi, která už je v tomto počítači.",
        "Empfohlen. Deaktivieren, um die Version zu verwenden, die bereits auf diesem Computer ist.");
    public static string FirstDownload => T(
        "The first demo downloads it, this can take a few minutes.",
        "První demo ji stáhne, může to trvat několik minut.",
        "Die erste Demo lädt sie herunter, das kann einige Minuten dauern.");
    public static string OpenWhenReady => T(
        "_Open in the browser when it is ready",
        "Po dokončení _otevřít v prohlížeči",
        "Im Browser _öffnen, sobald sie bereit ist");
    public static string NameClash(string name) => T(
        "You already have a demo called \"{0}\". Pick another name.",
        "Demo s názvem „{0}“ už máte. Zvolte jiný název.",
        "Sie haben bereits eine Demo namens „{0}“. Wählen Sie einen anderen Namen.", name);

    // About
    public static string Version(string? version) => T("Version {0}", "Verze {0}", "Version {0}", version ?? "");
    public static string Description => T(
        "Run throwaway Moodle/MuTMS demo sites on Windows, in a container.",
        "Spouštějte si na Windows odhoditelné demo stránky Moodle/MuTMS v kontejneru.",
        "Wegwerf-Demo-Websites für Moodle/MuTMS unter Windows ausführen, im Container.");
    public static string License => T("MIT License", "Licence MIT", "MIT-Lizenz");
    public static string LanguageMore => T("Language…", "Jazyk…", "Sprache…");
    public static string TestLanguage => T(
        "Test another language", "Vyzkoušet jiný jazyk", "Andere Sprache testen");
    public static string TestLanguageText => T(
        "MDL Demo uses the language of Windows. The language you pick here lasts until you close the app.",
        "MDL Demo používá jazyk Windows. Jazyk vybraný zde platí do zavření aplikace.",
        "MDL Demo verwendet die Sprache von Windows. Die hier gewählte Sprache gilt, bis Sie die App schließen.");

    // WSL problems
    public static string WslNotInstalled => T(
        "MDL Demo needs the Windows Subsystem for Linux (WSL), a free part of Windows from Microsoft. " +
        "To install it, open Terminal as administrator and run:  wsl --install --no-distribution  " +
        "then restart the computer and open MDL Demo again.",
        "MDL Demo potřebuje Subsystém Windows pro Linux (WSL), bezplatnou součást Windows od Microsoftu. " +
        "Chcete-li ho nainstalovat, otevřete Terminál jako správce a spusťte:  wsl --install --no-distribution  " +
        "poté restartujte počítač a znovu otevřete MDL Demo.",
        "MDL Demo benötigt das Windows-Subsystem für Linux (WSL), einen kostenlosen Bestandteil von Windows " +
        "von Microsoft. Zur Installation öffnen Sie das Terminal als Administrator und führen Sie aus:  " +
        "wsl --install --no-distribution  Starten Sie danach den Computer neu und öffnen Sie MDL Demo erneut.");
    public static string WslTooOld => T(
        "MDL Demo needs a newer version of the Windows Subsystem for Linux (WSL), with WSL containers. " +
        "To update it, open Terminal and run:  wsl --update  then open MDL Demo again.",
        "MDL Demo potřebuje novější verzi Subsystému Windows pro Linux (WSL) s kontejnery WSL. " +
        "Chcete-li ho aktualizovat, otevřete Terminál a spusťte:  wsl --update  poté znovu otevřete MDL Demo.",
        "MDL Demo benötigt eine neuere Version des Windows-Subsystems für Linux (WSL) mit WSL-Containern. " +
        "Zum Aktualisieren öffnen Sie das Terminal und führen Sie aus:  wsl --update  " +
        "Öffnen Sie danach MDL Demo erneut.");
    public static string WslNotResponding => T(
        "The Windows Subsystem for Linux (WSL) is installed but not responding. " +
        "Open Terminal and run:  wsl --update  then click the refresh button.",
        "Subsystém Windows pro Linux (WSL) je nainstalován, ale neodpovídá. " +
        "Otevřete Terminál a spusťte:  wsl --update  poté klikněte na tlačítko pro obnovení.",
        "Das Windows-Subsystem für Linux (WSL) ist installiert, reagiert aber nicht. " +
        "Öffnen Sie das Terminal und führen Sie aus:  wsl --update  " +
        "Klicken Sie danach auf die Schaltfläche zum Aktualisieren.");
    public static string HowToInstallWsl => T(
        "How to install WSL (Microsoft)", "Jak nainstalovat WSL (Microsoft)", "WSL installieren (Microsoft)");
    public static string AboutWslContainers => T(
        "About WSL containers (Microsoft)", "O kontejnerech WSL (Microsoft)", "Über WSL-Container (Microsoft)");
    public static string AlreadyExists(string name) => T(
        "{0} already exists.", "{0} už existuje.", "{0} ist bereits vorhanden.", name);
    public static string WslcFailed(int exitCode) => T(
        "wslc failed (exit code {0}).",
        "Příkaz wslc selhal (návratový kód {0}).",
        "wslc ist fehlgeschlagen (Exitcode {0}).", exitCode);
}
