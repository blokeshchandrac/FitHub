export interface Recommendation{readonly id:string;readonly ruleCode:string;readonly title:string;readonly content:string;readonly category:string;readonly reason:string;readonly createdAt:string;}
export interface RecommendationOverview{readonly fitnessGoal:string;readonly activityLevel:string;readonly nutritionDisclaimer:string;readonly recommendations:readonly Recommendation[];}
