# -*- coding: utf-8 -*-
"""DrawRush — Geometrik Level Tasarim Plani PDF uretici.
Sekil thumbnaillari (PIL) + zorluk egrisi + reportlab dokuman."""
import math, os
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
SH = os.path.join(HERE, "shapes")
os.makedirs(SH, exist_ok=True)

ARIAL = "/System/Library/Fonts/Supplemental/Arial.ttf"
ARIALB = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"

# ---------- colors ----------
RED   = (226, 58, 52)
BLUE  = (52, 144, 220)
PURP  = (149, 97, 226)
GOLD  = (246, 173, 60)
TEAL  = (56, 193, 114)
PINK  = (246, 109, 155)
DROP  = (255, 255, 255)
FARM  = (122, 198, 92)
MEAD  = (120, 205, 150)
SAND  = (233, 201, 128)
NIGHT = (58, 62, 104)
CANDY = (244, 198, 216)

# ---------- geometry ----------
def poly(n, rot=-90):
    cs=[(math.cos(math.radians(rot+360*i/n)), math.sin(math.radians(rot+360*i/n))) for i in range(n)]
    return cs+[cs[0]], cs

def circle(seg=4):
    out=[(math.cos(2*math.pi*i/160), math.sin(2*math.pi*i/160)) for i in range(161)]
    cs=[(math.cos(math.radians(-90+360*i/seg)), math.sin(math.radians(-90+360*i/seg))) for i in range(seg)]
    return out, cs

def ellipse(a=1.0,b=0.62):
    out=[(a*math.cos(2*math.pi*i/160), b*math.sin(2*math.pi*i/160)) for i in range(161)]
    cs=[(a,0),(0,b),(-a,0),(0,-b)]
    return out, cs

def star(n=5, ri=0.42, rot=-90):
    pts=[]
    for i in range(2*n):
        r=1.0 if i%2==0 else ri
        a=math.radians(rot+180.0*i/n)
        pts.append((r*math.cos(a), r*math.sin(a)))
    return pts+[pts[0]], pts

def stadium(L=0.55,r=0.6):
    out=[]
    for i in range(41):
        t=math.radians(-90+180*i/40); out.append((L+r*math.cos(t), r*math.sin(t)))
    for i in range(41):
        t=math.radians(90+180*i/40); out.append((-L+r*math.cos(t), r*math.sin(t)))
    out.append(out[0])
    cs=[(L,r),(-L,r),(-L,-r),(L,-r)]
    return out,cs

def dshape():  # semicircle + base ("D")
    out=[]
    for i in range(81):
        t=math.radians(-90+180*i/80); out.append((0.0+1.0*math.cos(t), -0.2+1.0*math.sin(t)))
    out.append((0,0.8)); out.append((0,-1.2)); out.append(out[0])
    # simpler: flat left, round right
    out=[]
    for i in range(81):
        t=math.radians(90-180*i/80); out.append((1.0*math.cos(t), 1.0*math.sin(t)))
    out.append((0,-1.0)); out.append((0,1.0))
    out.append(out[0])
    cs=[(0,1.0),(1.0,0),(0,-1.0)]
    return out,cs

def arch():
    out=[(-0.8,-1.0),(-0.8,0.1)]
    for i in range(41):
        t=math.radians(180-180*i/40); out.append((0.8*math.cos(t),0.1+0.8*math.sin(t)))
    out+=[(0.8,-1.0)]; out.append(out[0])
    cs=[(-0.8,-1.0),(-0.8,0.1),(0,0.9),(0.8,0.1),(0.8,-1.0)]
    return out,cs

def heart():
    raw=[]
    for i in range(161):
        t=2*math.pi*i/160
        x=16*math.sin(t)**3
        y=13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t)
        raw.append((x,y))
    m=max(max(abs(p[0]) for p in raw), max(abs(p[1]) for p in raw))
    out=[(p[0]/m, p[1]/m) for p in raw]
    cs=[out[k] for k in (0,20,40,80,120,140)]
    return out,cs

def crescent():
    R,r,cx=1.0,0.92,0.62
    xi=(cx*cx+R*R-r*r)/(2*cx); yi=math.sqrt(max(0,R*R-xi*xi))
    Tang=math.atan2(yi,xi)
    out=[]
    # outer arc: buyuk (sol) yay, T -> B (CCW, ust+sol+alt)
    for i in range(101):
        t=Tang+(2*math.pi-2*Tang)*i/100; out.append((R*math.cos(t),R*math.sin(t)))
    # inner arc: B -> T, ic dairenin SOL yayindan (icbukey kesim), pi'den gecerek
    beta=math.atan2(yi, xi-cx)         # (pi/2, pi)
    a0=2*math.pi-beta; a1=beta         # azalarak pi'den gec
    for i in range(81):
        t=a0+(a1-a0)*i/80; out.append((cx+r*math.cos(t), r*math.sin(t)))
    out.append(out[0])
    cs=[(xi,yi),(cx-r,0),(xi,-yi)]     # ust boynuz, ic-sol, alt boynuz
    return out,cs

def teardrop():
    pts=[]
    for i in range(161):
        t=2*math.pi*i/160
        x=math.cos(t); y=math.sin(t)*(math.sin(t/2)**3)
        pts.append((-y, x))          # 90 don -> sivri uc yukari, yuvarlak dip asagi
    m=max(max(abs(p[0]) for p in pts), max(abs(p[1]) for p in pts))
    out=[(p[0]/m, p[1]/m) for p in pts]
    cs=[out[k] for k in (0,40,80,120)]
    return out,cs

def cross(t=0.36):
    p=[(-t,1),(t,1),(t,t),(1,t),(1,-t),(t,-t),(t,-1),(-t,-1),(-t,-t),(-1,-t),(-1,t),(-t,t)]
    return p+[p[0]], p

def flower(petals=6, ri=0.45):
    out=[]
    for i in range(241):
        a=2*math.pi*i/240
        r=ri+(1-ri)*(0.5+0.5*math.cos(petals*a))
        out.append((r*math.cos(a), r*math.sin(a)))
    cs=[]
    for k in range(petals):
        a=2*math.pi*k/petals
        cs.append((math.cos(a),math.sin(a)))
        a2=2*math.pi*(k+0.5)/petals
        cs.append((ri*math.cos(a2), ri*math.sin(a2)))
    return out,cs

def arrow():
    p=[(0,1),(0.7,0.2),(0.32,0.2),(0.32,-1),(-0.32,-1),(-0.32,0.2),(-0.7,0.2)]
    return p+[p[0]], p

def lightning():
    p=[(0.1,1),(-0.45,0.05),(-0.05,0.05),(-0.35,-1),(0.5,0.15),(0.08,0.15)]
    return p+[p[0]], p

def hexagram():
    a,b=poly(3,-90); c,d=poly(3,90)
    # star of david outline: weave the two triangles -> 12-point hexagram via rose
    pts=[]
    for i in range(12):
        r=1.0 if i%2==0 else 0.58
        ang=math.radians(-90+30*i)
        pts.append((r*math.cos(ang), r*math.sin(ang)))
    return pts+[pts[0]], pts

