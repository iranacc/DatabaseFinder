using System.Data;
using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using MySqlConnector;
using Npgsql;
using StackExchange.Redis;

namespace DatabaseFinder
{
    public class QueryRunnerForm : AppForm
    {
        protected override bool ModernLayout => true;
        private readonly DatabaseInfo _db;
        private readonly TextBox _txtHost;
        private readonly NumericUpDown _numPort;
        private readonly TextBox _txtUser;
        private readonly TextBox _txtPass;
        private readonly CheckBox _chkWinAuth;
        private readonly TextBox _txtDbName;
        private readonly TextBox _txtQuery;
        private readonly Button _btnRun;
        private readonly Button _btnSaveProfile;
        private readonly DataGridView _dgvResult;
        private readonly Label _lblStatus;

        public QueryRunnerForm(DatabaseInfo db)
        {
            _db = db;
            var profile = ProfileManager.Load().FirstOrDefault(p => p.Type == db.Type && p.Host == db.Host && p.Port == (db.Port ?? 0));
            var useWinAuth = db.Type == DatabaseType.SQLServer &&
                (profile == null || string.IsNullOrEmpty(profile.Username));

            Text = L.Format("S263", db.TypeDisplayName);
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 620);
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.White;
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(600, 480);

            // تب‌های اتصال
            var lblTitle = new Label
            {
                Text = L.Format("S264", db.TypeDisplayName, db.Host),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 150, 243),
                AutoSize = true,
                Location = new Point(12, 8)
            };
            Controls.Add(lblTitle);

            var grpConn = new GroupBox
            {
                Text = L.Text("S265"),
                Location = new Point(12, 40),
                Size = new Size(736, 90)
            };

            var lblHost = new Label { Text = L.Text("S266"), Location = new Point(12, 30), AutoSize = true };
            _txtHost = new TextBox { Location = new Point(80, 26), Size = new Size(150, 27), Text = db.Host };
            _txtHost.TextChanged += UpdateTitle;

            var lblPort = new Label { Text = L.Text("S267"), Location = new Point(245, 30), AutoSize = true };
            _numPort = new NumericUpDown
            {
                Location = new Point(300, 26),
                Size = new Size(70, 27),
                Minimum = 0,
                Maximum = 65535,
                Value = db.Port ?? 0
            };
            if ((db.Port ?? 0) == 0) _numPort.Value = GetDefaultPort(db.Type);

            var lblUser = new Label { Text = L.Text("S268"), Location = new Point(390, 30), AutoSize = true };
            _txtUser = new TextBox { Location = new Point(450, 26), Size = new Size(120, 27) };

            var lblPass = new Label { Text = L.Text("S269"), Location = new Point(585, 30), AutoSize = true };
            _txtPass = new TextBox { Location = new Point(635, 26), Size = new Size(90, 27), UseSystemPasswordChar = true };

            _chkWinAuth = new CheckBox
            {
                Text = L.Text("S328"),
                Checked = useWinAuth,
                Visible = db.Type == DatabaseType.SQLServer,
                AutoSize = true,
                Padding = new Padding(6, 24, 0, 0)
            };
            _chkWinAuth.CheckedChanged += (s, e) =>
            {
                _txtUser.Enabled = !_chkWinAuth.Checked;
                _txtPass.Enabled = !_chkWinAuth.Checked;
            };
            _txtUser.Enabled = !useWinAuth;
            _txtPass.Enabled = !useWinAuth;

            var lblDb = new Label { Text = L.Text("S270"), Location = new Point(12, 60), AutoSize = true };
            _txtDbName = new TextBox { Location = new Point(95, 56), Size = new Size(150, 27) };

            grpConn.Controls.AddRange(new Control[] {
                lblHost, _txtHost, lblPort, _numPort, lblUser, _txtUser, lblPass, _txtPass, lblDb, _txtDbName, _chkWinAuth
            });
            Controls.Add(grpConn);

            var lblQuery = new Label
            {
                Text = L.Text("S271"),
                Location = new Point(12, 145),
                AutoSize = true
            };
            Controls.Add(lblQuery);

