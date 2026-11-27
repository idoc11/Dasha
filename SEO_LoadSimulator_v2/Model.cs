using System;
using System.Collections.Generic;

namespace SEO_LoadSimulator
{
    public enum ServiceDist
    {
        Exponential = 0,
        Erlang2 = 1,
        Constant = 2,
    }

    /// <summary>
    /// Параметры нагрузки (исходные данные ТЗ) и режима расчёта.
    /// </summary>
    public class SimParams
    {
        public int PerDay; // заявителей в сутки
        public double PeakK; // коэффициент неравномерности в час пик
        public double ServiceMin; // среднее время обслуживания, мин
        public int Windows; // число окон
        public int OpenHour,
            CloseHour;
        public double PrivilegedPct,
            AppointmentPct,
            NoShowPct;
        public double NormMin; // норматив ожидания, мин
        public ServiceDist Dist;
        public int Seed; // 0 – случайно
        public bool Stress; // поток часа пик держится весь день
        public int Runs; // число прогонов (дней)
    }

    class Ticket
    {
        public double Arrival; // момент выдачи талона, с от начала дня

        // 1 – льготные, 2 – предзапись, 3 – остальные
        public int Priority;
        public double Service; // длительность обслуживания, с
        public bool NoShow; // заявитель не явился
        public double Start; // момент начала обслуживания, с
    }

    public class SimResult
    {
        public int Runs = 1;
        public int WorkHours;
        public double Total,
            Served,
            NoShow,
            MeanWait,
            MedianWait,
            P95Wait,
            MaxWait,
            OkShare;
        public double PeakQueue,
            Util,
            PeakUtil,
            OvertimeMin,
            LambdaPeak,
            Erlang,
            Rho;
        public double[] HArr = new double[0],
            HAvg = new double[0],
            HMax = new double[0],
            HUtil = new double[0],
            Queue = new double[0];
        public int PeakQueueMin;
        public double DaysOkPct,
            WorstOkShare,
            WorstMaxWait,
            WorstPeakQueue;
    }

    /// <summary>
    /// Имитационная модель: пуассоновский поток с часовым профилем,
    /// универсальные окна,
    /// порядок вызова: льготные → предзапись → остальные, внутри группы
    /// по времени выдачи.</summary>
    public static class Simulator
    {
        // неявка занимает окно на 3 мин (3 вызова по 60 с)
        const double NoShowBlockSec = 180.0;

        static double Exp(Random rnd, double mean)
        {
            return -mean * Math.Log(1.0 - rnd.NextDouble());
        }

        static double Sample(Random rnd, ServiceDist d, double meanSec)
        {
            switch (d)
            {
                case ServiceDist.Exponential:
                    return Exp(rnd, meanSec);
                case ServiceDist.Erlang2:
                    return Exp(rnd, meanSec / 2.0)
                        + Exp(rnd, meanSec / 2.0);
                default:
                    return meanSec;
            }
        }

        public static int PeakIndex(int hours)
        {
            return Math.Min(3, hours - 2);
        }

        public static bool WeightsValid(int hours, double k)
        {
            if (hours < 4)
                return false;
            return ((hours - 1) - 2.0 * k) / (hours - 3) > 0.05;
        }

        // веса часов: пик = K, соседние часы = (1 + K) / 2, остальные
        // подобраны так, чтобы сумма была равна числу часов
        public static double[] HourWeights(int hours, double k)
        {
            var w = new double[hours];
            int pk = PeakIndex(hours);
            double rest = ((hours - 1) - 2.0 * k) / (hours - 3);
            for (int i = 0; i < hours; i++)
                w[i] = rest;
            w[pk] = k;
            w[pk - 1] = (1.0 + k) / 2.0;
            w[pk + 1] = (1.0 + k) / 2.0;
            return w;
        }