def gear(teeth=8, ri=0.78):
    out=[]
    steps=teeth*4
    for i in range(steps+1):
        a=2*math.pi*i/steps
        phase=(i%4)
        r=1.0 if phase in (1,2) else ri
        out.append((r*math.cos(a), r*math.sin(a)))
    cs=[(math.cos(2*math.pi*k/teeth),math.sin(2*math.pi*k/teeth)) for k in range(teeth)]
    return out,cs

def shield():
    out=[(-0.75,0.95),(0.75,0.95)]
    for i in range(61):
        t=i/60
        out.append((0.75*(1-t), 0.95-1.0*t - 0.9*t*t))
    for i in range(61):
        t=i/60
        out.append((-0.75*t, -0.95 + 0.0))
    # simpler shield: top straight, sides down, curve to bottom point
    out=[(-0.7,0.9),(0.7,0.9),(0.7,-0.1)]
    for i in range(41):
        t=i/40; out.append((0.7*(1-t), -0.1-1.0*t))
    for i in range(41):
        t=i/40; out.append((-0.7*t, -1.1+1.0*t))
    out.append(out[0])
    cs=[(-0.7,0.9),(0.7,0.9),(0.7,-0.1),(0,-1.1),(-0.7,-0.1)]
    return out,cs

def kite():
    p=[(0,1),(0.6,0.2),(0,-1),(-0.6,0.2)]
    return p+[p[0]], p

def clover(lobes=3):
    out=[]
    for i in range(241):
        a=2*math.pi*i/240
        r=0.55+0.45*abs(math.cos(1.5*a))
        out.append((r*math.cos(a-math.pi/2), r*math.sin(a-math.pi/2)))
    cs=[(math.cos(2*math.pi*k/lobes-math.pi/2),math.sin(2*math.pi*k/lobes-math.pi/2)) for k in range(lobes)]
    return out,cs

def line():
    return [(-0.85,0),(0.85,0)], [(-0.85,0),(0.85,0)]

# ---- emoji / cok-parcali (MULTI): yuz + gozler + agiz ayri parcalar ----
def _circ(cx,cy,r,seg=4,nout=64):
    out=[(cx+r*math.cos(2*math.pi*i/nout), cy+r*math.sin(2*math.pi*i/nout)) for i in range(nout+1)]
    cs=[(cx+r*math.cos(math.radians(-90+360*i/seg)), cy+r*math.sin(math.radians(-90+360*i/seg))) for i in range(seg)]
    return out,cs
def _arc(cx,cy,r,a0,a1,n=44):
    out=[(cx+r*math.cos(math.radians(a0+(a1-a0)*i/n)), cy+r*math.sin(math.radians(a0+(a1-a0)*i/n))) for i in range(n+1)]
    return out,[out[0],out[-1]]
def smiley(mouth='smile'):
    f=_circ(0,0,1.0,4); el=_circ(-0.4,0.34,0.14,4,32); er=_circ(0.4,0.34,0.14,4,32)
    if   mouth=='smile': m=_arc(0,0.02,0.55,206,334)
    elif mouth=='grin':  m=_arc(0,0.06,0.66,198,342)
    elif mouth=='sad':   m=_arc(0,-0.58,0.5,24,156)
    elif mouth=='o':     m=_circ(0,-0.4,0.2,4,32)
    pieces=[(f[0],f[1],'ring'),(el[0],el[1],'fill'),(er[0],er[1],'fill'),
            (m[0],m[1],'ring' if mouth=='o' else 'stroke')]
    return ('MULTI',pieces)

# ---- basit meyveler ----
def apple():
    out=[]
    for i in range(201):
        a=2*math.pi*i/200
        dt=min(abs(a-math.pi/2),2*math.pi-abs(a-math.pi/2)); db=min(abs(a-3*math.pi/2),2*math.pi-abs(a-3*math.pi/2))
        dip=0.0
        if dt<0.5: dip=0.32*(1-dt/0.5)
        if db<0.4: dip=max(dip,0.16*(1-db/0.4))
        r=1.0-dip; out.append((r*math.cos(a)*1.06, r*math.sin(a)))
    return out,[out[k] for k in range(0,200,33)]
def pear():
    out=[]
    for i in range(161):
        t=2*math.pi*i/160; s=math.sin(t); r=0.95-0.42*max(0.0,s)
        out.append((r*math.cos(t), r*math.sin(t)*1.18-0.12))
    return out,[out[k] for k in (0,40,80,120)]
def banana():
    Ro,Ri,yo=1.05,0.72,0.35; a0,a1=200,340; out=[]
    for i in range(45):
        t=math.radians(a0+(a1-a0)*i/44); out.append((Ro*math.cos(t), Ro*math.sin(t)+yo))
    for i in range(45):
        t=math.radians(a1-(a1-a0)*i/44); out.append((Ri*math.cos(t), Ri*math.sin(t)+yo))
    out.append(out[0])
    return out,[(Ro*math.cos(math.radians(a0)),Ro*math.sin(math.radians(a0))+yo),(0,-Ro+yo),(Ro*math.cos(math.radians(a1)),Ro*math.sin(math.radians(a1))+yo)]
def watermelon():
    out=[(-1.0,0.0),(1.0,0.0)]
    for i in range(61):
        t=math.radians(-180*i/60); out.append((math.cos(t), math.sin(t)))
    out.append(out[0]); return out,[(-1,0),(1,0),(0,-1)]
def lemon():
    out=[(1.08*math.cos(2*math.pi*i/160)*(0.9+0.1*abs(math.cos(2*math.pi*i/160))), 0.6*math.sin(2*math.pi*i/160)) for i in range(161)]
    return out,[(1.08,0),(0,0.6),(-1.08,0),(0,-0.6)]
def cherry():
    c1=_circ(-0.42,-0.45,0.34,4,40); c2=_circ(0.44,-0.55,0.34,4,40); top=(0.05,0.95)
    s1=[(-0.42,-0.11),top]; s2=[(0.44,-0.21),top]
    return ('MULTI',[(c1[0],c1[1],'ring'),(c2[0],c2[1],'ring'),(s1,[s1[0],s1[1]],'stroke'),(s2,[s2[0],s2[1]],'stroke')])

GEN={
 'smiley':lambda:smiley('smile'),'grin':lambda:smiley('grin'),'sad':lambda:smiley('sad'),'surprised':lambda:smiley('o'),
 'apple':apple,'pear':pear,'banana':banana,'watermelon':watermelon,'lemon':lemon,'cherry':cherry,
 'line':line,'tri':lambda:poly(3),'sq':lambda:poly(4,-45),'diamond':lambda:poly(4,-90),
 'penta':lambda:poly(5),'hexa':lambda:poly(6,-90),'hepta':lambda:poly(7),'octa':lambda:poly(8,-67.5),
 'circle':lambda:circle(4),'circle6':lambda:circle(6),'ellipse':ellipse,'stadium':stadium,'d':dshape,
 'arch':arch,'star':star,'star6':lambda:star(6,0.55),'heart':heart,'crescent':crescent,'tear':teardrop,
 'cross':cross,'flower':flower,'flower5':lambda:flower(5,0.5),'arrow':arrow,'bolt':lightning,
 'hexagram':hexagram,'gear':gear,'shield':shield,'kite':kite,'clover':clover,
}

