import './configure.mjs';
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { HttpSearchClient } from '../src/comments/searchClient.ts';
import { messages } from '../src/messages.ts';
test('search encodes literal Unicode input and page, forwards cancellation and avoids cache',async()=>{
  const controller=new AbortController();
  const client=new HttpSearchClient(async(url,options)=>{
    assert.equal(new URL(url,'http://local').searchParams.get('query'),'Привіт & {"match_all":{}}');
    assert.equal(options.signal,controller.signal);assert.equal(options.cache,'no-store');
    return Response.json({items:[],page:2,pageSize:10,hasMore:false});
  });
  assert.equal((await client.search('Привіт & {"match_all":{}}',2,controller.signal)).page,2);
});
test('search failures do not expose provider responses and discussion loads by encoded root ID',async()=>{
  await assert.rejects(new HttpSearchClient(async()=>new Response('private secret',{status:503})).search('x',1),error=>error.message==='Search unavailable');
  const client=new HttpSearchClient(async(url,options)=>{assert.equal(url,'/api/comments/root%2Fid');assert.equal(options.cache,'no-store');return Response.json({id:'root'});});
  assert.equal((await client.root('root/id')).id,'root');
});
test('search messages cover all interface languages',()=>{
  for(const lang of ['en','ru','uk'])for(const key of ['searchTitle','searchUnavailable','searchHint','openDiscussion','selectedComment'])assert.equal(typeof messages[lang][key],'string');
});
