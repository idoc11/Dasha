from svgkit import *
s=S(1500,1120)
def win(x,y,w,h,title):
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#f4f6f8" stroke="#333" stroke-width="2"/>')
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="28" fill="#333"/>'); s.text(x+10,y+14,title,13,anchor='start',fill='#fff')
def fld(x,y,w,lab=None,val=''):
    if lab: s.text(x,y-12,lab,12,anchor='start')
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="22" fill="#fff" stroke="#666"/>'); s.text(x+5,y+11,val,12,anchor='start')
def btn(x,y,w,t,blue=False):
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="26" fill="{"#4a7fb0" if blue else "#fff"}" stroke="#333"/>'); s.text(x+w/2,y+13,t,12,fill='#fff' if blue else '#000')
def grid(x,y,w,h,cols,rows=6):
    s.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="#fff" stroke="#666"/>'); s.add(f'<rect x="{x}" y="{y}" width="{w}" height="22" fill="#e8e8e8" stroke="#666"/>')
    cw=w/len(cols)
    for i,c in enumerate(cols): s.text(x+i*cw+5,y+11,c,11,anchor='start')
    for r in range(rows):
        for i in range(len(cols)): s.add(f'<rect x="{x+i*cw+6}" y="{y+32+r*18}" width="{cw*0.6}" height="5" rx="2" fill="#c4c4c4"/>')
def arrow(pts,lab,lx,ly,anchor='middle'):
    poly(s,pts); s.text(lx,ly,lab,13,anchor=anchor,style='italic')