# ---------- thumbnail render ----------
def render(spec, path, bg, wall, enemies, esize=0.0):
    S=2  # supersample
    W=560*S; H=460*S
    img=Image.new("RGB",(W,H),bg)
    d=ImageDraw.Draw(img,"RGBA")
    res=GEN[spec]()
    if isinstance(res,tuple) and len(res)==2 and res[0]=='MULTI':
        pieces=res[1]
    else:
        out,cs=res; pieces=[(out,cs,'ring')]
    pad=64*S
    span=1.7  # arena half-extent shown
    def tx(p): return (W/2 + p[0]/span*(W/2-pad), H/2 - p[1]/span*(H/2-pad))
    # enemies first (behind), drawn at arena positions
    for ex,ey in enemies:
        cxp,cyp=tx((ex,ey)); rr=20*S
        d.ellipse([cxp-rr,cyp-rr,cxp+rr,cyp+rr], fill=(150,40,40,255), outline=(90,20,20,255), width=3*S)
        d.ellipse([cxp-9*S,cyp-7*S,cxp-2*S,cyp+1*S], fill=(255,255,255,255))
        d.ellipse([cxp+2*S,cyp-7*S,cxp+9*S,cyp+1*S], fill=(255,255,255,255))
        d.ellipse([cxp-7*S,cyp-6*S,cxp-3*S,cyp-1*S], fill=(0,0,0,255))
        d.ellipse([cxp+4*S,cyp-6*S,cxp+8*S,cyp-1*S], fill=(0,0,0,255))
    # pieces (single sekiller = 1 'ring' parca; emoji = yuz+gozler+agiz)
    base=int(26*S)
    for out,cs,kind in pieces:
        pts=[tx(p) for p in out]
        if kind=='fill':
            d.polygon(pts, fill=wall+(255,)); continue
        ext=max(max(abs(p[0]) for p in out), max(abs(p[1]) for p in out))
        lw=base if ext>0.55 else int(15*S)
        d.line([(x+3*S,y+5*S) for (x,y) in pts], fill=(0,0,0,55), width=lw, joint="curve")
        d.line(pts, fill=wall+(255,), width=lw, joint="curve")
        for (x,y) in pts[::2]:
            d.ellipse([x-lw/2,y-lw/2,x+lw/2,y+lw/2], fill=wall+(255,))
        if kind=='ring' and ext>0.55:
            for (cx,cy) in cs:
                cxp,cyp=tx((cx,cy)); rr=11*S
                for off in ((-7*S,0),(7*S,0)):
                    d.ellipse([cxp-rr+off[0],cyp-rr,cxp+rr+off[0],cyp+rr], fill=DROP+(255,), outline=wall+(255,), width=3*S)
    # player center
    px,py=tx((0,0))
    d.ellipse([px-15*S,py-15*S,px+15*S,py+15*S], fill=(80,150,255,255), outline=(30,60,160,255), width=3*S)
    d.ellipse([px-7*S,py-19*S,px+7*S,py-6*S], fill=(245,190,140,255))  # head
    img=img.resize((560,460), Image.LANCZOS)
    img.save(path)

# ---------- difficulty curve ----------
def render_curve(levels, path):
    S=2; W=1000*S; Hh=420*S
    img=Image.new("RGB",(W,Hh),(250,250,252)); d=ImageDraw.Draw(img)
    fb=ImageFont.truetype(ARIALB,22*S); fr=ImageFont.truetype(ARIAL,16*S)
    mx=60*S; my=50*S; gw=W-2*mx; gh=Hh-2*my-30*S
    n=len(levels)
    def X(i): return mx + gw*i/(n-1)
    def Y(v): return my+gh - gh*v/10.0
    # grid
    for v in range(0,11,2):
        yy=Y(v); d.line([(mx,yy),(W-mx,yy)], fill=(225,225,230), width=2*S)
        d.text((mx-30*S,yy-12*S), str(v), font=fr, fill=(120,120,130))
    # enemy bars
    for i,lv in enumerate(levels):
        x=X(i); e=lv['enemies']
        bw=gw/n*0.5
        yy=Y(e*2.2)
        d.rectangle([x-bw/2,yy,x+bw/2,my+gh], fill=(255,210,210))
    # difficulty line + filled area (dalgayi vurgula)
    line=[(X(i),Y(lv['diff'])) for i,lv in enumerate(levels)]
    d.polygon([(mx,my+gh)]+line+[(W-mx,my+gh)], fill=(252,233,233))
    d.line(line, fill=(226,58,52), width=5*S, joint="curve")
    ROLEC={'intro':(52,120,226),'peak':(150,30,30),'breather':(56,165,95),'ramp':(226,58,52)}
    NEW={3:"dusman",6:"YAY kenar",7:"2 dusman",9:"karisik kenar",14:"ICBUKEY",22:"3 dusman",31:"COK-PARCALI"}
    fnew=ImageFont.truetype(ARIALB,13*S)
    for (x,y),lv in zip(line,levels):
        role=lv.get('role','ramp'); c=ROLEC.get(role,(226,58,52))
        r=(8 if role in ('peak','intro') else 5)*S
        d.ellipse([x-r,y-r,x+r,y+r], fill=c, outline=(255,255,255), width=2*S)
        d.text((x-6*S, my+gh+6*S), str(lv['n']), font=fr, fill=(110,110,120))
        if lv['n'] in NEW:
            d.line([(x,y-9*S),(x,y-20*S)], fill=(52,120,226), width=2*S)
            d.text((x-16*S, y-36*S), "YENI: "+NEW[lv['n']], font=fnew, fill=(52,120,226))
    d.text((mx, 10*S), "Zorluk DALGASI - testere deseni (surekli yukselmez!)", font=fb, fill=(40,40,50))
    # legend
    lx=mx; ly=Hh-22*S; fl=ImageFont.truetype(ARIAL,15*S)
    for txt,col in [("yeni mekanik (intro)",(52,120,226)),("zirve (peak)",(150,30,30)),("nefes (breather)",(56,165,95))]:
        d.ellipse([lx,ly,lx+14*S,ly+14*S],fill=col); d.text((lx+20*S,ly-2*S),txt,font=fl,fill=(90,90,100)); lx+=int(len(txt)*9*S+50*S)
    img=img.resize((1000,420), Image.LANCZOS); img.save(path)

# ===================== LEVEL DATA =====================
# diff = composite difficulty score 0-10
def L(n,name,shape,tier,edges,size,enemies,espeed,espawn,mech,coins,fun,ready,wall,bg,diff):
    return dict(n=n,name=name,shape=shape,tier=tier,edges=edges,size=size,enemies=enemies,
                espeed=espeed,espawn=espawn,mech=mech,coins=coins,fun=fun,ready=ready,wall=wall,bg=bg,diff=diff)

