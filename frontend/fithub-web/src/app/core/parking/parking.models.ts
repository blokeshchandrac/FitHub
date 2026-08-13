export interface ParkingSpace{readonly id:string;readonly code:string;readonly zone:string;readonly isAccessible:boolean;readonly state:string;readonly confidence:number;readonly source:string;readonly eventTime:string;}
export interface ParkingOverview{readonly total:number;readonly available:number;readonly occupied:number;readonly unknown:number;readonly disabled:number;readonly spaces:readonly ParkingSpace[];}
export interface ParkingChanged{readonly spaceId:string;readonly code:string;readonly state:string;readonly confidence:number;readonly source:string;readonly eventTime:string;}
