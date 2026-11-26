import random,math,json
def weights(h,k):
    pk=min(3,h-2); rest=((h-1)-2*k)/(h-3); w=[rest]*h; w[pk]=k; w[pk-1]=w[pk+1]=(1+k)/2; return w
def run(rnd,perday=1000,k=1.35,svc=12,win=22,h=12,priv=10,appt=25,ns=3,norm=15):
    w=weights(h,k);T=[]
    for hh in range(h):
        rate=perday*w[hh]/h; mean=3600/rate; t=hh*3600+rnd.expovariate(1/mean)
        while t<(hh+1)*3600:
            x=rnd.random()*100; pr=1 if x<priv else 2 if x<priv+appt else 3
            nos=rnd.random()*100<ns; m=svc*60/2
            T.append((t,pr,rnd.expovariate(1/m)+rnd.expovariate(1/m),nos)); t+=rnd.expovariate(1/mean)
    free=[0]*win; q={1:[],2:[],3:[]}; nxt=0;waiting=0;last=0;out=[]
    while nxt<len(T) or waiting:
        wi=min(range(win),key=lambda j:free[j]); now=max(free[wi],last)
        if waiting==0 and nxt<len(T): now=max(now,T[nxt][0])
        while nxt<len(T) and T[nxt][0]<=now: q[T[nxt][1]].append(T[nxt]);waiting+=1;nxt+=1
        for p in (1,2,3):
            if q[p]: cur=q[p].pop(0);break
        waiting-=1; free[wi]=now+(180 if cur[3] else cur[2]); last=now; out.append((cur,now))
    return out,max(free)
R=random.Random(2026);D=100;H=12
ok=0;tot=0;wsum=[0]*H;wc=[0]*H;arr=[0]*H;hmax=[0]*H;busy=[0]*H;qlen=[0]*(H*60+1);allw=[];Tk=0
for d in range(D):
    out,mf=run(random.Random(2026+d*7919))
    for (t,pr,s,nos),st in out:
        ha=int(t//3600);arr[ha]+=1/D;Tk+=1/D
        dur=180 if nos else s
        hb=min(int(st//3600),H-1);busy[hb]+=dur/D
        a=int(t//60);b=min(int(st//60),H*60)
        for m in range(a,min(b,H*60)):qlen[m]+=1/D
        if nos:continue
        wt=st-t;allw.append(wt/60);wsum[ha]+=wt/D;wc[ha]+=1/D
        hmax[ha]=max(hmax[ha],wt/60)
okp=100*sum(1 for x in allw if x<=15)/len(allw);allw.sort()
res=dict(arr=arr,avg=[wsum[i]/60/wc[i] if wc[i] else 0 for i in range(H)],max=hmax,util=[100*busy[i]/(22*3600) for i in range(H)],q=qlen,ok=okp,mean=sum(allw)/len(allw),med=allw[len(allw)//2],p95=allw[int(.95*len(allw))-1],mx=allw[-1],total=Tk)
json.dump(res,open('res.json','w'));print({k:v for k,v in res.items() if not isinstance(v,list)})
