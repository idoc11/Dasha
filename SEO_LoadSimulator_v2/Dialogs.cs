using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SEO_LoadSimulator
{
    static class UI
    {
        public static readonly Font Normal = new Font("Segoe UI", 10f);
        static readonly Font ListFont = new Font("Segoe UI", 9f);

        public static Button Btn(
            string text,
            EventHandler click,
            int width = 150
        )
        {
            var b = new Button
            {
                Text = text,
                Width = width,
                Height = 32,
                Font = Normal,
                Margin = new Padding(4),
            };
            b.Click += click;
            return b;
        }

        /// <summary>
        /// Подбирает ширину столбцов списка по заголовку и содержимому.
        /// </summary>
        public static void FitColumns(ListView lv)
        {
            foreach (ColumnHeader c in lv.Columns)
            {
                int w = TextRenderer.MeasureText(c.Text, lv.Font).Width;
                foreach (ListViewItem it in lv.Items)
                    w = Math.Max(
                        w,
                        TextRenderer
                            .MeasureText(
                                it.SubItems[c.Index].Text,
                                lv.Font
                            )
                            .Width
                    );
                c.Width = w + 12;
            }
        }

        public static ListView List(params string[] columns)
        {
            var lv = new ListView
            {
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                Dock = DockStyle.Fill,
                Font = ListFont,
                MultiSelect = false,
            };
            foreach (var c in columns)
                lv.Columns.Add(c, 120);
            return lv;
        }

        public static string DistName(ServiceDist d) =>
            d == ServiceDist.Exponential ? "экспоненциальное"
            : d == ServiceDist.Erlang2 ? "Эрланга-2"
            : "постоянное";

        public static void Error(string text) =>
            MessageBox.Show(
                text,
                "Ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );

        // обращение к БД: при сбое показывается сообщение, программа не
        // закрывается
        public static bool Try(Action a)
        {
            try
            {
                a();
                return true;
            }
            catch (Npgsql.PostgresException ex)
            {
                // сервер ответил ошибкой (нарушено ограничение и т. п.)
                Error("Ошибка базы данных: " + ex.MessageText);
                return false;
            }
            catch (Npgsql.NpgsqlException)
            {
                // сервер недоступен или строка подключения неверна
                Error(
                    "Нет связи с базой данных. Проверьте, что сервер "
                        + "PostgreSQL запущен и строка подключения "
                        + "SIM_DB "
                        + "указана верно."
                );
                return false;
            }
            catch (Exception ex)
            {
                Error("Ошибка работы с базой данных: " + ex.Message);
                return false;
            }
        }
    }

    class InputBox : Form
    {
        readonly TextBox _text = new TextBox
        {
            Width = 340,
            Font = UI.Normal,
        };
        public string Value => _text.Text.Trim();

        InputBox(
            string title,
            string prompt,
            string initial,
            bool password
        )
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(380, 130);
            _text.Text = initial;
            _text.UseSystemPasswordChar = password;
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(16, 12, 16, 8),
            };
            p.Controls.Add(
                new Label
                {
                    Text = prompt,
                    AutoSize = true,
                    Font = UI.Normal,
                }
            );
            p.Controls.Add(_text);
            var ok = UI.Btn(
                "OK",
                (s, e) =>
                {
                    DialogResult = DialogResult.OK;
                },
                100
            );
            var cancel = UI.Btn(
                "Отмена",
                (s, e) =>
                {
                    DialogResult = DialogResult.Cancel;
                },
                100
            );
            AcceptButton = ok;
            CancelButton = cancel;
            var buttons = new FlowLayoutPanel
            {
                AutoSize = true,
                Margin = new Padding(0),
            };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            p.Controls.Add(buttons);
            Controls.Add(p);
        }

        public static string? Ask(
            IWin32Window owner,
            string title,
            string prompt,
            string initial = "",
            bool password = false
        )
        {
            using var f = new InputBox(
                title,
                prompt,
                initial,
                password
            );
            return f.ShowDialog(owner) == DialogResult.OK
                ? f.Value
                : null;
        }
    }

    class LoginForm : Form
    {
        readonly TextBox _login = new TextBox
        {
            Width = 240,
            Font = UI.Normal,
            MaxLength = 30,
        };
        readonly TextBox _pass = new TextBox
        {
            Width = 240,
            Font = UI.Normal,
            UseSystemPasswordChar = true,
            MaxLength = 100,
        };
        readonly AppDb _db;
        public AppUser? User;

        public LoginForm(AppDb db)
        {
            _db = db;
            Text = "Симулятор нагрузки СЭО МФЦ – вход";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(300, 170);
            var p = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(24, 16, 16, 8),
            };
            p.Controls.Add(
                new Label
                {
                    Text = "Логин",
                    AutoSize = true,
                    Font = UI.Normal,
                }
            );
            p.Controls.Add(_login);
            p.Controls.Add(
                new Label
                {
                    Text = "Пароль",
                    AutoSize = true,
                    Font = UI.Normal,
                }
            );
            p.Controls.Add(_pass);
            var ok = UI.Btn("Войти", (s, e) => Submit(), 120);
            AcceptButton = ok;
            p.Controls.Add(ok);
            Controls.Add(p);
        }

        void Submit()
        {
            if (
                _login.Text.Trim().Length == 0
                || _pass.Text.Length == 0
            )
            {
                UI.Error("Введите логин и пароль.");
                return;
            }
            AppUser? u = null;
            if (
                !UI.Try(
                    () =>
                        u = _db.Authenticate(
                            _login.Text.Trim(),
                            _pass.Text
                        )
                )
            )
                return;
            if (u == null)
            {
                UI.Error("Неверный логин или пароль.");
                _pass.Clear();
                return;
            }
            User = u;
            DialogResult = DialogResult.OK;
        }
    }

    /// <summary>Сохранённые сценарии: загрузка и удаление.</summary>
    class ScenariosForm : Form
    {
        readonly AppDb _db;
        readonly AppUser _user;
        readonly ListView _list = UI.List(
            "Название",
            "Заявителей",
            "Окон",
            "Режим",
            "Закон",
            "Создан"
        );
        List<ScenarioRow> _rows = new List<ScenarioRow>();
        public SimParams? Selected;

        public ScenariosForm(AppDb db, AppUser user)
        {
            _db = db;
            _user = user;
            Text = "Сохранённые сценарии";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(760, 420);
            _list.DoubleClick += (s, e) => LoadSelected();
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
            };
            bar.Controls.Add(
                UI.Btn("Загрузить", (s, e) => LoadSelected())
            );
            bar.Controls.Add(UI.Btn("Удалить", (s, e) => Delete()));
            var close = UI.Btn("Закрыть", (s, e) => Close());
            CancelButton = close;
            bar.Controls.Add(close);
            Controls.Add(_list);
            Controls.Add(bar);
            Reload();
        }

        void Reload()
        {
            UI.Try(() => _rows = _db.GetScenarios(_user));
            _list.Items.Clear();
            foreach (var r in _rows)
                _list.Items.Add(
                    new ListViewItem(
                        new[]
                        {
                            r.Name,
                            r.Params.PerDay.ToString(),
                            r.Params.Windows.ToString(),
                            r.Params.Stress ? "стресс" : "обычный",
                            UI.DistName(r.Params.Dist),
                            r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
                        }
                    )
                );
            UI.FitColumns(_list);
        }

        ScenarioRow? Current() =>
            _list.SelectedIndices.Count == 0
                ? null
                : _rows[_list.SelectedIndices[0]];

        void LoadSelected()
        {
            var r = Current();
            if (r == null)
            {
                MessageBox.Show(
                    "Выберите сценарий в списке.",
                    "Сценарии"
                );
                return;
            }
            Selected = r.Params;
            DialogResult = DialogResult.OK;
        }

        void Delete()
        {
            var r = Current();
            if (r == null)
            {
                MessageBox.Show(
                    "Выберите сценарий в списке.",
                    "Сценарии"
                );
                return;
            }
            if (
                MessageBox.Show(
                    "Удалить сценарий «" + r.Name + "»?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo
                ) != DialogResult.Yes
            )
                return;
            UI.Try(() => _db.DeleteScenario(r.Id, _user));
            Reload();
        }
    }

    /// <summary>
    /// История расчётов: расчётчик видит свои, администратор – все.
    /// </summary>
    class HistoryForm : Form
    {
        readonly AppDb _db;
        readonly AppUser _user;
        readonly ListView _list = UI.List(
            "Дата",
            "Расчётчик",
            "Заявителей",
            "Окон",
            "Режим",
            "Закон",
            "В норме, %",
            "Ср. ожидание, мин",
            "95-й перц., мин",
            "Дней в норме, %",
            "Итог"
        );
        List<RunRow> _rows = new List<RunRow>();
        public SimParams? Selected;

        readonly CheckBox _period = new CheckBox
        {
            Text = "Период с",
            AutoSize = true,
            Margin = new Padding(4, 9, 0, 0),
        };
        readonly DateTimePicker _from = new DateTimePicker
        {
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Enabled = false,
        };
        readonly DateTimePicker _to = new DateTimePicker
        {
            Width = 120,
            Format = DateTimePickerFormat.Short,
            Enabled = false,
        };
        readonly ComboBox _verdict = new ComboBox
        {
            Width = 150,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        readonly Label _count = new Label
        {
            AutoSize = true,
            Margin = new Padding(12, 10, 0, 0),
        };

        public HistoryForm(AppDb db, AppUser user)
        {
            _db = db;
            _user = user;
            Text = "История расчётов";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1250, 500);
            _list.DoubleClick += (s, e) => LoadSelected();
            _verdict.Items.AddRange(
                new object[] { "все", "выполняется", "не выполняется" }
            );
            _verdict.SelectedIndex = 0;
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(6),
            };
            _from.Value = DateTime.Today.AddDays(-30);
            _to.Value = DateTime.Today;
            _period.CheckedChanged += (s, e) =>
                _from.Enabled = _to.Enabled = _period.Checked;
            top.Controls.Add(_period);
            top.Controls.Add(_from);
            top.Controls.Add(
                new Label
                {
                    Text = "по",
                    AutoSize = true,
                    Margin = new Padding(4, 10, 0, 0),
                }
            );
            top.Controls.Add(_to);
            top.Controls.Add(
                new Label
                {
                    Text = "Итог",
                    AutoSize = true,
                    Margin = new Padding(12, 10, 0, 0),
                }
            );
            top.Controls.Add(_verdict);
            top.Controls.Add(UI.Btn("Найти", (s, e) => Reload(), 100));
            top.Controls.Add(
                UI.Btn("Сбросить", (s, e) => ResetFilter(), 100)
            );
            top.Controls.Add(_count);
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
            };
            bar.Controls.Add(
                UI.Btn(
                    "Загрузить параметры",
                    (s, e) => LoadSelected(),
                    200
                )
            );
            if (user.IsAdmin)
                bar.Controls.Add(
                    UI.Btn("Удалить запись", (s, e) => Delete())
                );
            var close = UI.Btn("Закрыть", (s, e) => Close());
            CancelButton = close;
            bar.Controls.Add(close);
            Controls.Add(_list);
            Controls.Add(top);
            Controls.Add(bar);
            Reload();
        }

        void ResetFilter()
        {
            _period.Checked = false;
            _from.Value = DateTime.Today.AddDays(-30);
            _to.Value = DateTime.Today;
            _verdict.SelectedIndex = 0;
            Reload();
        }

        void Reload()
        {
            // граница «по» включает весь указанный день
            DateTime? from = _period.Checked ? _from.Value.Date : null;
            DateTime? to = _period.Checked
                ? _to.Value.Date.AddDays(1)
                : null;
            if (from != null && to != null && from >= to)
            {
                MessageBox.Show(
                    "Дата «с» не может быть позже даты «по».",
                    "Поиск"
                );
                return;
            }
            bool? verdict =
                _verdict.SelectedIndex == 0
                    ? null
                    : _verdict.SelectedIndex == 1;
            UI.Try(() => _rows = _db.GetRuns(_user, from, to, verdict));
            _list.Items.Clear();
            foreach (var r in _rows)
                _list.Items.Add(
                    new ListViewItem(
                        new[]
                        {
                            r.At.ToString("dd.MM.yyyy HH:mm"),
                            r.UserName,
                            r.Params.PerDay.ToString(),
                            r.Params.Windows.ToString(),
                            r.Params.Stress ? "стресс" : "обычный",
                            UI.DistName(r.Params.Dist),
                            r.OkShare.ToString("0.0"),
                            r.MeanWait.ToString("0.0"),
                            r.P95Wait.ToString("0.0"),
                            r.DaysOkPct.ToString("0"),
                            r.Verdict
                                ? "выполняется"
                                : "не выполняется",
                        }
                    )
                );
            _count.Text = "Найдено записей: " + _rows.Count;
            UI.FitColumns(_list);
        }

        RunRow? Current() =>
            _list.SelectedIndices.Count == 0
                ? null
                : _rows[_list.SelectedIndices[0]];

        void LoadSelected()
        {
            var r = Current();
            if (r == null)
            {
                MessageBox.Show(
                    "Выберите запись в списке.",
                    "История расчётов"
                );
                return;
            }
            Selected = r.Params;
            DialogResult = DialogResult.OK;
        }

        void Delete()
        {
            var r = Current();
            if (r == null)
            {
                MessageBox.Show(
                    "Выберите запись в списке.",
                    "История расчётов"
                );
                return;
            }
            if (
                MessageBox.Show(
                    "Удалить выбранную запись истории?",
                    "Подтверждение",
                    MessageBoxButtons.YesNo
                ) != DialogResult.Yes
            )
                return;
            UI.Try(() => _db.DeleteRun(r.Id));
            Reload();
        }
    }

    /// <summary>
    /// Управление пользователями (только администратор).
    /// </summary>
    class UsersForm : Form
    {
        readonly AppDb _db;
        readonly AppUser _me;
        readonly ListView _list = UI.List(
            "Логин",
            "ФИО",
            "Роль",
            "Активен"
        );
        readonly TextBox _login = new TextBox
        {
            Width = 120,
            Font = UI.Normal,
            MaxLength = 30,
        };
        readonly TextBox _name = new TextBox
        {
            Width = 220,
            Font = UI.Normal,
            MaxLength = 100,
        };
        readonly TextBox _pass = new TextBox
        {
            Width = 120,
            Font = UI.Normal,
            UseSystemPasswordChar = true,
        };
        readonly ComboBox _role = new ComboBox
        {
            Width = 130,
            Font = UI.Normal,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        List<UserRow> _rows = new List<UserRow>();

        public UsersForm(AppDb db, AppUser me)
        {
            _db = db;
            _me = me;
            Text = "Пользователи";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(820, 480);
            _role.Items.AddRange(
                new object[] { AppDb.RoleCalc, AppDb.RoleAdmin }
            );
            _role.SelectedIndex = 0;
            var top = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 76,
                Padding = new Padding(6),
            };
            top.Controls.Add(
                new Label
                {
                    Text = "Логин",
                    AutoSize = true,
                    Margin = new Padding(4, 8, 0, 0),
                }
            );
            top.Controls.Add(_login);
            top.Controls.Add(
                new Label
                {
                    Text = "ФИО",
                    AutoSize = true,
                    Margin = new Padding(4, 8, 0, 0),
                }
            );
            top.Controls.Add(_name);
            top.Controls.Add(
                new Label
                {
                    Text = "Пароль",
                    AutoSize = true,
                    Margin = new Padding(4, 8, 0, 0),
                }
            );
            top.Controls.Add(_pass);
            top.Controls.Add(
                new Label
                {
                    Text = "Роль",
                    AutoSize = true,
                    Margin = new Padding(4, 8, 0, 0),
                }
            );
            top.Controls.Add(_role);
            top.Controls.Add(UI.Btn("Добавить", (s, e) => Add(), 110));
            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
            };
            bar.Controls.Add(
                UI.Btn(
                    "Блокировать / разблокировать",
                    (s, e) => Toggle(),
                    280
                )
            );
            bar.Controls.Add(
                UI.Btn("Сбросить пароль", (s, e) => Reset(), 150)
            );
            var close = UI.Btn("Закрыть", (s, e) => Close());
            CancelButton = close;
            bar.Controls.Add(close);
            Controls.Add(_list);
            Controls.Add(top);
            Controls.Add(bar);
            Reload();
        }

        void Reload()
        {
            UI.Try(() => _rows = _db.GetUsers());
            _list.Items.Clear();
            foreach (var u in _rows)
                _list.Items.Add(
                    new ListViewItem(
                        new[]
                        {
                            u.Login,
                            u.FullName,
                            u.Role,
                            u.Active ? "да" : "нет",
                        }
                    )
                );
            UI.FitColumns(_list);
        }

        UserRow? Current() =>
            _list.SelectedIndices.Count == 0
                ? null
                : _rows[_list.SelectedIndices[0]];

        void Add()
        {
            string msg = "";
            if (
                !UI.Try(
                    () =>
                        msg = _db.AddUser(
                            _login.Text,
                            _name.Text,
                            (string)_role.SelectedItem!,
                            _pass.Text
                        )
                )
            )
                return;
            if (msg.Length > 0)
            {
                MessageBox.Show(msg, "Пользователи");
                return;
            }
            _login.Clear();
            _name.Clear();
            _pass.Clear();
            Reload();
        }

        void Toggle()
        {
            var u = Current();
            if (u == null)
            {
                MessageBox.Show(
                    "Выберите пользователя.",
                    "Пользователи"
                );
                return;
            }
            if (u.Id == _me.Id && u.Active)
            {
                MessageBox.Show(
                    "Нельзя заблокировать собственную учётную запись.",
                    "Пользователи"
                );
                return;
            }
            UI.Try(() => _db.SetUserActive(u.Id, !u.Active));
            Reload();
        }

        void Reset()
        {
            var u = Current();
            if (u == null)
            {
                MessageBox.Show(
                    "Выберите пользователя.",
                    "Пользователи"
                );
                return;
            }
            var pw = InputBox.Ask(
                this,
                "Сброс пароля",
                "Новый пароль для "
                    + u.Login
                    + " (не короче 6 символов):",
                "",
                true
            );
            if (pw == null)
                return;
            string msg = "";
            if (!UI.Try(() => msg = _db.ResetPassword(u.Id, pw)))
                return;
            MessageBox.Show(
                msg.Length > 0 ? msg : "Пароль изменён.",
                "Пользователи"
            );
        }
    }
}
