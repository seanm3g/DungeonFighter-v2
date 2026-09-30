// Small standards-compliant stored ZIP writer, avoiding a runtime dependency.
const table=Uint32Array.from({length:256},(_,i)=>{for(let j=0;j<8;j++)i=i&1?0xedb88320^(i>>>1):i>>>1;return i>>>0;});
function crc32(bytes){let n=0xffffffff;for(const b of bytes)n=table[(n^b)&255]^(n>>>8);return (n^0xffffffff)>>>0;}
export async function zipFiles(files){let offset=0;const chunks=[],directory=[];
 for(const [name,value] of Object.entries(files)){
  const nameBytes=new TextEncoder().encode(name),data=value instanceof Blob?new Uint8Array(await value.arrayBuffer()):new TextEncoder().encode(value),crc=crc32(data);
  const header=new Uint8Array(30+nameBytes.length),h=new DataView(header.buffer);h.setUint32(0,0x04034b50,true);h.setUint16(4,20,true);h.setUint16(6,0x800,true);h.setUint32(14,crc,true);h.setUint32(18,data.length,true);h.setUint32(22,data.length,true);h.setUint16(26,nameBytes.length,true);header.set(nameBytes,30);
  const entry=new Uint8Array(46+nameBytes.length),e=new DataView(entry.buffer);e.setUint32(0,0x02014b50,true);e.setUint16(4,20,true);e.setUint16(6,20,true);e.setUint16(8,0x800,true);e.setUint32(16,crc,true);e.setUint32(20,data.length,true);e.setUint32(24,data.length,true);e.setUint16(28,nameBytes.length,true);e.setUint32(42,offset,true);entry.set(nameBytes,46);
  chunks.push(header,data);directory.push(entry);offset+=header.length+data.length;
 }
 const end=new Uint8Array(22),e=new DataView(end.buffer);e.setUint32(0,0x06054b50,true);e.setUint16(8,directory.length,true);e.setUint16(10,directory.length,true);e.setUint32(12,directory.reduce((s,v)=>s+v.length,0),true);e.setUint32(16,offset,true);return new Blob([...chunks,...directory,end],{type:'application/zip'});
}
