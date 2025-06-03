!function(){var t=("undefined"!=typeof globalThis?globalThis:"undefined"!=typeof self?self:"undefined"!=typeof window?window:"undefined"!=typeof global?global:{}).parcelRequire6955;(0,t.register)("82c6m",function(e,i){Object.defineProperty(e.exports,"diagram",{get:function(){return tP},set:void 0,enumerable:!0,configurable:!0});var a=t("fxedn"),s=t("3tpah"),n=t("kNZMB");t("2Bif5");var r=t("6t5tb"),o=(0,r.b)((t,e)=>{"use strict";var i,a;i=t,a=function(){return function(t,e,i){var a=(0,r.a)(function(t){return t.add(4-t.isoWeekday(),"day")},"a"),s=e.prototype;s.isoWeekYear=function(){return a(this).year()},s.isoWeek=function(t){if(!this.$utils().u(t))return this.add(7*(t-this.isoWeek()),"day");var e,s,n,r=a(this),o=(e=this.isoWeekYear(),n=4-(s=(this.$u?i.utc:i)().year(e).startOf("year")).isoWeekday(),s.isoWeekday()>4&&(n+=7),s.add(n,"day"));return r.diff(o,"week")+1},s.isoWeekday=function(t){return this.$utils().u(t)?this.day()||7:this.day(this.day()%7?t:t-7)};var n=s.startOf;s.startOf=function(t,e){var i=this.$utils(),a=!!i.u(e)||e;return"isoweek"===i.p(t)?a?this.date(this.date()-(this.isoWeekday()-1)).startOf("day"):this.date(this.date()-1-(this.isoWeekday()-1)+7).endOf("day"):n.bind(this)(t,e)}}},"object"==typeof t&&"u">typeof e?e.exports=a():"function"==typeof define&&define.amd?define(a):(i="u">typeof globalThis?globalThis:i||self).dayjs_plugin_isoWeek=a()}),l=(0,r.b)((t,e)=>{"use strict";var i,a;i=t,a=function(){var t={LTS:"h:mm:ss A",LT:"h:mm A",L:"MM/DD/YYYY",LL:"MMMM D, YYYY",LLL:"MMMM D, YYYY h:mm A",LLLL:"dddd, MMMM D, YYYY h:mm A"},e=/(\[[^[]*\])|([-_:/.,()\s]+)|(A|a|YYYY|YY?|MM?M?M?|Do|DD?|hh?|HH?|mm?|ss?|S{1,3}|z|ZZ?)/g,i=/\d\d/,a=/\d\d?/,s=/\d*[^-_:/,()\s\d]+/,n={},o=(0,r.a)(function(t){return(t*=1)+(t>68?1900:2e3)},"s"),l=(0,r.a)(function(t){return function(e){this[t]=+e}},"a"),c=[/[+-]\d\d:?(\d\d)?|Z/,function(t){(this.zone||(this.zone={})).offset=function(t){if(!t||"Z"===t)return 0;var e=t.match(/([+-]|\d\d)/g),i=60*e[1]+(+e[2]||0);return 0===i?0:"+"===e[0]?-i:i}(t)}],d=(0,r.a)(function(t){var e=n[t];return e&&(e.indexOf?e:e.s.concat(e.f))},"h"),u=(0,r.a)(function(t,e){var i,a=n.meridiem;if(a){for(var s=1;s<=24;s+=1)if(t.indexOf(a(s,0,e))>-1){i=s>12;break}}else i=t===(e?"pm":"PM");return i},"u"),h={A:[s,function(t){this.afternoon=u(t,!1)}],a:[s,function(t){this.afternoon=u(t,!0)}],S:[/\d/,function(t){this.milliseconds=100*t}],SS:[i,function(t){this.milliseconds=10*t}],SSS:[/\d{3}/,function(t){this.milliseconds=+t}],s:[a,l("seconds")],ss:[a,l("seconds")],m:[a,l("minutes")],mm:[a,l("minutes")],H:[a,l("hours")],h:[a,l("hours")],HH:[a,l("hours")],hh:[a,l("hours")],D:[a,l("day")],DD:[i,l("day")],Do:[s,function(t){var e=n.ordinal,i=t.match(/\d+/);if(this.day=i[0],e)for(var a=1;a<=31;a+=1)e(a).replace(/\[|\]/g,"")===t&&(this.day=a)}],M:[a,l("month")],MM:[i,l("month")],MMM:[s,function(t){var e=d("months"),i=(d("monthsShort")||e.map(function(t){return t.slice(0,3)})).indexOf(t)+1;if(i<1)throw Error();this.month=i%12||i}],MMMM:[s,function(t){var e=d("months").indexOf(t)+1;if(e<1)throw Error();this.month=e%12||e}],Y:[/[+-]?\d+/,l("year")],YY:[i,function(t){this.year=o(t)}],YYYY:[/\d{4}/,l("year")],Z:c,ZZ:c};function f(i){var a,s;a=i,s=n&&n.formats;for(var r=(i=a.replace(/(\[[^\]]+])|(LTS?|l{1,4}|L{1,4})/g,function(e,i,a){var n=a&&a.toUpperCase();return i||s[a]||t[a]||s[n].replace(/(\[[^\]]+])|(MMMM|MM|DD|dddd)/g,function(t,e,i){return e||i.slice(1)})})).match(e),o=r.length,l=0;l<o;l+=1){var c=r[l],d=h[c],u=d&&d[0],f=d&&d[1];r[l]=f?{regex:u,parser:f}:c.replace(/^\[|\]$/g,"")}return function(t){for(var e={},i=0,a=0;i<o;i+=1){var s=r[i];if("string"==typeof s)a+=s.length;else{var n=s.regex,l=s.parser,c=t.slice(a),d=n.exec(c)[0];l.call(e,d),t=t.replace(d,"")}}return function(t){var e=t.afternoon;if(void 0!==e){var i=t.hours;e?i<12&&(t.hours+=12):12===i&&(t.hours=0),delete t.afternoon}}(e),e}}return(0,r.a)(f,"c"),function(t,e,i){i.p.customParseFormat=!0,t&&t.parseTwoDigitYear&&(o=t.parseTwoDigitYear);var a=e.prototype,s=a.parse;a.parse=function(t){var e=t.date,a=t.utc,r=t.args;this.$u=a;var o=r[1];if("string"==typeof o){var l=!0===r[2],c=!0===r[3],d=r[2];c&&(d=r[2]),n=this.$locale(),!l&&d&&(n=i.Ls[d]),this.$d=function(t,e,i){try{if(["x","X"].indexOf(e)>-1)return new Date(("X"===e?1e3:1)*t);var a=f(e)(t),s=a.year,n=a.month,r=a.day,o=a.hours,l=a.minutes,c=a.seconds,d=a.milliseconds,u=a.zone,h=new Date,y=r||(s||n?1:h.getDate()),m=s||h.getFullYear(),k=0;s&&!n||(k=n>0?n-1:h.getMonth());var p=o||0,g=l||0,b=c||0,T=d||0;return u?new Date(Date.UTC(m,k,y,p,g,b,T+60*u.offset*1e3)):i?new Date(Date.UTC(m,k,y,p,g,b,T)):new Date(m,k,y,p,g,b,T)}catch{return new Date("")}}(e,o,a),this.init(),d&&!0!==d&&(this.$L=this.locale(d).$L),(l||c)&&e!=this.format(o)&&(this.$d=new Date("")),n={}}else if(o instanceof Array)for(var u=o.length,h=1;h<=u;h+=1){r[1]=o[h-1];var y=i.apply(this,r);if(y.isValid()){this.$d=y.$d,this.$L=y.$L,this.init();break}h===u&&(this.$d=new Date(""))}else s.call(this,t)}}},"object"==typeof t&&"u">typeof e?e.exports=a():"function"==typeof define&&define.amd?define(a):(i="u">typeof globalThis?globalThis:i||self).dayjs_plugin_customParseFormat=a()}),c=(0,r.b)((t,e)=>{"use strict";var i,a;i=t,a=function(){return function(t,e){var i=e.prototype,a=i.format;i.format=function(t){var e=this,i=this.$locale();if(!this.isValid())return a.bind(this)(t);var s=this.$utils(),n=(t||"YYYY-MM-DDTHH:mm:ssZ").replace(/\[([^\]]+)]|Q|wo|ww|w|WW|W|zzz|z|gggg|GGGG|Do|X|x|k{1,2}|S/g,function(t){switch(t){case"Q":return Math.ceil((e.$M+1)/3);case"Do":return i.ordinal(e.$D);case"gggg":return e.weekYear();case"GGGG":return e.isoWeekYear();case"wo":return i.ordinal(e.week(),"W");case"w":case"ww":return s.s(e.week(),"w"===t?1:2,"0");case"W":case"WW":return s.s(e.isoWeek(),"W"===t?1:2,"0");case"k":case"kk":return s.s(String(0===e.$H?24:e.$H),"k"===t?1:2,"0");case"X":return Math.floor(e.$d.getTime()/1e3);case"x":return e.$d.getTime();case"z":return"["+e.offsetName()+"]";case"zzz":return"["+e.offsetName("long")+"]";default:return t}});return a.bind(this)(n)}}},"object"==typeof t&&"u">typeof e?e.exports=a():"function"==typeof define&&define.amd?define(a):(i="u">typeof globalThis?globalThis:i||self).dayjs_plugin_advancedFormat=a()}),d=function(){var t=(0,r.a)(function(t,e,i,a){for(i=i||{},a=t.length;a--;i[t[a]]=e);return i},"o"),e=[6,8,10,12,13,14,15,16,17,18,20,21,22,23,24,25,26,27,28,29,30,31,33,35,36,38,40],i=[1,26],a=[1,27],s=[1,28],n=[1,29],o=[1,30],l=[1,31],c=[1,32],d=[1,33],u=[1,34],h=[1,9],f=[1,10],y=[1,11],m=[1,12],k=[1,13],p=[1,14],g=[1,15],b=[1,16],T=[1,19],x=[1,20],v=[1,21],w=[1,22],_=[1,23],D=[1,25],$=[1,35],S={trace:(0,r.a)(function(){},"trace"),yy:{},symbols_:{error:2,start:3,gantt:4,document:5,EOF:6,line:7,SPACE:8,statement:9,NL:10,weekday:11,weekday_monday:12,weekday_tuesday:13,weekday_wednesday:14,weekday_thursday:15,weekday_friday:16,weekday_saturday:17,weekday_sunday:18,weekend:19,weekend_friday:20,weekend_saturday:21,dateFormat:22,inclusiveEndDates:23,topAxis:24,axisFormat:25,tickInterval:26,excludes:27,includes:28,todayMarker:29,title:30,acc_title:31,acc_title_value:32,acc_descr:33,acc_descr_value:34,acc_descr_multiline_value:35,section:36,clickStatement:37,taskTxt:38,taskData:39,click:40,callbackname:41,callbackargs:42,href:43,clickStatementDebug:44,$accept:0,$end:1},terminals_:{2:"error",4:"gantt",6:"EOF",8:"SPACE",10:"NL",12:"weekday_monday",13:"weekday_tuesday",14:"weekday_wednesday",15:"weekday_thursday",16:"weekday_friday",17:"weekday_saturday",18:"weekday_sunday",20:"weekend_friday",21:"weekend_saturday",22:"dateFormat",23:"inclusiveEndDates",24:"topAxis",25:"axisFormat",26:"tickInterval",27:"excludes",28:"includes",29:"todayMarker",30:"title",31:"acc_title",32:"acc_title_value",33:"acc_descr",34:"acc_descr_value",35:"acc_descr_multiline_value",36:"section",38:"taskTxt",39:"taskData",40:"click",41:"callbackname",42:"callbackargs",43:"href"},productions_:[0,[3,3],[5,0],[5,2],[7,2],[7,1],[7,1],[7,1],[11,1],[11,1],[11,1],[11,1],[11,1],[11,1],[11,1],[19,1],[19,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,1],[9,2],[9,2],[9,1],[9,1],[9,1],[9,2],[37,2],[37,3],[37,3],[37,4],[37,3],[37,4],[37,2],[44,2],[44,3],[44,3],[44,4],[44,3],[44,4],[44,2]],performAction:(0,r.a)(function(t,e,i,a,s,n,r){var o=n.length-1;switch(s){case 1:return n[o-1];case 2:case 6:case 7:this.$=[];break;case 3:n[o-1].push(n[o]),this.$=n[o-1];break;case 4:case 5:this.$=n[o];break;case 8:a.setWeekday("monday");break;case 9:a.setWeekday("tuesday");break;case 10:a.setWeekday("wednesday");break;case 11:a.setWeekday("thursday");break;case 12:a.setWeekday("friday");break;case 13:a.setWeekday("saturday");break;case 14:a.setWeekday("sunday");break;case 15:a.setWeekend("friday");break;case 16:a.setWeekend("saturday");break;case 17:a.setDateFormat(n[o].substr(11)),this.$=n[o].substr(11);break;case 18:a.enableInclusiveEndDates(),this.$=n[o].substr(18);break;case 19:a.TopAxis(),this.$=n[o].substr(8);break;case 20:a.setAxisFormat(n[o].substr(11)),this.$=n[o].substr(11);break;case 21:a.setTickInterval(n[o].substr(13)),this.$=n[o].substr(13);break;case 22:a.setExcludes(n[o].substr(9)),this.$=n[o].substr(9);break;case 23:a.setIncludes(n[o].substr(9)),this.$=n[o].substr(9);break;case 24:a.setTodayMarker(n[o].substr(12)),this.$=n[o].substr(12);break;case 27:a.setDiagramTitle(n[o].substr(6)),this.$=n[o].substr(6);break;case 28:this.$=n[o].trim(),a.setAccTitle(this.$);break;case 29:case 30:this.$=n[o].trim(),a.setAccDescription(this.$);break;case 31:a.addSection(n[o].substr(8)),this.$=n[o].substr(8);break;case 33:a.addTask(n[o-1],n[o]),this.$="task";break;case 34:this.$=n[o-1],a.setClickEvent(n[o-1],n[o],null);break;case 35:this.$=n[o-2],a.setClickEvent(n[o-2],n[o-1],n[o]);break;case 36:this.$=n[o-2],a.setClickEvent(n[o-2],n[o-1],null),a.setLink(n[o-2],n[o]);break;case 37:this.$=n[o-3],a.setClickEvent(n[o-3],n[o-2],n[o-1]),a.setLink(n[o-3],n[o]);break;case 38:this.$=n[o-2],a.setClickEvent(n[o-2],n[o],null),a.setLink(n[o-2],n[o-1]);break;case 39:this.$=n[o-3],a.setClickEvent(n[o-3],n[o-1],n[o]),a.setLink(n[o-3],n[o-2]);break;case 40:this.$=n[o-1],a.setLink(n[o-1],n[o]);break;case 41:case 47:this.$=n[o-1]+" "+n[o];break;case 42:case 43:case 45:this.$=n[o-2]+" "+n[o-1]+" "+n[o];break;case 44:case 46:this.$=n[o-3]+" "+n[o-2]+" "+n[o-1]+" "+n[o]}},"anonymous"),table:[{3:1,4:[1,2]},{1:[3]},t(e,[2,2],{5:3}),{6:[1,4],7:5,8:[1,6],9:7,10:[1,8],11:17,12:i,13:a,14:s,15:n,16:o,17:l,18:c,19:18,20:d,21:u,22:h,23:f,24:y,25:m,26:k,27:p,28:g,29:b,30:T,31:x,33:v,35:w,36:_,37:24,38:D,40:$},t(e,[2,7],{1:[2,1]}),t(e,[2,3]),{9:36,11:17,12:i,13:a,14:s,15:n,16:o,17:l,18:c,19:18,20:d,21:u,22:h,23:f,24:y,25:m,26:k,27:p,28:g,29:b,30:T,31:x,33:v,35:w,36:_,37:24,38:D,40:$},t(e,[2,5]),t(e,[2,6]),t(e,[2,17]),t(e,[2,18]),t(e,[2,19]),t(e,[2,20]),t(e,[2,21]),t(e,[2,22]),t(e,[2,23]),t(e,[2,24]),t(e,[2,25]),t(e,[2,26]),t(e,[2,27]),{32:[1,37]},{34:[1,38]},t(e,[2,30]),t(e,[2,31]),t(e,[2,32]),{39:[1,39]},t(e,[2,8]),t(e,[2,9]),t(e,[2,10]),t(e,[2,11]),t(e,[2,12]),t(e,[2,13]),t(e,[2,14]),t(e,[2,15]),t(e,[2,16]),{41:[1,40],43:[1,41]},t(e,[2,4]),t(e,[2,28]),t(e,[2,29]),t(e,[2,33]),t(e,[2,34],{42:[1,42],43:[1,43]}),t(e,[2,40],{41:[1,44]}),t(e,[2,35],{43:[1,45]}),t(e,[2,36]),t(e,[2,38],{42:[1,46]}),t(e,[2,37]),t(e,[2,39])],defaultActions:{},parseError:(0,r.a)(function(t,e){if(e.recoverable)this.trace(t);else{var i=Error(t);throw i.hash=e,i}},"parseError"),parse:(0,r.a)(function(t){var e=this,i=[0],a=[],s=[null],n=[],o=this.table,l="",c=0,d=0,u=0,h=n.slice.call(arguments,1),f=Object.create(this.lexer),y={yy:{}};for(var m in this.yy)Object.prototype.hasOwnProperty.call(this.yy,m)&&(y.yy[m]=this.yy[m]);f.setInput(t,y.yy),y.yy.lexer=f,y.yy.parser=this,typeof f.yylloc>"u"&&(f.yylloc={});var k=f.yylloc;n.push(k);var p=f.options&&f.options.ranges;function g(){var t;return"number"!=typeof(t=a.pop()||f.lex()||1)&&(t instanceof Array&&(t=(a=t).pop()),t=e.symbols_[t]||t),t}"function"==typeof y.yy.parseError?this.parseError=y.yy.parseError:this.parseError=Object.getPrototypeOf(this).parseError,(0,r.a)(function(t){i.length=i.length-2*t,s.length=s.length-t,n.length=n.length-t},"popStack"),(0,r.a)(g,"lex");for(var b,T,x,v,w,_,D,$,S,C={};;){if(x=i[i.length-1],this.defaultActions[x]?v=this.defaultActions[x]:((null===b||typeof b>"u")&&(b=g()),v=o[x]&&o[x][b]),typeof v>"u"||!v.length||!v[0]){var E="";for(_ in S=[],o[x])this.terminals_[_]&&_>2&&S.push("'"+this.terminals_[_]+"'");E=f.showPosition?"Parse error on line "+(c+1)+`:
`+f.showPosition()+`
Expecting `+S.join(", ")+", got '"+(this.terminals_[b]||b)+"'":"Parse error on line "+(c+1)+": Unexpected "+(1==b?"end of input":"'"+(this.terminals_[b]||b)+"'"),this.parseError(E,{text:f.match,token:this.terminals_[b]||b,line:f.yylineno,loc:k,expected:S})}if(v[0]instanceof Array&&v.length>1)throw Error("Parse Error: multiple actions possible at state: "+x+", token: "+b);switch(v[0]){case 1:i.push(b),s.push(f.yytext),n.push(f.yylloc),i.push(v[1]),b=null,T?(b=T,T=null):(d=f.yyleng,l=f.yytext,c=f.yylineno,k=f.yylloc,u>0&&u--);break;case 2:if(D=this.productions_[v[1]][1],C.$=s[s.length-D],C._$={first_line:n[n.length-(D||1)].first_line,last_line:n[n.length-1].last_line,first_column:n[n.length-(D||1)].first_column,last_column:n[n.length-1].last_column},p&&(C._$.range=[n[n.length-(D||1)].range[0],n[n.length-1].range[1]]),"u">typeof(w=this.performAction.apply(C,[l,d,c,y.yy,v[1],s,n].concat(h))))return w;D&&(i=i.slice(0,-1*D*2),s=s.slice(0,-1*D),n=n.slice(0,-1*D)),i.push(this.productions_[v[1]][0]),s.push(C.$),n.push(C._$),$=o[i[i.length-2]][i[i.length-1]],i.push($);break;case 3:return!0}}return!0},"parse")};function C(){this.yy={}}return S.lexer={EOF:1,parseError:(0,r.a)(function(t,e){if(this.yy.parser)this.yy.parser.parseError(t,e);else throw Error(t)},"parseError"),setInput:(0,r.a)(function(t,e){return this.yy=e||this.yy||{},this._input=t,this._more=this._backtrack=this.done=!1,this.yylineno=this.yyleng=0,this.yytext=this.matched=this.match="",this.conditionStack=["INITIAL"],this.yylloc={first_line:1,first_column:0,last_line:1,last_column:0},this.options.ranges&&(this.yylloc.range=[0,0]),this.offset=0,this},"setInput"),input:(0,r.a)(function(){var t=this._input[0];return this.yytext+=t,this.yyleng++,this.offset++,this.match+=t,this.matched+=t,t.match(/(?:\r\n?|\n).*/g)?(this.yylineno++,this.yylloc.last_line++):this.yylloc.last_column++,this.options.ranges&&this.yylloc.range[1]++,this._input=this._input.slice(1),t},"input"),unput:(0,r.a)(function(t){var e=t.length,i=t.split(/(?:\r\n?|\n)/g);this._input=t+this._input,this.yytext=this.yytext.substr(0,this.yytext.length-e),this.offset-=e;var a=this.match.split(/(?:\r\n?|\n)/g);this.match=this.match.substr(0,this.match.length-1),this.matched=this.matched.substr(0,this.matched.length-1),i.length-1&&(this.yylineno-=i.length-1);var s=this.yylloc.range;return this.yylloc={first_line:this.yylloc.first_line,last_line:this.yylineno+1,first_column:this.yylloc.first_column,last_column:i?(i.length===a.length?this.yylloc.first_column:0)+a[a.length-i.length].length-i[0].length:this.yylloc.first_column-e},this.options.ranges&&(this.yylloc.range=[s[0],s[0]+this.yyleng-e]),this.yyleng=this.yytext.length,this},"unput"),more:(0,r.a)(function(){return this._more=!0,this},"more"),reject:(0,r.a)(function(){return this.options.backtrack_lexer?(this._backtrack=!0,this):this.parseError("Lexical error on line "+(this.yylineno+1)+`. You can only invoke reject() in the lexer when the lexer is of the backtracking persuasion (options.backtrack_lexer = true).
`+this.showPosition(),{text:"",token:null,line:this.yylineno})},"reject"),less:(0,r.a)(function(t){this.unput(this.match.slice(t))},"less"),pastInput:(0,r.a)(function(){var t=this.matched.substr(0,this.matched.length-this.match.length);return(t.length>20?"...":"")+t.substr(-20).replace(/\n/g,"")},"pastInput"),upcomingInput:(0,r.a)(function(){var t=this.match;return t.length<20&&(t+=this._input.substr(0,20-t.length)),(t.substr(0,20)+(t.length>20?"...":"")).replace(/\n/g,"")},"upcomingInput"),showPosition:(0,r.a)(function(){var t=this.pastInput(),e=Array(t.length+1).join("-");return t+this.upcomingInput()+`
`+e+"^"},"showPosition"),test_match:(0,r.a)(function(t,e){var i,a,s;if(this.options.backtrack_lexer&&(s={yylineno:this.yylineno,yylloc:{first_line:this.yylloc.first_line,last_line:this.last_line,first_column:this.yylloc.first_column,last_column:this.yylloc.last_column},yytext:this.yytext,match:this.match,matches:this.matches,matched:this.matched,yyleng:this.yyleng,offset:this.offset,_more:this._more,_input:this._input,yy:this.yy,conditionStack:this.conditionStack.slice(0),done:this.done},this.options.ranges&&(s.yylloc.range=this.yylloc.range.slice(0))),(a=t[0].match(/(?:\r\n?|\n).*/g))&&(this.yylineno+=a.length),this.yylloc={first_line:this.yylloc.last_line,last_line:this.yylineno+1,first_column:this.yylloc.last_column,last_column:a?a[a.length-1].length-a[a.length-1].match(/\r?\n?/)[0].length:this.yylloc.last_column+t[0].length},this.yytext+=t[0],this.match+=t[0],this.matches=t,this.yyleng=this.yytext.length,this.options.ranges&&(this.yylloc.range=[this.offset,this.offset+=this.yyleng]),this._more=!1,this._backtrack=!1,this._input=this._input.slice(t[0].length),this.matched+=t[0],i=this.performAction.call(this,this.yy,this,e,this.conditionStack[this.conditionStack.length-1]),this.done&&this._input&&(this.done=!1),i)return i;if(this._backtrack)for(var n in s)this[n]=s[n];return!1},"test_match"),next:(0,r.a)(function(){if(this.done)return this.EOF;this._input||(this.done=!0),this._more||(this.yytext="",this.match="");for(var t,e,i,a,s=this._currentRules(),n=0;n<s.length;n++)if((i=this._input.match(this.rules[s[n]]))&&(!e||i[0].length>e[0].length)){if(e=i,a=n,this.options.backtrack_lexer){if(!1!==(t=this.test_match(i,s[n])))return t;if(!this._backtrack)return!1;e=!1;continue}else if(!this.options.flex)break}return e?!1!==(t=this.test_match(e,s[a]))&&t:""===this._input?this.EOF:this.parseError("Lexical error on line "+(this.yylineno+1)+`. Unrecognized text.
`+this.showPosition(),{text:"",token:null,line:this.yylineno})},"next"),lex:(0,r.a)(function(){return this.next()||this.lex()},"lex"),begin:(0,r.a)(function(t){this.conditionStack.push(t)},"begin"),popState:(0,r.a)(function(){return this.conditionStack.length-1>0?this.conditionStack.pop():this.conditionStack[0]},"popState"),_currentRules:(0,r.a)(function(){return this.conditionStack.length&&this.conditionStack[this.conditionStack.length-1]?this.conditions[this.conditionStack[this.conditionStack.length-1]].rules:this.conditions.INITIAL.rules},"_currentRules"),topState:(0,r.a)(function(t){return(t=this.conditionStack.length-1-Math.abs(t||0))>=0?this.conditionStack[t]:"INITIAL"},"topState"),pushState:(0,r.a)(function(t){this.begin(t)},"pushState"),stateStackSize:(0,r.a)(function(){return this.conditionStack.length},"stateStackSize"),options:{"case-insensitive":!0},performAction:(0,r.a)(function(t,e,i,a){switch(i){case 0:return this.begin("open_directive"),"open_directive";case 1:return this.begin("acc_title"),31;case 2:return this.popState(),"acc_title_value";case 3:return this.begin("acc_descr"),33;case 4:return this.popState(),"acc_descr_value";case 5:this.begin("acc_descr_multiline");break;case 6:case 15:case 18:case 21:case 24:this.popState();break;case 7:return"acc_descr_multiline_value";case 8:case 9:case 10:case 12:case 13:break;case 11:return 10;case 14:this.begin("href");break;case 16:return 43;case 17:this.begin("callbackname");break;case 19:this.popState(),this.begin("callbackargs");break;case 20:return 41;case 22:return 42;case 23:this.begin("click");break;case 25:return 40;case 26:return 4;case 27:return 22;case 28:return 23;case 29:return 24;case 30:return 25;case 31:return 26;case 32:return 28;case 33:return 27;case 34:return 29;case 35:return 12;case 36:return 13;case 37:return 14;case 38:return 15;case 39:return 16;case 40:return 17;case 41:return 18;case 42:return 20;case 43:return 21;case 44:return"date";case 45:return 30;case 46:return"accDescription";case 47:return 36;case 48:return 38;case 49:return 39;case 50:return":";case 51:return 6;case 52:return"INVALID"}},"anonymous"),rules:[/^(?:%%\{)/i,/^(?:accTitle\s*:\s*)/i,/^(?:(?!\n||)*[^\n]*)/i,/^(?:accDescr\s*:\s*)/i,/^(?:(?!\n||)*[^\n]*)/i,/^(?:accDescr\s*\{\s*)/i,/^(?:[\}])/i,/^(?:[^\}]*)/i,/^(?:%%(?!\{)*[^\n]*)/i,/^(?:[^\}]%%*[^\n]*)/i,/^(?:%%*[^\n]*[\n]*)/i,/^(?:[\n]+)/i,/^(?:\s+)/i,/^(?:%[^\n]*)/i,/^(?:href[\s]+["])/i,/^(?:["])/i,/^(?:[^"]*)/i,/^(?:call[\s]+)/i,/^(?:\([\s]*\))/i,/^(?:\()/i,/^(?:[^(]*)/i,/^(?:\))/i,/^(?:[^)]*)/i,/^(?:click[\s]+)/i,/^(?:[\s\n])/i,/^(?:[^\s\n]*)/i,/^(?:gantt\b)/i,/^(?:dateFormat\s[^#\n;]+)/i,/^(?:inclusiveEndDates\b)/i,/^(?:topAxis\b)/i,/^(?:axisFormat\s[^#\n;]+)/i,/^(?:tickInterval\s[^#\n;]+)/i,/^(?:includes\s[^#\n;]+)/i,/^(?:excludes\s[^#\n;]+)/i,/^(?:todayMarker\s[^\n;]+)/i,/^(?:weekday\s+monday\b)/i,/^(?:weekday\s+tuesday\b)/i,/^(?:weekday\s+wednesday\b)/i,/^(?:weekday\s+thursday\b)/i,/^(?:weekday\s+friday\b)/i,/^(?:weekday\s+saturday\b)/i,/^(?:weekday\s+sunday\b)/i,/^(?:weekend\s+friday\b)/i,/^(?:weekend\s+saturday\b)/i,/^(?:\d\d\d\d-\d\d-\d\d\b)/i,/^(?:title\s[^\n]+)/i,/^(?:accDescription\s[^#\n;]+)/i,/^(?:section\s[^\n]+)/i,/^(?:[^:\n]+)/i,/^(?::[^#\n;]+)/i,/^(?::)/i,/^(?:$)/i,/^(?:.)/i],conditions:{acc_descr_multiline:{rules:[6,7],inclusive:!1},acc_descr:{rules:[4],inclusive:!1},acc_title:{rules:[2],inclusive:!1},callbackargs:{rules:[21,22],inclusive:!1},callbackname:{rules:[18,19,20],inclusive:!1},href:{rules:[15,16],inclusive:!1},click:{rules:[24,25],inclusive:!1},INITIAL:{rules:[0,1,3,5,8,9,10,11,12,13,14,17,23,26,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52],inclusive:!0}}},(0,r.a)(C,"Parser"),C.prototype=S,S.Parser=C,new C}();d.parser=d;var u=(0,r.e)((0,s.a)(),1),h=(0,r.e)((0,n.a)(),1),f=(0,r.e)(o(),1),y=(0,r.e)(l(),1),m=(0,r.e)(c(),1);h.default.extend(f.default),h.default.extend(y.default),h.default.extend(m.default);var k,p,g,b={friday:5,saturday:6},T="",x="",v="",w=[],_=[],D=new Map,$=[],S=[],C="",E="",M=["active","done","crit","milestone"],A=[],L=!1,Y=!1,I="sunday",W="saturday",F=0,O=(0,r.a)(function(){$=[],S=[],C="",A=[],tm=0,p=void 0,g=void 0,tb=[],T="",x="",E="",k=void 0,v="",w=[],_=[],L=!1,Y=!1,F=0,D=new Map,(0,n.P)(),I="sunday",W="saturday"},"clear"),P=(0,r.a)(function(t){x=t},"setAxisFormat"),B=(0,r.a)(function(){return x},"getAxisFormat"),z=(0,r.a)(function(t){k=t},"setTickInterval"),j=(0,r.a)(function(){return k},"getTickInterval"),N=(0,r.a)(function(t){v=t},"setTodayMarker"),H=(0,r.a)(function(){return v},"getTodayMarker"),G=(0,r.a)(function(t){T=t},"setDateFormat"),R=(0,r.a)(function(){L=!0},"enableInclusiveEndDates"),X=(0,r.a)(function(){return L},"endDatesAreInclusive"),V=(0,r.a)(function(){Y=!0},"enableTopAxis"),Z=(0,r.a)(function(){return Y},"topAxisEnabled"),U=(0,r.a)(function(t){E=t},"setDisplayMode"),q=(0,r.a)(function(){return E},"getDisplayMode"),Q=(0,r.a)(function(){return T},"getDateFormat"),J=(0,r.a)(function(t){w=t.toLowerCase().split(/[\s,]+/)},"setIncludes"),K=(0,r.a)(function(){return w},"getIncludes"),tt=(0,r.a)(function(t){_=t.toLowerCase().split(/[\s,]+/)},"setExcludes"),te=(0,r.a)(function(){return _},"getExcludes"),ti=(0,r.a)(function(){return D},"getLinks"),ta=(0,r.a)(function(t){C=t,$.push(t)},"addSection"),ts=(0,r.a)(function(){return $},"getSections"),tn=(0,r.a)(function(){let t=t_(),e=0;for(;!t&&e<10;)t=t_(),e++;return S=tb},"getTasks"),tr=(0,r.a)(function(t,e,i,a){return!a.includes(t.format(e.trim()))&&(!!(i.includes("weekends")&&(t.isoWeekday()===b[W]||t.isoWeekday()===b[W]+1)||i.includes(t.format("dddd").toLowerCase()))||i.includes(t.format(e.trim())))},"isInvalidDate"),to=(0,r.a)(function(t){I=t},"setWeekday"),tl=(0,r.a)(function(){return I},"getWeekday"),tc=(0,r.a)(function(t){W=t},"setWeekend"),td=(0,r.a)(function(t,e,i,a){let s,n;if(!i.length||t.manualEndTime)return;let[r,o]=tu(s=(s=t.startTime instanceof Date?(0,h.default)(t.startTime):(0,h.default)(t.startTime,e,!0)).add(1,"d"),t.endTime instanceof Date?(0,h.default)(t.endTime):(0,h.default)(t.endTime,e,!0),e,i,a);t.endTime=r.toDate(),t.renderEndTime=o},"checkTaskDates"),tu=(0,r.a)(function(t,e,i,a,s){let n=!1,r=null;for(;t<=e;)n||(r=e.toDate()),(n=tr(t,i,a,s))&&(e=e.add(1,"d")),t=t.add(1,"d");return[e,r]},"fixTaskDates"),th=(0,r.a)(function(t,e,i){i=i.trim();let a=/^after\s+(?<ids>[\d\w- ]+)/.exec(i);if(null!==a){let t=null;for(let e of a.groups.ids.split(" ")){let i=tv(e);void 0!==i&&(!t||i.endTime>t.endTime)&&(t=i)}if(t)return t.endTime;let e=new Date;return e.setHours(0,0,0,0),e}let s=(0,h.default)(i,e.trim(),!0);if(s.isValid())return s.toDate();{n.b.debug("Invalid date:"+i),n.b.debug("With date format:"+e.trim());let t=new Date(i);if(void 0===t||isNaN(t.getTime())||-1e4>t.getFullYear()||t.getFullYear()>1e4)throw Error("Invalid date:"+i);return t}},"getStartDate"),tf=(0,r.a)(function(t){let e=/^(\d+(?:\.\d+)?)([Mdhmswy]|ms)$/.exec(t.trim());return null!==e?[Number.parseFloat(e[1]),e[2]]:[NaN,"ms"]},"parseDuration"),ty=(0,r.a)(function(t,e,i,a=!1){i=i.trim();let s=/^until\s+(?<ids>[\d\w- ]+)/.exec(i);if(null!==s){let t=null;for(let e of s.groups.ids.split(" ")){let i=tv(e);void 0!==i&&(!t||i.startTime<t.startTime)&&(t=i)}if(t)return t.startTime;let e=new Date;return e.setHours(0,0,0,0),e}let n=(0,h.default)(i,e.trim(),!0);if(n.isValid())return a&&(n=n.add(1,"d")),n.toDate();let r=(0,h.default)(t),[o,l]=tf(i);if(!Number.isNaN(o)){let t=r.add(o,l);t.isValid()&&(r=t)}return r.toDate()},"getEndDate"),tm=0,tk=(0,r.a)(function(t){return void 0===t?"task"+(tm+=1):t},"parseId"),tp=(0,r.a)(function(t,e){let i,a=(":"===e.substr(0,1)?e.substr(1,e.length):e).split(","),s={};tL(a,s,M);for(let t=0;t<a.length;t++)a[t]=a[t].trim();let n="";switch(a.length){case 1:s.id=tk(),s.startTime=t.endTime,n=a[0];break;case 2:s.id=tk(),s.startTime=th(void 0,T,a[0]),n=a[1];break;case 3:s.id=tk(a[0]),s.startTime=th(void 0,T,a[1]),n=a[2]}return n&&(s.endTime=ty(s.startTime,T,n,L),s.manualEndTime=(0,h.default)(n,"YYYY-MM-DD",!0).isValid(),td(s,T,_,w)),s},"compileData"),tg=(0,r.a)(function(t,e){let i,a=(":"===e.substr(0,1)?e.substr(1,e.length):e).split(","),s={};tL(a,s,M);for(let t=0;t<a.length;t++)a[t]=a[t].trim();switch(a.length){case 1:s.id=tk(),s.startTime={type:"prevTaskEnd",id:t},s.endTime={data:a[0]};break;case 2:s.id=tk(),s.startTime={type:"getStartDate",startData:a[0]},s.endTime={data:a[1]};break;case 3:s.id=tk(a[0]),s.startTime={type:"getStartDate",startData:a[1]},s.endTime={data:a[2]}}return s},"parseData"),tb=[],tT={},tx=(0,r.a)(function(t,e){let i={section:C,type:C,processed:!1,manualEndTime:!1,renderEndTime:null,raw:{data:e},task:t,classes:[]},a=tg(g,e);i.raw.startTime=a.startTime,i.raw.endTime=a.endTime,i.id=a.id,i.prevTaskId=g,i.active=a.active,i.done=a.done,i.crit=a.crit,i.milestone=a.milestone,i.order=F,F++;let s=tb.push(i);g=i.id,tT[i.id]=s-1},"addTask"),tv=(0,r.a)(function(t){return tb[tT[t]]},"findTaskById"),tw=(0,r.a)(function(t,e){let i={section:C,type:C,description:t,task:t,classes:[]},a=tp(p,e);i.startTime=a.startTime,i.endTime=a.endTime,i.id=a.id,i.active=a.active,i.done=a.done,i.crit=a.crit,i.milestone=a.milestone,p=i,S.push(i)},"addTaskOrg"),t_=(0,r.a)(function(){let t=(0,r.a)(function(t){let e=tb[t],i="";switch(tb[t].raw.startTime.type){case"prevTaskEnd":{let t=tv(e.prevTaskId);e.startTime=t.endTime;break}case"getStartDate":(i=th(void 0,T,tb[t].raw.startTime.startData))&&(tb[t].startTime=i)}return tb[t].startTime&&(tb[t].endTime=ty(tb[t].startTime,T,tb[t].raw.endTime.data,L),tb[t].endTime&&(tb[t].processed=!0,tb[t].manualEndTime=(0,h.default)(tb[t].raw.endTime.data,"YYYY-MM-DD",!0).isValid(),td(tb[t],T,_,w))),tb[t].processed},"compileTask"),e=!0;for(let[i,a]of tb.entries())t(i),e=e&&a.processed;return e},"compileTasks"),tD=(0,r.a)(function(t,e){let i=e;"loose"!==(0,n.X)().securityLevel&&(i=(0,u.sanitizeUrl)(e)),t.split(",").forEach(function(t){void 0!==tv(t)&&(tC(t,()=>{window.open(i,"_self")}),D.set(t,i))}),t$(t,"clickable")},"setLink"),t$=(0,r.a)(function(t,e){t.split(",").forEach(function(t){let i=tv(t);void 0!==i&&i.classes.push(e)})},"setClass"),tS=(0,r.a)(function(t,e,i){if("loose"!==(0,n.X)().securityLevel||void 0===e)return;let s=[];if("string"==typeof i){s=i.split(/,(?=(?:(?:[^"]*"){2})*[^"]*$)/);for(let t=0;t<s.length;t++){let e=s[t].trim();e.startsWith('"')&&e.endsWith('"')&&(e=e.substr(1,e.length-2)),s[t]=e}}0===s.length&&s.push(t),void 0!==tv(t)&&tC(t,()=>{a.m.runFunc(e,...s)})},"setClickFun"),tC=(0,r.a)(function(t,e){A.push(function(){let i=document.querySelector(`[id="${t}"]`);null!==i&&i.addEventListener("click",function(){e()})},function(){let i=document.querySelector(`[id="${t}-text"]`);null!==i&&i.addEventListener("click",function(){e()})})},"pushFun"),tE=(0,r.a)(function(t,e,i){t.split(",").forEach(function(t){tS(t,e,i)}),t$(t,"clickable")},"setClickEvent"),tM=(0,r.a)(function(t){A.forEach(function(e){e(t)})},"bindFunctions"),tA={getConfig:(0,r.a)(()=>(0,n.X)().gantt,"getConfig"),clear:O,setDateFormat:G,getDateFormat:Q,enableInclusiveEndDates:R,endDatesAreInclusive:X,enableTopAxis:V,topAxisEnabled:Z,setAxisFormat:P,getAxisFormat:B,setTickInterval:z,getTickInterval:j,setTodayMarker:N,getTodayMarker:H,setAccTitle:n.Q,getAccTitle:n.R,setDiagramTitle:n.U,getDiagramTitle:n.V,setDisplayMode:U,getDisplayMode:q,setAccDescription:n.S,getAccDescription:n.T,addSection:ta,getSections:ts,getTasks:tn,addTask:tx,findTaskById:tv,addTaskOrg:tw,setIncludes:J,getIncludes:K,setExcludes:tt,getExcludes:te,setClickEvent:tE,setLink:tD,getLinks:ti,bindFunctions:tM,parseDuration:tf,isInvalidDate:tr,setWeekday:to,getWeekday:tl,setWeekend:tc};function tL(t,e,i){let a=!0;for(;a;)a=!1,i.forEach(function(i){let s=RegExp("^\\s*"+i+"\\s*$");t[0].match(s)&&(e[i]=!0,t.shift(1),a=!0)})}(0,r.a)(tL,"getTaskTags");var tY,tI=(0,r.e)((0,n.a)(),1),tW=(0,r.a)(function(){n.b.debug("Something is calling, setConf, remove the call")},"setConf"),tF={monday:n.qa,tuesday:n.ra,wednesday:n.sa,thursday:n.ta,friday:n.ua,saturday:n.va,sunday:n.pa},tO=(0,r.a)((t,e)=>{let i=[...t].map(()=>-1/0),a=[...t].sort((t,e)=>t.startTime-e.startTime||t.order-e.order),s=0;for(let t of a)for(let a=0;a<i.length;a++)if(t.startTime>=i[a]){i[a]=t.endTime,t.order=a+e,a>s&&(s=a);break}return s},"getMaxIntersections"),tP={parser:d,db:tA,renderer:{setConf:tW,draw:(0,r.a)(function(t,e,i,a){let s=(0,n.X)().gantt,o=(0,n.X)().securityLevel,l;"sandbox"===o&&(l=(0,n.fa)("#i"+e));let c="sandbox"===o?(0,n.fa)(l.nodes()[0].contentDocument.body):(0,n.fa)("body"),d="sandbox"===o?l.nodes()[0].contentDocument:document,u=d.getElementById(e);void 0===(tY=u.parentElement.offsetWidth)&&(tY=1200),void 0!==s.useWidth&&(tY=s.useWidth);let h=a.db.getTasks(),f=[];for(let t of h)f.push(t.type);f=D(f);let y={},m=2*s.topPadding;if("compact"===a.db.getDisplayMode()||"compact"===s.displayMode){let t={};for(let e of h)void 0===t[e.section]?t[e.section]=[e]:t[e.section].push(e);let e=0;for(let i of Object.keys(t)){let a=tO(t[i],e)+1;e+=a,m+=a*(s.barHeight+s.barGap),y[i]=a}}else for(let t of(m+=h.length*(s.barHeight+s.barGap),f))y[t]=h.filter(e=>e.type===t).length;u.setAttribute("viewBox","0 0 "+tY+" "+m);let k=c.select(`[id="${e}"]`),p=(0,n.ya)().domain([(0,n.ca)(h,function(t){return t.startTime}),(0,n.ba)(h,function(t){return t.endTime})]).rangeRound([0,tY-s.leftPadding-s.rightPadding]);function g(t,e){let i=t.startTime,a=e.startTime,s=0;return i>a?s=1:i<a&&(s=-1),s}function b(t,e,i){let r=s.barHeight,o=r+s.barGap,l=s.topPadding,c=s.leftPadding,d=(0,n.ja)().domain([0,f.length]).range(["#00B9FA","#F95002"]).interpolate(n.ga);x(o,l,c,e,i,t,a.db.getExcludes(),a.db.getIncludes()),v(c,l,e,i),T(t,o,l,c,r,d,e,i),w(o,l,c,r,d),_(c,l,e,i)}function T(t,i,r,o,l,c,d){let u=[...new Set(t.map(t=>t.order))].map(e=>t.find(t=>t.order===e));k.append("g").selectAll("rect").data(u).enter().append("rect").attr("x",0).attr("y",function(t,e){return t.order*i+r-2}).attr("width",function(){return d-s.rightPadding/2}).attr("height",i).attr("class",function(t){for(let[e,i]of f.entries())if(t.type===i)return"section section"+e%s.numberSectionStyles;return"section section0"});let h=k.append("g").selectAll("rect").data(t).enter(),y=a.db.getLinks();if(h.append("rect").attr("id",function(t){return t.id}).attr("rx",3).attr("ry",3).attr("x",function(t){return t.milestone?p(t.startTime)+o+.5*(p(t.endTime)-p(t.startTime))-.5*l:p(t.startTime)+o}).attr("y",function(t,e){return t.order*i+r}).attr("width",function(t){return t.milestone?l:p(t.renderEndTime||t.endTime)-p(t.startTime)}).attr("height",l).attr("transform-origin",function(t,e){return e=t.order,(p(t.startTime)+o+.5*(p(t.endTime)-p(t.startTime))).toString()+"px "+(e*i+r+.5*l).toString()+"px"}).attr("class",function(t){let e="";t.classes.length>0&&(e=t.classes.join(" "));let i=0;for(let[e,a]of f.entries())t.type===a&&(i=e%s.numberSectionStyles);let a="";return t.active?t.crit?a+=" activeCrit":a=" active":t.done?a=t.crit?" doneCrit":" done":t.crit&&(a+=" crit"),0===a.length&&(a=" task"),t.milestone&&(a=" milestone "+a),a+=i,"task"+(a+=" "+e)}),h.append("text").attr("id",function(t){return t.id+"-text"}).text(function(t){return t.task}).attr("font-size",s.fontSize).attr("x",function(t){let e=p(t.startTime),i=p(t.renderEndTime||t.endTime);t.milestone&&(e+=.5*(p(t.endTime)-p(t.startTime))-.5*l),t.milestone&&(i=e+l);let a=this.getBBox().width;return a>i-e?i+a+1.5*s.leftPadding>d?e+o-5:i+o+5:(i-e)/2+e+o}).attr("y",function(t,e){return t.order*i+s.barHeight/2+(s.fontSize/2-2)+r}).attr("text-height",l).attr("class",function(t){let e=p(t.startTime),i=p(t.endTime);t.milestone&&(i=e+l);let a=this.getBBox().width,n="";t.classes.length>0&&(n=t.classes.join(" "));let r=0;for(let[e,i]of f.entries())t.type===i&&(r=e%s.numberSectionStyles);let o="";return t.active&&(o=t.crit?"activeCritText"+r:"activeText"+r),t.done?o=t.crit?o+" doneCritText"+r:o+" doneText"+r:t.crit&&(o=o+" critText"+r),t.milestone&&(o+=" milestoneText"),a>i-e?i+a+1.5*s.leftPadding>d?n+" taskTextOutsideLeft taskTextOutside"+r+" "+o:n+" taskTextOutsideRight taskTextOutside"+r+" "+o+" width-"+a:n+" taskText taskText"+r+" "+o+" width-"+a}),"sandbox"===(0,n.X)().securityLevel){let t=(0,n.fa)("#i"+e).nodes()[0].contentDocument;h.filter(function(t){return y.has(t.id)}).each(function(e){var i=t.querySelector("#"+e.id),a=t.querySelector("#"+e.id+"-text");let s=i.parentNode;var n=t.createElement("a");n.setAttribute("xlink:href",y.get(e.id)),n.setAttribute("target","_top"),s.appendChild(n),n.appendChild(i),n.appendChild(a)})}}function x(t,e,i,r,o,l,c,d){let u,h;if(0===c.length&&0===d.length)return;for(let{startTime:t,endTime:e}of l)(void 0===u||t<u)&&(u=t),(void 0===h||e>h)&&(h=e);if(!u||!h)return;if((0,tI.default)(h).diff((0,tI.default)(u),"year")>5)return void n.b.warn("The difference between the min and max time is more than 5 years. This will cause performance issues. Skipping drawing exclude days.");let f=a.db.getDateFormat(),y=[],m=null,g=(0,tI.default)(u);for(;g.valueOf()<=h;)a.db.isInvalidDate(g,f,c,d)?m?m.end=g:m={start:g,end:g}:m&&(y.push(m),m=null),g=g.add(1,"d");k.append("g").selectAll("rect").data(y).enter().append("rect").attr("id",function(t){return"exclude-"+t.start.format("YYYY-MM-DD")}).attr("x",function(t){return p(t.start)+i}).attr("y",s.gridLineStartPadding).attr("width",function(t){return p(t.end.add(1,"day"))-p(t.start)}).attr("height",o-e-s.gridLineStartPadding).attr("transform-origin",function(e,a){return(p(e.start)+i+.5*(p(e.end)-p(e.start))).toString()+"px "+(a*t+.5*o).toString()+"px"}).attr("class","exclude-range")}function v(t,e,i,r){let o=(0,n.ea)(p).tickSize(-r+e+s.gridLineStartPadding).tickFormat((0,n.xa)(a.db.getAxisFormat()||s.axisFormat||"%Y-%m-%d")),l=/^([1-9]\d*)(millisecond|second|minute|hour|day|week|month)$/.exec(a.db.getTickInterval()||s.tickInterval);if(null!==l){let t=l[1],e=l[2],i=a.db.getWeekday()||s.weekday;switch(e){case"millisecond":o.ticks(n.ka.every(t));break;case"second":o.ticks(n.la.every(t));break;case"minute":o.ticks(n.ma.every(t));break;case"hour":o.ticks(n.na.every(t));break;case"day":o.ticks(n.oa.every(t));break;case"week":o.ticks(tF[i].every(t));break;case"month":o.ticks(n.wa.every(t))}}if(k.append("g").attr("class","grid").attr("transform","translate("+t+", "+(r-50)+")").call(o).selectAll("text").style("text-anchor","middle").attr("fill","#000").attr("stroke","none").attr("font-size",10).attr("dy","1em"),a.db.topAxisEnabled()||s.topAxis){let i=(0,n.da)(p).tickSize(-r+e+s.gridLineStartPadding).tickFormat((0,n.xa)(a.db.getAxisFormat()||s.axisFormat||"%Y-%m-%d"));if(null!==l){let t=l[1],e=l[2],r=a.db.getWeekday()||s.weekday;switch(e){case"millisecond":i.ticks(n.ka.every(t));break;case"second":i.ticks(n.la.every(t));break;case"minute":i.ticks(n.ma.every(t));break;case"hour":i.ticks(n.na.every(t));break;case"day":i.ticks(n.oa.every(t));break;case"week":i.ticks(tF[r].every(t));break;case"month":i.ticks(n.wa.every(t))}}k.append("g").attr("class","grid").attr("transform","translate("+t+", "+e+")").call(i).selectAll("text").style("text-anchor","middle").attr("fill","#000").attr("stroke","none").attr("font-size",10)}}function w(t,e){let i=0,a=Object.keys(y).map(t=>[t,y[t]]);k.append("g").selectAll("text").data(a).enter().append(function(t){let e=t[0].split(n.L.lineBreakRegex),i=-(e.length-1)/2,a=d.createElementNS("http://www.w3.org/2000/svg","text");for(let[t,s]of(a.setAttribute("dy",i+"em"),e.entries())){let e=d.createElementNS("http://www.w3.org/2000/svg","tspan");e.setAttribute("alignment-baseline","central"),e.setAttribute("x","10"),t>0&&e.setAttribute("dy","1em"),e.textContent=s,a.appendChild(e)}return a}).attr("x",10).attr("y",function(s,n){if(!(n>0))return s[1]*t/2+e;for(let r=0;r<n;r++)return i+=a[n-1][1],s[1]*t/2+i*t+e}).attr("font-size",s.sectionFontSize).attr("class",function(t){for(let[e,i]of f.entries())if(t[0]===i)return"sectionTitle sectionTitle"+e%s.numberSectionStyles;return"sectionTitle"})}function _(t,e,i,n){let r=a.db.getTodayMarker();if("off"===r)return;let o=k.append("g").attr("class","today"),l=new Date,c=o.append("line");c.attr("x1",p(l)+t).attr("x2",p(l)+t).attr("y1",s.titleTopMargin).attr("y2",n-s.titleTopMargin).attr("class","today"),""!==r&&c.attr("style",r.replace(/,/g,";"))}function D(t){let e={},i=[];for(let a=0,s=t.length;a<s;++a)Object.prototype.hasOwnProperty.call(e,t[a])||(e[t[a]]=!0,i.push(t[a]));return i}(0,r.a)(g,"taskCompare"),h.sort(g),b(h,tY,m),(0,n.M)(k,m,tY,s.useMaxWidth),k.append("text").text(a.db.getDiagramTitle()).attr("x",tY/2).attr("y",s.titleTopMargin).attr("class","titleText"),(0,r.a)(b,"makeGantt"),(0,r.a)(T,"drawRects"),(0,r.a)(x,"drawExcludeDays"),(0,r.a)(v,"makeGrid"),(0,r.a)(w,"vertLabels"),(0,r.a)(_,"drawToday"),(0,r.a)(D,"checkUnique")},"draw")},styles:(0,r.a)(t=>`
  .mermaid-main-font {
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }

  .exclude-range {
    fill: ${t.excludeBkgColor};
  }

  .section {
    stroke: none;
    opacity: 0.2;
  }

  .section0 {
    fill: ${t.sectionBkgColor};
  }

  .section2 {
    fill: ${t.sectionBkgColor2};
  }

  .section1,
  .section3 {
    fill: ${t.altSectionBkgColor};
    opacity: 0.2;
  }

  .sectionTitle0 {
    fill: ${t.titleColor};
  }

  .sectionTitle1 {
    fill: ${t.titleColor};
  }

  .sectionTitle2 {
    fill: ${t.titleColor};
  }

  .sectionTitle3 {
    fill: ${t.titleColor};
  }

  .sectionTitle {
    text-anchor: start;
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }


  /* Grid and axis */

  .grid .tick {
    stroke: ${t.gridColor};
    opacity: 0.8;
    shape-rendering: crispEdges;
  }

  .grid .tick text {
    font-family: ${t.fontFamily};
    fill: ${t.textColor};
  }

  .grid path {
    stroke-width: 0;
  }


  /* Today line */

  .today {
    fill: none;
    stroke: ${t.todayLineColor};
    stroke-width: 2px;
  }


  /* Task styling */

  /* Default task */

  .task {
    stroke-width: 2;
  }

  .taskText {
    text-anchor: middle;
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }

  .taskTextOutsideRight {
    fill: ${t.taskTextDarkColor};
    text-anchor: start;
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }

  .taskTextOutsideLeft {
    fill: ${t.taskTextDarkColor};
    text-anchor: end;
  }


  /* Special case clickable */

  .task.clickable {
    cursor: pointer;
  }

  .taskText.clickable {
    cursor: pointer;
    fill: ${t.taskTextClickableColor} !important;
    font-weight: bold;
  }

  .taskTextOutsideLeft.clickable {
    cursor: pointer;
    fill: ${t.taskTextClickableColor} !important;
    font-weight: bold;
  }

  .taskTextOutsideRight.clickable {
    cursor: pointer;
    fill: ${t.taskTextClickableColor} !important;
    font-weight: bold;
  }


  /* Specific task settings for the sections*/

  .taskText0,
  .taskText1,
  .taskText2,
  .taskText3 {
    fill: ${t.taskTextColor};
  }

  .task0,
  .task1,
  .task2,
  .task3 {
    fill: ${t.taskBkgColor};
    stroke: ${t.taskBorderColor};
  }

  .taskTextOutside0,
  .taskTextOutside2
  {
    fill: ${t.taskTextOutsideColor};
  }

  .taskTextOutside1,
  .taskTextOutside3 {
    fill: ${t.taskTextOutsideColor};
  }


  /* Active task */

  .active0,
  .active1,
  .active2,
  .active3 {
    fill: ${t.activeTaskBkgColor};
    stroke: ${t.activeTaskBorderColor};
  }

  .activeText0,
  .activeText1,
  .activeText2,
  .activeText3 {
    fill: ${t.taskTextDarkColor} !important;
  }


  /* Completed task */

  .done0,
  .done1,
  .done2,
  .done3 {
    stroke: ${t.doneTaskBorderColor};
    fill: ${t.doneTaskBkgColor};
    stroke-width: 2;
  }

  .doneText0,
  .doneText1,
  .doneText2,
  .doneText3 {
    fill: ${t.taskTextDarkColor} !important;
  }


  /* Tasks on the critical line */

  .crit0,
  .crit1,
  .crit2,
  .crit3 {
    stroke: ${t.critBorderColor};
    fill: ${t.critBkgColor};
    stroke-width: 2;
  }

  .activeCrit0,
  .activeCrit1,
  .activeCrit2,
  .activeCrit3 {
    stroke: ${t.critBorderColor};
    fill: ${t.activeTaskBkgColor};
    stroke-width: 2;
  }

  .doneCrit0,
  .doneCrit1,
  .doneCrit2,
  .doneCrit3 {
    stroke: ${t.critBorderColor};
    fill: ${t.doneTaskBkgColor};
    stroke-width: 2;
    cursor: pointer;
    shape-rendering: crispEdges;
  }

  .milestone {
    transform: rotate(45deg) scale(0.8,0.8);
  }

  .milestoneText {
    font-style: italic;
  }
  .doneCritText0,
  .doneCritText1,
  .doneCritText2,
  .doneCritText3 {
    fill: ${t.taskTextDarkColor} !important;
  }

  .activeCritText0,
  .activeCritText1,
  .activeCritText2,
  .activeCritText3 {
    fill: ${t.taskTextDarkColor} !important;
  }

  .titleText {
    text-anchor: middle;
    font-size: 18px;
    fill: ${t.titleColor||t.textColor};
    font-family: var(--mermaid-font-family, "trebuchet ms", verdana, arial, sans-serif);
  }
`,"getStyles")}})}();
//# sourceMappingURL=ganttDiagram-6SR64PWN.4000d9eb.js.map
