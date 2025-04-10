!function(){function t(t,e,a,i){Object.defineProperty(t,e,{get:a,set:i,enumerable:!0,configurable:!0})}var e=("undefined"!=typeof globalThis?globalThis:"undefined"!=typeof self?self:"undefined"!=typeof window?window:"undefined"!=typeof global?global:{}).parcelRequire6955,a=e.register;a("g2oYw",function(a,i){t(a.exports,"diagram",function(){return z});var r=e("8kOu7");e("3tpah");var n=e("kNZMB"),s=e("6t5tb"),o=function(){var t=(0,s.a)(function(t,e,a,i){for(a=a||{},i=t.length;i--;a[t[i]]=e);return a},"o"),e=[6,8,10,11,12,14,16,17,18],a=[1,9],i=[1,10],r=[1,11],n=[1,12],o=[1,13],l=[1,14],c={trace:(0,s.a)(function(){},"trace"),yy:{},symbols_:{error:2,start:3,journey:4,document:5,EOF:6,line:7,SPACE:8,statement:9,NEWLINE:10,title:11,acc_title:12,acc_title_value:13,acc_descr:14,acc_descr_value:15,acc_descr_multiline_value:16,section:17,taskName:18,taskData:19,$accept:0,$end:1},terminals_:{2:"error",4:"journey",6:"EOF",8:"SPACE",10:"NEWLINE",11:"title",12:"acc_title",13:"acc_title_value",14:"acc_descr",15:"acc_descr_value",16:"acc_descr_multiline_value",17:"section",18:"taskName",19:"taskData"},productions_:[0,[3,3],[5,0],[5,2],[7,2],[7,1],[7,1],[7,1],[9,1],[9,2],[9,2],[9,1],[9,1],[9,2]],performAction:(0,s.a)(function(t,e,a,i,r,n,s){var o=n.length-1;switch(r){case 1:return n[o-1];case 2:case 6:case 7:this.$=[];break;case 3:n[o-1].push(n[o]),this.$=n[o-1];break;case 4:case 5:this.$=n[o];break;case 8:i.setDiagramTitle(n[o].substr(6)),this.$=n[o].substr(6);break;case 9:this.$=n[o].trim(),i.setAccTitle(this.$);break;case 10:case 11:this.$=n[o].trim(),i.setAccDescription(this.$);break;case 12:i.addSection(n[o].substr(8)),this.$=n[o].substr(8);break;case 13:i.addTask(n[o-1],n[o]),this.$="task"}},"anonymous"),table:[{3:1,4:[1,2]},{1:[3]},t(e,[2,2],{5:3}),{6:[1,4],7:5,8:[1,6],9:7,10:[1,8],11:a,12:i,14:r,16:n,17:o,18:l},t(e,[2,7],{1:[2,1]}),t(e,[2,3]),{9:15,11:a,12:i,14:r,16:n,17:o,18:l},t(e,[2,5]),t(e,[2,6]),t(e,[2,8]),{13:[1,16]},{15:[1,17]},t(e,[2,11]),t(e,[2,12]),{19:[1,18]},t(e,[2,4]),t(e,[2,9]),t(e,[2,10]),t(e,[2,13])],defaultActions:{},parseError:(0,s.a)(function(t,e){if(e.recoverable)this.trace(t);else{var a=Error(t);throw a.hash=e,a}},"parseError"),parse:(0,s.a)(function(t){var e=this,a=[0],i=[],r=[null],n=[],o=this.table,l="",c=0,h=0,u=0,p=n.slice.call(arguments,1),y=Object.create(this.lexer),d={yy:{}};for(var f in this.yy)Object.prototype.hasOwnProperty.call(this.yy,f)&&(d.yy[f]=this.yy[f]);y.setInput(t,d.yy),d.yy.lexer=y,d.yy.parser=this,typeof y.yylloc>"u"&&(y.yylloc={});var g=y.yylloc;n.push(g);var x=y.options&&y.options.ranges;function m(){var t;return"number"!=typeof(t=i.pop()||y.lex()||1)&&(t instanceof Array&&(t=(i=t).pop()),t=e.symbols_[t]||t),t}"function"==typeof d.yy.parseError?this.parseError=d.yy.parseError:this.parseError=Object.getPrototypeOf(this).parseError,(0,s.a)(function(t){a.length=a.length-2*t,r.length=r.length-t,n.length=n.length-t},"popStack"),(0,s.a)(m,"lex");for(var k,b,_,w,v,$,T,M,S,E={};;){if(_=a[a.length-1],this.defaultActions[_]?w=this.defaultActions[_]:((null===k||typeof k>"u")&&(k=m()),w=o[_]&&o[_][k]),typeof w>"u"||!w.length||!w[0]){var A="";for($ in S=[],o[_])this.terminals_[$]&&$>2&&S.push("'"+this.terminals_[$]+"'");A=y.showPosition?"Parse error on line "+(c+1)+`:
`+y.showPosition()+`
Expecting `+S.join(", ")+", got '"+(this.terminals_[k]||k)+"'":"Parse error on line "+(c+1)+": Unexpected "+(1==k?"end of input":"'"+(this.terminals_[k]||k)+"'"),this.parseError(A,{text:y.match,token:this.terminals_[k]||k,line:y.yylineno,loc:g,expected:S})}if(w[0]instanceof Array&&w.length>1)throw Error("Parse Error: multiple actions possible at state: "+_+", token: "+k);switch(w[0]){case 1:a.push(k),r.push(y.yytext),n.push(y.yylloc),a.push(w[1]),k=null,b?(k=b,b=null):(h=y.yyleng,l=y.yytext,c=y.yylineno,g=y.yylloc,u>0&&u--);break;case 2:if(T=this.productions_[w[1]][1],E.$=r[r.length-T],E._$={first_line:n[n.length-(T||1)].first_line,last_line:n[n.length-1].last_line,first_column:n[n.length-(T||1)].first_column,last_column:n[n.length-1].last_column},x&&(E._$.range=[n[n.length-(T||1)].range[0],n[n.length-1].range[1]]),"u">typeof(v=this.performAction.apply(E,[l,h,c,d.yy,w[1],r,n].concat(p))))return v;T&&(a=a.slice(0,-1*T*2),r=r.slice(0,-1*T),n=n.slice(0,-1*T)),a.push(this.productions_[w[1]][0]),r.push(E.$),n.push(E._$),M=o[a[a.length-2]][a[a.length-1]],a.push(M);break;case 3:return!0}}return!0},"parse")};function h(){this.yy={}}return c.lexer={EOF:1,parseError:(0,s.a)(function(t,e){if(this.yy.parser)this.yy.parser.parseError(t,e);else throw Error(t)},"parseError"),setInput:(0,s.a)(function(t,e){return this.yy=e||this.yy||{},this._input=t,this._more=this._backtrack=this.done=!1,this.yylineno=this.yyleng=0,this.yytext=this.matched=this.match="",this.conditionStack=["INITIAL"],this.yylloc={first_line:1,first_column:0,last_line:1,last_column:0},this.options.ranges&&(this.yylloc.range=[0,0]),this.offset=0,this},"setInput"),input:(0,s.a)(function(){var t=this._input[0];return this.yytext+=t,this.yyleng++,this.offset++,this.match+=t,this.matched+=t,t.match(/(?:\r\n?|\n).*/g)?(this.yylineno++,this.yylloc.last_line++):this.yylloc.last_column++,this.options.ranges&&this.yylloc.range[1]++,this._input=this._input.slice(1),t},"input"),unput:(0,s.a)(function(t){var e=t.length,a=t.split(/(?:\r\n?|\n)/g);this._input=t+this._input,this.yytext=this.yytext.substr(0,this.yytext.length-e),this.offset-=e;var i=this.match.split(/(?:\r\n?|\n)/g);this.match=this.match.substr(0,this.match.length-1),this.matched=this.matched.substr(0,this.matched.length-1),a.length-1&&(this.yylineno-=a.length-1);var r=this.yylloc.range;return this.yylloc={first_line:this.yylloc.first_line,last_line:this.yylineno+1,first_column:this.yylloc.first_column,last_column:a?(a.length===i.length?this.yylloc.first_column:0)+i[i.length-a.length].length-a[0].length:this.yylloc.first_column-e},this.options.ranges&&(this.yylloc.range=[r[0],r[0]+this.yyleng-e]),this.yyleng=this.yytext.length,this},"unput"),more:(0,s.a)(function(){return this._more=!0,this},"more"),reject:(0,s.a)(function(){return this.options.backtrack_lexer?(this._backtrack=!0,this):this.parseError("Lexical error on line "+(this.yylineno+1)+`. You can only invoke reject() in the lexer when the lexer is of the backtracking persuasion (options.backtrack_lexer = true).
`+this.showPosition(),{text:"",token:null,line:this.yylineno})},"reject"),less:(0,s.a)(function(t){this.unput(this.match.slice(t))},"less"),pastInput:(0,s.a)(function(){var t=this.matched.substr(0,this.matched.length-this.match.length);return(t.length>20?"...":"")+t.substr(-20).replace(/\n/g,"")},"pastInput"),upcomingInput:(0,s.a)(function(){var t=this.match;return t.length<20&&(t+=this._input.substr(0,20-t.length)),(t.substr(0,20)+(t.length>20?"...":"")).replace(/\n/g,"")},"upcomingInput"),showPosition:(0,s.a)(function(){var t=this.pastInput(),e=Array(t.length+1).join("-");return t+this.upcomingInput()+`
`+e+"^"},"showPosition"),test_match:(0,s.a)(function(t,e){var a,i,r;if(this.options.backtrack_lexer&&(r={yylineno:this.yylineno,yylloc:{first_line:this.yylloc.first_line,last_line:this.last_line,first_column:this.yylloc.first_column,last_column:this.yylloc.last_column},yytext:this.yytext,match:this.match,matches:this.matches,matched:this.matched,yyleng:this.yyleng,offset:this.offset,_more:this._more,_input:this._input,yy:this.yy,conditionStack:this.conditionStack.slice(0),done:this.done},this.options.ranges&&(r.yylloc.range=this.yylloc.range.slice(0))),(i=t[0].match(/(?:\r\n?|\n).*/g))&&(this.yylineno+=i.length),this.yylloc={first_line:this.yylloc.last_line,last_line:this.yylineno+1,first_column:this.yylloc.last_column,last_column:i?i[i.length-1].length-i[i.length-1].match(/\r?\n?/)[0].length:this.yylloc.last_column+t[0].length},this.yytext+=t[0],this.match+=t[0],this.matches=t,this.yyleng=this.yytext.length,this.options.ranges&&(this.yylloc.range=[this.offset,this.offset+=this.yyleng]),this._more=!1,this._backtrack=!1,this._input=this._input.slice(t[0].length),this.matched+=t[0],a=this.performAction.call(this,this.yy,this,e,this.conditionStack[this.conditionStack.length-1]),this.done&&this._input&&(this.done=!1),a)return a;if(this._backtrack)for(var n in r)this[n]=r[n];return!1},"test_match"),next:(0,s.a)(function(){if(this.done)return this.EOF;this._input||(this.done=!0),this._more||(this.yytext="",this.match="");for(var t,e,a,i,r=this._currentRules(),n=0;n<r.length;n++)if((a=this._input.match(this.rules[r[n]]))&&(!e||a[0].length>e[0].length)){if(e=a,i=n,this.options.backtrack_lexer){if(!1!==(t=this.test_match(a,r[n])))return t;if(!this._backtrack)return!1;e=!1;continue}else if(!this.options.flex)break}return e?!1!==(t=this.test_match(e,r[i]))&&t:""===this._input?this.EOF:this.parseError("Lexical error on line "+(this.yylineno+1)+`. Unrecognized text.
`+this.showPosition(),{text:"",token:null,line:this.yylineno})},"next"),lex:(0,s.a)(function(){return this.next()||this.lex()},"lex"),begin:(0,s.a)(function(t){this.conditionStack.push(t)},"begin"),popState:(0,s.a)(function(){return this.conditionStack.length-1>0?this.conditionStack.pop():this.conditionStack[0]},"popState"),_currentRules:(0,s.a)(function(){return this.conditionStack.length&&this.conditionStack[this.conditionStack.length-1]?this.conditions[this.conditionStack[this.conditionStack.length-1]].rules:this.conditions.INITIAL.rules},"_currentRules"),topState:(0,s.a)(function(t){return(t=this.conditionStack.length-1-Math.abs(t||0))>=0?this.conditionStack[t]:"INITIAL"},"topState"),pushState:(0,s.a)(function(t){this.begin(t)},"pushState"),stateStackSize:(0,s.a)(function(){return this.conditionStack.length},"stateStackSize"),options:{"case-insensitive":!0},performAction:(0,s.a)(function(t,e,a,i){switch(a){case 0:case 1:case 3:case 4:break;case 2:return 10;case 5:return 4;case 6:return 11;case 7:return this.begin("acc_title"),12;case 8:return this.popState(),"acc_title_value";case 9:return this.begin("acc_descr"),14;case 10:return this.popState(),"acc_descr_value";case 11:this.begin("acc_descr_multiline");break;case 12:this.popState();break;case 13:return"acc_descr_multiline_value";case 14:return 17;case 15:return 18;case 16:return 19;case 17:return":";case 18:return 6;case 19:return"INVALID"}},"anonymous"),rules:[/^(?:%(?!\{)[^\n]*)/i,/^(?:[^\}]%%[^\n]*)/i,/^(?:[\n]+)/i,/^(?:\s+)/i,/^(?:#[^\n]*)/i,/^(?:journey\b)/i,/^(?:title\s[^#\n;]+)/i,/^(?:accTitle\s*:\s*)/i,/^(?:(?!\n||)*[^\n]*)/i,/^(?:accDescr\s*:\s*)/i,/^(?:(?!\n||)*[^\n]*)/i,/^(?:accDescr\s*\{\s*)/i,/^(?:[\}])/i,/^(?:[^\}]*)/i,/^(?:section\s[^#:\n;]+)/i,/^(?:[^#:\n;]+)/i,/^(?::[^#\n;]+)/i,/^(?::)/i,/^(?:$)/i,/^(?:.)/i],conditions:{acc_descr_multiline:{rules:[12,13],inclusive:!1},acc_descr:{rules:[10],inclusive:!1},acc_title:{rules:[8],inclusive:!1},INITIAL:{rules:[0,1,2,3,4,5,6,7,9,11,14,15,16,17,18,19],inclusive:!0}}},(0,s.a)(h,"Parser"),h.prototype=c,c.Parser=h,new h}();o.parser=o;var l="",c=[],h=[],u=[],p=(0,s.a)(function(){c.length=0,h.length=0,l="",u.length=0,(0,n.P)()},"clear"),y=(0,s.a)(function(t){l=t,c.push(t)},"addSection"),d=(0,s.a)(function(){return c},"getSections"),f=(0,s.a)(function(){let t=k(),e=0;for(;!t&&e<100;)t=k(),e++;return h.push(...u),h},"getTasks"),g=(0,s.a)(function(){let t=[];return h.forEach(e=>{e.people&&t.push(...e.people)}),[...new Set(t)].sort()},"updateActors"),x=(0,s.a)(function(t,e){let a=e.substr(1).split(":"),i=0,r=[];1===a.length?(i=Number(a[0]),r=[]):(i=Number(a[0]),r=a[1].split(","));let n=r.map(t=>t.trim()),s={section:l,type:l,people:n,task:t,score:i};u.push(s)},"addTask"),m=(0,s.a)(function(t){let e={section:l,type:l,description:t,task:t,classes:[]};h.push(e)},"addTaskOrg"),k=(0,s.a)(function(){let t=(0,s.a)(function(t){return u[t].processed},"compileTask"),e=!0;for(let[a,i]of u.entries())t(a),e=e&&i.processed;return e},"compileTasks"),b=(0,s.a)(function(){return g()},"getActors"),_={getConfig:(0,s.a)(()=>(0,n.X)().journey,"getConfig"),clear:p,setDiagramTitle:n.U,getDiagramTitle:n.V,setAccTitle:n.Q,getAccTitle:n.R,setAccDescription:n.S,getAccDescription:n.T,addSection:y,getSections:d,getTasks:f,addTask:x,addTaskOrg:m,getActors:b},w=(0,s.a)(t=>`.label {
    font-family: 'trebuchet ms', verdana, arial, sans-serif;
    font-family: var(--mermaid-font-family);
    color: ${t.textColor};
  }
  .mouth {
    stroke: #666;
  }

  line {
    stroke: ${t.textColor}
  }

  .legend {
    fill: ${t.textColor};
  }

  .label text {
    fill: #333;
  }
  .label {
    color: ${t.textColor}
  }

  .face {
    ${t.faceColor?`fill: ${t.faceColor}`:"fill: #FFF8DC"};
    stroke: #999;
  }

  .node rect,
  .node circle,
  .node ellipse,
  .node polygon,
  .node path {
    fill: ${t.mainBkg};
    stroke: ${t.nodeBorder};
    stroke-width: 1px;
  }

  .node .label {
    text-align: center;
  }
  .node.clickable {
    cursor: pointer;
  }

  .arrowheadPath {
    fill: ${t.arrowheadColor};
  }

  .edgePath .path {
    stroke: ${t.lineColor};
    stroke-width: 1.5px;
  }

  .flowchart-link {
    stroke: ${t.lineColor};
    fill: none;
  }

  .edgeLabel {
    background-color: ${t.edgeLabelBackground};
    rect {
      opacity: 0.5;
    }
    text-align: center;
  }

  .cluster rect {
  }

  .cluster text {
    fill: ${t.titleColor};
  }

  div.mermaidTooltip {
    position: absolute;
    text-align: center;
    max-width: 200px;
    padding: 2px;
    font-family: 'trebuchet ms', verdana, arial, sans-serif;
    font-family: var(--mermaid-font-family);
    font-size: 12px;
    background: ${t.tertiaryColor};
    border: 1px solid ${t.border2};
    border-radius: 2px;
    pointer-events: none;
    z-index: 100;
  }

  .task-type-0, .section-type-0  {
    ${t.fillType0?`fill: ${t.fillType0}`:""};
  }
  .task-type-1, .section-type-1  {
    ${t.fillType0?`fill: ${t.fillType1}`:""};
  }
  .task-type-2, .section-type-2  {
    ${t.fillType0?`fill: ${t.fillType2}`:""};
  }
  .task-type-3, .section-type-3  {
    ${t.fillType0?`fill: ${t.fillType3}`:""};
  }
  .task-type-4, .section-type-4  {
    ${t.fillType0?`fill: ${t.fillType4}`:""};
  }
  .task-type-5, .section-type-5  {
    ${t.fillType0?`fill: ${t.fillType5}`:""};
  }
  .task-type-6, .section-type-6  {
    ${t.fillType0?`fill: ${t.fillType6}`:""};
  }
  .task-type-7, .section-type-7  {
    ${t.fillType0?`fill: ${t.fillType7}`:""};
  }

  .actor-0 {
    ${t.actor0?`fill: ${t.actor0}`:""};
  }
  .actor-1 {
    ${t.actor1?`fill: ${t.actor1}`:""};
  }
  .actor-2 {
    ${t.actor2?`fill: ${t.actor2}`:""};
  }
  .actor-3 {
    ${t.actor3?`fill: ${t.actor3}`:""};
  }
  .actor-4 {
    ${t.actor4?`fill: ${t.actor4}`:""};
  }
  .actor-5 {
    ${t.actor5?`fill: ${t.actor5}`:""};
  }
`,"getStyles"),v=(0,s.a)(function(t,e){return(0,r.a)(t,e)},"drawRect"),$=(0,s.a)(function(t,e){let a=t.append("circle").attr("cx",e.cx).attr("cy",e.cy).attr("class","face").attr("r",15).attr("stroke-width",2).attr("overflow","visible"),i=t.append("g");function r(t){let a=(0,n.Aa)().startAngle(Math.PI/2).endAngle(Math.PI/2*3).innerRadius(7.5).outerRadius(6.8181818181818175);t.append("path").attr("class","mouth").attr("d",a).attr("transform","translate("+e.cx+","+(e.cy+2)+")")}function o(t){let a=(0,n.Aa)().startAngle(3*Math.PI/2).endAngle(Math.PI/2*5).innerRadius(7.5).outerRadius(6.8181818181818175);t.append("path").attr("class","mouth").attr("d",a).attr("transform","translate("+e.cx+","+(e.cy+7)+")")}function l(t){t.append("line").attr("class","mouth").attr("stroke",2).attr("x1",e.cx-5).attr("y1",e.cy+7).attr("x2",e.cx+5).attr("y2",e.cy+7).attr("class","mouth").attr("stroke-width","1px").attr("stroke","#666")}return i.append("circle").attr("cx",e.cx-5).attr("cy",e.cy-5).attr("r",1.5).attr("stroke-width",2).attr("fill","#666").attr("stroke","#666"),i.append("circle").attr("cx",e.cx+5).attr("cy",e.cy-5).attr("r",1.5).attr("stroke-width",2).attr("fill","#666").attr("stroke","#666"),(0,s.a)(r,"smile"),(0,s.a)(o,"sad"),(0,s.a)(l,"ambivalent"),e.score>3?r(i):e.score<3?o(i):l(i),a},"drawFace"),T=(0,s.a)(function(t,e){let a=t.append("circle");return a.attr("cx",e.cx),a.attr("cy",e.cy),a.attr("class","actor-"+e.pos),a.attr("fill",e.fill),a.attr("stroke",e.stroke),a.attr("r",e.r),void 0!==a.class&&a.attr("class",a.class),void 0!==e.title&&a.append("title").text(e.title),a},"drawCircle"),M=(0,s.a)(function(t,e){return(0,r.c)(t,e)},"drawText"),S=((0,s.a)(function(t,e){function a(t,e,a,i,r){return t+","+e+" "+(t+a)+","+e+" "+(t+a)+","+(e+i-r)+" "+(t+a-1.2*r)+","+(e+i)+" "+t+","+(e+i)}(0,s.a)(a,"genPoints");let i=t.append("polygon");i.attr("points",a(e.x,e.y,50,20,7)),i.attr("class","labelBox"),e.y=e.y+e.labelMargin,e.x=e.x+.5*e.labelMargin,M(t,e)},"drawLabel"),(0,s.a)(function(t,e,a){let i=t.append("g"),n=(0,r.f)();n.x=e.x,n.y=e.y,n.fill=e.fill,n.width=a.width*e.taskCount+a.diagramMarginX*(e.taskCount-1),n.height=a.height,n.class="journey-section section-type-"+e.num,n.rx=3,n.ry=3,v(i,n),I(a)(e.text,i,n.x,n.y,n.width,n.height,{class:"journey-section section-type-"+e.num},a,e.colour)},"drawSection")),E=-1,A=(0,s.a)(function(t,e,a){let i=e.x+a.width/2,n=t.append("g");E++,n.append("line").attr("id","task"+E).attr("x1",i).attr("y1",e.y).attr("x2",i).attr("y2",450).attr("class","task-line").attr("stroke-width","1px").attr("stroke-dasharray","4 2").attr("stroke","#666"),$(n,{cx:i,cy:300+(5-e.score)*30,score:e.score});let s=(0,r.f)();s.x=e.x,s.y=e.y,s.fill=e.fill,s.width=a.width,s.height=a.height,s.class="task task-type-"+e.num,s.rx=3,s.ry=3,v(n,s);let o=e.x+14;e.people.forEach(t=>{let a=e.actors[t].color;T(n,{cx:o,cy:e.y,r:7,fill:a,stroke:"#000",title:t,pos:e.actors[t].position}),o+=10}),I(a)(e.task,n,s.x,s.y,s.width,s.height,{class:"task"},a,e.colour)},"drawTask"),I=((0,s.a)(function(t,e){(0,r.b)(t,e)},"drawBackgroundRect"),function(){function t(t,e,a,r,n,s,o,l){i(e.append("text").attr("x",a+n/2).attr("y",r+s/2+5).style("font-color",l).style("text-anchor","middle").text(t),o)}function e(t,e,a,r,n,s,o,l,c){let{taskFontSize:h,taskFontFamily:u}=l,p=t.split(/<br\s*\/?>/gi);for(let t=0;t<p.length;t++){let l=t*h-h*(p.length-1)/2,y=e.append("text").attr("x",a+n/2).attr("y",r).attr("fill",c).style("text-anchor","middle").style("font-size",h).style("font-family",u);y.append("tspan").attr("x",a+n/2).attr("dy",l).text(p[t]),y.attr("y",r+s/2).attr("dominant-baseline","central").attr("alignment-baseline","central"),i(y,o)}}function a(t,a,r,n,s,o,l,c){let h=a.append("switch"),u=h.append("foreignObject").attr("x",r).attr("y",n).attr("width",s).attr("height",o).attr("position","fixed").append("xhtml:div").style("display","table").style("height","100%").style("width","100%");u.append("div").attr("class","label").style("display","table-cell").style("text-align","center").style("vertical-align","middle").text(t),e(t,h,r,n,s,o,l,c),i(u,l)}function i(t,e){for(let a in e)a in e&&t.attr(a,e[a])}return(0,s.a)(t,"byText"),(0,s.a)(e,"byTspan"),(0,s.a)(a,"byFo"),(0,s.a)(i,"_setTextAttrs"),function(i){return"fo"===i.textPlacement?a:"old"===i.textPlacement?t:e}}()),P={drawCircle:T,drawSection:S,drawText:M,drawTask:A,initGraphics:(0,s.a)(function(t){t.append("defs").append("marker").attr("id","arrowhead").attr("refX",5).attr("refY",2).attr("markerWidth",6).attr("markerHeight",4).attr("orient","auto").append("path").attr("d","M 0,0 V 4 L6,2 Z")},"initGraphics")},C=(0,s.a)(function(t){Object.keys(t).forEach(function(e){O[e]=t[e]})},"setConf"),j={};function V(t){let e=(0,n.X)().journey,a=60;Object.keys(j).forEach(i=>{let r=j[i].color,n={cx:20,cy:a,r:7,fill:r,stroke:"#000",pos:j[i].position};P.drawCircle(t,n);let s={x:40,y:a+7,fill:"#666",text:i,textMargin:5|e.boxTextMargin};P.drawText(t,s),a+=20})}(0,s.a)(V,"drawActorLegend");var O=(0,n.X)().journey,B=O.leftMargin,F=(0,s.a)(function(t,e,a,i){let r=(0,n.X)().journey,s=(0,n.X)().securityLevel,o;"sandbox"===s&&(o=(0,n.fa)("#i"+e));let l="sandbox"===s?(0,n.fa)(o.nodes()[0].contentDocument.body):(0,n.fa)("body");N.init();let c=l.select("#"+e);P.initGraphics(c);let h=i.db.getTasks(),u=i.db.getDiagramTitle(),p=i.db.getActors();for(let t in j)delete j[t];let y=0;p.forEach(t=>{j[t]={color:r.actorColours[y%r.actorColours.length],position:y},y++}),V(c),N.insert(0,0,B,50*Object.keys(j).length),R(c,h,0);let d=N.getBounds();u&&c.append("text").text(u).attr("x",B).attr("font-size","4ex").attr("font-weight","bold").attr("y",25);let f=d.stopy-d.starty+2*r.diagramMarginY,g=B+d.stopx+2*r.diagramMarginX;(0,n.M)(c,f,g,r.useMaxWidth),c.append("line").attr("x1",B).attr("y1",4*r.height).attr("x2",g-B-4).attr("y2",4*r.height).attr("stroke-width",4).attr("stroke","black").attr("marker-end","url(#arrowhead)");let x=70*!!u;c.attr("viewBox",`${d.startx} -25 ${g} ${f+x}`),c.attr("preserveAspectRatio","xMinYMin meet"),c.attr("height",f+x+25)},"draw"),N={data:{startx:void 0,stopx:void 0,starty:void 0,stopy:void 0},verticalPos:0,sequenceItems:[],init:(0,s.a)(function(){this.sequenceItems=[],this.data={startx:void 0,stopx:void 0,starty:void 0,stopy:void 0},this.verticalPos=0},"init"),updateVal:(0,s.a)(function(t,e,a,i){void 0===t[e]?t[e]=a:t[e]=i(a,t[e])},"updateVal"),updateBounds:(0,s.a)(function(t,e,a,i){let r=(0,n.X)().journey,o=this,l=0;function c(n){return(0,s.a)(function(s){l++;let c=o.sequenceItems.length-l+1;o.updateVal(s,"starty",e-c*r.boxMargin,Math.min),o.updateVal(s,"stopy",i+c*r.boxMargin,Math.max),o.updateVal(N.data,"startx",t-c*r.boxMargin,Math.min),o.updateVal(N.data,"stopx",a+c*r.boxMargin,Math.max),"activation"!==n&&(o.updateVal(s,"startx",t-c*r.boxMargin,Math.min),o.updateVal(s,"stopx",a+c*r.boxMargin,Math.max),o.updateVal(N.data,"starty",e-c*r.boxMargin,Math.min),o.updateVal(N.data,"stopy",i+c*r.boxMargin,Math.max))},"updateItemBounds")}(0,s.a)(c,"updateFn"),this.sequenceItems.forEach(c())},"updateBounds"),insert:(0,s.a)(function(t,e,a,i){let r=Math.min(t,a),n=Math.max(t,a),s=Math.min(e,i),o=Math.max(e,i);this.updateVal(N.data,"startx",r,Math.min),this.updateVal(N.data,"starty",s,Math.min),this.updateVal(N.data,"stopx",n,Math.max),this.updateVal(N.data,"stopy",o,Math.max),this.updateBounds(r,s,n,o)},"insert"),bumpVerticalPos:(0,s.a)(function(t){this.verticalPos=this.verticalPos+t,this.data.stopy=this.verticalPos},"bumpVerticalPos"),getVerticalPos:(0,s.a)(function(){return this.verticalPos},"getVerticalPos"),getBounds:(0,s.a)(function(){return this.data},"getBounds")},D=O.sectionFills,L=O.sectionColours,R=(0,s.a)(function(t,e,a){let i=(0,n.X)().journey,r="",s=a+(2*i.height+i.diagramMarginY),o=0,l="#CCC",c="black",h=0;for(let[a,n]of e.entries()){if(r!==n.section){l=D[o%D.length],h=o%D.length,c=L[o%L.length];let s=0,u=n.section;for(let t=a;t<e.length&&e[t].section==u;t++)s+=1;let p={x:a*i.taskMargin+a*i.width+B,y:50,text:n.section,fill:l,num:h,colour:c,taskCount:s};P.drawSection(t,p,i),r=n.section,o++}let u=n.people.reduce((t,e)=>(j[e]&&(t[e]=j[e]),t),{});n.x=a*i.taskMargin+a*i.width+B,n.y=s,n.width=i.diagramMarginX,n.height=i.diagramMarginY,n.colour=c,n.fill=l,n.num=h,n.actors=u,P.drawTask(t,n,i),N.insert(n.x,n.y,n.x+n.width+i.taskMargin,450)}},"drawTasks"),X={setConf:C,draw:F},z={parser:o,db:_,renderer:X,styles:w,init:(0,s.a)(t=>{X.setConf(t.journey),_.clear()},"init")}}),a("8kOu7",function(a,i){t(a.exports,"a",function(){return l}),t(a.exports,"b",function(){return c}),t(a.exports,"c",function(){return h}),t(a.exports,"d",function(){return u}),t(a.exports,"e",function(){return p}),t(a.exports,"f",function(){return y}),t(a.exports,"g",function(){return d});var r=e("3tpah"),n=e("kNZMB"),s=e("6t5tb"),o=(0,s.e)((0,r.a)(),1),l=(0,s.a)((t,e)=>{let a=t.append("rect");if(a.attr("x",e.x),a.attr("y",e.y),a.attr("fill",e.fill),a.attr("stroke",e.stroke),a.attr("width",e.width),a.attr("height",e.height),e.name&&a.attr("name",e.name),e.rx&&a.attr("rx",e.rx),e.ry&&a.attr("ry",e.ry),void 0!==e.attrs)for(let t in e.attrs)a.attr(t,e.attrs[t]);return e.class&&a.attr("class",e.class),a},"drawRect"),c=(0,s.a)((t,e)=>{l(t,{x:e.startx,y:e.starty,width:e.stopx-e.startx,height:e.stopy-e.starty,fill:e.fill,stroke:e.stroke,class:"rect"}).lower()},"drawBackgroundRect"),h=(0,s.a)((t,e)=>{let a=e.text.replace(n.E," "),i=t.append("text");i.attr("x",e.x),i.attr("y",e.y),i.attr("class","legend"),i.style("text-anchor",e.anchor),e.class&&i.attr("class",e.class);let r=i.append("tspan");return r.attr("x",e.x+2*e.textMargin),r.text(a),i},"drawText"),u=(0,s.a)((t,e,a,i)=>{let r=t.append("image");r.attr("x",e),r.attr("y",a);let n=(0,o.sanitizeUrl)(i);r.attr("xlink:href",n)},"drawImage"),p=(0,s.a)((t,e,a,i)=>{let r=t.append("use");r.attr("x",e),r.attr("y",a);let n=(0,o.sanitizeUrl)(i);r.attr("xlink:href",`#${n}`)},"drawEmbeddedImage"),y=(0,s.a)(()=>({x:0,y:0,width:100,height:100,fill:"#EDF2AE",stroke:"#666",anchor:"start",rx:0,ry:0}),"getNoteRect"),d=(0,s.a)(()=>({x:0,y:0,width:100,height:100,"text-anchor":"start",style:"#666",textMargin:0,rx:0,ry:0,tspan:!0}),"getTextObj")})}();
//# sourceMappingURL=journeyDiagram-VRXW2F6L.d756c986.js.map