# 1 вход
win(30,60,300,190,'Симулятор нагрузки СЭО МФЦ – вход')
fld(60,120,240,'Логин','calc1'); fld(60,175,240,'Пароль','••••••••'); btn(60,212,120,'Войти')
s.text(180,40,'1. Окно входа',15,weight='bold')
# 2 главное
MX,MY,MW,MH=470,60,560,370
win(MX,MY,MW,MH,'Симулятор нагрузки СЭО МФЦ – ФИО (Роль)')
s.add(f'<rect x="{MX}" y="{MY+28}" width="{MW}" height="22" fill="#fff" stroke="#999"/>')
for i,t in enumerate(['Файл','Сценарии','История расчётов','Администрирование*']): s.text(MX+10+[0,48,130,270][i],MY+39,t,12,anchor='start')
s.add(f'<rect x="{MX+6}" y="{MY+56}" width="170" height="{MH-62}" fill="#dde7f2" stroke="#4a7fb0"/>')
for i in range(8): s.add(f'<rect x="{MX+14}" y="{MY+70+i*22}" width="90" height="5" rx="2" fill="#9aa"/><rect x="{MX+112}" y="{MY+64+i*22}" width="56" height="16" fill="#fff" stroke="#888"/>')
for i,(t,b) in enumerate([('Запустить симуляцию',1),('Подобрать число окон',1),('Сохранить отчёт (CSV)',0)]): btn(MX+14,MY+252+i*32,154,t,b)
s.add(f'<rect x="{MX+184}" y="{MY+56}" width="{MW-190}" height="30" fill="#d4ead6" stroke="#999"/>'); s.text(MX+192,MY+71,'Норматив … ВЫПОЛНЯЕТСЯ',12,anchor='start',fill='#1b6b2a',weight='bold')
s.add(f'<rect x="{MX+184}" y="{MY+92}" width="{MW-190}" height="80" fill="#fff" stroke="#999"/>')
grid(MX+184,MY+178,MW-190,80,['Час','Заявителей','Ожидание','Загрузка'],2)
s.add(f'<rect x="{MX+184}" y="{MY+264}" width="{(MW-196)/2}" height="98" fill="#fff" stroke="#999"/><rect x="{MX+190+(MW-196)/2}" y="{MY+264}" width="{(MW-196)/2}" height="98" fill="#fff" stroke="#999"/>')
s.text(MX+MW/2,40,'2. Главное окно',15,weight='bold')
# 3 ввод названия сценария
win(1130,60,340,140,'Сохранение сценария'); fld(1150,120,300,'Название сценария:','Расчётная нагрузка, 25 окон'); btn(1150,158,100,'OK'); btn(1260,158,100,'Отмена')
s.text(1300,40,'3. Ввод названия',15,weight='bold')
# 4 сохранение CSV
win(1130,280,340,150,'Сохранить как'); fld(1150,340,300,'Имя файла:','otchet_nagruzka.csv'); fld(1150,390,190,None,'CSV (*.csv)'); btn(1350,390,100,'Сохранить')
s.text(1300,262,'4. Сохранение отчёта',15,weight='bold')
# 5 сценарии
win(30,560,440,300,'Сохранённые сценарии'); grid(40,600,420,200,['Название','Заявителей','Окон','Закон','Создан'])
btn(40,818,120,'Загрузить'); btn(170,818,120,'Удалить'); btn(300,818,120,'Закрыть')
s.text(30,540,'5. Сценарии',15,anchor='start',weight='bold')
# 6 история
win(500,560,540,300,'История расчётов')
s.add('<rect x="512" y="596" width="12" height="12" fill="#fff" stroke="#333"/>'); s.text(530,602,'Период с',11,anchor='start')
fld(590,592,80,None,'дд.мм.гггг'); s.text(676,602,'по',11,anchor='start'); fld(694,592,80,None,'дд.мм.гггг'); fld(790,592,80,None,'Итог ▾'); btn(880,590,70,'Найти'); btn(956,590,76,'Сбросить')
grid(510,626,520,176,['Дата','Расчётчик','Окон','В норме, %','Итог'])
btn(510,818,170,'Загрузить параметры'); btn(690,818,150,'Удалить запись*'); btn(850,818,120,'Закрыть')
s.text(500,540,'6. История расчётов',15,anchor='start',weight='bold')
# 7 пользователи
win(1070,560,400,300,'Пользователи*')
fld(1080,616,80,'Логин'); fld(1166,616,120,'ФИО'); fld(1292,616,80,'Пароль'); fld(1378,616,82,'Роль','▾'); btn(1080,644,100,'Добавить')
grid(1080,676,380,128,['Логин','ФИО','Роль','Активен'],5)
btn(1080,818,190,'Блокировать / разблок.'); btn(1278,818,120,'Сбросить пароль')
s.text(1070,540,'7. Пользователи',15,anchor='start',weight='bold')
# переходы
arrow([(330,225),(MX,225)],'«Войти»',400,190); s.text(400,207,'[данные верны]',13,style='italic')
arrow([(MX+MW,120),(1130,120)],'Сценарии →',1080,98)
s.text(1080,145,'Сохранить…',13,style='italic')
arrow([(MX+MW,365),(1130,365)],'«Сохранить',1080,388); s.text(1080,405,'отчёт (CSV)»',13,style='italic')
arrow([(MX+60,MY+MH),(MX+60,500),(250,500),(250,560)],'Сценарии → Открыть сохранённый…',330,488)
arrow([(MX+MW/2+50,MY+MH),(MX+MW/2+50,560)],'История расчётов',MX+MW/2+60,500,'start')
arrow([(MX+MW-30,MY+MH),(MX+MW-30,470),(1270,470),(1270,560)],'Администрирование → Пользователи…',1200,458)
arrow([(250,860),(250,900),(MX+30,900),(MX+30,1000)],'',0,0)
s.text(300,890,'«Загрузить» – параметры подставляются в главное окно',13,anchor='start',style='italic')
s.add(f'<rect x="{MX-30}" y="1000" width="240" height="40" fill="#fff" stroke="#333" stroke-dasharray="5,3"/>'); s.text(MX+90,1020,'Главное окно (2)',13)
arrow([(700,860),(700,1020),(MX+210,1020)],'',0,0)
s.text(710,940,'«Загрузить параметры»',13,anchor='start',style='italic')
s.text(30,1100,'* – доступно только роли «Администратор»; стрелка – переход по нажатию кнопки или пункта меню',14,anchor='start')
open('mock_flow.svg','w').write(s.svg())
