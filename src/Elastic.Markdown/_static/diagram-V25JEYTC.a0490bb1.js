!function(){function t(t,e,a,r){Object.defineProperty(t,e,{get:a,set:r,enumerable:!0,configurable:!0})}var e=("undefined"!=typeof globalThis?globalThis:"undefined"!=typeof self?self:"undefined"!=typeof window?window:"undefined"!=typeof global?global:{}).parcelRequire6955,a=e.register;a("f4kow",function(a,r){t(a.exports,"diagram",function(){return C});var l=e("72x9i"),o=e("fBY3b");e("eqau5"),e("cRsqf");var i=e("g3QNz"),n=e("fxedn");e("3tpah");var c=e("kNZMB");e("6XWbY"),e("jaQMJ"),e("ggtbI"),e("aWvOd"),e("2OCaF"),e("2Bif5");var s=e("6t5tb"),d={packet:[]},b=structuredClone(d),p=c.s.packet,k=(0,s.a)(()=>{let t=(0,n.l)({...p,...(0,c.A)().packet});return t.showBits&&(t.paddingY+=10),t},"getConfig"),f=(0,s.a)(()=>b.packet,"getPacket"),g={pushWord:(0,s.a)(t=>{t.length>0&&b.packet.push(t)},"pushWord"),getPacket:f,getConfig:k,clear:(0,s.a)(()=>{(0,c.P)(),b=structuredClone(d)},"clear"),setAccTitle:c.Q,getAccTitle:c.R,setDiagramTitle:c.U,getDiagramTitle:c.V,getAccDescription:c.T,setAccDescription:c.S},u=(0,s.a)(t=>{(0,l.a)(t,g);let e=-1,a=[],r=1,{bitsPerRow:o}=g.getConfig();for(let{start:l,end:i,label:n}of t.blocks){if(i&&i<l)throw Error(`Packet block ${l} - ${i} is invalid. End must be greater than start.`);if(l!==e+1)throw Error(`Packet block ${l} - ${i??l} is not contiguous. It should start from ${e+1}.`);for(e=i??l,c.b.debug(`Packet block ${l} - ${e} with label ${n}`);a.length<=o+1&&g.getPacket().length<1e4;){let[t,e]=h({start:l,end:i,label:n},r,o);if(a.push(t),t.end+1===r*o&&(g.pushWord(a),a=[],r++),!e)break;({start:l,end:i,label:n}=e)}}g.pushWord(a)},"populate"),h=(0,s.a)((t,e,a)=>{if(void 0===t.end&&(t.end=t.start),t.start>t.end)throw Error(`Block start ${t.start} is greater than block end ${t.end}.`);return t.end+1<=e*a?[t,void 0]:[{start:t.start,end:e*a-1,label:t.label},{start:e*a,end:t.end,label:t.label}]},"getNextFittingBlock"),x={parse:(0,s.a)(async t=>{let e=await (0,o.a)("packet",t);c.b.debug(e),u(e)},"parse")},y=(0,s.a)((t,e,a,r)=>{let l=r.db,o=l.getConfig(),{rowHeight:n,paddingY:s,bitWidth:d,bitsPerRow:b}=o,p=l.getPacket(),k=l.getDiagramTitle(),f=n+s,g=f*(p.length+1)-(k?0:n),u=d*b+2,h=(0,i.a)(e);for(let[t,e]of(h.attr("viewbox",`0 0 ${u} ${g}`),(0,c.M)(h,g,u,o.useMaxWidth),p.entries()))w(h,e,t,o);h.append("text").text(k).attr("x",u/2).attr("y",g-f/2).attr("dominant-baseline","middle").attr("text-anchor","middle").attr("class","packetTitle")},"draw"),w=(0,s.a)((t,e,a,{rowHeight:r,paddingX:l,paddingY:o,bitWidth:i,bitsPerRow:n,showBits:c})=>{let s=t.append("g"),d=a*(r+o)+o;for(let t of e){let e=t.start%n*i+1,a=(t.end-t.start+1)*i-l;if(s.append("rect").attr("x",e).attr("y",d).attr("width",a).attr("height",r).attr("class","packetBlock"),s.append("text").attr("x",e+a/2).attr("y",d+r/2).attr("class","packetLabel").attr("dominant-baseline","middle").attr("text-anchor","middle").text(t.label),!c)continue;let o=t.end===t.start,b=d-2;s.append("text").attr("x",e+(o?a/2:0)).attr("y",b).attr("class","packetByte start").attr("dominant-baseline","auto").attr("text-anchor",o?"middle":"start").text(t.start),o||s.append("text").attr("x",e+a).attr("y",b).attr("class","packetByte end").attr("dominant-baseline","auto").attr("text-anchor","end").text(t.end)}},"drawWord"),$={byteFontSize:"10px",startByteColor:"black",endByteColor:"black",labelColor:"black",labelFontSize:"12px",titleColor:"black",titleFontSize:"14px",blockStrokeColor:"black",blockStrokeWidth:"1",blockFillColor:"#efefef"},C={parser:x,db:g,renderer:{draw:y},styles:(0,s.a)(({packet:t}={})=>{let e=(0,n.l)($,t);return`
	.packetByte {
		font-size: ${e.byteFontSize};
	}
	.packetByte.start {
		fill: ${e.startByteColor};
	}
	.packetByte.end {
		fill: ${e.endByteColor};
	}
	.packetLabel {
		fill: ${e.labelColor};
		font-size: ${e.labelFontSize};
	}
	.packetTitle {
		fill: ${e.titleColor};
		font-size: ${e.titleFontSize};
	}
	.packetBlock {
		stroke: ${e.blockStrokeColor};
		stroke-width: ${e.blockStrokeWidth};
		fill: ${e.blockFillColor};
	}
	`},"styles")}}),a("72x9i",function(a,r){function l(t,e){t.accDescr&&e.setAccDescription?.(t.accDescr),t.accTitle&&e.setAccTitle?.(t.accTitle),t.title&&e.setDiagramTitle?.(t.title)}t(a.exports,"a",function(){return l}),(0,e("6t5tb").a)(l,"populateCommonDb")})}();
//# sourceMappingURL=diagram-V25JEYTC.a0490bb1.js.map
