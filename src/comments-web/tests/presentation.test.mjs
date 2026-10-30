import { test } from 'node:test';
import assert from 'node:assert/strict';
import { messages } from '../src/messages.ts';
import { fieldError } from '../src/comments/validation.ts';

test('all supported interface languages provide the same nonempty message keys',()=>{
 const keys=Object.keys(messages.en).sort();
 for(const labels of Object.values(messages)){
  assert.deepEqual(Object.keys(labels).sort(),keys);
  assert(Object.values(labels).every(value=>typeof value==='string'&&value.trim().length>0));
 }
});
test('server field failures use the selected language and never provider text',()=>{
 for(const labels of Object.values(messages)) for(const [field,code] of Object.entries({userName:'nameError',email:'emailError',homePage:'homeError',text:'textError'})) {
  assert.equal(fieldError(field,undefined,{[field]:['Private provider message <script>']},labels),labels[code]);
  assert.equal(fieldError(field,code,{},labels),labels[code]);
  assert.equal(fieldError(field,undefined,{},labels),undefined);
  assert.equal(fieldError(field,undefined,{[field]:[]},labels),undefined);
 }
});
test('English default messages describe implemented XHTML and accessibility controls',()=>{
 assert.match(messages.en.textError,/XHTML/);assert.match(messages.en.skipComments,/comments/);
 assert.match(messages.en.requiredHint,/required/);assert.match(messages.en.newTab,/new tab/);
 assert(!Object.values(messages.en).some(value=>/not supported yet/i.test(value)));
});
