import re,json,collections,os,meta
POT=r"C:/Program Files (x86)/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/StreamingAssets/strings/strings_template.pot"
L16=set(json.load(open('lits16.json')))
st=meta.streams(r"C:/Program Files (x86)/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed/Assembly-CSharp.dll")
d,off,size=st['#Strings']; blob=d[off:off+size]
S=set(); i=0
while i<len(blob):
    e=blob.find(b'\0',i)
    if e<0: break
    try: S.add(blob[i:e].decode())
    except: pass
    i=e+1
ALL=L16|S
vocab=json.load(open('vocab.json'))
order=sorted(vocab.items(),key=lambda kv:-len(kv[0]))
def tok(s):
    s=s.upper().replace('_','');i=0;out=[]
    while i<len(s):
        for k,v in order:
            if s.startswith(k,i): out.append(v);i+=len(k);break
        else: return None
    return ''.join(out)
def clean(n): return re.sub(r'<[^>]+>','',n).replace('\\"','"').strip()
def good(c): return c and any(ch.islower() for ch in c)
def resolve(seg,suffix=''):
    p=tok(seg)
    cands=[]
    if p: cands.append(p+suffix)
    cands.append(seg.replace('_','').capitalize()+suffix)
    for c in cands:
        if good(c) and c in ALL: return c
    return None
txt=open(POT,encoding='utf-8-sig').read()
entries=re.findall(r'msgctxt "(STRINGS\.[^"]+)"\s*\nmsgid "(.*?)"\n',txt,re.S)
keys={k:v for k,v in entries}
out=[];miss=collections.Counter()
for k,v in entries:
    p=k.split('.'); cat=seg=suf=None
    if p[-1]!='NAME': continue
    if p[1]=='ELEMENTS' and len(p)==4: cat,seg,suf='element',p[2],''
    elif p[1]=='ITEMS' and p[2]=='FOOD' and len(p)==5: cat,seg,suf='food',p[3],''
    elif p[1]=='ITEMS' and p[2]=='INDUSTRIAL_PRODUCTS' and len(p)==5 and 'VARIANT' not in k: cat,seg,suf='industrial',p[3],''
    elif p[1]=='ITEMS' and p[2] in ('PILLS',) and len(p)==5: cat,seg,suf='medicine',p[3],''
    elif p[1]=='ITEMS' and p[2]=='BIONIC_BOOSTERS' and len(p)==5: cat,seg,suf='booster',p[3],''
    elif p[1]=='ITEMS' and p[2]=='SPICES' and len(p)==5: cat,seg,suf='spice',p[3],''
    elif p[1]=='CREATURES' and p[2]=='SPECIES' and p[3]=='SEEDS' and len(p)==6: cat,seg,suf='seed',p[4],'Seed'
    elif p[1]=='CREATURES' and p[2]=='SPECIES' and len(p)==4: cat,seg,suf='critter',p[3],''
    if not cat: continue
    i=resolve(seg,suf)
    if i: out.append({'id':i,'cat':cat,'name':clean(v)})
    else: miss[cat]+=1
# eggs by EGG_NAME
for k,v in entries:
    p=k.split('.')
    if p[1]=='CREATURES' and p[2]=='SPECIES' and p[-1]=='EGG_NAME' and len(p)==5:
        i=resolve(p[3],'Egg')
        if i: out.append({'id':i,'cat':'egg','name':clean(v)})
# critters: only keep species that have BABY
baby={k.split('.')[3] for k in keys if k.startswith('STRINGS.CREATURES.SPECIES.') and k.endswith('.BABY.NAME')}
out=[o for o in out if o['cat']!='critter' or o['name']]  # placeholder
seen=set();uniq=[]
for o in out:
    if o['id'] in seen: continue
    seen.add(o['id']);uniq.append(o)
print("total",len(uniq),dict(miss))
print(collections.Counter(o['cat'] for o in uniq))
json.dump(uniq,open('items_raw.json','w'),ensure_ascii=False,indent=1)
for c in ['element','food','seed','industrial','critter','egg','booster']:
    xs=[o['id'] for o in uniq if o['cat']==c]
    bad=[x for x in xs if x.isupper()]
    print(f"## {c} n={len(xs)} upper={len(bad)}", ", ".join(sorted(xs))[:600])
