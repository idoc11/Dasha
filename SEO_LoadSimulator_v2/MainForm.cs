using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SEO_LoadSimulator
{
    public class MainForm : Form
    {
        const double Target = 95.0;   // доля талонов, ожидающих не дольше норматива, %

        readonly AppUser _user;
        readonly AppDb _db;
        NumericUpDown nPerDay = null!, nPeak = null!, nService = null!, nWindows = null!, nOpen = null!, nClose = null!;
        NumericUpDown nPriv = null!, nAppt = null!, nNoShow = null!, nNorm = null!, nSeed = null!, nRuns = null!;
        ComboBox cbDist = null!;
        CheckBox chkStress = null!;
        Label lblVerdict = null!, lblStatus = null!;
        TextBox txtResult = null!;
        DataGridView grid = null!;
        ChartBox chartQueue = null!, chartWait = null!;
        SimResult? last;
        SimParams? lastParams;
        int rowY;

        public MainForm(AppUser user, AppDb db)
        {
            _user = user; _db = db;
            Text = "Симулятор нагрузки СЭО МФЦ – " + user.FullName + " (" + user.Role + ")";
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);
            Size = new Size(1300, 820);
            MinimumSize = new Size(1120, 720);

            var left = new Panel { Dock = DockStyle.Left, Width = 350, AutoScroll = true, BackColor = Color.WhiteSmoke };
            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(6) };
            BuildRight(right);
            BuildLeft(left);
            var status = new StatusStrip();
            lblStatus = new Label { Text = "", AutoSize = true };
            status.Items.Add(new ToolStripControlHost(lblStatus));
            var menu = BuildMenu();
            Controls.Add(right);
            Controls.Add(left);
            Controls.Add(menu);
            Controls.Add(status);
            MainMenuStrip = menu;
            txtResult.Text = "  Задайте параметры и нажмите «Запустить симуляцию»";
        }

        // ---------- меню ----------
        MenuStrip BuildMenu()
        {
            var m = new MenuStrip();
            var file = new ToolStripMenuItem("Файл");
            file.DropDownItems.Add("Сохранить отчёт (CSV)", null, (s, e) => OnSave(s, e));
            file.DropDownItems.Add("Выход", null, (s, e) => Close());
            var sc = new ToolStripMenuItem("Сценарии");
            sc.DropDownItems.Add("Сохранить текущие параметры…", null, (s, e) => SaveScenario());
            sc.DropDownItems.Add("Открыть сохранённый…", null, (s, e) => OpenScenario());
            var hist = new ToolStripMenuItem("История расчётов");
            hist.Click += (s, e) => OpenHistory();
            m.Items.AddRange(new ToolStripItem[] { file, sc, hist });
            if (_user.IsAdmin)
            {
                var adm = new ToolStripMenuItem("Администрирование");
                adm.DropDownItems.Add("Пользователи…", null, (s, e) => { using var f = new UsersForm(_db, _user); f.ShowDialog(this); });
                m.Items.Add(adm);
            }
            return m;
        }

        // ---------- левая панель: параметры ----------
        NumericUpDown AddNum(Panel parent, string caption, decimal min, decimal max, int decimals, decimal value, decimal inc)
        {
            parent.Controls.Add(new Label { Text = caption, Left = 12, Top = rowY + 4, Width = 220, Height = 20 });
            var n = new NumericUpDown { Left = 238, Top = rowY, Width = 96, DecimalPlaces = decimals, Minimum = min, Maximum = max, Increment = inc, Value = value };
            parent.Controls.Add(n);
            rowY += 30;
            return n;
        }

        Button AddButton(Panel parent, string text, EventHandler click, bool primary)
        {
            var b = new Button { Text = text, Left = 12, Top = rowY, Width = 322, Height = 30, FlatStyle = FlatStyle.Flat };
            if (primary) { b.BackColor = Color.SteelBlue; b.ForeColor = Color.White; }
            b.Click += click;
            parent.Controls.Add(b);
            rowY += 36;
            return b;
        }

        void BuildLeft(Panel left)
        {
            left.Controls.Add(new Label { Text = "Параметры (таблица 3.1 ТЗ)", Left = 12, Top = 8, Width = 320, Height = 22, Font = new Font(Font, FontStyle.Bold) });
            rowY = 36;
            nPerDay = AddNum(left, "Заявителей в сутки, чел.", 10, 20000, 0, 1000, 50);
            nPeak = AddNum(left, "Коэфф. неравномерности (час пик)", 1, 5, 2, 1.35m, 0.05m);
            nService = AddNum(left, "Среднее время обслуживания, мин", 1, 60, 1, 12, 0.5m);
            nWindows = AddNum(left, "Количество окон приёма", 1, 100, 0, 22, 1);
            nOpen = AddNum(left, "Начало работы, ч", 0, 22, 0, 8, 1);
            nClose = AddNum(left, "Окончание работы, ч", 1, 24, 0, 20, 1);
            nPriv = AddNum(left, "Льготные категории, %", 0, 100, 0, 10, 1);
            nAppt = AddNum(left, "Предварительная запись, %", 0, 100, 0, 25, 1);
            nNoShow = AddNum(left, "Неявка заявителей, %", 0, 50, 0, 3, 1);
            nNorm = AddNum(left, "Норматив ожидания, мин", 1, 120, 0, 15, 1);
            nSeed = AddNum(left, "Начальное число ГСЧ (0 — случайно)", 0, 2000000000, 0, 2026, 1);
            nRuns = AddNum(left, "Число прогонов (дней)", 1, 1000, 0, 100, 10);

            left.Controls.Add(new Label { Text = "Закон времени обслуживания:", Left = 12, Top = rowY, Width = 320, Height = 20 });
            rowY += 22;
            cbDist = new ComboBox { Left = 12, Top = rowY, Width = 322, DropDownStyle = ComboBoxStyle.DropDownList };
            cbDist.Items.AddRange(new object[] { "Экспоненциальное (большой разброс)", "Эрланга-2 (умеренный разброс)", "Постоянное (без разброса)" });
            cbDist.SelectedIndex = 1;
            left.Controls.Add(cbDist);
            rowY += 32;
            chkStress = new CheckBox { Text = "Стресс-режим: поток часа пик весь день", Left = 12, Top = rowY, Width = 322, Height = 24 };
            left.Controls.Add(chkStress);
            rowY += 32;

            AddButton(left, "Текущая нагрузка (800 чел./сут)", (s, e) => nPerDay.Value = 800, false);
            AddButton(left, "Расчётная нагрузка (1000 чел./сут)", (s, e) => nPerDay.Value = 1000, false);
            AddButton(left, "Запустить симуляцию", OnRun, true);
            AddButton(left, "Подобрать число окон под норматив", OnFind, true);
            AddButton(left, "Сохранить отчёт (CSV)", OnSave, false);

            left.Controls.Add(new Label
            {
                Left = 12, Top = rowY + 6, Width = 322, Height = 250, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.25f),
                Text = "Модель: пуассоновский поток с часовым профилем (пик в 11:00–12:00, максимум = K × среднее); окна универсальные; порядок вызова по приложению Д: " +
                       "льготные → предзапись → остальные, внутри группы по времени выдачи; неявка занимает окно на 3 мин (3 вызова по 60 с). " +
                       "Стресс-режим — консервативная оценка: интенсивность часа пик держится весь день (число талонов становится N × K). " +
                       "Результат усредняется по заданному числу прогонов (дней): так он не зависит от одного «удачного» или «тяжёлого» дня. " +
                       "Подбор окон: минимальное число, при котором в среднем ≥ 95 % талонов ждут не дольше нормы."
            });
        }

        // ---------- правая часть: результаты ----------
        void BuildRight(TableLayoutPanel right)
        {
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            right.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

            lblVerdict = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 11f, FontStyle.Bold), Text = "Нет данных" };
            right.Controls.Add(lblVerdict, 0, 0); right.SetColumnSpan(lblVerdict, 2);
            txtResult = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9.5f), BackColor = Color.White };
            right.Controls.Add(txtResult, 0, 1); right.SetColumnSpan(txtResult, 2);
            grid = new DataGridView
            {
                Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false, ReadOnly = true,
                RowHeadersVisible = false, BackgroundColor = Color.White, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.Columns.Add("hour", "Час работы"); grid.Columns.Add("arr", "Заявителей");
            grid.Columns.Add("avg", "Среднее ожидание, мин"); grid.Columns.Add("max", "Макс. ожидание (худший день), мин");
            grid.Columns.Add("util", "Загрузка окон, %");
            right.Controls.Add(grid, 0, 2); right.SetColumnSpan(grid, 2);
            chartQueue = new ChartBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8f) };
            chartWait = new ChartBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 8f) };
            right.Controls.Add(chartQueue, 0, 3); right.Controls.Add(chartWait, 1, 3);
        }

        // ---------- параметры ----------
        SimParams ReadParams()
        {
            return new SimParams
            {
                PerDay = (int)nPerDay.Value, PeakK = (double)nPeak.Value, ServiceMin = (double)nService.Value, Windows = (int)nWindows.Value,
                OpenHour = (int)nOpen.Value, CloseHour = (int)nClose.Value, PrivilegedPct = (double)nPriv.Value, AppointmentPct = (double)nAppt.Value,
                NoShowPct = (double)nNoShow.Value, NormMin = (double)nNorm.Value, Dist = (ServiceDist)cbDist.SelectedIndex,
                Seed = (int)nSeed.Value, Stress = chkStress.Checked, Runs = (int)nRuns.Value
            };
        }

        static void SetNum(NumericUpDown n, double v)
        {
            decimal d = (decimal)v;
            n.Value = Math.Max(n.Minimum, Math.Min(n.Maximum, d));
        }

        void ApplyParams(SimParams p)
        {
            SetNum(nPerDay, p.PerDay); SetNum(nPeak, p.PeakK); SetNum(nService, p.ServiceMin); SetNum(nWindows, p.Windows);
            SetNum(nOpen, p.OpenHour); SetNum(nClose, p.CloseHour); SetNum(nPriv, p.PrivilegedPct); SetNum(nAppt, p.AppointmentPct);
            SetNum(nNoShow, p.NoShowPct); SetNum(nNorm, p.NormMin); SetNum(nSeed, p.Seed); SetNum(nRuns, p.Runs);
            cbDist.SelectedIndex = (int)p.Dist; chkStress.Checked = p.Stress;
        }

        bool Check(SimParams p)
        {
            int hours = p.CloseHour - p.OpenHour;
            if (hours < 4) { MessageBox.Show("Время работы должно быть не меньше 4 часов.", "Проверка параметров"); return false; }
            if (!Simulator.WeightsValid(hours, p.PeakK))
            {
                MessageBox.Show("При таком коэффициенте неравномерности и режиме работы построить профиль потока нельзя. Уменьшите коэффициент или увеличьте время работы.", "Проверка параметров");
                return false;
            }
            if (p.PrivilegedPct + p.AppointmentPct > 100.0)
            {
                MessageBox.Show("Сумма долей льготных и предварительной записи не может превышать 100 %.", "Проверка параметров");
                return false;
            }
            return true;
        }

        // ---------- действия ----------
        void OnRun(object? sender, EventArgs e)
        {
            var p = ReadParams();
            if (!Check(p)) return;
            int seed = p.Seed == 0 ? Environment.TickCount : p.Seed;
            Cursor = Cursors.WaitCursor;
            var res = Simulator.RunMany(p, p.Runs, seed, Target);
            Cursor = Cursors.Default;
            last = res;
            lastParams = p;
            ShowResult(res, p);
            bool ok = res.OkShare >= Target;
            lblStatus.Text = UiTry(() => _db.SaveRun(_user.Id, p, res, ok)) ? "Расчёт записан в историю" : "Расчёт не записан в историю (нет связи с БД)";
        }

        static bool UiTry(Action a)
        {
            try { a(); return true; } catch { return false; }
        }

        void OnFind(object? sender, EventArgs e)
        {
            var p = ReadParams();
            if (!Check(p)) return;
            int seed = p.Seed == 0 ? Environment.TickCount : p.Seed;
            int runs = Math.Max(5, p.Runs);
            Cursor = Cursors.WaitCursor;
            int found = -1;
            double share = 0.0;
            p.Windows = 100;
            if (Simulator.RunMany(p, runs, seed, Target).OkShare >= Target)
            {
                int lo = 1, hi = 100;
                while (lo < hi)
                {
                    int mid = (lo + hi) / 2;
                    p.Windows = mid;
                    if (Simulator.RunMany(p, runs, seed, Target).OkShare >= Target) hi = mid; else lo = mid + 1;
                }
                p.Windows = lo;
                share = Simulator.RunMany(p, runs, seed, Target).OkShare;
                found = lo;
            }
            Cursor = Cursors.Default;
            if (found < 0) { MessageBox.Show("Даже 100 окон не обеспечивают норматив при заданных параметрах.", "Подбор числа окон"); return; }
            nWindows.Value = found;
            OnRun(null, EventArgs.Empty);
            MessageBox.Show(string.Format("Минимальное число окон, обеспечивающее норматив {0:F0} мин:  {1}\n(в среднем по {2} дням {3:F1} % талонов ожидают не дольше нормы, требуется ≥ {4:F0} %)",
                p.NormMin, found, runs, share, Target), "Подбор числа окон");
        }

        // ---------- вывод результатов ----------
        string HourLabel(int h) { return string.Format("{0:00}:00–{1:00}:00", (int)nOpen.Value + h, (int)nOpen.Value + h + 1); }
        string Clock(int minute) { int t = (int)nOpen.Value * 60 + minute; return string.Format("{0:00}:{1:00}", t / 60, t % 60); }

        static void Line(StringBuilder sb, string label, string value) { sb.AppendLine("  " + label.PadRight(46) + value); }

        void ShowResult(SimResult r, SimParams p)
        {
            bool ok = r.OkShare >= Target;
            lblVerdict.Text = string.Format("  Норматив ожидания {0:F0} мин {1}: {2:F1} % талонов ждут не дольше нормы (нужно ≥ {3:F0} %){4}",
                p.NormMin, ok ? "ВЫПОЛНЯЕТСЯ" : "НЕ ВЫПОЛНЯЕТСЯ", r.OkShare, Target,
                r.Runs > 1 ? string.Format("; норматив соблюдён в {0:F0} % дней из {1}", r.DaysOkPct, r.Runs) : "");
            lblVerdict.ForeColor = ok ? Color.DarkGreen : Color.DarkRed;
            lblVerdict.BackColor = ok ? Color.Honeydew : Color.MistyRose;

            var sb = new StringBuilder();
            sb.AppendLine("  РАСЧЁТНАЯ НАГРУЗКА");
            Line(sb, "Заявителей в сутки:", p.PerDay + (p.Stress ? string.Format("  (стресс-режим: выдано {0:F0})", p.PerDay * p.PeakK) : ""));
            Line(sb, "Поток в час пик (N × K / часы работы):", string.Format("{0:F1} чел./ч", r.LambdaPeak));
            Line(sb, "Предложенная нагрузка (λ × время обслуж.):", string.Format("{0:F1} Эрл", r.Erlang));
            Line(sb, "Теоретическая загрузка окон ρ в час пик:", string.Format("{0:F2}  {1}", r.Rho, r.Rho < 1 ? "(< 1: окон достаточно)" : "(≥ 1: окон не хватает, очередь растёт)"));
            Line(sb, "Минимум окон по теории (≥ нагрузки):", Math.Ceiling(r.Erlang).ToString("F0"));
            sb.AppendLine();
            sb.AppendLine(r.Runs > 1 ? string.Format("  РЕЗУЛЬТАТЫ СИМУЛЯЦИИ (среднее по {0} дням)", r.Runs) : "  РЕЗУЛЬТАТЫ СИМУЛЯЦИИ (один день)");
            Line(sb, "Выдано талонов / обслужено / неявок:", string.Format("{0:F0} / {1:F0} / {2:F0}", r.Total, r.Served, r.NoShow));
            Line(sb, "Среднее ожидание:", string.Format("{0:F1} мин", r.MeanWait));
            Line(sb, "Медиана / 95-й перцентиль / максимум:", string.Format("{0:F1} / {1:F1} / {2:F1} мин", r.MedianWait, r.P95Wait, r.MaxWait));
            Line(sb, "Максимальная длина очереди:", string.Format("{0:F0} чел. (пик около {1})", r.PeakQueue, Clock(r.PeakQueueMin)));
            Line(sb, "Загрузка окон за день / в час пик:", string.Format("{0:F0} % / {1:F0} %", r.Util, r.PeakUtil));
            Line(sb, "Работа после закрытия МФЦ:", string.Format("{0:F0} мин", r.OvertimeMin));
            if (r.Runs > 1)
            {
                sb.AppendLine();
                sb.AppendLine("  ХУДШИЙ ИЗ ДНЕЙ");
                Line(sb, "Дней с выполненным нормативом:", string.Format("{0:F0} % ({1} дн.)", r.DaysOkPct, (int)Math.Round(r.DaysOkPct * r.Runs / 100.0)));
                Line(sb, "Наименьшая доля талонов в норме:", string.Format("{0:F1} %", r.WorstOkShare));
                Line(sb, "Наибольшее ожидание / очередь:", string.Format("{0:F1} мин / {1:F0} чел.", r.WorstMaxWait, r.WorstPeakQueue));
            }
            txtResult.Text = sb.ToString().Replace("\n", "\r\n");

            grid.Rows.Clear();
            for (int h = 0; h < r.HArr.Length; h++)
            {
                int idx = grid.Rows.Add(HourLabel(h), r.HArr[h].ToString("F0"), r.HAvg[h].ToString("F1"), r.HMax[h].ToString("F1"), r.HUtil[h].ToString("F0"));
                if (r.HAvg[h] > p.NormMin) grid.Rows[idx].DefaultCellStyle.BackColor = Color.MistyRose;
            }

            var labelsQ = new string[r.Queue.Length];
            for (int i = 0; i < labelsQ.Length; i++) labelsQ[i] = Clock(i);
            chartQueue.SetData(string.Format("Средняя длина очереди (по {0} дням), чел.", r.Runs), r.Queue, labelsQ, false, double.NaN, 60);
            var labelsW = new string[r.HAvg.Length];
            for (int i = 0; i < labelsW.Length; i++) labelsW[i] = ((int)nOpen.Value + i).ToString("00");
            chartWait.SetData(string.Format("Среднее ожидание по часам (по {0} дням), мин", r.Runs), r.HAvg, labelsW, true, p.NormMin, 1);
        }

        // ---------- сохранение отчёта ----------
        void OnSave(object? sender, EventArgs e)
        {
            if (last == null || lastParams == null) { MessageBox.Show("Сначала запустите симуляцию.", "Сохранение отчёта"); return; }
            using var dlg = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv", FileName = "otchet_nagruzka.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var r = last; var p = lastParams;
            try
            {
                using var w = new StreamWriter(dlg.FileName, false, new UTF8Encoding(true));
                w.WriteLine("Отчёт о симуляции нагрузки СЭО МФЦ");
                w.WriteLine("Дата;" + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                w.WriteLine("Заявителей в сутки;" + p.PerDay);
                w.WriteLine("Коэффициент неравномерности;" + p.PeakK);
                w.WriteLine("Среднее время обслуживания, мин;" + p.ServiceMin);
                w.WriteLine("Количество окон;" + p.Windows);
                w.WriteLine("Режим работы;" + p.OpenHour + ":00-" + p.CloseHour + ":00");
                w.WriteLine("Стресс-режим;" + (p.Stress ? "да" : "нет"));
                w.WriteLine("Число прогонов (дней);" + r.Runs);
                w.WriteLine("Результаты;" + (r.Runs > 1 ? "среднее по прогонам" : "один день"));
                w.WriteLine("Выдано талонов;" + r.Total.ToString("F0"));
                w.WriteLine("Обслужено;" + r.Served.ToString("F0"));
                w.WriteLine("Неявок;" + r.NoShow.ToString("F0"));
                w.WriteLine("Среднее ожидание, мин;" + r.MeanWait.ToString("F1"));
                w.WriteLine("Медиана ожидания, мин;" + r.MedianWait.ToString("F1"));
                w.WriteLine("95-й перцентиль ожидания, мин;" + r.P95Wait.ToString("F1"));
                w.WriteLine("Максимальное ожидание, мин;" + r.MaxWait.ToString("F1"));
                w.WriteLine("Доля ожидающих не дольше нормы, %;" + r.OkShare.ToString("F1"));
                w.WriteLine("Макс. длина очереди;" + r.PeakQueue.ToString("F0"));
                w.WriteLine("Загрузка окон за день, %;" + r.Util.ToString("F0"));
                w.WriteLine("Загрузка окон в час пик, %;" + r.PeakUtil.ToString("F0"));
                w.WriteLine("Работа после закрытия, мин;" + r.OvertimeMin.ToString("F0"));
                w.WriteLine("Дней с выполненным нормативом, %;" + r.DaysOkPct.ToString("F0"));
                w.WriteLine("Худшая доля талонов в норме, %;" + r.WorstOkShare.ToString("F1"));
                w.WriteLine("Наибольшее ожидание за все дни, мин;" + r.WorstMaxWait.ToString("F1"));
                w.WriteLine("Наибольшая очередь за все дни;" + r.WorstPeakQueue.ToString("F0"));
                w.WriteLine();
                w.WriteLine("Час;Заявителей;Среднее ожидание, мин;Макс. ожидание (худший день), мин;Загрузка окон, %");
                for (int h = 0; h < r.HArr.Length; h++)
                    w.WriteLine(string.Join(";", HourLabel(h), r.HArr[h].ToString("F0"), r.HAvg[h].ToString("F1"), r.HMax[h].ToString("F1"), r.HUtil[h].ToString("F0")));
                MessageBox.Show("Отчёт сохранён.", "Сохранение отчёта");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException) { MessageBox.Show("Не удалось сохранить файл: " + ex.Message, "Ошибка"); }
        }

        // ---------- сценарии и история ----------
        void SaveScenario()
        {
            var name = InputBox.Ask(this, "Сохранение сценария", "Название сценария:", "");
            if (string.IsNullOrWhiteSpace(name)) return;
            var p = ReadParams();
            if (UI.Try(() => _db.SaveScenario(_user.Id, name!, p))) lblStatus.Text = "Сценарий «" + name + "» сохранён";
        }

        void OpenScenario()
        {
            using var f = new ScenariosForm(_db, _user);
            if (f.ShowDialog(this) == DialogResult.OK && f.Selected != null) ApplyParams(f.Selected);
        }

        void OpenHistory()
        {
            using var f = new HistoryForm(_db, _user);
            if (f.ShowDialog(this) == DialogResult.OK && f.Selected != null) ApplyParams(f.Selected);
        }
    }
}
