// Timer spegnimento: programma lo spegnimento o il riavvio del PC e la chiusura dei programmi aperti.
// Compilare con build.bat (usa il csc del .NET Framework già presente in Windows, C# 5).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

static class Win32
{
    public delegate bool EnumProc(IntPtr h, IntPtr p);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc f, IntPtr p);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll")] public static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int ExtractIconEx(string file, int index, out IntPtr large, out IntPtr small, int n);

    static readonly string[] Escludi = { "TextInputHost", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost" };

    // chiude tutte le finestre "vere" (visibili, con titolo, non secondarie) con WM_CLOSE, come la X:
    // niente chiusura forzata, i programmi con modifiche non salvate chiedono di salvare
    public static int ChiudiFinestre()
    {
        int mio = Process.GetCurrentProcess().Id, n = 0;
        EnumWindows((h, p) =>
        {
            // GW_OWNER = 4, GWL_EXSTYLE = -20, WS_EX_TOOLWINDOW = 0x80
            if (!IsWindowVisible(h) || GetWindow(h, 4) != IntPtr.Zero || GetWindowTextLength(h) == 0 || (GetWindowLong(h, -20) & 0x80) != 0)
                return true;
            uint pid; GetWindowThreadProcessId(h, out pid);
            if (pid == mio) return true;
            string nome;
            try { nome = Process.GetProcessById((int)pid).ProcessName; } catch { return true; }
            if (Array.IndexOf(Escludi, nome) >= 0) return true;
            // di explorer si chiudono solo le cartelle (CabinetWClass), non barra delle applicazioni e desktop
            if (nome == "explorer")
            {
                var s = new StringBuilder(64); GetClassName(h, s, 64);
                if (s.ToString() != "CabinetWClass") return true;
            }
            PostMessage(h, 0x10, IntPtr.Zero, IntPtr.Zero);
            n++;
            return true;
        }, IntPtr.Zero);
        return n;
    }
}

class Finestra : Form
{
    DateTime? spegniAlle, chiudiAlle;
    bool chiusoPerSpegnimento, esci, riavvioProgrammato;
    ComboBox azioneComputer;

    RadioButton rbSpMin, rbSpOre, rbChMin, rbChOre;
    NumericUpDown numSpMin, numChMin, numSecPrima;
    DateTimePicker oraSp, oraCh;
    CheckBox chkChiudiPrima;
    Label statoSp, statoCh;
    NotifyIcon tray;
    Timer timer = new Timer { Interval = 1000 };

