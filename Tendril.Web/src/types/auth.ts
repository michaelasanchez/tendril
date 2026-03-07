declare global {
  interface Window {
    google: any;
  }
}

export interface GoogleAuthCodeResponse {
  code: string;
  scope: string;
  state?: string;
}

export interface UserProfile {
  email: string;
  pictureUrl: string;
  name: string;
}
