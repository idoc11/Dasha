using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Npgsql;

namespace SEO_LoadSimulator
{
    public record AppUser(int Id, string Login, string FullName, string Role)
    {
        public bool IsAdmin => Role == AppDb.RoleAdmin;
    }
    public record ScenarioRow(int Id, string Name, SimParams Params, DateTime CreatedAt);
    public record RunRow(long Id, DateTime At, string UserName, SimParams Params, double OkShare, double MeanWait,
                         double P95Wait, double DaysOkPct, bool Verdict);
    public record UserRow(int Id, string Login, string FullName, string Role, bool Active);

    /// <summary>Работа с базой данных программы: пользователи и роли, сценарии, история расчётов.</summary>
    public class AppDb
    {
        public const string RoleCalc = "Расчётчик";
        public const string RoleAdmin = "Администратор";

        readonly string _cs;
        public AppDb(string cs) { _cs = cs; }
        public static AppDb Default() => new AppDb(Environment.GetEnvironmentVariable("SIM_DB")
            ?? "Host=localhost;Database=simdb;Username=postgres;Password=postgres");

        NpgsqlConnection Open() { var c = new NpgsqlConnection(_cs); c.Open(); return c; }

        static NpgsqlCommand Q(NpgsqlConnection c, string sql, params object?[] p)
        {
            var cmd = new NpgsqlCommand(sql, c);
            for (int i = 0; i + 1 < p.Length; i += 2) cmd.Parameters.AddWithValue((string)p[i]!, p[i + 1] ?? DBNull.Value);
            return cmd;
        }