            _txtQuery = new TextBox
            {
                Multiline = true,
                Location = new Point(12, 170),
                Size = new Size(736, 150),
                Font = new Font("Consolas", 11F),
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Text = GetDefaultQuery(db.Type)
            };
            Controls.Add(_txtQuery);

            _btnRun = new Button
            {
                Text = L.Text("S272"),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Location = new Point(12, 330),
                Size = new Size(90, 35)
            };
            _btnRun.Click += BtnRun_Click;
            Controls.Add(_btnRun);

            _btnSaveProfile = new Button
            {
                Text = L.Text("S094"),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(112, 330),
                Size = new Size(150, 35)
            };
            _btnSaveProfile.Click += BtnSaveProfile_Click;
            Controls.Add(_btnSaveProfile);

            _dgvResult = new DataGridView
            {
                Location = new Point(12, 380),
                Size = new Size(736, 190),
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_dgvResult);

            _lblStatus = new Label
            {
                AutoSize = true,
                Location = new Point(12, 585),
                ForeColor = Color.Gray,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_lblStatus);

            // پر کردن از پروفایل ذخیره‌شده در صورت وجود
            if (profile != null)
            {
                _txtUser.Text = profile.Username;
                _txtPass.Text = profile.Password;
                _txtDbName.Text = profile.DatabaseName;
            }
            var fields=UiTheme.Flow(FormLayout.Field(L.Text("S266"),_txtHost,200),FormLayout.Field(L.Text("S267"),_numPort,90),FormLayout.Field(L.Text("S268"),_txtUser,150),FormLayout.Field(L.Text("S269"),_txtPass,150),FormLayout.Field(L.Text("S270"),_txtDbName,200),_chkWinAuth);
            var work=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3};work.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));work.RowStyles.Add(new RowStyle(SizeType.Absolute,26));work.RowStyles.Add(new RowStyle(SizeType.Percent,40));work.RowStyles.Add(new RowStyle(SizeType.Percent,60));
            work.Controls.Add(lblQuery,0,0);_txtQuery.Dock=DockStyle.Fill;_dgvResult.Dock=DockStyle.Fill;work.Controls.Add(_txtQuery,0,1);work.Controls.Add(_dgvResult,0,2);
            FormLayout.Build(this,lblTitle,fields,work,_lblStatus,_btnRun,_btnSaveProfile);
            ClientSize=new Size(1060,780);MinimumSize=new Size(920,700);
        }

        private static int GetDefaultPort(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.SQLServer: return 1433;
                case DatabaseType.MySQL: return 3306;
                case DatabaseType.MariaDB: return 3307;
                case DatabaseType.PostgreSQL: return 5432;
                case DatabaseType.Oracle: return 1521;
                case DatabaseType.MongoDB: return 27017;
                case DatabaseType.Redis: return 6379;
                case DatabaseType.Elasticsearch: return 9200;
                case DatabaseType.CouchDB: return 5984;
                default: return 0;
            }
        }

        private static string GetDefaultQuery(DatabaseType type)
        {
            switch (type)
            {
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    return "SHOW DATABASES;";
                case DatabaseType.PostgreSQL:
                    return "SELECT datname FROM pg_database;";
                case DatabaseType.SQLServer:
                    return "SELECT name FROM sys.databases;";
                case DatabaseType.SQLite:
                    return "SELECT name FROM sqlite_master WHERE type='table';";
                case DatabaseType.Redis:
                    return "INFO";
                default:
                    return "";
            }
        }

        private void UpdateTitle(object? sender, EventArgs e)
        {
            Text = L.Format("S263", _db.TypeDisplayName);
        }

        private void BtnSaveProfile_Click(object? sender, EventArgs e)
        {
            var profiles = ProfileManager.Load();
            var profile = profiles.FirstOrDefault(p =>
                p.Type == _db.Type && p.Host == _txtHost.Text.Trim() && p.Port == (int)_numPort.Value);

            if (profile == null)
            {
                profile = new DatabaseProfile
                {
                    Name = $"{_db.TypeDisplayName} @ {_txtHost.Text.Trim()}",
                    Type = _db.Type,
                    Host = _txtHost.Text.Trim(),
                    Port = (int)_numPort.Value,
                    Username = _chkWinAuth.Visible && _chkWinAuth.Checked ? "" : _txtUser.Text.Trim(),
                    Password = _chkWinAuth.Visible && _chkWinAuth.Checked ? "" : _txtPass.Text,
                    DatabaseName = _txtDbName.Text.Trim()
                };
                profiles.Add(profile);
            }
            else
            {
                profile.Username = _chkWinAuth.Visible && _chkWinAuth.Checked ? "" : _txtUser.Text.Trim();
                profile.Password = _chkWinAuth.Visible && _chkWinAuth.Checked ? "" : _txtPass.Text;
                profile.DatabaseName = _txtDbName.Text.Trim();
            }

            ProfileManager.Save(profiles);
            _lblStatus.Text = L.Text("S273");
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }

        private async void BtnRun_Click(object? sender, EventArgs e)
        {
            var query = _txtQuery.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                _lblStatus.Text = L.Text("S274");
                return;
            }

            _btnRun.Enabled = false;
            _btnRun.Text = L.Text("S275");
            _lblStatus.Text = "";
            _dgvResult.Columns.Clear();
            _dgvResult.Rows.Clear();

            var host = _txtHost.Text.Trim();
            var port = (int)_numPort.Value;
            var user = _txtUser.Text.Trim();
            var pass = _txtPass.Text;
            var dbName = _txtDbName.Text.Trim();
            var winAuth = _chkWinAuth.Visible && _chkWinAuth.Checked;

            try
            {
                var dt = await Task.Run(() => ExecuteQuery(_db.Type, host, port, user, pass, dbName, query, winAuth, _db.ServerName ?? ""));
                ShowResult(dt, L.Text("S276"));
            }
            catch (Exception ex)
            {
                _lblStatus.Text = L.Text("S277");
                _lblStatus.ForeColor = Color.FromArgb(244, 67, 54);
                MessageBox.Show(L.Format("S278", ex.Message), L.Text("S195"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnRun.Enabled = true;
                _btnRun.Text = L.Text("S272");
            }
        }

        private DataTable ExecuteQuery(DatabaseType type, string host, int port, string user, string pass, string dbName, string query, bool winAuth, string serverName)
        {
            switch (type)
            {
                case DatabaseType.MySQL:
                case DatabaseType.MariaDB:
                    return ExecuteMySql(host, port, user, pass, dbName, query);
                case DatabaseType.PostgreSQL:
                    return ExecutePostgre(host, port, user, pass, dbName, query);
                case DatabaseType.SQLServer:
                    return ExecuteSqlServer(host, port, user, pass, dbName, query, winAuth, serverName);
                case DatabaseType.SQLite:
                    return ExecuteSqlite(dbName, query);
                case DatabaseType.Redis:
                    return ExecuteRedis(host, port, pass, query);
                default:
                    throw new NotSupportedException(L.Format("S279", type));
            }
        }

        private static DataTable ExecuteMySql(string host, int port, string user, string pass, string dbName, string query)
        {
            var csb = new MySqlConnectionStringBuilder
            {
                Server = host,
                Port = (uint)port,
                UserID = user,
                Password = pass,
                Database = string.IsNullOrEmpty(dbName) ? "" : dbName,
                AllowUserVariables = true
            };

            using var conn = new MySqlConnection(csb.ConnectionString);
            conn.Open();
            return FillData(conn, query);
        }

        private static DataTable ExecutePostgre(string host, int port, string user, string pass, string dbName, string query)
        {
            var csb = new NpgsqlConnectionStringBuilder
            {
                Host = host,
                Port = port,
                Username = user,
                Password = pass,
                Database = string.IsNullOrEmpty(dbName) ? "postgres" : dbName
            };

            using var conn = new NpgsqlConnection(csb.ConnectionString);
            conn.Open();
            return FillData(conn, query);
        }

        private static DataTable ExecuteSqlServer(string host, int port, string user, string pass, string dbName, string query, bool winAuth, string serverName)
        {
            if (!winAuth)
            {
                using var conn = new SqlConnection(BuildSqlCs(host, port, user, pass, dbName, false));
                conn.Open();
                return FillData(conn, query);
            }

            var candidates = new List<string>();
            if (!IPAddress.TryParse(host, out _)) candidates.Add(host);
            if (!string.IsNullOrWhiteSpace(serverName)) candidates.Add(serverName);
            var resolved = ResolveForWinAuth(host);
            if (!string.IsNullOrWhiteSpace(resolved)) candidates.Add(resolved);
            if (!candidates.Contains(host)) candidates.Add(host);

            Exception? last = null;
            foreach (var candidate in candidates.Distinct())
            {
                try
                {
                    using var conn = new SqlConnection(BuildSqlCs(candidate, port, "", "", dbName, true));
                    conn.Open();
                    return FillData(conn, query);
                }
                catch (Exception ex) { last = ex; }
            }
            throw last ?? new InvalidOperationException(L.Text("S312"));
        }

        private static string BuildSqlCs(string host, int port, string user, string pass, string dbName, bool winAuth)
        {
            var server = port > 0 ? $"{host},{port}" : host;
            var csb = new SqlConnectionStringBuilder
            {
                DataSource = server,
                InitialCatalog = string.IsNullOrEmpty(dbName) ? "" : dbName,
                TrustServerCertificate = true
            };
            if (winAuth)
            {
                csb.IntegratedSecurity = true;
                csb.ConnectTimeout = 5;
            }
            else
            {
                csb.UserID = user;
                csb.Password = pass;
            }
            return csb.ConnectionString;
        }

        private static string ResolveForWinAuth(string host)
        {
            if (!IPAddress.TryParse(host, out _)) return host;
            try
            {
                var resolved = Dns.GetHostEntry(host).HostName;
                if (!string.IsNullOrWhiteSpace(resolved) &&
                    !resolved.Equals(host, StringComparison.OrdinalIgnoreCase) &&
                    resolved.Any(char.IsLetter))
                {
                    return resolved;
                }
            }
            catch { }
            return host;
        }

        private static DataTable ExecuteSqlite(string dbName, string query)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentException(L.Text("S280"));
            }

            var path = Path.GetFullPath(dbName);
            using var conn = new SqliteConnection($"Data Source={path}");
            conn.Open();
            return FillData(conn, query);
        }

        private static DataTable ExecuteRedis(string host, int port, string pass, string command)
        {
            var options = new ConfigurationOptions
            {
                EndPoints = { $"{host}:{port}" },
                AbortOnConnectFail = false
            };
            if (!string.IsNullOrEmpty(pass))
                options.Password = pass;

            using var redis = ConnectionMultiplexer.Connect(options);
            var db = redis.GetDatabase();

            string resultText;
            try
            {
                if (command.Trim().ToUpperInvariant() == "INFO")
                {
                    resultText = ((string?)db.Execute("INFO")) ?? "";
                }
                else
                {
                    var parts = command.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    var cmd = parts[0];
                    var redisArgs = parts.Skip(1).Select(a => (RedisValue)a).ToArray();
                    var value = db.Execute(cmd, redisArgs);
                    resultText = value?.ToString() ?? "(nil)";
                }
            }
            catch (Exception ex)
            {
                resultText = L.Format("S055", ex.Message);
            }

            var dt = new DataTable();
            dt.Columns.Add(L.Text("S281"));
            dt.Rows.Add(resultText);
            return dt;
        }

        private static DataTable FillData(IDbConnection conn, string query)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            cmd.CommandTimeout = 30;

            using var reader = cmd.ExecuteReader();
            var dt = new DataTable();
            dt.Load(reader);
            return dt;
        }

        private void ShowResult(DataTable dt, string status)
        {
            _dgvResult.DataSource = dt;
            _lblStatus.Text = status;
            _lblStatus.ForeColor = Color.FromArgb(76, 175, 80);
        }
    }
}