        public static SimResult Run(SimParams p, Random rnd)
        {
            int hours = p.CloseHour - p.OpenHour;
            double[] weights = HourWeights(hours, p.PeakK);

            // 1. поток талонов
            var tickets = new List<Ticket>();
            for (int h = 0; h < hours; h++)
            {
                double rate = p.Stress
                    ? p.PerDay * p.PeakK / hours
                    : p.PerDay * weights[h] / hours;
                if (rate <= 0)
                    continue;
                double mean = 3600.0 / rate;
                double t = h * 3600.0 + Exp(rnd, mean);
                while (t < (h + 1) * 3600.0)
                {
                    var tk = new Ticket();
                    tk.Arrival = t;
                    double x = rnd.NextDouble() * 100.0;
                    if (x < p.PrivilegedPct)
                        tk.Priority = 1;
                    else if (x < p.PrivilegedPct + p.AppointmentPct)
                        tk.Priority = 2;
                    else
                        tk.Priority = 3;
                    tk.NoShow = rnd.NextDouble() * 100.0 < p.NoShowPct;
                    tk.Service = Sample(
                        rnd,
                        p.Dist,
                        p.ServiceMin * 60.0
                    );
                    tickets.Add(tk);
                    t += Exp(rnd, mean);
                }
            }

            // 2. обслуживание: свободное окно берёт талон по приоритету
            int windows = p.Windows;
            var free = new double[windows];
            var queues = new Queue<Ticket>[4];
            for (int i = 1; i <= 3; i++)
                queues[i] = new Queue<Ticket>();
            var all = new List<Ticket>();
            int waiting = 0,
                next = 0;
            double last = 0.0;
            while (next < tickets.Count || waiting > 0)
            {
                int wi = 0;
                for (int j = 1; j < windows; j++)
                    if (free[j] < free[wi])
                        wi = j;
                double now = Math.Max(free[wi], last);
                if (waiting == 0 && next < tickets.Count)
                    now = Math.Max(now, tickets[next].Arrival);
                while (
                    next < tickets.Count && tickets[next].Arrival <= now
                )
                {
                    queues[tickets[next].Priority]
                        .Enqueue(tickets[next]);
                    waiting++;
                    next++;
                }
                Ticket? cur = null;
                for (int k = 1; k <= 3 && cur == null; k++)
                    if (queues[k].Count > 0)
                        cur = queues[k].Dequeue();
                waiting--;
                cur!.Start = now;
                free[wi] =
                    now + (cur.NoShow ? NoShowBlockSec : cur.Service);
                last = now;
                all.Add(cur);
            }

            // 3. статистика
            var r = new SimResult();
            r.WorkHours = hours;
            r.Total = tickets.Count;
            double maxFree = 0.0;
            for (int i = 0; i < windows; i++)
                if (free[i] > maxFree)
                    maxFree = free[i];
            double closeSec = hours * 3600.0;
            double end = Math.Max(closeSec, maxFree);
            int nH = (int)Math.Ceiling(end / 3600.0);
            r.HArr = new double[nH];
            r.HAvg = new double[nH];
            r.HMax = new double[nH];
            r.HUtil = new double[nH];
            var waitSum = new double[nH];
            var waitCnt = new int[nH];
            var busy = new double[nH];
            double busyTotal = 0.0;
            var waits = new List<double>();
            foreach (var tk in all)
            {
                int ha = (int)(tk.Arrival / 3600.0);
                r.HArr[ha] += 1.0;
                double dur = tk.NoShow ? NoShowBlockSec : tk.Service;
                double st = tk.Start,
                    en = st + dur;
                busyTotal += dur;
                int h1 = (int)(st / 3600.0),
                    h2 = (int)(en / 3600.0);
                if (h2 >= nH)
                    h2 = nH - 1;
                for (int k = h1; k <= h2; k++)
                {
                    double ov =
                        Math.Min(en, (k + 1) * 3600.0)
                        - Math.Max(st, k * 3600.0);
                    if (ov > 0)
                        busy[k] += ov;
                }
                if (tk.NoShow)
                    continue;
                double wait = st - tk.Arrival;
                waits.Add(wait);
                waitSum[ha] += wait;
                waitCnt[ha]++;
                if (wait / 60.0 > r.HMax[ha])
                    r.HMax[ha] = wait / 60.0;
            }
            for (int i = 0; i < nH; i++)
            {
                r.HAvg[i] =
                    waitCnt[i] > 0
                        ? waitSum[i] / waitCnt[i] / 60.0
                        : 0.0;
                r.HUtil[i] = busy[i] / (windows * 3600.0) * 100.0;
            }
            r.Served = waits.Count;
            r.NoShow = all.Count - waits.Count;
            if (waits.Count > 0)
            {
                waits.Sort();
                double sum = 0.0;
                int ok = 0;
                double norm = p.NormMin * 60.0;
                foreach (double w in waits)
                {
                    sum += w;
                    if (w <= norm)
                        ok++;
                }
                r.MeanWait = sum / waits.Count / 60.0;
                r.MedianWait = waits[waits.Count / 2] / 60.0;
                int idx = (int)Math.Ceiling(0.95 * waits.Count) - 1;
                if (idx < 0)
                    idx = 0;
                r.P95Wait = waits[idx] / 60.0;
                r.MaxWait = waits[waits.Count - 1] / 60.0;
                r.OkShare = 100.0 * ok / waits.Count;
            }
            r.Util = busyTotal / (windows * end) * 100.0;
            r.PeakUtil = r.HUtil[PeakIndex(hours)];
            r.OvertimeMin = Math.Max(0.0, maxFree - closeSec) / 60.0;

            // длина очереди по минутам
            int nm = (int)Math.Ceiling(end / 60.0) + 1;
            var delta = new int[nm + 1];
            foreach (var tk in all)
            {
                int a = (int)(tk.Arrival / 60.0),
                    s = (int)(tk.Start / 60.0);
                if (s > nm)
                    s = nm;
                delta[a]++;
                delta[s]--;
            }
            r.Queue = new double[nm];
            int run = 0;
            for (int m = 0; m < nm; m++)
            {
                run += delta[m];
                r.Queue[m] = run;
                if (run > r.PeakQueue)
                {
                    r.PeakQueue = run;
                    r.PeakQueueMin = m;
                }
            }
            r.LambdaPeak = p.PerDay * p.PeakK / hours;
            r.Erlang = r.LambdaPeak * p.ServiceMin / 60.0;
            r.Rho = r.Erlang / windows;
            return r;
        }