NE=(1.45,1.3); NW=(-1.45,1.3); SE=(1.45,-1.3); SW=(-1.45,-1.3); E=(1.55,0); Wp=(-1.55,0); N=(0,1.5); Sp=(0,-1.5)

LEVELS=[
 L(1,"Ilk Cizgi (Tutorial)","line",1,"1 duz kenar","orta",0,"-",[],"Dokun -> ciz: tek kenari boya",5,"Mekanigi ogretir, baski yok.",True,RED,FARM,0.5),
 L(2,"Ucgen","tri",1,"3 duz","orta",0,"-",[],"Kapali sekil = ilk duvar yukselir",10,"Ilk 'tamamlama' tatmini.",True,RED,FARM,1.2),
 L(3,"Kare","sq",1,"4 duz","orta",1,"Yavas",[NE],"Ilk dusman: cizerken kac",15,"Cizim + kacis dengesi baslar.",True,RED,FARM,2.0),
 L(4,"Elmas","diamond",1,"4 duz (donuk)","orta",1,"Yavas",[SW],"Ayni kare, farkli aci hissi",15,"Yon degisimi, kolay zafer.",True,BLUE,FARM,2.3),
 L(5,"Besgen","penta",1,"5 duz","orta",1,"Orta",[NE],"Daha cok kenar = daha uzun cizim",20,"Maruz kalma suresi artar.",True,GOLD,MEAD,3.0),
 L(6,"Cember","circle",1,"4 YAY (ceyrek)","orta",1,"Orta",[NW],"Ilk EGRI kenar - yay boyunca kay",25,"Yay mekanigi showcase, akici his.",True,TEAL,MEAD,3.4),
 L(7,"Altigen","hexa",2,"6 duz","buyuk",2,"Orta",[NE,SW],"2 dusman, zit kosegen",30,"Iki yonden tehdit.",True,PURP,MEAD,4.2),
 L(8,"Oval","ellipse",2,"4 yay","buyuk",2,"Orta",[E,Wp],"Uzun yaylar, genis alan",35,"Akici ama uzun tur.",True,BLUE,MEAD,4.5),
 L(9,"Stadyum","stadium",2,"2 duz + 2 yay","buyuk",2,"Orta",[NE,SW],"Duz+yay KARISIK kenar",40,"Hibrit sekil, ritim degisimi.",True,TEAL,MEAD,4.8),
 L(10,"Yedigen","hepta",2,"7 duz","buyuk",2,"Orta-Hizli",[N,Sp],"Cok kenar + hizli dusman",40,"Tempo yukselir.",True,GOLD,SAND,5.1),
 L(11,"D Sekli","d",2,"1 yay + 1 duz","orta",2,"Orta-Hizli",[NE,NW],"Yarim daire + taban",40,"Asimetrik, ilginc rota.",True,RED,SAND,5.0),
 L(12,"Sekizgen","octa",2,"8 duz","buyuk",2,"Hizli",[NE,SW],"En cok kenarli duzgun cokgen",45,"Uzun cizim + hizli dusman.",True,PURP,SAND,5.6),
 L(13,"Kemer","arch",2,"3 duz + 1 yay","orta",2,"Hizli",[E,Wp],"Duz tabanli kavis",45,"Mimari his, karisik kenar.",True,BLUE,SAND,5.4),
 L(14,"Kalp","heart",3,"2 yay + 2 duz (V)","orta",2,"Hizli",[NE,SW],"ICBUKEY sekil: ust cukur",55,"En cok paylasilan sekil - WOW.",False,PINK,CANDY,6.2),
 L(15,"Yildiz","star",3,"10 kenar (5 sivri)","buyuk",2,"Hizli",[NE,NW,Sp],"Sivri reflex koseler",60,"Klasik ikon, dramatik rota.",False,GOLD,CANDY,6.8),
 L(16,"Hilal","crescent",3,"2 yay (ic-dis)","orta",2,"Hizli",[E,Wp],"Disbukey + icbukey yay",55,"Gece temasiyla cok sik durur.",False,BLUE,NIGHT,6.5),
 L(17,"Damla","tear",3,"1 yay + sivri uc","orta",2,"Hizli",[NE,SW],"Yuvarlak alt + tepe nokta",55,"Oyunun kendi drop ikonu - tematik.",False,TEAL,CANDY,6.3),
 L(18,"Arti / Hac","cross",3,"12 duz","buyuk",2,"Hizli",[NE,NW,SE,SW],"Cok girintili rota",60,"Labirent hissi, cok kose.",False,RED,SAND,7.0),
 L(19,"Cicek","flower",3,"6 yapra (yay)","buyuk",2,"Hizli",[N,Sp],"Dalgali cok-yay loop",65,"Tatli, organik, renkli.",False,PINK,CANDY,6.9),
 L(20,"Ucurtma","kite",3,"4 duz","orta",2,"Hizli",[NE,SW],"Asimetrik dortgen",50,"Hizli ama net.",True,GOLD,SAND,6.0),
 L(21,"Ok","arrow",3,"7 duz","orta",2,"Hizli",[NW,SE],"Yon-belli sivri sekil",60,"Dinamik, agresif siluet.",False,PURP,NIGHT,6.7),
 L(22,"Simsek","bolt",3,"6 duz (zigzag)","orta",3,"Hizli",[NE,NW,Sp],"Keskin zigzag rota",70,"3 dusman + sert donusler.",False,GOLD,NIGHT,7.6),
 L(23,"Davud Yildizi","hexagram",4,"12 kenar","buyuk",3,"Hizli",[NE,NW,SE,SW],"6 sivri, simetrik",80,"Karmasik ama simetrik = tatmin.",False,BLUE,NIGHT,8.0),
 L(24,"Yonca","clover",4,"3 loblu","orta",3,"Hizli",[N,SE,SW],"Yuvarlak loblar",70,"Sevimli, sans temasi.",False,TEAL,NIGHT,7.7),
 L(25,"Cark / Disli","gear",4,"16 kenar","buyuk",3,"Cok Hizli",[NE,NW,SE,SW],"Cok disli mekanik loop",95,"Endustriyel, yogun cizim.",False,PURP,NIGHT,8.6),
 L(26,"Kalkan","shield",4,"3 duz + 2 yay","orta",3,"Cok Hizli",[NE,SW,N],"Duz ust + sivri alt kavis",85,"Heroik, oyun-sonu hissi.",False,RED,NIGHT,8.3),
 L(27,"6-Kollu Cicek","flower5",4,"5 yaprak","buyuk",3,"Cok Hizli",[NE,NW,SE,SW],"Sik yaprakli loop",90,"Gorsel zirve.",False,PINK,NIGHT,8.4),
 L(28,"Buyuk Yildiz","star6",4,"12 kenar","cok buyuk",3,"Cok Hizli",[NE,NW,SE,SW],"Buyuk + cok sivri",100,"Boss-boyut final hissi.",False,GOLD,NIGHT,9.2),
 L(29,"Cember-6","circle6",4,"6 yay","buyuk",3,"Cok Hizli",[N,Sp,E,Wp],"6 parcali pruzsuz cember",90,"Daha akici dev cember.",True,TEAL,NIGHT,8.5),
 L(30,"Dev Kalp (Ikon Finali)","heart",4,"2 yay + V","cok buyuk",3,"Cok Hizli",[NE,NW,SE,SW],"Tum mekanikler bir arada",120,"Duygusal ikon-finali.",False,PINK,NIGHT,9.6),
 # --- Kademe 5: EMOJI / COK-PARCALI (yeni mekanik: bir level birden cok ayrik parca) ---
 L(31,"Gulucuk","smiley",5,"Yuz(4 yay)+2 goz+agiz","buyuk",2,"Orta",[NE,SW],"COK-PARCALI: ayri parcalari sirayla ciz",80,"En sevimli level - emoji ciz!",False,GOLD,NIGHT,6.2),
 L(32,"Sirit","grin",5,"Yuz+gozler+genis gulus","buyuk",3,"Hizli",[NE,NW,SE],"Daha cok parca + 3 dusman",90,"Neseli, yogun.",False,GOLD,NIGHT,7.6),
 L(33,"Uzgun","sad",5,"Yuz+gozler+ters agiz","orta",2,"Hizli",[NE,SW],"Ayni parcalar, ters agiz kavisi",85,"Duygu varyasyonu, sakin nefes.",False,BLUE,NIGHT,6.9),
 L(34,"Saskin","surprised",5,"Yuz+gozler+O agiz","buyuk",3,"Hizli",[NE,NW,SE,SW],"Ekstra halka (O agiz)",95,"Fazla parca, daha zor.",False,PURP,NIGHT,8.1),
 L(35,"Final: Dev Gulucuk","grin",5,"Dev surat finali","cok buyuk",3,"Cok Hizli",[NE,NW,SE,SW],"Tum mekanikler + cok-parcali",130,"Paylasilabilir mutlu-yuz finali.",False,GOLD,NIGHT,9.5),
 # --- Kademe 6: MEYVELER (bonus dunya - sevimli, renkli, esnek siralanabilir) ---
 L(36,"Elma","apple",6,"Yuvarlak + ust/alt cukur","buyuk",2,"Orta",[NE,SW],"Tatli silüet, ic-bukey cukurlar",55,"Sevimli meyve dunyasi acilis.",False,RED,MEAD,5.5),
 L(37,"Armut","pear",6,"Genis alt + dar ust","orta",2,"Hizli",[NE,SW],"Asimetrik yumusak egri",60,"Organik, hosa giden sekil.",False,TEAL,MEAD,6.2),
 L(38,"Limon","lemon",6,"Sivri uclu oval","buyuk",2,"Orta",[E,Wp],"Kolay, ferah",50,"Renk patlamasi, rahat.",False,GOLD,MEAD,5.0),
 L(39,"Karpuz Dilimi","watermelon",6,"Duz ust + yarim daire","orta",2,"Hizli",[NE,NW],"Dilim silueti (yari-daire)",55,"Yazlik, tanidik.",False,TEAL,SAND,6.4),
 L(40,"Muz","banana",6,"2 yay (kalin hilal)","orta",3,"Hizli",[NE,NW,SE],"Egri kalin sekil",70,"Ikonik, eglenceli kavis.",False,GOLD,SAND,7.0),
 L(41,"Kiraz","cherry",6,"2 daire + saplar (cok-parcali)","buyuk",2,"Hizli",[NE,SW],"Cok-parcali: 2 kiraz + sap",75,"Tatli ikili, paylasilabilir.",False,RED,MEAD,6.0),
]

