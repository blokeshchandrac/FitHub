export interface CurrentUser {
  readonly id: string;
  readonly email: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly roles: readonly string[];
  readonly onboardingCompleted: boolean;
}

export interface AuthenticationResponse {
  readonly accessToken: string;
  readonly expiresAt: string;
  readonly user: CurrentUser;
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
}

export interface RegistrationRequest extends LoginRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly consentAccepted: boolean;
}

export interface ResetPasswordRequest {
  readonly email: string;
  readonly token: string;
  readonly newPassword: string;
  readonly confirmPassword: string;
}

export interface AuthenticationMessage {
  readonly message: string;
}
