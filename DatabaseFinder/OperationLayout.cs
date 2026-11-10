namespace DatabaseFinder;

internal static class OperationLayout
{
    public static void Build(
        Form form,
        Label title,
        TextBox destination,
        Button browse,
        Label? hint,
        TreeView tree,
        Control? treeToolbar,
        Control[] selectionActions,
        GroupBox? locked,
        Label logLabel,
        TextBox log,
        Label status,
        ProgressBar? progress = null,
        params Button[] actions)
    {
        form.SuspendLayout();
        form.Controls.Clear();
        form.ClientSize = new Size(1020, 680);
        form.MinimumSize = new Size(840, 580);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.BackColor = UiTheme.Canvas;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12, 10, 12, 10),
            ColumnCount = 1,
            RowCount = 4,
            BackColor = UiTheme.Canvas
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 0: Top Header & Destination Card
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));          // Row 1: Middle (Tree on Left, Actions on Right)
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 115));         // Row 2: Live Log Terminal Card
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));             // Row 3: Bottom Action Card

        // --- CARD 1: Top Header & Destination Card ---
        var topCard = new ModernCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(14, 10, 14, 10),
            CornerRadius = 8
        };

        title.Font = UiFont.Make(14.5f, FontStyle.Bold);
        title.ForeColor = UiTheme.Accent;
        title.AutoSize = true;
        title.Margin = new Padding(0, 0, 0, 6);

        var destLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            Padding = Padding.Empty,
            RightToLeft = RightToLeft.No,
            BackColor = UiTheme.Surface
        };
        destLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        destLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        destLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        destLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        destination.Dock = DockStyle.Fill;
        destination.Margin = new Padding(4, 3, 4, 3);
        destination.Font = UiFont.Make(9.5f);

        browse.AutoSize = true;
        browse.MinimumSize = new Size(115, 32);
        browse.Margin = new Padding(4, 2, 4, 2);
        browse.Font = UiFont.Make(9.5f, FontStyle.Bold);

        destLayout.Controls.Add(destination, 0, 0);
        destLayout.Controls.Add(browse, 1, 0);

        if (hint != null)
        {
            hint.AutoSize = true;
            hint.MaximumSize = new Size(920, 0);
            hint.Padding = new Padding(4, 4, 4, 0);
            hint.ForeColor = UiTheme.Muted;
            hint.Font = UiFont.Make(8.5f);
            destLayout.Controls.Add(hint, 0, 1);
            destLayout.SetColumnSpan(hint, 2);
        }

        var topStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = UiTheme.Surface
        };
        topStack.Controls.Add(title);
        topStack.Controls.Add(destLayout);
        topCard.Controls.Add(topStack);
        root.Controls.Add(topCard, 0, 0);

        // --- ROW 1: Middle Section (Tree on Left, Actions on Right) ---
        var middle = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 10),
            RightToLeft = RightToLeft.No,
            BackColor = UiTheme.Canvas
        };
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 365));

        // Left Card: TreeView
        var treeCard = new ModernCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 10, 0),
            Padding = new Padding(12),
            CornerRadius = 8
        };
        if (treeToolbar != null)
        {
            treeToolbar.Dock = DockStyle.Top;
            treeCard.Controls.Add(treeToolbar);
        }
        tree.Dock = DockStyle.Fill;
        tree.BorderStyle = BorderStyle.None;
        tree.RightToLeft = RightToLeft.No;
        tree.ItemHeight = 30;
        treeCard.Controls.Add(tree);
        if (treeToolbar != null)
        {
            treeToolbar.BringToFront();
        }
        middle.Controls.Add(treeCard, 0, 0);

        // Right Side: Actions & Locked Options
        var sideCard = new ModernCard
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(10),
            CornerRadius = 8
        };
        var sideStack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No,
            BackColor = UiTheme.Surface
        };

        // Selection buttons (Select All / Clear All)
        var selRow = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = UiTheme.Surface
        };
        if (selectionActions.Length >= 2)
        {
            var bAll = selectionActions[0];
            var bClear = selectionActions[1];
            bAll.Size = new Size(150, 30);
            bClear.Size = new Size(150, 30);
            selRow.Controls.Add(bAll);
            selRow.Controls.Add(bClear);
            sideStack.Controls.Add(selRow);
        }

        // Diagnostic / Tool buttons
        for (int i = 2; i < selectionActions.Length; i++)
        {
            var btn = selectionActions[i];
            btn.Size = new Size(310, 30);
            btn.Margin = new Padding(0, 1, 0, 1);
            sideStack.Controls.Add(btn);
        }

        // Locked / Options group
        if (locked != null)
        {
            locked.AutoSize = false;
            locked.Width = 330;
            int maxY = 0;
            foreach (Control c in locked.Controls)
            {
                if (c.Bottom > maxY) maxY = c.Bottom;
            }
            locked.Height = Math.Max(115, maxY + 14);
            locked.Margin = new Padding(0, 6, 0, 0);
            locked.Font = UiFont.Make(9f, FontStyle.Bold);

            foreach (Control c in locked.Controls)
            {
                c.Font = UiFont.Make(8.2f);
                if (c is RadioButton or CheckBox)
                {
                    c.Width = 315;
                }
                else if (c is Label lbl)
                {
                    lbl.Width = 310;
                    lbl.MaximumSize = new Size(310, 0);
                    lbl.ForeColor = UiTheme.Muted;
                }
            }
            sideStack.Controls.Add(locked);
        }

        sideCard.Controls.Add(sideStack);
        middle.Controls.Add(sideCard, 1, 0);
        root.Controls.Add(middle, 0, 1);

        // --- ROW 2: Live Log Terminal Card ---
        var logCard = new ModernCard
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 10),
            Padding = new Padding(10, 8, 10, 8),
            CornerRadius = 8,
            BackColor = UiTheme.Dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(24, 30, 42),
            BorderColor = UiTheme.Dark ? Color.FromArgb(51, 65, 85) : Color.FromArgb(45, 55, 72)
        };

        var logHeader = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 26,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
        };
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        logHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        logLabel.Dock = DockStyle.Fill;
        logLabel.Font = UiFont.Make(8.5f, FontStyle.Bold);
        logLabel.ForeColor = Color.FromArgb(148, 163, 184);
        logLabel.BackColor = Color.Transparent;
        logHeader.Controls.Add(logLabel, 0, 0);

        var btnCopyLog = new Button
        {
            Text = L.IsFa ? "کپی لاگ" : "Copy Log",
            AutoSize = true,
            Height = 24,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(148, 163, 184),
            BackColor = Color.Transparent,
            Font = UiFont.Make(8f),
            Cursor = Cursors.Hand
        };
        btnCopyLog.FlatAppearance.BorderSize = 0;
        btnCopyLog.Click += (_, _) =>
        {
            if (!string.IsNullOrEmpty(log.Text))
            {
                Clipboard.SetText(log.Text);
                btnCopyLog.Text = L.IsFa ? "کپی شد ✓" : "Copied ✓";
                Task.Delay(1500).ContinueWith(_ =>
                {
                    if (!btnCopyLog.IsDisposed)
                        btnCopyLog.Invoke(() => btnCopyLog.Text = L.IsFa ? "کپی لاگ" : "Copy Log");
                });
            }
        };
        logHeader.Controls.Add(btnCopyLog, 1, 0);

        log.Dock = DockStyle.Fill;
        log.BorderStyle = BorderStyle.None;
        log.BackColor = UiTheme.Dark ? Color.FromArgb(15, 23, 42) : Color.FromArgb(24, 30, 42);
        log.ForeColor = Color.FromArgb(226, 232, 240);
        log.Font = new Font("Consolas", 9f);

        logCard.Controls.Add(log);
        logCard.Controls.Add(logHeader);
        root.Controls.Add(logCard, 0, 2);

        // --- ROW 3: Bottom Action Card ---
        var actionCard = new ModernCard
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(10, 8, 10, 8),
            CornerRadius = 8,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink
        };
        var bottomGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = UiTheme.Surface,
            RightToLeft = L.IsFa ? RightToLeft.Yes : RightToLeft.No
        };
        bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Status & Progress (Leading edge: Left in LTR, Right in RTL)
        var statusPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = UiTheme.Surface,
            Margin = new Padding(0, 0, 10, 0)
        };
        if (progress != null)
        {
            progress.Height = 12;
            progress.Width = 240;
            progress.Margin = new Padding(0, 0, 0, 3);
            statusPanel.Controls.Add(progress);
        }
        status.AutoSize = true;
        status.Font = UiFont.Make(8.8f);
        status.ForeColor = UiTheme.Muted;
        status.BackColor = UiTheme.Surface;
        statusPanel.Controls.Add(status);
        bottomGrid.Controls.Add(statusPanel, 0, 0);

        // Action Buttons (Trailing edge: Right in LTR, Left in RTL)
        var actionFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = L.IsFa ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = UiTheme.Surface
        };
        foreach (var b in actions)
        {
            b.AutoSize = true;
            b.MinimumSize = new Size(95, 34);
            b.Height = 34;
            b.Padding = new Padding(8, 0, 8, 0);
            b.Margin = new Padding(3, 2, 3, 2);
            b.Font = UiFont.Make(9f);
            actionFlow.Controls.Add(b);
        }
        if (actions.Length > 0)
        {
            actions[0].Tag = "primary";
            actions[0].MinimumSize = new Size(115, 34);
            actions[0].Font = UiFont.Make(9.5f, FontStyle.Bold);
        }

        bottomGrid.Controls.Add(actionFlow, 1, 0);
        actionCard.Controls.Add(bottomGrid);
        root.Controls.Add(actionCard, 0, 3);

        form.Controls.Add(root);
        UiTheme.Apply(form);
        form.ResumeLayout(true);
    }
}