# === Zorluk yeniden tune: TESTERE / DALGA (yeni-tanit-sonra-rahatla, kontrast, spike yok) ===
# n: (diff 0-10, dusman, hiz, boyut, role)   role: intro/ramp/peak/breather
WAVE={
 1:(0.5,0,"-","orta","intro"),        2:(1.0,0,"-","orta","ramp"),
 3:(2.2,1,"Yavas","orta","intro"),    4:(1.6,1,"Yavas","buyuk","breather"),
 5:(2.9,1,"Orta","orta","ramp"),      6:(2.2,1,"Orta","orta","intro"),
 7:(4.0,2,"Orta","buyuk","intro"),    8:(3.2,2,"Orta","cok buyuk","breather"),
 9:(4.4,2,"Orta","buyuk","intro"),    10:(5.2,2,"Orta-Hizli","buyuk","peak"),
 11:(4.2,2,"Orta","orta","breather"), 12:(5.6,2,"Hizli","buyuk","ramp"),
 13:(4.9,2,"Hizli","orta","breather"),14:(4.6,2,"Orta","orta","intro"),
 15:(6.4,2,"Hizli","buyuk","peak"),   16:(5.6,2,"Hizli","orta","ramp"),
 17:(5.0,2,"Orta","orta","breather"), 18:(7.0,2,"Hizli","buyuk","peak"),
 19:(5.8,2,"Orta","buyuk","breather"),20:(5.6,2,"Hizli","orta","ramp"),
 21:(6.6,2,"Hizli","orta","ramp"),    22:(7.6,3,"Hizli","orta","intro"),
 23:(6.6,2,"Hizli","buyuk","breather"),24:(7.4,3,"Hizli","orta","ramp"),
 25:(8.4,3,"Cok Hizli","buyuk","peak"),26:(7.4,3,"Hizli","orta","breather"),
 27:(8.0,3,"Cok Hizli","buyuk","ramp"),28:(9.0,3,"Cok Hizli","cok buyuk","ramp"),
 29:(7.8,3,"Hizli","cok buyuk","breather"),30:(9.6,3,"Cok Hizli","cok buyuk","peak"),
 31:(6.2,2,"Orta","buyuk","intro"),   32:(7.6,3,"Hizli","buyuk","ramp"),
 33:(6.9,2,"Hizli","orta","breather"),34:(8.1,3,"Hizli","buyuk","ramp"),
 35:(9.5,3,"Cok Hizli","cok buyuk","peak"),
 36:(5.5,2,"Orta","buyuk","intro"),   37:(6.2,2,"Hizli","orta","ramp"),
 38:(5.0,2,"Orta","buyuk","breather"),39:(6.4,2,"Hizli","orta","ramp"),
 40:(7.0,3,"Hizli","orta","peak"),    41:(6.0,2,"Hizli","buyuk","breather"),
}
POOL=[NE,SW,NW,SE,N,Sp]
for lv in LEVELS:
    d,en,sp,sz,role=WAVE[lv['n']]
    lv['diff']=d; lv['enemies']=en; lv['espeed']=sp; lv['size']=sz; lv['role']=role
    lv['espawn']=POOL[:en] if en>0 else []

print("rendering shapes...")
for lv in LEVELS:
    render(lv['shape'], os.path.join(SH,f"lv{lv['n']:02d}.png"), lv['bg'], lv['wall'], lv['espawn'])
