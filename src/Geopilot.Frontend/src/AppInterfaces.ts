export type Language = "de" | "fr" | "it" | "en";

export interface TranslationFunction {
  (key: string): string;
}

export type ModalContentType = "markdown" | "raw";

export interface ClientSettings {
  authCache: {
    cacheLocation: string;
    storeAuthStateInCookie: boolean;
  };
  authScopes: string[];
  application: {
    name: string;
    logo: string;
    favicon: string;
  };
  vendor: {
    name: string;
    logo: string;
    url: string;
  };
  theme: object;
}

export interface Validation {
  allowedFileExtensions: string[];
}

export interface ErrorResponse {
  status: string;
  detail: string;
}

export interface Coordinate {
  x: number;
  y: number;
}

export interface Mandate {
  id: number;
  name: string;
  fileTypes: string[];
  spatialExtent: Coordinate[];
  organisations?: Organisation[];
  deliveries?: Delivery[];
}

export interface Organisation {
  id: number;
  name: string;
  mandates?: Mandate[];
  users?: User[];
}

export interface Delivery {
  id: number;
  date: Date;
  declaringUser: User;
  mandate: Mandate;
  comment: string;
}

export interface User {
  id: number;
  fullName: string;
  isAdmin: boolean;
  email: string;
  organisations?: Organisation[];
  deliveries?: Delivery[];
}
