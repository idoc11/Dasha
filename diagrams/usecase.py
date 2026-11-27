from svgkit import *
s=S(1320,1040)
s.rect(270,20,1030,1000,3)
s.text(785,52,'Программа моделирования нагрузки SEO_LoadSimulator',19,style='italic')
X1,X2,RX,RY=520,1060,175,36
U={'login':(X1,120,'Войти в систему'),'params':(X1,230,'Задать параметры\nнагрузки'),
'run':(X1,340,'Выполнить расчёт'),'find':(X1,450,'Подобрать число окон\nпод норматив'),
'csv':(X1,560,'Сохранить отчёт (CSV)'),'scen':(X1,670,'Сохранить, загрузить,\nудалить сценарий'),
'hist':(X1,780,'Просмотреть историю\nрасчётов'),'users':(X1,950,'Управлять учётными\nзаписями'),
'save':(X2,340,'Записать расчёт\nв историю'),'search':(X2,680,'Найти расчёты\nпо периоду и итогу'),
'del':(X2,850,'Удалить запись\nистории')}
for k,(x,y,t) in U.items(): s.ellipse(x,y,RX,RY,t,16)
cx,cy=110,430; ax,ay=110,900
s.actor(cx,cy,'Расчётчик'); s.actor(ax,ay,'Администратор')
s.line(ax,ay-75,cx,cy+118,2,arrow='tri')
for k in ['login','params','run','find','csv','scen','hist']:
    x,y,_=U[k]; s.line(cx+32,cy-28,x-RX,y,2)
x,y,_=U['users']; s.line(ax+32,ay-28,x-RX,y,2)
x,y,_=U['del']; s.path(f'M{ax+32},{ay-28} L{300},{ay-28} L{300},{1000} L{x},{1000} L{x},{y+RY}',2)
def dep(a,b,lab,lx,ly):
    x1,y1,_=U[a]; x2,y2,_=U[b]; p1=edge_pt(x1,y1,RX,RY,x2,y2); p2=edge_pt(x2,y2,RX,RY,x1,y1)
    s.line(*p1,*p2,2,dash='9,6',arrow='open'); s.text(lx,ly,lab,15)
dep('run','save','«include»',790,322)
dep('find','run','«include»',575,395)
dep('search','hist','«extend»',745,722)
dep('del','hist','«extend»',800,842)
open('usecase.svg','w').write(s.svg())