render_curve(LEVELS, os.path.join(HERE,"difficulty_curve.png"))
print("shapes done")

# ===================== PDF =====================
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import mm
from reportlab.lib import colors
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (SimpleDocTemplate, Paragraph, Spacer, Image as RLImage,
                                Table, TableStyle, PageBreak, KeepTogether)
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.enums import TA_CENTER, TA_LEFT

pdfmetrics.registerFont(TTFont("AR", ARIAL))
pdfmetrics.registerFont(TTFont("ARB", ARIALB))

ss=getSampleStyleSheet()
def st(name,**kw):
    base=dict(fontName="AR", fontSize=10, leading=14, textColor=colors.HexColor("#2b2b33"))
    base.update(kw); return ParagraphStyle(name,**base)
H1=st("H1",fontName="ARB",fontSize=24,leading=28,textColor=colors.HexColor("#e23a34"))
H2=st("H2",fontName="ARB",fontSize=16,leading=20,textColor=colors.HexColor("#3a3a55"),spaceBefore=8,spaceAfter=6)
H3=st("H3",fontName="ARB",fontSize=12,leading=15,textColor=colors.HexColor("#e23a34"))
BODY=st("BODY")
SMALL=st("SMALL",fontSize=8.5,leading=11,textColor=colors.HexColor("#555560"))
WHITEB=st("WB",fontName="ARB",fontSize=11,textColor=colors.white)
CENT=st("CENT",alignment=TA_CENTER)

PDF=os.path.join(HERE,"DrawRush-Level-Design-Plan.pdf")
doc=SimpleDocTemplate(PDF,pagesize=A4,leftMargin=16*mm,rightMargin=16*mm,topMargin=15*mm,bottomMargin=14*mm,
                      title="DrawRush Level Tasarim Plani", author="Studios208")
W=A4[0]-32*mm
story=[]

# ---- cover ----
story.append(Spacer(1,18*mm))
story.append(Paragraph("DrawRush", H1))
story.append(Paragraph("Geometrik Level Tasarim Plani", st("sub",fontName="ARB",fontSize=15,textColor=colors.HexColor("#3a3a55"))))
story.append(Spacer(1,3*mm))
story.append(Paragraph("30 levellik kampanya - zorluk egrisi, dusman tasarimi, sekil kataloğu ve mekanik akisi. "
   "Sistem: duz + 3-nokta cember yayi kenarlar, procedural duvarlar, kovalayan dusmanlar.", BODY))
story.append(Spacer(1,5*mm))
cov=RLImage(os.path.join(SH,"lv30.png")); cov._restrictSize(W,90*mm)
story.append(cov)
story.append(Spacer(1,3*mm))
story.append(Paragraph("Studios208 - DrawAndRush2  -  Hyper-casual mobile (Android oncelik, portrait)", SMALL))
story.append(PageBreak())

# ---- philosophy + curve ----
story.append(Paragraph("1. Tasarim Felsefesi ve Pacing", H2))
for t in [
 "<b>1) Zorluk SUREKLI YUKSELMEZ - en onemli kural.</b> Ayni gerilime surekli maruz kalan oyuncu DUYARSIZLASIR = sikilir. Bunun yerine <b>testere/dalga</b> deseni: yuksek meydan-okuma ile gorece sakinlik arasinda <b>kontrast</b>. Bizim egrimiz (asagida) ust trendi yukari, ama ic ice dip'lerle dalgalaniyor.",
 "<b>2) Yeni-tanit -> sonra-rahatla.</b> Yeni bir sey (mekanik / dusman / sekil tipi) eklendiginde zorlugu KISA SURE dusur ki oyuncu yeniyi <b>sakin bir ortamda</b> ogrensin; sonra tekrar tirmandir. Orn: ilk yay (Lv6), ilk 2 dusman (Lv7->Lv8 nefes), ilk icbukey/kalp (Lv14 yavas dusmanla).",
 "<b>3) Ani SPIKE'tan kac.</b> Hazirliksiz oyuncuyu cokerten sicrama (klasik: Battletoads turbo tunnel) oyunu biraktirir. Degisim olsun ama her adim <b>yonetilebilir</b> - komsu leveller arasi fark kucuk.",
 "<b>4) Sadece zorluk degil, her sey dalgalanmali.</b> Gorsel/tema/renk de degissin (her kademe yeni tema: ciftlik -> cayir -> col/seker -> gece). Buyuk bir peak'ten sonra rahatlatici, tatmin edici bir bolum (orn. buyuk-ferah sekil) koy.",
 "<b>Kac level?</b> Kalicilik unique level sayisindan degil <b>donguden</b> gelir (son levelden sonra random cycler = sonsuz). Soft-launch icin ~30 el-yapimi tatli nokta; ustune per-level renk/tema cesitliligi.",
 "<b>Parcalar (DUZELTME):</b> Her edge-grubu kapali bir loop; AMA bir level birden cok AYRIK parca icerebilir - hepsi boyaninca win. Yani <b>emoji/surat = cok-parcali level</b> (yuz + gozler + agiz). Oyuncu bir parcayi bitirip serbest yuruyerek digerine gecer. Yeni ve cok eglenceli bir tip (Kademe 5).",
 "<b>Zorluk knoblari:</b> kenar sayisi/uzunlugu, duz/yay/icbukey, sekil boyutu (buyuk = ferah = daha kolay dodge ama uzun cizim), dusman sayisi, dusman hizi, spawn mesafesi. HP sabit = 3. <b>Nefes leveli</b> yapmak icin: sekli buyut + dusmani yavaslat/azalt.",
]:
    story.append(Paragraph(t, BODY)); story.append(Spacer(1,1.5*mm))
# new-element -> breather map
ne=[["Yeni oge","Tanitildigi Lv","Hemen sonra nefes"],
 ["Ilk dusman","Lv3 (yavas)","Lv4 (buyuk, kolay)"],
 ["Ilk YAY kenar","Lv6 (yavas)","Lv8 (cok buyuk oval)"],
 ["Ilk 2 dusman","Lv7","Lv8 / Lv11"],
 ["Ilk ICBUKEY (kalp)","Lv14 (orta hiz)","Lv17 (damla, sakin)"],
 ["Ilk 3 dusman","Lv22","Lv23 (2'ye dus)"]]
net=Table(ne,colWidths=[42*mm,38*mm,W-80*mm])
net.setStyle(TableStyle([("BACKGROUND",(0,0),(-1,0),colors.HexColor("#3a3a55")),("TEXTCOLOR",(0,0),(-1,0),colors.white),
 ("FONTNAME",(0,0),(-1,0),"ARB"),("FONTNAME",(0,1),(-1,-1),"AR"),("FONTSIZE",(0,0),(-1,-1),9),
 ("GRID",(0,0),(-1,-1),0.4,colors.HexColor("#cccccc")),("ROWBACKGROUNDS",(0,1),(-1,-1),[colors.white,colors.HexColor("#f1f1f6")]),
 ("TOPPADDING",(0,0),(-1,-1),3),("BOTTOMPADDING",(0,0),(-1,-1),3)]))
