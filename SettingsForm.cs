namespace LensLite;

sealed class SettingsForm : Form
{
    readonly TextBox hotkeyBox = new() { ReadOnly = true, Width = 200 };
    readonly ComboBox lang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    readonly CheckBox autoCopy = new() { Text = "Copy recognised text to clipboard", AutoSize = true };
    readonly CheckBox startup = new() { Text = "Start with Windows", AutoSize = true };
    string hotkey;

    static readonly (string Label, string Code)[] Langs =
        { ("Persian + English", "fas+eng"), ("Persian", "fas"), ("English", "eng") };

    /// <param name="apply">Applies the hotkey; returns false if registration failed so the form can stay open.</param>
    public SettingsForm(Settings s, Func<string, bool> apply)
    {
        Text = "LensLite settings";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true; AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Padding = new Padding(12);

        hotkey = s.Hotkey;
        hotkeyBox.Text = hotkey + "   (click, then press new keys)";
        hotkeyBox.KeyDown += (_, e) =>
        {
            e.SuppressKeyPress = true;
            if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin) return;
            if (e.Modifiers == Keys.None) return; // a bare key would hijack typing
            hotkey = Hotkey.Format(e.Modifiers, e.KeyCode);
            hotkeyBox.Text = hotkey;
        };

        foreach (var l in Langs) lang.Items.Add(l.Label);
        lang.SelectedIndex = Math.Max(0, Array.FindIndex(Langs, l => l.Code == s.Languages));
        autoCopy.Checked = s.AutoCopy;
        startup.Checked = Settings.StartWithWindows;

        var ok = new Button { Text = "Save", DialogResult = DialogResult.None, AutoSize = true };
        ok.Click += (_, _) =>
        {
            if (hotkey != s.Hotkey && !apply(hotkey))
            {
                MessageBox.Show(this, $"{hotkey} is already used by another program. Pick a different shortcut.", "LensLite");
                return;
            }
            s.Hotkey = hotkey;
            s.Languages = Langs[lang.SelectedIndex].Code;
            s.AutoCopy = autoCopy.Checked;
            s.Save();
            try { Settings.StartWithWindows = startup.Checked; } catch { /* registry blocked by policy */ }
            Close();
        };

        var t = new TableLayoutPanel { AutoSize = true, ColumnCount = 2 };
        t.Controls.Add(new Label { Text = "Shortcut", AutoSize = true, Anchor = AnchorStyles.Left });
        t.Controls.Add(hotkeyBox);
        t.Controls.Add(new Label { Text = "Languages", AutoSize = true, Anchor = AnchorStyles.Left });
        t.Controls.Add(lang);
        t.Controls.Add(autoCopy); t.SetColumnSpan(autoCopy, 2);
        t.Controls.Add(startup); t.SetColumnSpan(startup, 2);
        t.Controls.Add(ok); t.SetColumnSpan(ok, 2);
        Controls.Add(t);
    }
}
