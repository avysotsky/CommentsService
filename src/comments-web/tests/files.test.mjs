import { test } from 'node:test';
import assert from 'node:assert/strict';
import { fileUrl, attachments } from '../src/files/model.ts';
import { HttpFileClient } from '../src/files/client.ts';
const text = { id: '00000000-0000-0000-0000-000000000001', fileName: 'sample.txt', kind: 'text' };
const image = { ...text, kind: 'image' };
test('only UUID attachment routes can be requested; download stays on own origin', () => {
 assert.equal(fileUrl(image,true),'/api/images/'+image.id+'?download=true');
 assert.equal(fileUrl(text,true),'/api/text-files/'+text.id);
 for (const id of ['../x','https://evil.test','x?download=true','<script>']) assert.throws(()=>fileUrl({...text,id}));
});
test('gallery contains attachments in visible comment order', () => {
 assert.deepEqual(attachments([{image:null,textFile:null},{image:{id:image.id,fileName:'i.png'},textFile:null},{image:null,textFile:{id:text.id,fileName:'t.txt'}}]).map(f=>f.kind),['image','text']);
});
test('TXT viewer decodes strict UTF-8 as literal text and preserves CRLF/tabs',async()=>{
 const value='Привіт 😀\r\n<script>window.executed=true</script>\tend';
 const controller=new AbortController();
 const client=new HttpFileClient(async(url,options)=>{assert.equal(url,fileUrl(text));assert.equal(options.signal,controller.signal);assert.equal(options.cache,'no-store');return new Response('\ufeff'+value,{headers:{'content-type':'text/plain; charset=utf-8'}})});
 assert.equal(await client.load(text,controller.signal),value);
});
test('viewer rejects missing, unavailable, wrong-type, invalid UTF-8, empty and oversized TXT',async()=>{
 for(const response of [new Response('private',{status:404}),new Response('private',{status:503}),new Response('<script>',{headers:{'content-type':'text/html'}}),new Response(new Uint8Array([255]),{headers:{'content-type':'text/plain'}}),new Response('',{headers:{'content-type':'text/plain'}}),new Response('x'.repeat(102401),{headers:{'content-type':'text/plain'}})]) {
  await assert.rejects(new HttpFileClient(async()=>response).load(text,new AbortController().signal));
 }
});
test('viewer accepts 100 KiB and rejects a chunked response when limit is exceeded',async()=>{
 const headers={'content-type':'text/plain'};
 assert.equal((await new HttpFileClient(async()=>new Response('a'.repeat(102400),{headers})).load(text,new AbortController().signal)).length,102400);
 let canceled=false;
 const stream=new ReadableStream({start(c){c.enqueue(new Uint8Array(60000));c.enqueue(new Uint8Array(60000));},cancel(){canceled=true}});
 await assert.rejects(new HttpFileClient(async()=>new Response(stream,{headers})).load(text,new AbortController().signal));assert(canceled);
});
test('image viewer returns a typed PNG blob with original bytes',async()=>{
 const data=new Uint8Array([137,80,78,71]);
 const blob=await new HttpFileClient(async()=>new Response(data,{headers:{'content-type':'image/png'}})).load(image,new AbortController().signal);
 assert.equal(blob.type,'image/png');assert.deepEqual(new Uint8Array(await blob.arrayBuffer()),data);
});
test('transport cancellation is propagated without exposing provider errors',async()=>{
 const controller=new AbortController();controller.abort();
 await assert.rejects(new HttpFileClient(async(url,{signal})=>{assert(signal.aborted);throw new DOMException('Aborted','AbortError')}).load(text,controller.signal),{name:'AbortError'});
});
