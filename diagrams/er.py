from svgkit import *
import collections
cols=collections.OrderedDict()
for l in open('cols.txt',encoding='utf-8'):
    t,c,dt,ml,np_,ns,nul,pos=l.rstrip('\n').split('|')
    typ={'integer':'INT','bigint':'BIGINT','boolean':'BOOLEAN','smallint':'SMALLINT','timestamp without time zone':'TIMESTAMP'}.get(dt,dt)
    if dt=='character varying': typ=f'VARCHAR({ml})'
    if dt=='numeric': typ=f'NUMERIC({np_},{ns})'
    cols.setdefault(t,[]).append((c,typ))
pk=set();fk=set()
for l in open('keys.txt',encoding='utf-8'):
    t,c,k,ref=l.rstrip('\n').split('|')
    if k=='PRIMARY KEY': pk.add((t,c))
    if k=='FOREIGN KEY': fk.add((t,c))
RH=26; TW=420
def table(s,x,y,t,title):
    rows=cols[t]; h=44+RH*len(rows)+8
    s.rect(x,y,TW,h,2.2); s.add(f'<rect x="{x}" y="{y}" width="{TW}" height="40" fill="#e8e8e8" stroke="#000" stroke-width="2.2"/>')
    s.text(x+TW/2,y+20,f'{t} ({title})',16,weight='bold')
    pos={}
    for i,(c,typ) in enumerate(rows):
        yy=y+44+RH*i+RH/2
        mark='ПК' if (t,c) in pk else ('ВК' if (t,c) in fk else '')
        s.text(x+12,yy,mark,13,anchor='start',weight='bold')
        dec=' text-decoration="underline"' if mark=='ПК' else ''
        s.add(f'<text x="{x+52}" y="{yy+5}" font-family="DejaVu Sans" font-size="14"{dec}>{c}</text>')
        s.text(x+TW-12,yy,typ,13,anchor='end')
        pos[c]=yy
    return pos,h
s=S(1100,1290)
pa,ha=table(s,40,420,'app_user','Пользователь')
ps,hs=table(s,640,30,'scenario','Сценарий')
pr,hr=table(s,640,30+hs+50,'sim_run','Расчёт')
y0=pa['user_id']
poly(s,[(40+TW,y0),(540,y0)],arrow=None); s.text(40+TW+18,y0-14,'1',17,weight='bold')
s.add(f'<circle cx="540" cy="{y0}" r="4" fill="#000"/>')
for y2 in (ps['user_id'],pr['user_id']):
    poly(s,[(540,y0),(540,y2),(640,y2)],arrow=None); s.text(640-18,y2-14,'∞',21,weight='bold')
lx,ly=40,420+ha+40
s.rect(lx,ly,TW,250,1.5,dash='6,4')
s.text(lx+14,ly+24,'Обозначения',15,anchor='start',weight='bold')
for i,t in enumerate(['ПК – первичный ключ (имя подчёркнуто)','ВК – внешний ключ','1 — ∞ – связь «один ко многим»','INT, BIGINT, SMALLINT – целые числа','VARCHAR(n) – строка длиной до n','NUMERIC(p,s) – число с дробной частью','BOOLEAN – логическое; TIMESTAMP – дата','и время']):
    s.text(lx+14,ly+56+i*24,t,14,anchor='start')
open('er.svg','w').write(s.svg())