        // ---------- пароли: v1$итерации$соль$хеш (PBKDF2-SHA256) ----------
        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100000, HashAlgorithmName.SHA256, 32);
            return "v1$100000$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool VerifyPassword(string password, string stored)
        {
            var p = stored.Split('$');
            if (p.Length != 4 || p[0] != "v1") return false;
            var expected = Convert.FromBase64String(p[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, Convert.FromBase64String(p[2]), int.Parse(p[1]), HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }

        public AppUser? Authenticate(string login, string password)
        {
            using var c = Open();
            using var cmd = Q(c, "select u.user_id, u.login, u.full_name, u.role_name, u.password_hash from app_user u " +
                                 "where u.login = @l and u.is_active", "l", login);
            using var r = cmd.ExecuteReader();
            if (!r.Read() || !VerifyPassword(password, r.GetString(4))) return null;
            return new AppUser(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3));
        }

        // ---------- сценарии ----------
        const string ParamCols = "per_day, peak_k, service_min, windows, open_hour, close_hour, priv_pct, appt_pct, noshow_pct, norm_min, dist, seed, stress, runs";

        static SimParams ReadParams(NpgsqlDataReader r, int o) => new SimParams
        {
            PerDay = r.GetInt32(o), PeakK = Convert.ToDouble(r.GetValue(o + 1)), ServiceMin = Convert.ToDouble(r.GetValue(o + 2)),
            Windows = r.GetInt32(o + 3), OpenHour = r.GetInt32(o + 4), CloseHour = r.GetInt32(o + 5),
            PrivilegedPct = Convert.ToDouble(r.GetValue(o + 6)), AppointmentPct = Convert.ToDouble(r.GetValue(o + 7)),
            NoShowPct = Convert.ToDouble(r.GetValue(o + 8)), NormMin = Convert.ToDouble(r.GetValue(o + 9)),
            Dist = (ServiceDist)Convert.ToInt32(r.GetValue(o + 10)), Seed = r.GetInt32(o + 11), Stress = r.GetBoolean(o + 12), Runs = r.GetInt32(o + 13)
        };

        static object?[] ParamValues(SimParams p) => new object?[]
        {
            "pd", p.PerDay, "pk", (decimal)p.PeakK, "sm", (decimal)p.ServiceMin, "w", p.Windows, "oh", p.OpenHour, "ch", p.CloseHour,
            "pr", (decimal)p.PrivilegedPct, "ap", (decimal)p.AppointmentPct, "ns", (decimal)p.NoShowPct, "nm", (decimal)p.NormMin,
            "d", (short)p.Dist, "sd", p.Seed, "st", p.Stress, "rn", p.Runs
        };

        static object?[] Join(object?[] a, params object?[] b)
        {
            var res = new object?[a.Length + b.Length];
            a.CopyTo(res, 0); b.CopyTo(res, a.Length); return res;
        }

        /// <summary>Сохраняет сценарий; если у пользователя уже есть сценарий с таким именем – обновляет его.</summary>
        public void SaveScenario(int userId, string name, SimParams p)
        {
            using var c = Open();
            using var cmd = Q(c,
                "insert into scenario(user_id, name, " + ParamCols + ") values (@u, @n, @pd, @pk, @sm, @w, @oh, @ch, @pr, @ap, @ns, @nm, @d, @sd, @st, @rn) " +
                "on conflict (user_id, name) do update set per_day = @pd, peak_k = @pk, service_min = @sm, windows = @w, open_hour = @oh, " +
                "close_hour = @ch, priv_pct = @pr, appt_pct = @ap, noshow_pct = @ns, norm_min = @nm, dist = @d, seed = @sd, stress = @st, runs = @rn",
                Join(ParamValues(p), "u", userId, "n", name));
            cmd.ExecuteNonQuery();
        }

        public List<ScenarioRow> GetScenarios(AppUser user)
        {
            var res = new List<ScenarioRow>();
            using var c = Open();
            using var cmd = Q(c, "select scenario_id, name, created_at, " + ParamCols + " from scenario " +
                                 (user.IsAdmin ? "" : "where user_id = @u ") + "order by name", "u", user.Id);
            using var r = cmd.ExecuteReader();
            while (r.Read()) res.Add(new ScenarioRow(r.GetInt32(0), r.GetString(1), ReadParams(r, 3), r.GetDateTime(2)));
            return res;
        }

        public bool DeleteScenario(int id, AppUser user)
        {
            using var c = Open();
            using var cmd = Q(c, "delete from scenario where scenario_id = @id" + (user.IsAdmin ? "" : " and user_id = @u"), "id", id, "u", user.Id);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ---------- история расчётов ----------
        public void SaveRun(int userId, SimParams p, SimResult r, bool verdict)
        {
            using var c = Open();
            using var cmd = Q(c,
                "insert into sim_run(user_id, " + ParamCols + ", ok_share, mean_wait, p95_wait, days_ok_pct, verdict) " +
                "values (@u, @pd, @pk, @sm, @w, @oh, @ch, @pr, @ap, @ns, @nm, @d, @sd, @st, @rn, @ok, @mw, @p95, @dk, @v)",
                Join(ParamValues(p), "u", userId, "ok", (decimal)Math.Round(r.OkShare, 1), "mw", (decimal)Math.Round(r.MeanWait, 1),
                     "p95", (decimal)Math.Round(r.P95Wait, 1), "dk", (decimal)Math.Round(r.DaysOkPct, 1), "v", verdict));
            cmd.ExecuteNonQuery();
        }

        public List<RunRow> GetRuns(AppUser user, int limit = 200)
        {
            var res = new List<RunRow>();
            using var c = Open();
            using var cmd = Q(c, "select s.run_id, s.run_at, u.full_name, " + ParamCols.Replace("per_day", "s.per_day").Replace("peak_k", "s.peak_k") +
                ", s.ok_share, s.mean_wait, s.p95_wait, s.days_ok_pct, s.verdict from sim_run s join app_user u on u.user_id = s.user_id " +
                (user.IsAdmin ? "" : "where s.user_id = @u ") + "order by s.run_at desc limit @lim", "u", user.Id, "lim", limit);
            using var r = cmd.ExecuteReader();
            while (r.Read())
                res.Add(new RunRow(r.GetInt64(0), r.GetDateTime(1), r.GetString(2), ReadParams(r, 3),
                    Convert.ToDouble(r.GetValue(17)), Convert.ToDouble(r.GetValue(18)), Convert.ToDouble(r.GetValue(19)),
                    Convert.ToDouble(r.GetValue(20)), r.GetBoolean(21)));
            return res;
        }

        public bool DeleteRun(long id)
        {
            using var c = Open();
            using var cmd = Q(c, "delete from sim_run where run_id = @id", "id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        // ---------- пользователи (администратор) ----------
        public List<UserRow> GetUsers()
        {
            var res = new List<UserRow>();
            using var c = Open();
            using var cmd = Q(c, "select u.user_id, u.login, u.full_name, u.role_name, u.is_active from app_user u order by u.role_name, u.login");
            using var r = cmd.ExecuteReader();
            while (r.Read()) res.Add(new UserRow(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetBoolean(4)));
            return res;
        }

        public string AddUser(string login, string fullName, string role, string password)
        {
            if (login.Trim().Length < 3) return "Логин не короче 3 символов";
            if (fullName.Trim().Length == 0) return "Укажите ФИО";
            if (password.Length < 6) return "Пароль не короче 6 символов";
            try
            {
                using var c = Open();
                using var cmd = Q(c, "insert into app_user(login, password_hash, full_name, role_name) values (@l, @h, @f, @r)",
                    "l", login.Trim(), "h", HashPassword(password), "f", fullName.Trim(), "r", role);
                cmd.ExecuteNonQuery();
                return "";
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") { return "Пользователь с таким логином уже есть"; }
        }

        public void SetUserActive(int id, bool active)
        {
            using var c = Open();
            using var cmd = Q(c, "update app_user set is_active = @a where user_id = @id", "a", active, "id", id);
            cmd.ExecuteNonQuery();
        }

        public string ResetPassword(int id, string password)
        {
            if (password.Length < 6) return "Пароль не короче 6 символов";
            using var c = Open();
            using var cmd = Q(c, "update app_user set password_hash = @h where user_id = @id", "h", HashPassword(password), "id", id);
            cmd.ExecuteNonQuery();
            return "";
        }
    }
}
