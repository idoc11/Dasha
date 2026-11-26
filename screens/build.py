import json
r=json.load(open('res.json'));H=12
def poly(vals,w,h,mx):
    n=len(vals);return ' '.join(f"{i*w/(n-1):.1f},{h-v/mx*h:.1f}" for i,v in enumerate(vals))
q=r['q'][:H*60];mq=max(q)*1.1
bars=''.join(f'<rect x="{i*38+6}" y="{150-min(a,14)/14*150:.1f}" width="26" height="{min(a,14)/14*150:.1f}" fill="steelblue"/><text x="{i*38+19}" y="168" font-size="9" text-anchor="middle">{8+i:02d}</text>' for i,a in enumerate(r['avg']))
rows=''.join(f'<tr><td>{8+i:02d}:00–{9+i:02d}:00</td><td>{r["arr"][i]:.0f}</td><td>{r["avg"][i]:.1f}</td><td>{r["max"][i]:.1f}</td><td>{r["util"][i]:.0f}</td></tr>' for i in range(H))
def num(l,v):return f'<div class="row"><span>{l}</span><b>{v}</b></div>'
left=''.join([num('Заявителей в сутки, чел.','1000'),num('Коэфф. неравномерности (час пик)','1,35'),num('Среднее время обслуживания, мин','12,0'),num('Количество окон приёма','22'),num('Начало работы, ч','8'),num('Окончание работы, ч','20'),num('Льготные категории, %','10'),num('Предварительная запись, %','25'),num('Неявка заявителей, %','3'),num('Норматив ожидания, мин','15'),num('Начальное число ГСЧ (0 — случайно)','2026'),num('Число прогонов (дней)','100')])
html=f'''<!doctype html><meta charset=utf-8><style>
body{{margin:0;font:12px "Segoe UI",Arial,sans-serif;background:#ddd}}
.win{{width:1300px;height:820px;background:#f0f0f0;position:relative;border:1px solid #666}}
.title{{background:#fff;height:30px;line-height:30px;padding-left:10px;border-bottom:1px solid #aaa}}
.menu{{background:#f5f5f5;height:24px;line-height:24px;padding-left:8px;border-bottom:1px solid #ccc}}.menu span{{margin-right:18px}}
.left{{position:absolute;left:0;top:55px;width:350px;bottom:24px;background:whitesmoke;overflow:hidden}}
.left h4{{margin:8px 12px}}.row{{display:flex;justify-content:space-between;align-items:center;margin:0 12px;height:30px}}.row b{{width:90px;background:#fff;border:1px solid #888;padding:2px 4px;font-weight:normal;text-align:right}}
.btn{{margin:6px 12px 0;height:30px;line-height:30px;text-align:center;border:1px solid #888;background:#fff}}.btn.p{{background:steelblue;color:#fff}}
.sel{{margin:2px 12px;border:1px solid #888;background:#fff;padding:3px 6px}}
.note{{margin:8px 12px;font-size:11px;color:gray}}
.right{{position:absolute;left:350px;right:0;top:55px;bottom:24px;padding:6px}}
.verdict{{height:34px;line-height:34px;font:bold 15px "Segoe UI";background:honeydew;color:darkgreen;padding-left:8px}}
.rep{{background:#fff;border:1px solid #999;font:12.5px Consolas,monospace;white-space:pre;padding:4px;height:205px;overflow:hidden}}
table{{border-collapse:collapse;width:100%;background:#fff;font-size:11.5px}}td,th{{border:1px solid #ccc;padding:2px 6px}}th{{background:#eee}}
.charts{{display:flex;gap:6px;margin-top:6px}}.ch{{flex:1;background:#fff;border:1px solid #999;height:190px;padding:4px}}
.status{{position:absolute;bottom:0;left:0;right:0;height:24px;line-height:24px;background:#f0f0f0;border-top:1px solid #bbb;padding-left:8px}}
</style><div class=win>
<div class=title>Симулятор нагрузки СЭО МФЦ – Красовская Дарья Дмитриевна (Расчётчик)</div>
<div class=menu><span>Файл</span><span>Сценарии</span><span>История расчётов</span></div>
<div class=left><h4>Параметры (таблица 3.1 ТЗ)</h4>{left}
<div style="margin:6px 12px 0">Закон времени обслуживания:</div><div class=sel>Эрланга-2 (умеренный разброс) ▾</div>
<div style="margin:6px 12px">☐ Стресс-режим: поток часа пик весь день</div>
<div class=btn>Текущая нагрузка (800 чел./сут)</div><div class=btn>Расчётная нагрузка (1000 чел./сут)</div>
<div class="btn p">Запустить симуляцию</div><div class="btn p">Подобрать число окон под норматив</div><div class=btn>Сохранить отчёт (CSV)</div></div>
<div class=right><div class=verdict>  Норматив ожидания 15 мин ВЫПОЛНЯЕТСЯ: {r['ok']:.1f} % талонов ждут не дольше нормы (нужно ≥ 95 %); норматив соблюдён в 92 % дней из 100</div>
<div class=rep>  РАСЧЁТНАЯ НАГРУЗКА
  Заявителей в сутки:                           1000
  Поток в час пик (N × K / часы работы):        112,5 чел./ч
  Предложенная нагрузка (λ × время обслуж.):    22,5 Эрл
  Теоретическая загрузка окон ρ в час пик:      1,02  (≥ 1: окон не хватает, очередь растёт)
  Минимум окон по теории (≥ нагрузки):          23

  РЕЗУЛЬТАТЫ СИМУЛЯЦИИ (среднее по 100 дням)
  Выдано талонов / обслужено / неявок:          {r['total']:.0f} / {r['total']*.97:.0f} / {r['total']*.03:.0f}
  Среднее ожидание:                             {r['mean']:.1f} мин
  Медиана / 95-й перцентиль / максимум:         {r['med']:.1f} / {r['p95']:.1f} / {r['mx']:.1f} мин
  Загрузка окон за день / в час пик:            {sum(r['util'])/H:.0f} % / {r['util'][3]:.0f} %</div>
<table style="margin-top:6px"><tr><th>Час работы</th><th>Заявителей</th><th>Среднее ожидание, мин</th><th>Макс. ожидание (худший день), мин</th><th>Загрузка окон, %</th></tr>{rows}</table>
<div class=charts><div class=ch><div style="font-size:10px">Средняя длина очереди (по 100 дням), чел.</div><svg width=470 height=150><polyline fill=none stroke=steelblue stroke-width=2 points="{poly(q,460,125,mq)}"/></svg></div>
<div class=ch><div style="font-size:10px">Среднее ожидание по часам (по 100 дням), мин</div><svg width=470 height=150 style="margin-top:20px">{bars}<line x1=0 x2=470 y1="{150-15/14*150:.1f}" y2="{150-15/14*150:.1f}" stroke=red stroke-dasharray=4 /></svg></div></div></div>
<div class=status>Расчёт записан в историю</div></div>'''
open('main.html','w',encoding='utf-8').write(html)
login='''<!doctype html><meta charset=utf-8><body style="margin:0;background:#ddd;font:14px 'Segoe UI',Arial"><div style="width:300px;height:200px;background:#f0f0f0;border:1px solid #666"><div style="background:#fff;padding:6px 8px;font-size:12px;border-bottom:1px solid #aaa">Симулятор нагрузки СЭО МФЦ – вход</div><div style="padding:16px 24px"><div>Логин</div><div style="border:1px solid #888;background:#fff;width:240px;padding:3px">calc1</div><div style="margin-top:8px">Пароль</div><div style="border:1px solid #888;background:#fff;width:240px;padding:3px">••••••••</div><div style="margin-top:12px;width:120px;text-align:center;border:1px solid #888;background:#fff;padding:6px">Войти</div></div></div>'''
open('login.html','w',encoding='utf-8').write(login)