story.append(Spacer(1,1*mm)); story.append(net)
curve=RLImage(os.path.join(HERE,"difficulty_curve.png")); curve._restrictSize(W,75*mm)
story.append(Spacer(1,2*mm)); story.append(curve)
story.append(PageBreak())

# ---- enemy design ----
story.append(Paragraph("2. Dusman Tasarimi", H2))
story.append(Paragraph("Dusmanlar NavMesh uzerinde oyuncuyu kovalar; temas = 1 hasar + geri tepme + 3sn dokunulmazlik. "
   "Her dusman authored bir spawn noktasinda baslar (level grubunun icinde), level aktive olunca oraya warp eder. "
   "DrawArea guvenli bolgesi YOK - tum arena tehlikeli.", BODY))
story.append(Spacer(1,2*mm))
edata=[["Kademe","Level","Dusman","Hiz","Spawn (nereden)"],
 ["Ogren","1-2","0","-","yok"],
 ["Ogren","3-6","1","Yavas->Orta","uzak kose (arena kenari)"],
 ["Kur","7-13","2","Orta->Hizli","zit kenarlar / kosegenler"],
 ["Eglence","14-22","2 (22:3)","Hizli","sekli kusatan 2-3 kose"],
 ["Usta","23-30","3","Hizli->Cok Hizli","4 kose, sekle yakin"]]
et=Table(edata, colWidths=[22*mm,16*mm,16*mm,28*mm,W-82*mm])
et.setStyle(TableStyle([
 ("BACKGROUND",(0,0),(-1,0),colors.HexColor("#e23a34")),("TEXTCOLOR",(0,0),(-1,0),colors.white),
 ("FONTNAME",(0,0),(-1,0),"ARB"),("FONTNAME",(0,1),(-1,-1),"AR"),("FONTSIZE",(0,0),(-1,-1),9),
 ("GRID",(0,0),(-1,-1),0.4,colors.HexColor("#cccccc")),("ROWBACKGROUNDS",(0,1),(-1,-1),[colors.white,colors.HexColor("#f6f1f1")]),
 ("VALIGN",(0,0),(-1,-1),"MIDDLE"),("TOPPADDING",(0,0),(-1,-1),4),("BOTTOMPADDING",(0,0),(-1,-1),4)]))
story.append(et)
story.append(Spacer(1,3*mm))
story.append(Paragraph("Gelecek dusman varyantlari (fikir):", H3))
for t in [
 "<b>Kovalayici</b> (mevcut): duz oyuncuya gider.",
 "<b>Devriye:</b> arenayi tur atar, kovalamaz - bolge reddi yaratir, oyuncuyu rotasini degistirmeye zorlar.",
 "<b>Atilgan:</b> periyodik hiz patlamasi - ritim/tehdit cesitliligi.",
 "<b>Bekci:</b> bir kosede durur, oyuncu yaklasinca hamle yapar - belli bolgeyi tehlikeli kilar.",
]:
    story.append(Paragraph("- "+t, BODY))
story.append(Spacer(1,2*mm))
story.append(Paragraph("<b>Onemli teknik not (icbukey sekiller):</b> Procedural duvarin 'dis yon'u su an centroid'den hesaplaniyor. "
   "Kalp/yildiz/hilal gibi ICBUKEY sekillerde icbukey kenarda dis yon ters cikar (duvar inside-out). Cozum: dis yonu "
   "polygon winding'den (kenarin loop'taki sol/sagi) hesaplamak - ~1 odakli degisiklik, tum eglenceli kataloğu acar. "
   "Asagida 'Hazir' = bugun calisir, 'Upgrade' = bu degisiklikten sonra.", st("note",fontSize=9,leading=12,textColor=colors.HexColor("#7a3a10"),backColor=colors.HexColor("#fdf3e7"),borderPadding=5)))
story.append(PageBreak())

# ---- per-level cards ----
TIERNAMES={0:"Kademe 1 - Ogren (ciftlik)",1:"Kademe 1 - Ogren (ciftlik)",2:"Kademe 2 - Kur (cayir/col)",
           3:"Kademe 3 - Eglence & Ikonlar (seker/gece)",4:"Kademe 4 - Usta (gece)",
           5:"Kademe 5 - Emoji / Cok-Parcali (gece)",
           6:"Kademe 6 - Meyveler (bonus dunya)"}
def chip(txt,color):
    return Paragraph(f'<font color="white"><b>{txt}</b></font>', st("chip",fontSize=8,alignment=TA_CENTER))

story.append(Paragraph("3. Level Bazinda Plan", H2))
story.append(Paragraph("Her kart: sekil cizimi (oyun-stili: kirmizi duvar + kosede 2 damla + ortada oyuncu + dusman spawn isaretleri), ve tum spec.", SMALL))
story.append(Spacer(1,2*mm))

last_tier=None
for lv in LEVELS:
    if lv['tier']!=last_tier:
        story.append(Spacer(1,2*mm))
        story.append(Paragraph(TIERNAMES.get(lv['tier'],""), H3))
        last_tier=lv['tier']
    img=RLImage(os.path.join(SH,f"lv{lv['n']:02d}.png")); img._restrictSize(58*mm,48*mm)
    ready = "HAZIR" if lv['ready'] else "UPGRADE"
    rc = colors.HexColor("#38a169") if lv['ready'] else colors.HexColor("#dd6b20")
    spawn = ", ".join("?" for _ in lv['espawn']) if False else (str(len(lv['espawn']))+" nok." if lv['espawn'] else "yok")
    info=[
      [Paragraph(f"<b>Lv {lv['n']} - {lv['name']}</b>", st("ln",fontName='ARB',fontSize=12,textColor=colors.HexColor('#2b2b33'))),""],
      [Paragraph(f"<b>Kenarlar:</b> {lv['edges']}", SMALL), Paragraph(f"<b>Boyut:</b> {lv['size']}", SMALL)],
      [Paragraph(f"<b>Dusman:</b> {lv['enemies']} ({lv['espeed']})", SMALL), Paragraph(f"<b>Coin:</b> {lv['coins']} (+%50 Perfect)", SMALL)],
      [Paragraph(f"<b>Spawn:</b> {spawn}", SMALL), Paragraph(f"<b>Durum:</b> <font color='{rc.hexval()[2:] if False else ('#38a169' if lv['ready'] else '#dd6b20')}'><b>{ready}</b></font>", SMALL)],
      [Paragraph(f"<b>Mekanik:</b> {lv['mech']}", SMALL),""],
      [Paragraph(f"<b>Neden eglenceli:</b> {lv['fun']}", SMALL),""],
    ]
    it=Table(info, colWidths=[ (W-62*mm)/2 ]*2)
    it.setStyle(TableStyle([("SPAN",(0,0),(1,0)),("SPAN",(0,4),(1,4)),("SPAN",(0,5),(1,5)),
       ("TOPPADDING",(0,0),(-1,-1),1.5),("BOTTOMPADDING",(0,0),(-1,-1),1.5),
       ("LEFTPADDING",(0,0),(-1,-1),4),("VALIGN",(0,0),(-1,-1),"TOP")]))
    card=Table([[img, it]], colWidths=[62*mm, W-62*mm])
    card.setStyle(TableStyle([("BOX",(0,0),(-1,-1),0.6,colors.HexColor("#e0d6d6")),
       ("BACKGROUND",(0,0),(-1,-1),colors.HexColor("#fcfafa")),("VALIGN",(0,0),(-1,-1),"MIDDLE"),
       ("LEFTPADDING",(0,0),(-1,-1),5),("RIGHTPADDING",(0,0),(-1,-1),5),
       ("TOPPADDING",(0,0),(-1,-1),5),("BOTTOMPADDING",(0,0),(-1,-1),5)]))
    story.append(KeepTogether([card, Spacer(1,3*mm)]))

