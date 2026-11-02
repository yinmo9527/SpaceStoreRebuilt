import struct
def streams(path):
    d=open(path,'rb').read()
    pe=struct.unpack_from('<I',d,0x3c)[0]
    coff=pe+4
    numsec=struct.unpack_from('<H',d,coff+2)[0]
    optsz=struct.unpack_from('<H',d,coff+16)[0]
    opt=coff+20
    magic=struct.unpack_from('<H',d,opt)[0]
    ddoff=opt+(96 if magic==0x10b else 112)
    cli_rva,_=struct.unpack_from('<II',d,ddoff+14*8)
    secs=[];so=opt+optsz
    for i in range(numsec):
        s=so+i*40
        vsz,vaddr,rsz,raddr=struct.unpack_from('<IIII',d,s+8)
        secs.append((vaddr,vsz,raddr,rsz))
    def r2o(rva):
        for vaddr,vsz,raddr,rsz in secs:
            if vaddr<=rva<vaddr+max(vsz,rsz): return raddr+(rva-vaddr)
        return None
    cli=r2o(cli_rva)
    md_rva,_=struct.unpack_from('<II',d,cli+8)
    md=r2o(md_rva)
    vlen=struct.unpack_from('<I',d,md+12)[0]
    p=md+16+vlen; p+=2
    n=struct.unpack_from('<H',d,p)[0]; p+=2
    out={}
    for _ in range(n):
        off,size=struct.unpack_from('<II',d,p); p+=8
        e=d.index(b'\0',p); name=d[p:e].decode(); p=e+1
        p=(p+3)//4*4
        out[name]=(d,md+off,size)
    return out
def read_compress(b,i):
    x=b[i]
    if x&0x80==0: return x,i+1
    if x&0xC0==0x80: return ((x&0x3f)<<8)|b[i+1],i+2
    return ((x&0x1f)<<24)|(b[i+1]<<16)|(b[i+2]<<8)|b[i+3],i+4
def us_literals(path):
    st=streams(path); d,off,size=st['#US']
    b=d[off:off+size]; out=set(); i=1
    while i<len(b):
        ln,i2=read_compress(b,i)
        if ln==0: i=i2; continue
        raw=b[i2:i2+ln]
        if len(raw)<ln: break
        try:
            s=raw.decode('utf-16-le').rstrip('\x00')
            if s: out.add(s)
        except Exception: pass
        i=i2+ln
    return out