        /// <summary>
        /// Несколько независимых дней: начальное значение ГСЧ i-го дня
        /// = baseSeed + i·7919.
        /// </summary>
        public static SimResult RunMany(
            SimParams p,
            int runs,
            int baseSeed,
            double target
        )
        {
            if (runs < 1)
                runs = 1;
            var list = new List<SimResult>();
            for (int i = 0; i < runs; i++)
                list.Add(Run(p, new Random(baseSeed + i * 7919)));
            return Average(list, target);
        }

        static SimResult Average(List<SimResult> list, double target)
        {
            int n = list.Count;
            var f = list[0];
            var r = new SimResult();
            r.Runs = n;
            r.WorkHours = f.WorkHours;
            r.LambdaPeak = f.LambdaPeak;
            r.Erlang = f.Erlang;
            r.Rho = f.Rho;
            int hLen = 0,
                qLen = 0;
            foreach (var x in list)
            {
                hLen = Math.Max(hLen, x.HArr.Length);
                qLen = Math.Max(qLen, x.Queue.Length);
            }
            r.HArr = new double[hLen];
            r.HAvg = new double[hLen];
            r.HMax = new double[hLen];
            r.HUtil = new double[hLen];
            r.Queue = new double[qLen];
            r.WorstOkShare = double.MaxValue;
            int daysOk = 0;
            foreach (var x in list)
            {
                r.Total += x.Total;
                r.Served += x.Served;
                r.NoShow += x.NoShow;
                r.MeanWait += x.MeanWait;
                r.MedianWait += x.MedianWait;
                r.P95Wait += x.P95Wait;
                r.MaxWait += x.MaxWait;
                r.OkShare += x.OkShare;
                r.PeakQueue += x.PeakQueue;
                r.Util += x.Util;
                r.PeakUtil += x.PeakUtil;
                r.OvertimeMin += x.OvertimeMin;
                if (x.OkShare >= target)
                    daysOk++;
                if (x.OkShare < r.WorstOkShare)
                    r.WorstOkShare = x.OkShare;
                if (x.MaxWait > r.WorstMaxWait)
                    r.WorstMaxWait = x.MaxWait;
                if (x.PeakQueue > r.WorstPeakQueue)
                    r.WorstPeakQueue = x.PeakQueue;
                for (int i = 0; i < x.HArr.Length; i++)
                {
                    r.HArr[i] += x.HArr[i] / n;
                    r.HAvg[i] += x.HAvg[i] / n;
                    r.HUtil[i] += x.HUtil[i] / n;
                    if (x.HMax[i] > r.HMax[i])
                        r.HMax[i] = x.HMax[i];
                }
                for (int i = 0; i < x.Queue.Length; i++)
                    r.Queue[i] += x.Queue[i] / n;
            }
            r.Total /= n;
            r.Served /= n;
            r.NoShow /= n;
            r.MeanWait /= n;
            r.MedianWait /= n;
            r.P95Wait /= n;
            r.MaxWait /= n;
            r.OkShare /= n;
            r.PeakQueue /= n;
            r.Util /= n;
            r.PeakUtil /= n;
            r.OvertimeMin /= n;
            r.DaysOkPct = 100.0 * daysOk / n;
            double best = -1.0;
            for (int i = 0; i < qLen; i++)
                if (r.Queue[i] > best)
                {
                    best = r.Queue[i];
                    r.PeakQueueMin = i;
                }
            return r;
        }
    }
}
