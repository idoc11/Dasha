// Строка подключения берётся из переменной SIM_DB, иначе – значение по умолчанию.
using SEO_LoadSimulator; using Npgsql;
var db = AppDb.Default();
int n = 0, fail = 0;
void T(string name, bool ok) { n++; if (!ok) fail++; Console.WriteLine((ok ? "PASS " : "FAIL ") + name); }
var calc = db.Authenticate("calc1", "Demo123!"); T("вход calc1 верный пароль", calc != null && calc.Role == AppDb.RoleCalc && calc.FullName == "Красовская Дарья Дмитриевна");
T("вход calc1 неверный пароль", db.Authenticate("calc1", "123") == null);
T("вход несуществующий логин", db.Authenticate("nobody", "Demo123!") == null);
var adm = db.Authenticate("admin", "Demo123!"); T("вход admin", adm != null && adm.IsAdmin);
var c2 = db.Authenticate("calc2", "Demo123!")!;
T("сценарии: расчётчик видит только свои", db.GetScenarios(calc!).Count == 3);
T("сценарии: администратор видит все", db.GetScenarios(adm!).Count == 17);
var p = new SimParams { PerDay = 950, PeakK = 1.35, ServiceMin = 12, Windows = 23, OpenHour = 8, CloseHour = 20, PrivilegedPct = 10, AppointmentPct = 25, NoShowPct = 3, NormMin = 15, Dist = ServiceDist.Erlang2, Seed = 2026, Stress = false, Runs = 100 };
db.SaveScenario(calc.Id, "Проверка", p); T("сохранение сценария", db.GetScenarios(calc).Any(s => s.Name == "Проверка" && s.Params.PerDay == 950));
p.Windows = 24; db.SaveScenario(calc.Id, "Проверка", p); var mine = db.GetScenarios(calc);
T("повторное сохранение обновляет, а не дублирует", mine.Count(s => s.Name == "Проверка") == 1 && mine.First(s => s.Name == "Проверка").Params.Windows == 24);
var other = db.GetScenarios(adm).First(s => s.Name.StartsWith("Расчётная нагрузка 1000, 27"));
T("расчётчик не может удалить чужой сценарий", !db.DeleteScenario(other.Id, calc));
T("удаление своего сценария", db.DeleteScenario(mine.First(s => s.Name == "Проверка").Id, calc));
bool chk; try { p.Windows = 150; db.SaveScenario(calc.Id, "Плохой", p); chk = false; } catch (PostgresException e) { chk = e.SqlState == "23514"; } p.Windows = 24;
T("CHECK: 150 окон в БД не записываются", chk);
var res = Simulator.RunMany(p, 100, 2026, 95); int before = db.GetRuns(calc).Count; db.SaveRun(calc.Id, p, res, res.OkShare >= 95);
T("запись расчёта в историю", db.GetRuns(calc).Count == before + 1);
T("история: расчётчик видит только свои", db.GetRuns(calc).All(r => r.UserName == calc.FullName));
T("история: администратор видит все", db.GetRuns(adm).Count == 17);
T("поиск: только невыполненные", db.GetRuns(adm, verdict: false).All(r => !r.Verdict) && db.GetRuns(adm, verdict: false).Count == 2);
T("поиск: период в будущем – пустой список", db.GetRuns(adm, DateTime.Today.AddDays(10), DateTime.Today.AddDays(11)).Count == 0);
T("поиск: за последние сутки", db.GetRuns(adm, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1)).Count == db.GetRuns(adm).Count(r => r.At >= DateTime.Now.AddDays(-1)));
var last = db.GetRuns(adm)[0]; T("удаление записи истории", db.DeleteRun(last.Id) && db.GetRuns(adm).Count == 16);
T("добавление: короткий логин", db.AddUser("ab", "Тест", AppDb.RoleCalc, "secret1") == "Логин не короче 3 символов.");
T("добавление: пустое ФИО", db.AddUser("test1", "  ", AppDb.RoleCalc, "secret1") == "Укажите ФИО.");
T("добавление: короткий пароль", db.AddUser("test1", "Тест Т.Т.", AppDb.RoleCalc, "123") == "Пароль не короче 6 символов.");
T("добавление пользователя", db.AddUser("test1", "Тестов Тест Тестович", AppDb.RoleCalc, "secret1") == "" && db.Authenticate("test1", "secret1") != null);
T("добавление: повтор логина", db.AddUser("test1", "Другой", AppDb.RoleCalc, "secret1") == "Пользователь с таким логином уже есть.");
var t1 = db.GetUsers().First(u => u.Login == "test1"); db.SetUserActive(t1.Id, false);
T("заблокированный не входит", db.Authenticate("test1", "secret1") == null);
db.SetUserActive(t1.Id, true); T("разблокированный входит", db.Authenticate("test1", "secret1") != null);
T("сброс пароля", db.ResetPassword(t1.Id, "newpass1") == "" && db.Authenticate("test1", "newpass1") != null && db.Authenticate("test1", "secret1") == null);
T("пароль хранится как хеш", !db.GetUsers().Any(u => u.Login == "secret1") && AppDb.VerifyPassword("x", "мусор") == false);
bool sqli = db.Authenticate("admin' --", "x") == null; T("SQL-инъекция в логине не проходит", sqli);
try { new AppDb("Host=127.0.0.1;Port=5999;Database=simdb;Username=postgres;Password=postgres;Timeout=3").Authenticate("calc1", "Demo123!"); T("нет связи с БД -> исключение", false); }
catch (Exception e) { T("нет связи с БД -> исключение (" + e.GetType().Name + ")", true); }
Console.WriteLine($"Итого: {n - fail} из {n}");