# ---- variety / fun ideas ----
story.append(PageBreak())
story.append(Paragraph("4. Cesitlilik & Eglence Fikirleri", H2))
story.append(Paragraph("Sekil cesitliligi tek basina yetmez - mekanik cesitliligi 'bagimlilik' yaratir. "
   "Asagidakiler temel cizim-kac dongusunu bozmadan ustune eklenir. (kolay) = hizli kazanim.", SMALL))
story.append(Spacer(1,2*mm))
IDEAS=[
 ("Power-up & toplanabilirler",[
   "<b>Cizgi ustu coin/yildiz (kolay):</b> kenar boyunca toplanir, 'tam ciz' odulu - tatmin + ekonomi.",
   "<b>Kalkan:</b> 1 dusman vurusunu emer (per-level veya satin al).",
   "<b>Hiz patlamasi:</b> kisa sureli hizli cizim - kacarken nefes.",
   "<b>Dusman dondurma / yavaslatma:</b> 2-3sn dusmanlari durdur.",
   "<b>MIknatis / x2 coin:</b> coin cekme veya carpan.",
 ]),
 ("Engeller & tehlikeler (arena cesitliligi)",[
   "<b>Hareketli duvar/testere:</b> arenada gidip gelen tehlike - zamanlama.",
   "<b>Buz zemin (kolay):</b> kayma - kontrol zorlugu, yeni his.",
   "<b>Bosluk/ucurum:</b> sadece YAY ile asilabilen acikliklar - yay mekanigini zorunlu kilar.",
   "<b>Konveyor:</b> seni iten zemin seridi.",
 ]),
 ("Dusman cesitleri",[
   "<b>Devriye:</b> tur atar, kovalamaz - bolge reddi.",
   "<b>Bekci:</b> bir kosede durur, yaklasinca hamle - belli bolge tehlikeli.",
   "<b>Atilgan:</b> periyodik hiz patlamasi - ritim tehdidi.",
   "<b>'Sen cizerken hizlanan':</b> cizdikce hizlanir, durunca yavaslar - gerilim.",
 ]),
 ("Oyun modlari",[
   "<b>Sonsuz / Gunluk meydan okuma (kolay):</b> retention motoru, mevcut random cycler uzerine.",
   "<b>Boss level:</b> dev tek dusman, buyuk sekil - kademe finalleri.",
   "<b>Zaman yarisi / No-hit:</b> hizli veya hic-vurulmadan = ekstra yildiz/coin.",
 ]),
 ("Cizim mekanik varyasyonlari",[
   "<b>Ayna/simetri modu:</b> yarisini ciz, otomatik yansir - taze his, kolay 'akilli' hissi.",
   "<b>Tek-seferde-ciz combo:</b> durmadan tamamla = bonus.",
   "<b>Coklu renk:</b> her kenar farkli renk - gorsel zenginlik (drop rengi=duvar rengi zaten var).",
 ]),
 ("Meta & ilerleme",[
   "<b>Skin'ler (kolay):</b> player + duvar + zemin renkleri/temalari coin ile acilir - en guclu HC meta.",
   "<b>1-3 yildiz:</b> vurus/sure'ye gore - tekrar oynatir.",
   "<b>Sekil galerisi:</b> cizdigin sekiller koleksiyonu - tamamlama hissi.",
   "<b>Seri/combo + gunluk giris odulu.</b>",
 ]),
 ("Juice & paylasilabilirlik (kolay, yuksek etki)",[
   "<b>Sekil bitince gercek gorsel parlar:</b> kalp -> gercek kalp ikonu + temali konfeti.",
   "<b>Ekran sarsintisi + iz partikulu</b> vurus/tamamlama aninda.",
   "<b>Dinamik muzik:</b> dusman arttikca yogunlasir, win'de coz.",
   "<b>'Draw the Heart!' paylasim karti / GIF replay:</b> organik buyume.",
 ]),
]
for head,items in IDEAS:
    story.append(Paragraph(head, H3))
    for it in items: story.append(Paragraph("- "+it, st("idea",fontSize=9.5,leading=12.5)))
    story.append(Spacer(1,1.5*mm))
story.append(Paragraph("<b>En yuksek etki/dusuk maliyet uclusu:</b> (1) skin'ler (coin meta), (2) win-juice (gercek-gorsel parlama + konfeti + paylasim), (3) cizgi-ustu coin toplama. Bunlar donguyu bozmadan oyunu 'tamamlanmis' ve bagimlilik yapan hale getirir.", st("hl",fontSize=10,leading=13,textColor=colors.HexColor("#7a3a10"),backColor=colors.HexColor("#fdf3e7"),borderPadding=5)))

# ---- roadmap ----
story.append(PageBreak())
story.append(Paragraph("5. Uygulama Yol Haritasi", H2))
for t in [
 "<b>Faz A (bugun, sistem hazir):</b> Tier 1-2'nin tum sekilleri (ucgen, kare, elmas, besgen, cember, altigen, oval, stadyum, yedigen, D, sekizgen, kemer, ucurtma, 6-cember). Klonla-yerlestir, ~yarim gun.",
 "<b>Faz B (icbukey upgrade):</b> Procedural duvar dis-yonunu winding'e cevir. Sonra ikon tier'i: KALP + YILDIZ once (HC'de en cok donen 2 sekil), sonra hilal/damla/cicek/arti/ok/simsek.",
 "<b>Faz C (usta + juice):</b> hexagram, cark, kalkan, dev yildiz, dev kalp. Per-level tema/renk gecisleri, 'Perfect' bonusu, win'de sekil-nabiz + tema konfeti.",
 "<b>Meta:</b> coin -> skin/renk acilimi; 'Draw the Heart!' gibi sekil-isimli level basliklari paylasilabilirligi artirir.",
]:
    story.append(Paragraph(t, BODY)); story.append(Spacer(1,2*mm))
story.append(Spacer(1,3*mm))
story.append(Paragraph("Oncelik onerisi: once Faz A ile level sayisini hizla cogalt (oynanabilir kampanya), sonra Faz B'deki "
   "kalp+yildiz icin tek seferlik winding upgrade'i yap - bu iki sekil paylasilabilirlik motorudur.", BODY))

doc.build(story)
print("PDF:", PDF)
