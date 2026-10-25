export interface Notification{readonly id:string;readonly type:string;readonly title:string;readonly message:string;readonly isRead:boolean;readonly createdAt:string;readonly readAt:string|null;}

export interface NotificationPreference {
  readonly category: string;
  readonly displayName: string;
  readonly description: string;
  readonly enabled: boolean;
  readonly canDisable: boolean;
}

export interface NotificationPreferenceUpdate {
  readonly category: string;
  readonly enabled: boolean;
}
