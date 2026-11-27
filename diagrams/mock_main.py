from svgkit import *
s=S(1460,1010)
BL='#1f4e8c'
def num(x,y,n):
    s.add(f'<circle cx="{x}" cy="{y}" r="15" fill="{BL}"/>'); s.text(x,y,str(n),15,weight='bold',fill='#fff')
def lead(x1,y1,x2,y2): s.add(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{BL}" stroke-width="1.5"/>')
def bar(x,y,w): s.add(f'<rect x="{x}" y="{y}" width="{w}" height="6" rx="3" fill="#c4c4c4"/>')
X,Y,WW,HH=70,110,1300,820; LP=380*WW/1300
# размеры
s.add(f'<line x1="{X}" y1="40" x2="{X+WW}" y2="40" stroke="{BL}" stroke-width="1.5"/>'); s.text(X+WW/2,24,'Окно: 1300 × 820 px (минимум 1120 × 720), открывается по центру экрана',15,weight='bold',fill=BL)
s.add(f'<line x1="{X}" y1="84" x2="{X+LP}" y2="84" stroke="{BL}" stroke-width="1.5"/><line x1="{X+LP}" y1="84" x2="{X+WW}" y2="84" stroke="{BL}" stroke-width="1.5"/>')
s.text(X+LP/2,70,'380 px (Dock = Left)',14,weight='bold',fill=BL); s.text(X+LP+(WW-LP)/2,70,'Dock = Fill (TableLayoutPanel)',14,weight='bold',fill=BL)
s.add(f'<rect x="{X}" y="{Y}" width="{WW}" height="{HH}" fill="#f4f6f8" stroke="#333" stroke-width="2"/>')
s.add(f'<rect x="{X}" y="{Y}" width="{WW}" height="32" fill="#333"/>'); s.text(X+12,Y+16,'Симулятор нагрузки СЭО МФЦ – Фамилия Имя Отчество (Роль)',15,anchor='start',fill='#fff')
s.add(f'<rect x="{X}" y="{Y+32}" width="{WW}" height="26" fill="#fff" stroke="#999" stroke-dasharray="5,3"/>')
for i,t in enumerate(['Файл','Сценарии','История расчётов','Администрирование*']): s.text(X+14+[0,60,160,310][i],Y+45,t,13,anchor='start')
top=Y+58; bot=Y+HH-26
s.add(f'<rect x="{X+2}" y="{top+2}" width="{LP-4}" height="{bot-top-4}" fill="#dde7f2" stroke="{BL}" stroke-width="2" stroke-dasharray="6,3"/>')
s.text(X+16,top+22,'Параметры нагрузки',14,anchor='start',weight='bold')
fields=['Заявителей в сутки, чел.','Коэфф. неравномерности (час пик)','Среднее время обслуживания, мин','Количество окон приёма','Начало работы, ч','Окончание работы, ч','Льготные категории, %','Предварительная запись, %','Неявка заявителей, %','Норматив ожидания, мин','Начальное число ГСЧ (0 — случайно)','Число прогонов (дней)']
fy=top+48
for f in fields:
    s.text(X+16,fy,f,12,anchor='start'); s.add(f'<rect x="{X+LP-110}" y="{fy-10}" width="94" height="20" fill="#fff" stroke="#666"/>'); fy+=25
s.text(X+16,fy,'Закон времени обслуживания:',12,anchor='start'); fy+=20
s.add(f'<rect x="{X+16}" y="{fy-10}" width="{LP-32}" height="22" fill="#fff" stroke="#666"/>'); s.text(X+22,fy+1,'Экспоненциальное / Эрланга-2 / Постоянное ▾',12,anchor='start'); cb=fy; fy+=30
s.add(f'<rect x="{X+16}" y="{fy-7}" width="13" height="13" fill="#fff" stroke="#333"/>'); s.text(X+36,fy,'Стресс-режим: поток часа пик весь день',12,anchor='start'); st=fy; fy+=24
btn_y=[]
for t,bold in [('Текущая нагрузка (800 чел./сут)',0),('Расчётная нагрузка (1000 чел./сут)',0),('Запустить симуляцию',1),('Подобрать число окон под норматив',1),('Сохранить отчёт (CSV)',0)]:
    fill='#4a7fb0' if bold else '#fff'; col='#fff' if bold else '#000'
    s.add(f'<rect x="{X+16}" y="{fy}" width="{LP-32}" height="26" fill="{fill}" stroke="#333"/>'); s.text(X+LP/2,fy+13,t,12,fill=col); btn_y.append(fy+13); fy+=31
for i in range(4): bar(X+16,fy+6+i*11,LP-40 if i<3 else LP-120)
expl=fy+20
# правая часть
RX=X+LP+6; RW=WW-LP-12; avail=bot-top-46-12
vy=top+6; s.add(f'<rect x="{RX}" y="{vy}" width="{RW/2}" height="40" fill="#d4ead6" stroke="{BL}" stroke-dasharray="6,3"/>'); s.add(f'<rect x="{RX+RW/2}" y="{vy}" width="{RW/2}" height="40" fill="#f3d6d6" stroke="{BL}" stroke-dasharray="6,3"/>')
s.text(RX+8,vy+20,'Норматив ожидания 15 мин ВЫПОЛНЯЕТСЯ: … % талонов …',12,anchor='start',weight='bold',fill='#1b6b2a'); s.text(RX+RW/2+8,vy+20,'Норматив … НЕ ВЫПОЛНЯЕТСЯ: …',12,anchor='start',weight='bold',fill='#8b1a1a')
s.text(RX+RW-8,vy+20,'46 px',13,anchor='end',weight='bold',fill=BL)
ry=vy+46; rh=avail*0.30
s.add(f'<rect x="{RX}" y="{ry}" width="{RW}" height="{rh}" fill="#fff" stroke="{BL}" stroke-dasharray="6,3"/>')
for j,h in enumerate(['РАСЧЁТНАЯ НАГРУЗКА','РЕЗУЛЬТАТЫ СИМУЛЯЦИИ (среднее по N дням)']):
    yy=ry+20+j*95; s.add(f'<text x="{RX+10}" y="{yy}" font-family="DejaVu Sans Mono" font-size="12" font-weight="bold">{h}</text>')
    for k in range(4): bar(RX+20,yy+12+k*16,280); bar(RX+340,yy+12+k*16,170)
s.text(RX+RW-8,ry+rh/2,'30 %',13,anchor='end',weight='bold',fill=BL)
gy=ry+rh+6; gh=avail*0.28
s.add(f'<rect x="{RX}" y="{gy}" width="{RW}" height="{gh}" fill="#fff" stroke="{BL}" stroke-dasharray="6,3"/>')
heads=['Час работы','Заявителей','Среднее ожидание, мин','Макс. ожидание (худший день), мин','Загрузка окон, %']; cw=RW/5
s.add(f'<rect x="{RX}" y="{gy}" width="{RW}" height="34" fill="#e8e8e8"/>')
for i,h in enumerate(heads): s.text(RX+i*cw+6,gy+17,h if len(h)<24 else 'Макс. ожидание (худший…',11,anchor='start')
for r in range(6):
    yy=gy+40+r*20
    if r==3: s.add(f'<rect x="{RX}" y="{yy-8}" width="{RW}" height="18" fill="#f3d6d6"/>')
    for i in range(5): bar(RX+i*cw+8,yy,70)
s.text(RX+RW-8,gy+gh/2,'28 %',13,anchor='end',weight='bold',fill=BL)
s.text(RX+8,gy+gh-10,'Розовая строка — час, в котором среднее ожидание превышает норматив',11,anchor='start',style='italic',fill='#8b1a1a')
cy0=gy+gh+6; ch=bot-6-cy0
for k in range(2):
    x0=RX+k*RW/2+2; w=RW/2-6
    s.add(f'<rect x="{x0}" y="{cy0}" width="{w}" height="{ch}" fill="#fff" stroke="{BL}" stroke-dasharray="6,3"/>')
    s.text(x0+10,cy0+18,['Средняя длина очереди (по N дням), чел.','Среднее ожидание по часам (по N дням), мин'][k],12,anchor='start',weight='bold')
    px,py,pw,ph=x0+40,cy0+34,w-60,ch-60
    s.add(f'<rect x="{px}" y="{py}" width="{pw}" height="{ph}" fill="none" stroke="#999"/>')
    if k==0:
        pts=[(0,0.97),(0.2,0.95),(0.3,0.8),(0.42,0.15),(0.55,0.55),(0.7,0.9),(1,0.97)]
        s.path('M'+' L'.join(f'{px+a*pw},{py+b*ph}' for a,b in pts),2.5)
        s.add(f'<path d="{"M"+" L".join(f"{px+a*pw},{py+b*ph}" for a,b in pts)}" fill="none" stroke="#4a7fb0" stroke-width="2.5"/>')
    else:
        hs=[0.2,0.4,0.62,0.95,0.85,0.45,0.35,0.25,0.2,0.15]; bw=pw/len(hs)
        for i,hh in enumerate(hs): s.add(f'<rect x="{px+i*bw+4}" y="{py+ph*(1-hh*0.8)}" width="{bw-8}" height="{ph*hh*0.8}" fill="{"#c05050" if hh>0.9 else "#4a7fb0"}"/>')
        ly=py+ph*0.2; s.add(f'<line x1="{px}" y1="{ly}" x2="{px+pw}" y2="{ly}" stroke="#d00" stroke-width="2" stroke-dasharray="7,4"/>'); s.text(px+pw-4,ly-10,'норматив N мин',11,anchor='end',fill='#d00')
s.text(RX+RW-8,cy0+ch/2,'42 %',13,anchor='end',weight='bold',fill=BL)
s.add(f'<rect x="{X}" y="{bot}" width="{WW}" height="26" fill="#eee" stroke="#999" stroke-dasharray="5,3"/>'); s.text(X+10,bot+13,'Расчёт записан в историю',12,anchor='start')
# номера
L=X-38
for n,y in [(1,Y+16),(2,Y+45),(3,top+22),(4,top+150),(5,cb),(6,st),(7,(btn_y[0]+btn_y[1])/2),(8,btn_y[3]),(9,expl)]:
    num(L,y,n); lead(L+15,y,X+ (4 if n>2 else 0),y)
R=X+WW+38
for n,y in [(10,vy+20),(11,ry+rh/2),(12,gy+gh/2),(15,bot+13)]:
    num(R,y,n); lead(X+WW,y,R-15,y)
for n,x in [(13,RX+RW/4),(14,RX+3*RW/4)]:
    num(x,bot+65,n); lead(x,bot+26,x,bot+50)
open('mock_main.svg','w').write(s.svg())
