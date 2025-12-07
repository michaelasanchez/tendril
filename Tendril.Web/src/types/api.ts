// src/types/api.ts
export type Guid = string;

export interface Venue {
  id: Guid;
  name: string;
  address: string;
  website?: string;
}

export type ScraperState = "Unknown" | "Healthy" | "Unhealthy";

export interface ScraperDefinition {
  id: Guid;
  name: string;
  baseUrl: string;
  isDynamic: boolean;
  schedule: string;
  state: ScraperState;
  lastSuccessUtc?: string | null;
  lastFailureUtc?: string | null;
  lastErrorMessage?: string | null;
  venueId?: Guid | null;
}

export type SelectorType = "Navigate" | "Text" | "Href" | "Click" | "Hover";

export interface ScraperSelector {
  id: Guid;
  scraperDefinitionId: Guid;
  fieldName: string;
  selector: string;
  order: number;
  root: boolean;
  type: SelectorType;
  // if you later add ReturnMode, extend here
  // returnMode: "First" | "All";
}

export interface ScraperMappingRule {
  id: Guid;
  scraperDefinitionId: Guid;
  targetField: string;
  sourceField: string;
  combineWithField?: string | null;
  // if you use enum TransformType as string in JSON:
  transformType: string;
}

export interface Event {
  id: Guid;
  venueId: Guid;
  title: string;
  description?: string;
  startUtc: string;
  endUtc?: string | null;
  ticketUrl?: string | null;
  category?: string | null;
  imageUrl?: string | null;
  scrapedAtUtc: string;
  venue?: Venue;
}

export interface ScrapeRunResponse {
  success: boolean;
  error?: string | null;
  raw?: unknown;
  mapped?: unknown;
}
