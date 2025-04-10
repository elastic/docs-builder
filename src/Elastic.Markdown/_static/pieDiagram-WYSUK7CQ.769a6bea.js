!function(){function e(e,t,a,i){Object.defineProperty(e,t,{get:a,set:i,enumerable:!0,configurable:!0})}var t=("undefined"!=typeof globalThis?globalThis:"undefined"!=typeof self?self:"undefined"!=typeof window?window:"undefined"!=typeof global?global:{}).parcelRequire6955,a=t.register;a("4O9g8",function(a,i){e(a.exports,"diagram",function(){return v});var l=t("72x9i"),r=t("fBY3b");t("eqau5"),t("cRsqf");var n=t("g3QNz"),o=t("fxedn");t("3tpah");var s=t("kNZMB");t("6XWbY"),t("jaQMJ"),t("ggtbI"),t("aWvOd"),t("2OCaF"),t("2Bif5");var c=t("6t5tb"),d=s.s.pie,p={sections:new Map,showData:!1,config:d},f=p.sections,u=p.showData,g=structuredClone(d),h=(0,c.a)(()=>structuredClone(g),"getConfig"),x=(0,c.a)(()=>{f=new Map,u=p.showData,(0,s.P)()},"clear"),m=(0,c.a)(({label:e,value:t})=>{f.has(e)||(f.set(e,t),s.b.debug(`added new section: ${e}, with value: ${t}`))},"addSection"),b=(0,c.a)(()=>f,"getSections"),w=(0,c.a)(e=>{u=e},"setShowData"),S=(0,c.a)(()=>u,"getShowData"),y={getConfig:h,clear:x,setDiagramTitle:s.U,getDiagramTitle:s.V,setAccTitle:s.Q,getAccTitle:s.R,setAccDescription:s.S,getAccDescription:s.T,addSection:m,getSections:b,setShowData:w,getShowData:S},T=(0,c.a)((e,t)=>{(0,l.a)(e,t),t.setShowData(e.showData),e.sections.map(t.addSection)},"populateDb"),D={parse:(0,c.a)(async e=>{let t=await (0,r.a)("pie",e);s.b.debug(t),T(t,y)},"parse")},$=(0,c.a)(e=>`
  .pieCircle{
    stroke: ${e.pieStrokeColor};
    stroke-width : ${e.pieStrokeWidth};
    opacity : ${e.pieOpacity};
  }
  .pieOuterCircle{
    stroke: ${e.pieOuterStrokeColor};
    stroke-width: ${e.pieOuterStrokeWidth};
    fill: none;
  }
  .pieTitleText {
    text-anchor: middle;
    font-size: ${e.pieTitleTextSize};
    fill: ${e.pieTitleTextColor};
    font-family: ${e.fontFamily};
  }
  .slice {
    font-family: ${e.fontFamily};
    fill: ${e.pieSectionTextColor};
    font-size:${e.pieSectionTextSize};
    // fill: white;
  }
  .legend text {
    fill: ${e.pieLegendTextColor};
    font-family: ${e.fontFamily};
    font-size: ${e.pieLegendTextSize};
  }
`,"getStyles"),C=(0,c.a)(e=>{let t=[...e.entries()].map(e=>({label:e[0],value:e[1]})).sort((e,t)=>t.value-e.value);return(0,s.Da)().value(e=>e.value)(t)},"createPieArcs"),v={parser:D,db:y,renderer:{draw:(0,c.a)((e,t,a,i)=>{s.b.debug(`rendering pie chart
`+e);let l=i.db,r=(0,s.X)(),c=(0,o.l)(l.getConfig(),r.pie),d=(0,n.a)(t),p=d.append("g");p.attr("transform","translate(225,225)");let{themeVariables:f}=r,[u]=(0,o.k)(f.pieOuterStrokeWidth);u??=2;let g=c.textPosition,h=(0,s.Aa)().innerRadius(0).outerRadius(185),x=(0,s.Aa)().innerRadius(185*g).outerRadius(185*g);p.append("circle").attr("cx",0).attr("cy",0).attr("r",185+u/2).attr("class","pieOuterCircle");let m=l.getSections(),b=C(m),w=[f.pie1,f.pie2,f.pie3,f.pie4,f.pie5,f.pie6,f.pie7,f.pie8,f.pie9,f.pie10,f.pie11,f.pie12],S=(0,s.ha)(w);p.selectAll("mySlices").data(b).enter().append("path").attr("d",h).attr("fill",e=>S(e.data.label)).attr("class","pieCircle");let y=0;m.forEach(e=>{y+=e}),p.selectAll("mySlices").data(b).enter().append("text").text(e=>(e.data.value/y*100).toFixed(0)+"%").attr("transform",e=>"translate("+x.centroid(e)+")").style("text-anchor","middle").attr("class","slice"),p.append("text").text(l.getDiagramTitle()).attr("x",0).attr("y",-200).attr("class","pieTitleText");let T=p.selectAll(".legend").data(S.domain()).enter().append("g").attr("class","legend").attr("transform",(e,t)=>"translate(216,"+(22*t-22*S.domain().length/2)+")");T.append("rect").attr("width",18).attr("height",18).style("fill",S).style("stroke",S),T.data(b).append("text").attr("x",22).attr("y",14).text(e=>{let{label:t,value:a}=e.data;return l.getShowData()?`${t} [${a}]`:t});let D=512+Math.max(...T.selectAll("text").nodes().map(e=>e?.getBoundingClientRect().width??0));d.attr("viewBox",`0 0 ${D} 450`),(0,s.M)(d,450,D,c.useMaxWidth)},"draw")},styles:$}}),a("72x9i",function(a,i){function l(e,t){e.accDescr&&t.setAccDescription?.(e.accDescr),e.accTitle&&t.setAccTitle?.(e.accTitle),e.title&&t.setDiagramTitle?.(e.title)}e(a.exports,"a",function(){return l}),(0,t("6t5tb").a)(l,"populateCommonDb")})}();
//# sourceMappingURL=pieDiagram-WYSUK7CQ.769a6bea.js.map
