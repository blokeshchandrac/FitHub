export const nextState=state=>({Available:'Occupied',Occupied:'Available',Unknown:'Occupied'}[state]??state);
export const detectionEvent=(space,state,confidence)=>({spaceCode:space.code,state,confidence:Number(confidence),source:'SyntheticClickSimulator',eventTime:new Date().toISOString()});
