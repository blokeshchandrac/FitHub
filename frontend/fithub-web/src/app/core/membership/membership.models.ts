export type MembershipPurchaseKind = 'Purchase' | 'Renewal' | 'Upgrade';
export type EffectiveMembershipState = 'None' | 'PendingPayment' | 'Active' | 'Expired' | 'Cancelled' | 'PastDue';

export interface MembershipPlan {
  id: string;
  name: string;
  description: string | null;
  price: number;
  currency: string;
  billingPeriodMonths: number;
  isActive: boolean;
}

export interface Membership {
  id: string;
  plan: MembershipPlan;
  status: string;
  startsAt: string;
  endsAt: string;
  isActive: boolean;
}

export interface Payment {
  id: string;
  amount: number;
  currency: string;
  status: string;
  purchaseKind: MembershipPurchaseKind;
  planName: string;
  createdAt: string;
  paidAt: string | null;
}

export interface MembershipOverview {
  effectiveState: EffectiveMembershipState;
  currentMembership: Membership | null;
  plans: MembershipPlan[];
  payments: Payment[];
}

export interface MembershipPlanCommand {
  name: string;
  description: string;
  price: number;
  billingPeriodMonths: number;
}
