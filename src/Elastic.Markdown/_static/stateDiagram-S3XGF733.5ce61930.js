!function(){function t(t,e,s,i){Object.defineProperty(t,e,{get:s,set:i,enumerable:!0,configurable:!0})}var e=("undefined"!=typeof globalThis?globalThis:"undefined"!=typeof self?self:"undefined"!=typeof window?window:"undefined"!=typeof global?global:{}).parcelRequire6955;(0,e.register)("7JDNJ",function(s,i){t(s.exports,"a",function(){return h}),t(s.exports,"b",function(){return F}),t(s.exports,"c",function(){return tP}),t(s.exports,"d",function(){return tY});var r=e("6ZUrg"),a=e("1wdMA"),n=e("fxedn"),o=e("kNZMB"),l=e("6t5tb"),c=function(){var t=(0,l.a)(function(t,e,s,i){for(s=s||{},i=t.length;i--;s[t[i]]=e);return s},"o"),e=[1,2],s=[1,3],i=[1,4],r=[2,4],a=[1,9],n=[1,11],o=[1,16],c=[1,17],h=[1,18],d=[1,19],u=[1,32],p=[1,20],y=[1,21],f=[1,22],g=[1,23],S=[1,24],m=[1,26],b=[1,27],_=[1,28],T=[1,29],k=[1,30],E=[1,31],x=[1,34],C=[1,35],D=[1,36],$=[1,37],L=[1,33],v=[1,4,5,16,17,19,21,22,24,25,26,27,28,29,33,35,37,38,42,45,48,49,50,51,54],A=[1,4,5,14,15,16,17,19,21,22,24,25,26,27,28,29,33,35,37,38,42,45,48,49,50,51,54],I=[4,5,16,17,19,21,22,24,25,26,27,28,29,33,35,37,38,42,45,48,49,50,51,54],O={trace:(0,l.a)(function(){},"trace"),yy:{},symbols_:{error:2,start:3,SPACE:4,NL:5,SD:6,document:7,line:8,statement:9,classDefStatement:10,styleStatement:11,cssClassStatement:12,idStatement:13,DESCR:14,"--\x3e":15,HIDE_EMPTY:16,scale:17,WIDTH:18,COMPOSIT_STATE:19,STRUCT_START:20,STRUCT_STOP:21,STATE_DESCR:22,AS:23,ID:24,FORK:25,JOIN:26,CHOICE:27,CONCURRENT:28,note:29,notePosition:30,NOTE_TEXT:31,direction:32,acc_title:33,acc_title_value:34,acc_descr:35,acc_descr_value:36,acc_descr_multiline_value:37,classDef:38,CLASSDEF_ID:39,CLASSDEF_STYLEOPTS:40,DEFAULT:41,style:42,STYLE_IDS:43,STYLEDEF_STYLEOPTS:44,class:45,CLASSENTITY_IDS:46,STYLECLASS:47,direction_tb:48,direction_bt:49,direction_rl:50,direction_lr:51,eol:52,";":53,EDGE_STATE:54,STYLE_SEPARATOR:55,left_of:56,right_of:57,$accept:0,$end:1},terminals_:{2:"error",4:"SPACE",5:"NL",6:"SD",14:"DESCR",15:"--\x3e",16:"HIDE_EMPTY",17:"scale",18:"WIDTH",19:"COMPOSIT_STATE",20:"STRUCT_START",21:"STRUCT_STOP",22:"STATE_DESCR",23:"AS",24:"ID",25:"FORK",26:"JOIN",27:"CHOICE",28:"CONCURRENT",29:"note",31:"NOTE_TEXT",33:"acc_title",34:"acc_title_value",35:"acc_descr",36:"acc_descr_value",37:"acc_descr_multiline_value",38:"classDef",39:"CLASSDEF_ID",40:"CLASSDEF_STYLEOPTS",41:"DEFAULT",42:"style",43:"STYLE_IDS",44:"STYLEDEF_STYLEOPTS",45:"class",46:"CLASSENTITY_IDS",47:"STYLECLASS",48:"direction_tb",49:"direction_bt",50:"direction_rl",51:"direction_lr",53:";",54:"EDGE_STATE",55:"STYLE_SEPARATOR",56:"left_of",57:"right_of"},productions_:[0,[3,2],[3,2],[3,2],[7,0],[7,2],[8,2],[8,1],[8,1],[9,1],[9,1],[9,1],[9,1],[9,2],[9,3],[9,4],[9,1],[9,2],[9,1],[9,4],[9,3],[9,6],[9,1],[9,1],[9,1],[9,1],[9,4],[9,4],[9,1],[9,2],[9,2],[9,1],[10,3],[10,3],[11,3],[12,3],[32,1],[32,1],[32,1],[32,1],[52,1],[52,1],[13,1],[13,1],[13,3],[13,3],[30,1],[30,1]],performAction:(0,l.a)(function(t,e,s,i,r,a,n){var o=a.length-1;switch(r){case 3:return i.setRootDoc(a[o]),a[o];case 4:this.$=[];break;case 5:"nl"!=a[o]&&(a[o-1].push(a[o]),this.$=a[o-1]);break;case 6:case 7:case 12:this.$=a[o];break;case 8:this.$="nl";break;case 13:let l=a[o-1];l.description=i.trimColon(a[o]),this.$=l;break;case 14:this.$={stmt:"relation",state1:a[o-2],state2:a[o]};break;case 15:let c=i.trimColon(a[o]);this.$={stmt:"relation",state1:a[o-3],state2:a[o-1],description:c};break;case 19:this.$={stmt:"state",id:a[o-3],type:"default",description:"",doc:a[o-1]};break;case 20:var h=a[o],d=a[o-2].trim();if(a[o].match(":")){var u=a[o].split(":");h=u[0],d=[d,u[1]]}this.$={stmt:"state",id:h,type:"default",description:d};break;case 21:this.$={stmt:"state",id:a[o-3],type:"default",description:a[o-5],doc:a[o-1]};break;case 22:this.$={stmt:"state",id:a[o],type:"fork"};break;case 23:this.$={stmt:"state",id:a[o],type:"join"};break;case 24:this.$={stmt:"state",id:a[o],type:"choice"};break;case 25:this.$={stmt:"state",id:i.getDividerId(),type:"divider"};break;case 26:this.$={stmt:"state",id:a[o-1].trim(),note:{position:a[o-2].trim(),text:a[o].trim()}};break;case 29:this.$=a[o].trim(),i.setAccTitle(this.$);break;case 30:case 31:this.$=a[o].trim(),i.setAccDescription(this.$);break;case 32:case 33:this.$={stmt:"classDef",id:a[o-1].trim(),classes:a[o].trim()};break;case 34:this.$={stmt:"style",id:a[o-1].trim(),styleClass:a[o].trim()};break;case 35:this.$={stmt:"applyClass",id:a[o-1].trim(),styleClass:a[o].trim()};break;case 36:i.setDirection("TB"),this.$={stmt:"dir",value:"TB"};break;case 37:i.setDirection("BT"),this.$={stmt:"dir",value:"BT"};break;case 38:i.setDirection("RL"),this.$={stmt:"dir",value:"RL"};break;case 39:i.setDirection("LR"),this.$={stmt:"dir",value:"LR"};break;case 42:case 43:this.$={stmt:"state",id:a[o].trim(),type:"default",description:""};break;case 44:case 45:this.$={stmt:"state",id:a[o-2].trim(),classes:[a[o].trim()],type:"default",description:""}}},"anonymous"),table:[{3:1,4:e,5:s,6:i},{1:[3]},{3:5,4:e,5:s,6:i},{3:6,4:e,5:s,6:i},t([1,4,5,16,17,19,22,24,25,26,27,28,29,33,35,37,38,42,45,48,49,50,51,54],r,{7:7}),{1:[2,1]},{1:[2,2]},{1:[2,3],4:a,5:n,8:8,9:10,10:12,11:13,12:14,13:15,16:o,17:c,19:h,22:d,24:u,25:p,26:y,27:f,28:g,29:S,32:25,33:m,35:b,37:_,38:T,42:k,45:E,48:x,49:C,50:D,51:$,54:L},t(v,[2,5]),{9:38,10:12,11:13,12:14,13:15,16:o,17:c,19:h,22:d,24:u,25:p,26:y,27:f,28:g,29:S,32:25,33:m,35:b,37:_,38:T,42:k,45:E,48:x,49:C,50:D,51:$,54:L},t(v,[2,7]),t(v,[2,8]),t(v,[2,9]),t(v,[2,10]),t(v,[2,11]),t(v,[2,12],{14:[1,39],15:[1,40]}),t(v,[2,16]),{18:[1,41]},t(v,[2,18],{20:[1,42]}),{23:[1,43]},t(v,[2,22]),t(v,[2,23]),t(v,[2,24]),t(v,[2,25]),{30:44,31:[1,45],56:[1,46],57:[1,47]},t(v,[2,28]),{34:[1,48]},{36:[1,49]},t(v,[2,31]),{39:[1,50],41:[1,51]},{43:[1,52]},{46:[1,53]},t(A,[2,42],{55:[1,54]}),t(A,[2,43],{55:[1,55]}),t(v,[2,36]),t(v,[2,37]),t(v,[2,38]),t(v,[2,39]),t(v,[2,6]),t(v,[2,13]),{13:56,24:u,54:L},t(v,[2,17]),t(I,r,{7:57}),{24:[1,58]},{24:[1,59]},{23:[1,60]},{24:[2,46]},{24:[2,47]},t(v,[2,29]),t(v,[2,30]),{40:[1,61]},{40:[1,62]},{44:[1,63]},{47:[1,64]},{24:[1,65]},{24:[1,66]},t(v,[2,14],{14:[1,67]}),{4:a,5:n,8:8,9:10,10:12,11:13,12:14,13:15,16:o,17:c,19:h,21:[1,68],22:d,24:u,25:p,26:y,27:f,28:g,29:S,32:25,33:m,35:b,37:_,38:T,42:k,45:E,48:x,49:C,50:D,51:$,54:L},t(v,[2,20],{20:[1,69]}),{31:[1,70]},{24:[1,71]},t(v,[2,32]),t(v,[2,33]),t(v,[2,34]),t(v,[2,35]),t(A,[2,44]),t(A,[2,45]),t(v,[2,15]),t(v,[2,19]),t(I,r,{7:72}),t(v,[2,26]),t(v,[2,27]),{4:a,5:n,8:8,9:10,10:12,11:13,12:14,13:15,16:o,17:c,19:h,21:[1,73],22:d,24:u,25:p,26:y,27:f,28:g,29:S,32:25,33:m,35:b,37:_,38:T,42:k,45:E,48:x,49:C,50:D,51:$,54:L},t(v,[2,21])],defaultActions:{5:[2,1],6:[2,2],46:[2,46],47:[2,47]},parseError:(0,l.a)(function(t,e){if(e.recoverable)this.trace(t);else{var s=Error(t);throw s.hash=e,s}},"parseError"),parse:(0,l.a)(function(t){var e=this,s=[0],i=[],r=[null],a=[],n=this.table,o="",c=0,h=0,d=0,u=a.slice.call(arguments,1),p=Object.create(this.lexer),y={yy:{}};for(var f in this.yy)Object.prototype.hasOwnProperty.call(this.yy,f)&&(y.yy[f]=this.yy[f]);p.setInput(t,y.yy),y.yy.lexer=p,y.yy.parser=this,typeof p.yylloc>"u"&&(p.yylloc={});var g=p.yylloc;a.push(g);var S=p.options&&p.options.ranges;function m(){var t;return"number"!=typeof(t=i.pop()||p.lex()||1)&&(t instanceof Array&&(t=(i=t).pop()),t=e.symbols_[t]||t),t}"function"==typeof y.yy.parseError?this.parseError=y.yy.parseError:this.parseError=Object.getPrototypeOf(this).parseError,(0,l.a)(function(t){s.length=s.length-2*t,r.length=r.length-t,a.length=a.length-t},"popStack"),(0,l.a)(m,"lex");for(var b,_,T,k,E,x,C,D,$,L={};;){if(T=s[s.length-1],this.defaultActions[T]?k=this.defaultActions[T]:((null===b||typeof b>"u")&&(b=m()),k=n[T]&&n[T][b]),typeof k>"u"||!k.length||!k[0]){var v="";for(x in $=[],n[T])this.terminals_[x]&&x>2&&$.push("'"+this.terminals_[x]+"'");v=p.showPosition?"Parse error on line "+(c+1)+`:
`+p.showPosition()+`
Expecting `+$.join(", ")+", got '"+(this.terminals_[b]||b)+"'":"Parse error on line "+(c+1)+": Unexpected "+(1==b?"end of input":"'"+(this.terminals_[b]||b)+"'"),this.parseError(v,{text:p.match,token:this.terminals_[b]||b,line:p.yylineno,loc:g,expected:$})}if(k[0]instanceof Array&&k.length>1)throw Error("Parse Error: multiple actions possible at state: "+T+", token: "+b);switch(k[0]){case 1:s.push(b),r.push(p.yytext),a.push(p.yylloc),s.push(k[1]),b=null,_?(b=_,_=null):(h=p.yyleng,o=p.yytext,c=p.yylineno,g=p.yylloc,d>0&&d--);break;case 2:if(C=this.productions_[k[1]][1],L.$=r[r.length-C],L._$={first_line:a[a.length-(C||1)].first_line,last_line:a[a.length-1].last_line,first_column:a[a.length-(C||1)].first_column,last_column:a[a.length-1].last_column},S&&(L._$.range=[a[a.length-(C||1)].range[0],a[a.length-1].range[1]]),"u">typeof(E=this.performAction.apply(L,[o,h,c,y.yy,k[1],r,a].concat(u))))return E;C&&(s=s.slice(0,-1*C*2),r=r.slice(0,-1*C),a=a.slice(0,-1*C)),s.push(this.productions_[k[1]][0]),r.push(L.$),a.push(L._$),D=n[s[s.length-2]][s[s.length-1]],s.push(D);break;case 3:return!0}}return!0},"parse")};function w(){this.yy={}}return O.lexer={EOF:1,parseError:(0,l.a)(function(t,e){if(this.yy.parser)this.yy.parser.parseError(t,e);else throw Error(t)},"parseError"),setInput:(0,l.a)(function(t,e){return this.yy=e||this.yy||{},this._input=t,this._more=this._backtrack=this.done=!1,this.yylineno=this.yyleng=0,this.yytext=this.matched=this.match="",this.conditionStack=["INITIAL"],this.yylloc={first_line:1,first_column:0,last_line:1,last_column:0},this.options.ranges&&(this.yylloc.range=[0,0]),this.offset=0,this},"setInput"),input:(0,l.a)(function(){var t=this._input[0];return this.yytext+=t,this.yyleng++,this.offset++,this.match+=t,this.matched+=t,t.match(/(?:\r\n?|\n).*/g)?(this.yylineno++,this.yylloc.last_line++):this.yylloc.last_column++,this.options.ranges&&this.yylloc.range[1]++,this._input=this._input.slice(1),t},"input"),unput:(0,l.a)(function(t){var e=t.length,s=t.split(/(?:\r\n?|\n)/g);this._input=t+this._input,this.yytext=this.yytext.substr(0,this.yytext.length-e),this.offset-=e;var i=this.match.split(/(?:\r\n?|\n)/g);this.match=this.match.substr(0,this.match.length-1),this.matched=this.matched.substr(0,this.matched.length-1),s.length-1&&(this.yylineno-=s.length-1);var r=this.yylloc.range;return this.yylloc={first_line:this.yylloc.first_line,last_line:this.yylineno+1,first_column:this.yylloc.first_column,last_column:s?(s.length===i.length?this.yylloc.first_column:0)+i[i.length-s.length].length-s[0].length:this.yylloc.first_column-e},this.options.ranges&&(this.yylloc.range=[r[0],r[0]+this.yyleng-e]),this.yyleng=this.yytext.length,this},"unput"),more:(0,l.a)(function(){return this._more=!0,this},"more"),reject:(0,l.a)(function(){return this.options.backtrack_lexer?(this._backtrack=!0,this):this.parseError("Lexical error on line "+(this.yylineno+1)+`. You can only invoke reject() in the lexer when the lexer is of the backtracking persuasion (options.backtrack_lexer = true).
`+this.showPosition(),{text:"",token:null,line:this.yylineno})},"reject"),less:(0,l.a)(function(t){this.unput(this.match.slice(t))},"less"),pastInput:(0,l.a)(function(){var t=this.matched.substr(0,this.matched.length-this.match.length);return(t.length>20?"...":"")+t.substr(-20).replace(/\n/g,"")},"pastInput"),upcomingInput:(0,l.a)(function(){var t=this.match;return t.length<20&&(t+=this._input.substr(0,20-t.length)),(t.substr(0,20)+(t.length>20?"...":"")).replace(/\n/g,"")},"upcomingInput"),showPosition:(0,l.a)(function(){var t=this.pastInput(),e=Array(t.length+1).join("-");return t+this.upcomingInput()+`
`+e+"^"},"showPosition"),test_match:(0,l.a)(function(t,e){var s,i,r;if(this.options.backtrack_lexer&&(r={yylineno:this.yylineno,yylloc:{first_line:this.yylloc.first_line,last_line:this.last_line,first_column:this.yylloc.first_column,last_column:this.yylloc.last_column},yytext:this.yytext,match:this.match,matches:this.matches,matched:this.matched,yyleng:this.yyleng,offset:this.offset,_more:this._more,_input:this._input,yy:this.yy,conditionStack:this.conditionStack.slice(0),done:this.done},this.options.ranges&&(r.yylloc.range=this.yylloc.range.slice(0))),(i=t[0].match(/(?:\r\n?|\n).*/g))&&(this.yylineno+=i.length),this.yylloc={first_line:this.yylloc.last_line,last_line:this.yylineno+1,first_column:this.yylloc.last_column,last_column:i?i[i.length-1].length-i[i.length-1].match(/\r?\n?/)[0].length:this.yylloc.last_column+t[0].length},this.yytext+=t[0],this.match+=t[0],this.matches=t,this.yyleng=this.yytext.length,this.options.ranges&&(this.yylloc.range=[this.offset,this.offset+=this.yyleng]),this._more=!1,this._backtrack=!1,this._input=this._input.slice(t[0].length),this.matched+=t[0],s=this.performAction.call(this,this.yy,this,e,this.conditionStack[this.conditionStack.length-1]),this.done&&this._input&&(this.done=!1),s)return s;if(this._backtrack)for(var a in r)this[a]=r[a];return!1},"test_match"),next:(0,l.a)(function(){if(this.done)return this.EOF;this._input||(this.done=!0),this._more||(this.yytext="",this.match="");for(var t,e,s,i,r=this._currentRules(),a=0;a<r.length;a++)if((s=this._input.match(this.rules[r[a]]))&&(!e||s[0].length>e[0].length)){if(e=s,i=a,this.options.backtrack_lexer){if(!1!==(t=this.test_match(s,r[a])))return t;if(!this._backtrack)return!1;e=!1;continue}else if(!this.options.flex)break}return e?!1!==(t=this.test_match(e,r[i]))&&t:""===this._input?this.EOF:this.parseError("Lexical error on line "+(this.yylineno+1)+`. Unrecognized text.
`+this.showPosition(),{text:"",token:null,line:this.yylineno})},"next"),lex:(0,l.a)(function(){return this.next()||this.lex()},"lex"),begin:(0,l.a)(function(t){this.conditionStack.push(t)},"begin"),popState:(0,l.a)(function(){return this.conditionStack.length-1>0?this.conditionStack.pop():this.conditionStack[0]},"popState"),_currentRules:(0,l.a)(function(){return this.conditionStack.length&&this.conditionStack[this.conditionStack.length-1]?this.conditions[this.conditionStack[this.conditionStack.length-1]].rules:this.conditions.INITIAL.rules},"_currentRules"),topState:(0,l.a)(function(t){return(t=this.conditionStack.length-1-Math.abs(t||0))>=0?this.conditionStack[t]:"INITIAL"},"topState"),pushState:(0,l.a)(function(t){this.begin(t)},"pushState"),stateStackSize:(0,l.a)(function(){return this.conditionStack.length},"stateStackSize"),options:{"case-insensitive":!0},performAction:(0,l.a)(function(t,e,s,i){switch(s){case 0:return 41;case 1:case 42:return 48;case 2:case 43:return 49;case 3:case 44:return 50;case 4:case 45:return 51;case 5:case 6:case 8:case 9:case 10:case 11:case 54:case 56:case 62:break;case 7:case 77:return 5;case 12:case 32:return this.pushState("SCALE"),17;case 13:case 33:return 18;case 14:case 20:case 34:case 49:case 52:this.popState();break;case 15:return this.begin("acc_title"),33;case 16:return this.popState(),"acc_title_value";case 17:return this.begin("acc_descr"),35;case 18:return this.popState(),"acc_descr_value";case 19:this.begin("acc_descr_multiline");break;case 21:return"acc_descr_multiline_value";case 22:return this.pushState("CLASSDEF"),38;case 23:return this.popState(),this.pushState("CLASSDEFID"),"DEFAULT_CLASSDEF_ID";case 24:return this.popState(),this.pushState("CLASSDEFID"),39;case 25:return this.popState(),40;case 26:return this.pushState("CLASS"),45;case 27:return this.popState(),this.pushState("CLASS_STYLE"),46;case 28:return this.popState(),47;case 29:return this.pushState("STYLE"),42;case 30:return this.popState(),this.pushState("STYLEDEF_STYLES"),43;case 31:return this.popState(),44;case 35:this.pushState("STATE");break;case 36:case 39:return this.popState(),e.yytext=e.yytext.slice(0,-8).trim(),25;case 37:case 40:return this.popState(),e.yytext=e.yytext.slice(0,-8).trim(),26;case 38:case 41:return this.popState(),e.yytext=e.yytext.slice(0,-10).trim(),27;case 46:this.pushState("STATE_STRING");break;case 47:return this.pushState("STATE_ID"),"AS";case 48:case 64:return this.popState(),"ID";case 50:return"STATE_DESCR";case 51:return 19;case 53:return this.popState(),this.pushState("struct"),20;case 55:return this.popState(),21;case 57:return this.begin("NOTE"),29;case 58:return this.popState(),this.pushState("NOTE_ID"),56;case 59:return this.popState(),this.pushState("NOTE_ID"),57;case 60:this.popState(),this.pushState("FLOATING_NOTE");break;case 61:return this.popState(),this.pushState("FLOATING_NOTE_ID"),"AS";case 63:return"NOTE_TEXT";case 65:return this.popState(),this.pushState("NOTE_TEXT"),24;case 66:return this.popState(),e.yytext=e.yytext.substr(2).trim(),31;case 67:return this.popState(),e.yytext=e.yytext.slice(0,-8).trim(),31;case 68:case 69:return 6;case 70:return 16;case 71:return 54;case 72:return 24;case 73:return e.yytext=e.yytext.trim(),14;case 74:return 15;case 75:return 28;case 76:return 55;case 78:return"INVALID"}},"anonymous"),rules:[/^(?:default\b)/i,/^(?:.*direction\s+TB[^\n]*)/i,/^(?:.*direction\s+BT[^\n]*)/i,/^(?:.*direction\s+RL[^\n]*)/i,/^(?:.*direction\s+LR[^\n]*)/i,/^(?:%%(?!\{)[^\n]*)/i,/^(?:[^\}]%%[^\n]*)/i,/^(?:[\n]+)/i,/^(?:[\s]+)/i,/^(?:((?!\n)\s)+)/i,/^(?:#[^\n]*)/i,/^(?:%[^\n]*)/i,/^(?:scale\s+)/i,/^(?:\d+)/i,/^(?:\s+width\b)/i,/^(?:accTitle\s*:\s*)/i,/^(?:(?!\n||)*[^\n]*)/i,/^(?:accDescr\s*:\s*)/i,/^(?:(?!\n||)*[^\n]*)/i,/^(?:accDescr\s*\{\s*)/i,/^(?:[\}])/i,/^(?:[^\}]*)/i,/^(?:classDef\s+)/i,/^(?:DEFAULT\s+)/i,/^(?:\w+\s+)/i,/^(?:[^\n]*)/i,/^(?:class\s+)/i,/^(?:(\w+)+((,\s*\w+)*))/i,/^(?:[^\n]*)/i,/^(?:style\s+)/i,/^(?:[\w,]+\s+)/i,/^(?:[^\n]*)/i,/^(?:scale\s+)/i,/^(?:\d+)/i,/^(?:\s+width\b)/i,/^(?:state\s+)/i,/^(?:.*<<fork>>)/i,/^(?:.*<<join>>)/i,/^(?:.*<<choice>>)/i,/^(?:.*\[\[fork\]\])/i,/^(?:.*\[\[join\]\])/i,/^(?:.*\[\[choice\]\])/i,/^(?:.*direction\s+TB[^\n]*)/i,/^(?:.*direction\s+BT[^\n]*)/i,/^(?:.*direction\s+RL[^\n]*)/i,/^(?:.*direction\s+LR[^\n]*)/i,/^(?:["])/i,/^(?:\s*as\s+)/i,/^(?:[^\n\{]*)/i,/^(?:["])/i,/^(?:[^"]*)/i,/^(?:[^\n\s\{]+)/i,/^(?:\n)/i,/^(?:\{)/i,/^(?:%%(?!\{)[^\n]*)/i,/^(?:\})/i,/^(?:[\n])/i,/^(?:note\s+)/i,/^(?:left of\b)/i,/^(?:right of\b)/i,/^(?:")/i,/^(?:\s*as\s*)/i,/^(?:["])/i,/^(?:[^"]*)/i,/^(?:[^\n]*)/i,/^(?:\s*[^:\n\s\-]+)/i,/^(?:\s*:[^:\n;]+)/i,/^(?:[\s\S]*?end note\b)/i,/^(?:stateDiagram\s+)/i,/^(?:stateDiagram-v2\s+)/i,/^(?:hide empty description\b)/i,/^(?:\[\*\])/i,/^(?:[^:\n\s\-\{]+)/i,/^(?:\s*:[^:\n;]+)/i,/^(?:-->)/i,/^(?:--)/i,/^(?::::)/i,/^(?:$)/i,/^(?:.)/i],conditions:{LINE:{rules:[9,10],inclusive:!1},struct:{rules:[9,10,22,26,29,35,42,43,44,45,54,55,56,57,71,72,73,74,75],inclusive:!1},FLOATING_NOTE_ID:{rules:[64],inclusive:!1},FLOATING_NOTE:{rules:[61,62,63],inclusive:!1},NOTE_TEXT:{rules:[66,67],inclusive:!1},NOTE_ID:{rules:[65],inclusive:!1},NOTE:{rules:[58,59,60],inclusive:!1},STYLEDEF_STYLEOPTS:{rules:[],inclusive:!1},STYLEDEF_STYLES:{rules:[31],inclusive:!1},STYLE_IDS:{rules:[],inclusive:!1},STYLE:{rules:[30],inclusive:!1},CLASS_STYLE:{rules:[28],inclusive:!1},CLASS:{rules:[27],inclusive:!1},CLASSDEFID:{rules:[25],inclusive:!1},CLASSDEF:{rules:[23,24],inclusive:!1},acc_descr_multiline:{rules:[20,21],inclusive:!1},acc_descr:{rules:[18],inclusive:!1},acc_title:{rules:[16],inclusive:!1},SCALE:{rules:[13,14,33,34],inclusive:!1},ALIAS:{rules:[],inclusive:!1},STATE_ID:{rules:[48],inclusive:!1},STATE_STRING:{rules:[49,50],inclusive:!1},FORK_STATE:{rules:[],inclusive:!1},STATE:{rules:[9,10,36,37,38,39,40,41,46,47,51,52,53],inclusive:!1},ID:{rules:[9,10],inclusive:!1},INITIAL:{rules:[0,1,2,3,4,5,6,7,8,10,11,12,15,17,19,22,26,29,32,35,53,57,68,69,70,71,72,73,74,76,77,78],inclusive:!0}}},(0,l.a)(w,"Parser"),w.prototype=O,O.Parser=w,new w}();c.parser=c;var h=c,d="TB",u="state",p="relation",y="default",f="divider",g="fill:none",S="fill: #333",m="text",b="normal",_="rect",T="rectWithTitle",k="divider",E="roundedWithTitle",x="statediagram",C=`${x}-state`,D="transition",$=`${D} note-edge`,L=`${x}-note`,v=`${x}-cluster`,A=`${x}-cluster-alt`,I="parent",O="note",w="----",R=`${w}${O}`,N=`${w}${I}`,B=(0,l.a)((t,e=d)=>{if(!t.doc)return e;let s=e;for(let e of t.doc)"dir"===e.stmt&&(s=e.value);return s},"getDir"),F={getClasses:(0,l.a)(function(t,e){return e.db.extract(e.db.getRootDocV2()),e.db.getClasses()},"getClasses"),draw:(0,l.a)(async function(t,e,s,i){o.b.info("REF0:"),o.b.info("Drawing state diagram (v2)",e);let{securityLevel:l,state:c,layout:h}=(0,o.X)();i.db.extract(i.db.getRootDocV2());let d=i.db.getData(),u=(0,r.a)(e,l);d.type=i.type,d.layoutAlgorithm=h,d.nodeSpacing=c?.nodeSpacing||50,d.rankSpacing=c?.rankSpacing||50,d.markers=["barb"],d.diagramId=e,await (0,a.b)(d,u),n.m.insertTitle(u,"statediagramTitleText",c?.titleTopMargin??25,i.db.getDiagramTitle()),(0,r.b)(u,8,x,c?.useMaxWidth??!0)},"draw"),getDir:B},P=new Map,Y=0;function G(t="",e=0,s="",i=w){let r=null!==s&&s.length>0?`${i}${s}`:"";return`state-${t}${r}-${e}`}(0,l.a)(G,"stateDomId");var X=(0,l.a)((t,e,s,i,r,a,n,l)=>{o.b.trace("items",e),e.forEach(e=>{switch(e.stmt){case u:case y:H(t,e,s,i,r,a,n,l);break;case p:{H(t,e.state1,s,i,r,a,n,l),H(t,e.state2,s,i,r,a,n,l);let c={id:"edge"+Y,start:e.state1.id,end:e.state2.id,arrowhead:"normal",arrowTypeEnd:"arrow_barb",style:g,labelStyle:"",label:o.L.sanitizeText(e.description,(0,o.X)()),arrowheadStyle:S,labelpos:"c",labelType:m,thickness:b,classes:D,look:n};r.push(c),Y++}}})},"setupDoc"),j=(0,l.a)((t,e=d)=>{let s=e;if(t.doc)for(let e of t.doc)"dir"===e.stmt&&(s=e.value);return s},"getDir");function z(t,e,s){if(!e.id||"</join></fork>"===e.id||"</choice>"===e.id)return;e.cssClasses&&(Array.isArray(e.cssCompiledStyles)||(e.cssCompiledStyles=[]),e.cssClasses.split(" ").forEach(t=>{if(s.get(t)){let i=s.get(t);e.cssCompiledStyles=[...e.cssCompiledStyles,...i.styles]}}));let i=t.find(t=>t.id===e.id);i?Object.assign(i,e):t.push(e)}function U(t){return t?.classes?.join(" ")??""}function M(t){return t?.styles??[]}(0,l.a)(z,"insertOrUpdateNode"),(0,l.a)(U,"getClassesFromDbInfo"),(0,l.a)(M,"getStylesFromDbInfo");var H=(0,l.a)((t,e,s,i,r,a,n,l)=>{let c=e.id,h=s.get(c),d=U(h),u=M(h);if(o.b.info("dataFetcher parsedItem",e,h,u),"root"!==c){let s=_;!0===e.start?s="stateStart":!1===e.start&&(s="stateEnd"),e.type!==y&&(s=e.type),P.get(c)||P.set(c,{id:c,shape:s,description:o.L.sanitizeText(c,(0,o.X)()),cssClasses:`${d} ${C}`,cssStyles:u});let h=P.get(c);e.description&&(Array.isArray(h.description)?(h.shape=T,h.description.push(e.description)):h.description?.length>0?(h.shape=T,h.description===c?h.description=[e.description]:h.description=[h.description,e.description]):(h.shape=_,h.description=e.description),h.description=o.L.sanitizeTextOrArray(h.description,(0,o.X)())),h.description?.length===1&&h.shape===T&&("group"===h.type?h.shape=E:h.shape=_),!h.type&&e.doc&&(o.b.info("Setting cluster for XCX",c,j(e)),h.type="group",h.isGroup=!0,h.dir=j(e),h.shape=e.type===f?k:E,h.cssClasses=`${h.cssClasses} ${v} ${a?A:""}`);let p={labelStyle:"",shape:h.shape,label:h.description,cssClasses:h.cssClasses,cssCompiledStyles:[],cssStyles:h.cssStyles,id:c,dir:h.dir,domId:G(c,Y),type:h.type,isGroup:"group"===h.type,padding:8,rx:10,ry:10,look:n};if(p.shape===k&&(p.label=""),t&&"root"!==t.id&&(o.b.trace("Setting node ",c," to be child of its parent ",t.id),p.parentId=t.id),p.centerLabel=!0,e.note){let t={labelStyle:"",shape:"note",label:e.note.text,cssClasses:L,cssStyles:[],cssCompilesStyles:[],id:c+R+"-"+Y,domId:G(c,Y,O),type:h.type,isGroup:"group"===h.type,padding:(0,o.X)().flowchart.padding,look:n,position:e.note.position},s=c+N,a={labelStyle:"",shape:"noteGroup",label:e.note.text,cssClasses:h.cssClasses,cssStyles:[],id:c+N,domId:G(c,Y,I),type:"group",isGroup:!0,padding:16,look:n,position:e.note.position};Y++,a.id=s,t.parentId=s,z(i,a,l),z(i,t,l),z(i,p,l);let d=c,u=t.id;"left of"===e.note.position&&(d=t.id,u=c),r.push({id:d+"-"+u,start:d,end:u,arrowhead:"none",arrowTypeEnd:"",style:g,labelStyle:"",classes:$,arrowheadStyle:S,labelpos:"c",labelType:m,thickness:b,look:n})}else z(i,p,l)}e.doc&&(o.b.trace("Adding nodes children "),X(e,e.doc,s,i,r,!a,n,l))},"dataFetcher"),J=(0,l.a)(()=>{P.clear(),Y=0},"reset"),V="start",W="color",K="fill";function Z(){return new Map}(0,l.a)(Z,"newClassesList");var q=[],Q=[],tt="LR",te=[],ts=Z(),ti=(0,l.a)(()=>({relations:[],states:new Map,documents:{}}),"newDoc"),tr={root:ti()},ta=tr.root,tn=0,to=0,tl=(0,l.a)(t=>JSON.parse(JSON.stringify(t)),"clone"),tc=(0,l.a)(t=>{o.b.info("Setting root doc",t),te=t},"setRootDoc"),th=(0,l.a)(()=>te,"getRootDoc"),td=(0,l.a)((t,e,s)=>{if(e.stmt===p)td(t,e.state1,!0),td(t,e.state2,!1);else if(e.stmt===u&&("[*]"===e.id?(e.id=s?t.id+"_start":t.id+"_end",e.start=s):e.id=e.id.trim()),e.doc){let t=[],s=[],i;for(i=0;i<e.doc.length;i++)if(e.doc[i].type===f){let r=tl(e.doc[i]);r.doc=tl(s),t.push(r),s=[]}else s.push(e.doc[i]);if(t.length>0&&s.length>0){let i={stmt:u,id:(0,n.e)(),type:"divider",doc:tl(s)};t.push(tl(i)),e.doc=t}e.doc.forEach(t=>td(e,t,!0))}},"docTranslator"),tu=(0,l.a)(()=>(td({id:"root"},{id:"root",doc:te},!0),{id:"root",doc:te}),"getRootDocV2"),tp=(0,l.a)(t=>{let e;e=t.doc?t.doc:t,o.b.info(e),tf(!0),o.b.info("Extract initial document:",e),e.forEach(t=>{switch(o.b.warn("Statement",t.stmt),t.stmt){case u:ty(t.id.trim(),t.type,t.doc,t.description,t.note,t.classes,t.styles,t.textStyles);break;case p:tC(t.state1,t.state2,t.description);break;case"classDef":tv(t.id.trim(),t.classes);break;case"style":{let e=t.id.trim().split(","),s=t.styleClass.split(",");e.forEach(t=>{let e=tg(t);if(void 0===e){let s=t.trim();ty(s),e=tg(s)}e.styles=s.map(t=>t.replace(/;/g,"")?.trim())})}break;case"applyClass":tI(t.id.trim(),t.styleClass)}});let s=tS(),i=(0,o.X)().look;J(),H(void 0,tu(),s,q,Q,!0,i,ts),q.forEach(t=>{if(Array.isArray(t.label)){if(t.description=t.label.slice(1),t.isGroup&&t.description.length>0)throw Error("Group nodes can only have label. Remove the additional description for node ["+t.id+"]");t.label=t.label[0]}})},"extract"),ty=(0,l.a)(function(t,e=y,s=null,i=null,r=null,a=null,n=null,l=null){let c=t?.trim();if(ta.states.has(c)?(ta.states.get(c).doc||(ta.states.get(c).doc=s),ta.states.get(c).type||(ta.states.get(c).type=e)):(o.b.info("Adding state ",c,i),ta.states.set(c,{id:c,descriptions:[],type:e,doc:s,note:r,classes:[],styles:[],textStyles:[]})),i&&(o.b.info("Setting state description",c,i),"string"==typeof i&&tD(c,i.trim()),"object"==typeof i&&i.forEach(t=>tD(c,t.trim()))),r){let t=ta.states.get(c);t.note=r,t.note.text=o.L.sanitizeText(t.note.text,(0,o.X)())}a&&(o.b.info("Setting state classes",c,a),("string"==typeof a?[a]:a).forEach(t=>tI(c,t.trim()))),n&&(o.b.info("Setting state styles",c,n),("string"==typeof n?[n]:n).forEach(t=>tO(c,t.trim()))),l&&(o.b.info("Setting state styles",c,n),("string"==typeof l?[l]:l).forEach(t=>tw(c,t.trim())))},"addState"),tf=(0,l.a)(function(t){q=[],Q=[],ta=(tr={root:ti()}).root,tn=0,ts=Z(),t||(0,o.P)()},"clear"),tg=(0,l.a)(function(t){return ta.states.get(t)},"getState"),tS=(0,l.a)(function(){return ta.states},"getStates"),tm=(0,l.a)(function(){o.b.info("Documents = ",tr)},"logDocuments"),tb=(0,l.a)(function(){return ta.relations},"getRelations");function t_(t=""){let e=t;return"[*]"===t&&(tn++,e=`${V}${tn}`),e}function tT(t="",e=y){return"[*]"===t?V:e}function tk(t=""){let e=t;return"[*]"===t&&(tn++,e=`end${tn}`),e}function tE(t="",e=y){return"[*]"===t?"end":e}function tx(t,e,s){let i=t_(t.id.trim()),r=tT(t.id.trim(),t.type),a=t_(e.id.trim()),n=tT(e.id.trim(),e.type);ty(i,r,t.doc,t.description,t.note,t.classes,t.styles,t.textStyles),ty(a,n,e.doc,e.description,e.note,e.classes,e.styles,e.textStyles),ta.relations.push({id1:i,id2:a,relationTitle:o.L.sanitizeText(s,(0,o.X)())})}(0,l.a)(t_,"startIdIfNeeded"),(0,l.a)(tT,"startTypeIfNeeded"),(0,l.a)(tk,"endIdIfNeeded"),(0,l.a)(tE,"endTypeIfNeeded"),(0,l.a)(tx,"addRelationObjs");var tC=(0,l.a)(function(t,e,s){if("object"==typeof t)tx(t,e,s);else{let i=t_(t.trim()),r=tT(t),a=tk(e.trim()),n=tE(e);ty(i,r),ty(a,n),ta.relations.push({id1:i,id2:a,title:o.L.sanitizeText(s,(0,o.X)())})}},"addRelation"),tD=(0,l.a)(function(t,e){let s=ta.states.get(t),i=e.startsWith(":")?e.replace(":","").trim():e;s.descriptions.push(o.L.sanitizeText(i,(0,o.X)()))},"addDescription"),t$=(0,l.a)(function(t){return":"===t.substring(0,1)?t.substr(2).trim():t.trim()},"cleanupLabel"),tL=(0,l.a)(()=>"divider-id-"+ ++to,"getDividerId"),tv=(0,l.a)(function(t,e=""){ts.has(t)||ts.set(t,{id:t,styles:[],textStyles:[]});let s=ts.get(t);e?.split(",").forEach(t=>{let e=t.replace(/([^;]*);/,"$1").trim();if(RegExp(W).exec(t)){let t=e.replace(K,"bgFill").replace(W,K);s.textStyles.push(t)}s.styles.push(e)})},"addStyleClass"),tA=(0,l.a)(function(){return ts},"getClasses"),tI=(0,l.a)(function(t,e){t.split(",").forEach(function(t){let s=tg(t);if(void 0===s){let e=t.trim();ty(e),s=tg(e)}s.classes.push(e)})},"setCssClass"),tO=(0,l.a)(function(t,e){let s=tg(t);void 0!==s&&s.styles.push(e)},"setStyle"),tw=(0,l.a)(function(t,e){let s=tg(t);void 0!==s&&s.textStyles.push(e)},"setTextStyle"),tR=(0,l.a)(()=>tt,"getDirection"),tN=(0,l.a)(t=>{tt=t},"setDirection"),tB=(0,l.a)(t=>t&&":"===t[0]?t.substr(1).trim():t.trim(),"trimColon"),tF=(0,l.a)(()=>{let t=(0,o.X)();return{nodes:q,edges:Q,other:{},config:t,direction:B(tu())}},"getData"),tP={getConfig:(0,l.a)(()=>(0,o.X)().state,"getConfig"),getData:tF,addState:ty,clear:tf,getState:tg,getStates:tS,getRelations:tb,getClasses:tA,getDirection:tR,addRelation:tC,getDividerId:tL,setDirection:tN,cleanupLabel:t$,lineType:{LINE:0,DOTTED_LINE:1},relationType:{AGGREGATION:0,EXTENSION:1,COMPOSITION:2,DEPENDENCY:3},logDocuments:tm,getRootDoc:th,setRootDoc:tc,getRootDocV2:tu,extract:tp,trimColon:tB,getAccTitle:o.R,setAccTitle:o.Q,getAccDescription:o.T,setAccDescription:o.S,addStyleClass:tv,setCssClass:tI,addDescription:tD,setDiagramTitle:o.U,getDiagramTitle:o.V},tY=(0,l.a)(t=>`
defs #statediagram-barbEnd {
    fill: ${t.transitionColor};
    stroke: ${t.transitionColor};
  }
g.stateGroup text {
  fill: ${t.nodeBorder};
  stroke: none;
  font-size: 10px;
}
g.stateGroup text {
  fill: ${t.textColor};
  stroke: none;
  font-size: 10px;

}
g.stateGroup .state-title {
  font-weight: bolder;
  fill: ${t.stateLabelColor};
}

g.stateGroup rect {
  fill: ${t.mainBkg};
  stroke: ${t.nodeBorder};
}

g.stateGroup line {
  stroke: ${t.lineColor};
  stroke-width: 1;
}

.transition {
  stroke: ${t.transitionColor};
  stroke-width: 1;
  fill: none;
}

.stateGroup .composit {
  fill: ${t.background};
  border-bottom: 1px
}

.stateGroup .alt-composit {
  fill: #e0e0e0;
  border-bottom: 1px
}

.state-note {
  stroke: ${t.noteBorderColor};
  fill: ${t.noteBkgColor};

  text {
    fill: ${t.noteTextColor};
    stroke: none;
    font-size: 10px;
  }
}

.stateLabel .box {
  stroke: none;
  stroke-width: 0;
  fill: ${t.mainBkg};
  opacity: 0.5;
}

.edgeLabel .label rect {
  fill: ${t.labelBackgroundColor};
  opacity: 0.5;
}
.edgeLabel {
  background-color: ${t.edgeLabelBackground};
  p {
    background-color: ${t.edgeLabelBackground};
  }
  rect {
    opacity: 0.5;
    background-color: ${t.edgeLabelBackground};
    fill: ${t.edgeLabelBackground};
  }
  text-align: center;
}
.edgeLabel .label text {
  fill: ${t.transitionLabelColor||t.tertiaryTextColor};
}
.label div .edgeLabel {
  color: ${t.transitionLabelColor||t.tertiaryTextColor};
}

.stateLabel text {
  fill: ${t.stateLabelColor};
  font-size: 10px;
  font-weight: bold;
}

.node circle.state-start {
  fill: ${t.specialStateColor};
  stroke: ${t.specialStateColor};
}

.node .fork-join {
  fill: ${t.specialStateColor};
  stroke: ${t.specialStateColor};
}

.node circle.state-end {
  fill: ${t.innerEndBackground};
  stroke: ${t.background};
  stroke-width: 1.5
}
.end-state-inner {
  fill: ${t.compositeBackground||t.background};
  // stroke: ${t.background};
  stroke-width: 1.5
}

.node rect {
  fill: ${t.stateBkg||t.mainBkg};
  stroke: ${t.stateBorder||t.nodeBorder};
  stroke-width: 1px;
}
.node polygon {
  fill: ${t.mainBkg};
  stroke: ${t.stateBorder||t.nodeBorder};;
  stroke-width: 1px;
}
#statediagram-barbEnd {
  fill: ${t.lineColor};
}

.statediagram-cluster rect {
  fill: ${t.compositeTitleBackground};
  stroke: ${t.stateBorder||t.nodeBorder};
  stroke-width: 1px;
}

.cluster-label, .nodeLabel {
  color: ${t.stateLabelColor};
  // line-height: 1;
}

.statediagram-cluster rect.outer {
  rx: 5px;
  ry: 5px;
}
.statediagram-state .divider {
  stroke: ${t.stateBorder||t.nodeBorder};
}

.statediagram-state .title-state {
  rx: 5px;
  ry: 5px;
}
.statediagram-cluster.statediagram-cluster .inner {
  fill: ${t.compositeBackground||t.background};
}
.statediagram-cluster.statediagram-cluster-alt .inner {
  fill: ${t.altBackground?t.altBackground:"#efefef"};
}

.statediagram-cluster .inner {
  rx:0;
  ry:0;
}

.statediagram-state rect.basic {
  rx: 5px;
  ry: 5px;
}
.statediagram-state rect.divider {
  stroke-dasharray: 10,10;
  fill: ${t.altBackground?t.altBackground:"#efefef"};
}

.note-edge {
  stroke-dasharray: 5;
}

.statediagram-note rect {
  fill: ${t.noteBkgColor};
  stroke: ${t.noteBorderColor};
  stroke-width: 1px;
  rx: 0;
  ry: 0;
}
.statediagram-note rect {
  fill: ${t.noteBkgColor};
  stroke: ${t.noteBorderColor};
  stroke-width: 1px;
  rx: 0;
  ry: 0;
}

.statediagram-note text {
  fill: ${t.noteTextColor};
}

.statediagram-note .nodeLabel {
  color: ${t.noteTextColor};
}
.statediagram .edgeLabel {
  color: red; // ${t.noteTextColor};
}

#dependencyStart, #dependencyEnd {
  fill: ${t.lineColor};
  stroke: ${t.lineColor};
  stroke-width: 1;
}

.statediagramTitleText {
  text-anchor: middle;
  font-size: 18px;
  fill: ${t.textColor};
}
`,"getStyles")})}();
//# sourceMappingURL=stateDiagram-S3XGF733.5ce61930.js.map
