# Простой построитель SVG для диаграмм курсового проекта (чёрно-белый стиль)
import html, math
F='DejaVu Sans'
class S:
    def __init__(s,w,h): s.w,s.h,s.e=w,h,[]
    def add(s,x): s.e.append(x)
    def text(s,x,y,t,size=15,anchor='middle',weight='normal',style='normal',fill='#000'):
        lines=t.split('\n'); y0=y-(len(lines)-1)*size*0.6
        for i,l in enumerate(lines):
            s.add(f'<text x="{x}" y="{y0+i*size*1.2+size*0.35}" font-family="{F}" font-size="{size}" text-anchor="{anchor}" font-weight="{weight}" font-style="{style}" fill="{fill}">{html.escape(l)}</text>')
    def rect(s,x,y,w,h,sw=2,dash=None,fill='#fff',rx=0):
        d=f' stroke-dasharray="{dash}"' if dash else ''
        s.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{rx}" fill="{fill}" stroke="#000" stroke-width="{sw}"{d}/>')
    def ellipse(s,cx,cy,rx,ry,t,size=15):
        s.add(f'<ellipse cx="{cx}" cy="{cy}" rx="{rx}" ry="{ry}" fill="#fff" stroke="#000" stroke-width="2.2"/>'); s.text(cx,cy,t,size)
    def line(s,x1,y1,x2,y2,sw=2,dash=None,arrow=None):
        d=f' stroke-dasharray="{dash}"' if dash else ''
        m=f' marker-end="url(#{arrow})"' if arrow else ''
        s.add(f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="#000" stroke-width="{sw}"{d}{m}/>')
    def path(s,d,sw=2,dash=None,arrow=None,fill='none'):
        dd=f' stroke-dasharray="{dash}"' if dash else ''
        m=f' marker-end="url(#{arrow})"' if arrow else ''
        s.add(f'<path d="{d}" fill="{fill}" stroke="#000" stroke-width="{sw}"{dd}{m}/>')
    def actor(s,x,y,name):
        s.add(f'<circle cx="{x}" cy="{y-58}" r="16" fill="#fff" stroke="#000" stroke-width="2.5"/>')
        s.line(x,y-42,x,y+5,2.5); s.line(x-30,y-28,x+30,y-28,2.5); s.line(x,y+5,x-24,y+45,2.5); s.line(x,y+5,x+24,y+45,2.5)
        s.text(x,y+68,name,18)
    def svg(s):
        defs='''<defs>
<marker id="open" markerWidth="14" markerHeight="14" refX="12" refY="7" orient="auto" markerUnits="userSpaceOnUse"><path d="M1,1 L12,7 L1,13" fill="none" stroke="#000" stroke-width="2"/></marker>
<marker id="tri" markerWidth="24" markerHeight="24" refX="21" refY="12" orient="auto" markerUnits="userSpaceOnUse"><path d="M2,2 L21,12 L2,22 Z" fill="#fff" stroke="#000" stroke-width="2"/></marker>
<marker id="fill" markerWidth="14" markerHeight="14" refX="12" refY="7" orient="auto" markerUnits="userSpaceOnUse"><path d="M1,1 L12,7 L1,13 Z" fill="#000"/></marker>
</defs>'''
        return f'<svg xmlns="http://www.w3.org/2000/svg" width="{s.w}" height="{s.h}" viewBox="0 0 {s.w} {s.h}"><rect width="100%" height="100%" fill="#fff"/>{defs}'+''.join(s.e)+'</svg>'
def edge_pt(cx,cy,rx,ry,tx,ty):
    a=math.atan2(ty-cy,tx-cx); return cx+rx*math.cos(a), cy+ry*math.sin(a)
def box(s,cx,cy,w,h,t,size=15,rx=16):
    s.rect(cx-w/2,cy-h/2,w,h,2,rx=rx); s.text(cx,cy,t,size)
def diamond(s,cx,cy,w,h,t='',size=14):
    s.add(f'<polygon points="{cx},{cy-h/2} {cx+w/2},{cy} {cx},{cy+h/2} {cx-w/2},{cy}" fill="#fff" stroke="#000" stroke-width="2"/>')
    if t: s.text(cx,cy,t,size)
def poly(s,pts,arrow='fill',dash=None):
    d='M'+' L'.join(f'{x},{y}' for x,y in pts); s.path(d,2,dash=dash,arrow=arrow)
def state(s,cx,cy,w,h,name,do,size=15):
    s.rect(cx-w/2,cy-h/2,w,h,2,rx=18); s.text(cx,cy-h/2+20,name,size+1,weight='bold')
    s.line(cx-w/2,cy-h/2+38,cx+w/2,cy-h/2+38,1.5); s.text(cx,cy+12,do,size-1)
def final(s,x,y):
    s.add(f'<circle cx="{x}" cy="{y}" r="18" fill="#fff" stroke="#000" stroke-width="2.5"/><circle cx="{x}" cy="{y}" r="10" fill="#000"/>')