    public Finestra()
    {
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        Font = SystemFonts.MessageBoxFont;
        Text = "Timer spegnimento";
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(410, 506);
        IntPtr grande, piccola;
        if (Win32.ExtractIconEx("shell32.dll", 27, out grande, out piccola, 1) > 0) Icon = Icon.FromHandle(grande);

        // --- spegnimento ---
        var gSp = Gruppo("Spegnimento o riavvio computer", 12, 241);
        Etichetta(gSp, "Azione", 18, 30);
        azioneComputer = new ComboBox { Location = new Point(100, 27), Width = 170, DropDownStyle = ComboBoxStyle.DropDownList };
        azioneComputer.Items.AddRange(new object[] { "Spegni il computer", "Riavvia il computer" });
        azioneComputer.SelectedIndex = 0;
        gSp.Controls.Add(azioneComputer);
        rbSpMin = Radio(gSp, "Tra", 18, 66, true);
        numSpMin = Numero(gSp, 100, 64, 1, 10080, 30, rbSpMin);
        Etichetta(gSp, "minuti", 172, 67);
        rbSpOre = Radio(gSp, "Alle ore", 18, 98, false);
        oraSp = Ora(gSp, 100, 96, rbSpOre);
        chkChiudiPrima = new CheckBox { Text = "Chiudi i programmi", Location = new Point(18, 134), AutoSize = true, Checked = true };
        gSp.Controls.Add(chkChiudiPrima);
        numSecPrima = Numero(gSp, 200, 132, 5, 600, 20, null);
        Etichetta(gSp, "secondi prima", 272, 135);
        Bottone(gSp, "Programma", 18, 168, 170, delegate { ProgrammaSpegnimento(); });
        Bottone(gSp, "Annulla", 198, 168, 170, delegate { AnnullaSpegnimento(); });
        statoSp = Stato(gSp, 208);

        // --- chiusura programmi ---
        var gCh = Gruppo("Chiusura programmi", 266, 228);
        rbChMin = Radio(gCh, "Tra", 18, 30, true);
        numChMin = Numero(gCh, 100, 28, 1, 10080, 30, rbChMin);
        Etichetta(gCh, "minuti", 172, 31);
        rbChOre = Radio(gCh, "Alle ore", 18, 62, false);
        oraCh = Ora(gCh, 100, 60, rbChOre);
        Bottone(gCh, "Programma chiusura", 18, 98, 170, delegate { ProgrammaChiusura(); });
        Bottone(gCh, "Annulla", 198, 98, 170, delegate { chiudiAlle = null; Aggiorna(); });
        Bottone(gCh, "Chiudi tutto adesso", 18, 140, 350, delegate
        {
            if (MessageBox.Show(this, "Chiudere tutte le finestre aperte?\nI programmi con modifiche non salvate chiederanno di salvare.",
                    "Conferma", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                Win32.ChiudiFinestre();
        });
        statoCh = Stato(gCh, 186);

        // --- area di notifica: i timer di chiusura funzionano solo finché il programma è aperto ---
        var menu = new ContextMenuStrip();
        menu.Items.Add("Apri", null, delegate { Mostra(); });
        menu.Items.Add("Esci", null, delegate { esci = true; Close(); });
        tray = new NotifyIcon { Icon = Icon, Text = Text, ContextMenuStrip = menu };
        tray.DoubleClick += delegate { Mostra(); };

        timer.Tick += delegate { Tick(); };
        timer.Start();
        Aggiorna();
    }

    // ---------- costruzione UI ----------
    GroupBox Gruppo(string testo, int y, int h)
    {
        var g = new GroupBox { Text = testo, Location = new Point(12, y), Size = new Size(386, h) };
        Controls.Add(g);
        return g;
    }
    RadioButton Radio(Control p, string testo, int x, int y, bool on)
    {
        var r = new RadioButton { Text = testo, Location = new Point(x, y), AutoSize = true, Checked = on };
        p.Controls.Add(r);
        return r;
    }
    NumericUpDown Numero(Control p, int x, int y, int min, int max, int val, RadioButton seleziona)
    {
        var n = new NumericUpDown { Location = new Point(x, y), Width = 66, Minimum = min, Maximum = max, Value = val, TextAlign = HorizontalAlignment.Right };
        if (seleziona != null) n.Enter += delegate { seleziona.Checked = true; };
        p.Controls.Add(n);
        return n;
    }
    DateTimePicker Ora(Control p, int x, int y, RadioButton seleziona)
    {
        var d = new DateTimePicker { Location = new Point(x, y), Width = 80, Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Value = DateTime.Now.AddHours(1) };
        d.Enter += delegate { seleziona.Checked = true; };
        p.Controls.Add(d);
        return d;
    }
    void Etichetta(Control p, string testo, int x, int y)
    {
        p.Controls.Add(new Label { Text = testo, Location = new Point(x, y), AutoSize = true });
    }
    void Bottone(Control p, string testo, int x, int y, int w, EventHandler click)
    {
        var b = new Button { Text = testo, Location = new Point(x, y), Size = new Size(w, 32) };
        b.Click += click;
        p.Controls.Add(b);
    }
    Label Stato(Control p, int y)
    {
        var l = new Label { Location = new Point(18, y), Size = new Size(350, 22), Font = new Font(Font, FontStyle.Bold) };
        p.Controls.Add(l);
        return l;
    }

    // ---------- logica ----------
    static DateTime Bersaglio(RadioButton minuti, NumericUpDown num, DateTimePicker ora)
    {
        if (minuti.Checked) return DateTime.Now.AddMinutes((double)num.Value);
        var t = DateTime.Today + new TimeSpan(ora.Value.Hour, ora.Value.Minute, 0);
        return t <= DateTime.Now ? t.AddDays(1) : t;   // ora già passata = domani
    }

    static int Shutdown(string args)
    {
        var p = Process.Start(new ProcessStartInfo("shutdown", args) { CreateNoWindow = true, UseShellExecute = false });
        p.WaitForExit();
        return p.ExitCode;
    }

    internal static string ComandoComputer(bool riavvio, int secondi)
    {
        return (riavvio ? "/r /t " : "/s /t ") + secondi;
    }

    void ProgrammaSpegnimento()
    {
        var t = Bersaglio(rbSpMin, numSpMin, oraSp);
        int sec = (int)Math.Ceiling((t - DateTime.Now).TotalSeconds);
        Shutdown("/a");   // sostituisce un eventuale spegnimento già programmato
        bool riavvio = azioneComputer.SelectedIndex == 1;
        int rc = Shutdown(ComandoComputer(riavvio, sec));
        if (rc != 0) { MessageBox.Show(this, "Impossibile programmare " + (riavvio ? "il riavvio" : "lo spegnimento") + " (codice " + rc + ").", Text, MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        riavvioProgrammato = riavvio;
        spegniAlle = t;
        chiusoPerSpegnimento = false;
        Aggiorna();
    }

    void AnnullaSpegnimento()
    {
        bool cera = Shutdown("/a") == 0 || spegniAlle != null;
        spegniAlle = null;
        Aggiorna();
        if (!cera) statoSp.Text = "Nessuno spegnimento o riavvio attivo.";
    }

    void ProgrammaChiusura()
    {
        chiudiAlle = Bersaglio(rbChMin, numChMin, oraCh);
        Aggiorna();
    }

    void Tick()
    {
        var ora = DateTime.Now;
        if (spegniAlle != null && chkChiudiPrima.Checked && !chiusoPerSpegnimento &&
            (spegniAlle.Value - ora).TotalSeconds <= (double)numSecPrima.Value)
        {
            chiusoPerSpegnimento = true;
            Win32.ChiudiFinestre();
        }
        if (spegniAlle != null && ora >= spegniAlle.Value) spegniAlle = null;
        if (chiudiAlle != null && ora >= chiudiAlle.Value)
        {
            chiudiAlle = null;
            Win32.ChiudiFinestre();
            Aggiorna();
            statoCh.Text = "Programmi chiusi alle " + ora.ToString("HH:mm") + ".";
            return;
        }
        Aggiorna();
    }

    static string Mancano(DateTime t)
    {
        var r = t - DateTime.Now;
        if (r < TimeSpan.Zero) r = TimeSpan.Zero;
        return ((int)r.TotalHours).ToString("00") + r.ToString(@"\:mm\:ss");
    }

    void Aggiorna()
    {
        statoSp.ForeColor = spegniAlle != null ? Color.Firebrick : SystemColors.GrayText;
        statoSp.Text = spegniAlle != null
            ? (riavvioProgrammato ? "Riavvio alle " : "Spegnimento alle ") + spegniAlle.Value.ToString("HH:mm") + "  ·  mancano " + Mancano(spegniAlle.Value)
            : "Nessuno spegnimento o riavvio attivo.";
        statoCh.ForeColor = chiudiAlle != null ? Color.DarkOrange : SystemColors.GrayText;
        statoCh.Text = chiudiAlle != null
            ? "Chiusura alle " + chiudiAlle.Value.ToString("HH:mm") + "  ·  mancano " + Mancano(chiudiAlle.Value)
            : "Nessuna chiusura programmata.";
        string t = "Timer spegnimento";
        if (spegniAlle != null) t += "\n" + (riavvioProgrammato ? "Riavvio tra " : "Spegnimento tra ") + Mancano(spegniAlle.Value);
        if (chiudiAlle != null) t += "\nChiusura tra " + Mancano(chiudiAlle.Value);
        tray.Text = t.Length > 63 ? t.Substring(0, 63) : t;
    }

    void Mostra()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        tray.Visible = false;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // con un timer attivo la X riduce a icona nell'area di notifica, altrimenti la chiusura programmata non avverrebbe
        if (!esci && e.CloseReason == CloseReason.UserClosing && (spegniAlle != null || chiudiAlle != null))
        {
            e.Cancel = true;
            Hide();
            tray.Visible = true;
            tray.ShowBalloonTip(3000, "Timer spegnimento", "Il timer continua qui. Clic destro sull'icona per uscire.", ToolTipIcon.Info);
            return;
        }
        tray.Visible = false;
        base.OnFormClosing(e);
    }

    [STAThread]
    static void Main()
    {
        Win32.SetProcessDPIAware();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new Finestra());
    }
}
