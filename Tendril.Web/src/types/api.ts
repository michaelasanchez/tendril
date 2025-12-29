// src/types/api.ts
export type Guid = string;

export interface Venue {
  id: Guid;
  name: string;
  address: string;
  website?: string;
}

export type PaginationType = "None" | "InfiniteScroll" | "NextButton";

export type ScraperState = "Unknown" | "Healthy" | "Unhealthy";

export interface ScraperDefinition {
  id: Guid;
  name: string;
  baseUrl: string;
  paginationType: PaginationType;
  schedule: string;
  state: ScraperState;
  lastSuccessUtc?: string | null;
  lastFailureUtc?: string | null;
  lastErrorMessage?: string | null;
  venueId?: Guid | null;
}

export type SelectorType =
  | "Container"
  | "Text"
  | "Attribute"
  | "Href"
  | "Src"
  | "Click"
  | "Hover"
  | "Scroll"
  | "Input"
  | "FollowLink"
  | "CaptureLink";

export interface ScraperSelector {
  id: Guid;
  scraperDefinitionId: Guid;
  fieldName: string;
  selector: string;
  order: number;
  root: boolean;
  type: SelectorType;
  attribute: string | null;
  delay: number | null;
  interactionValue: string | null;
  childScraperId: Guid | null;
  isPaginationTrigger: boolean;
}

export type TransformType =
  | "None"
  | "Trim"
  | "RegexExtract"
  | "RegexReplace"
  | "Split"
  | "Combine"
  | "ParseDate"
  | "ParseTime"
  | "ParseExact"
  | "ParseLoose"
  | "ToLower"
  | "ToUpper"
  | "Currency"
  | "SrcSetToUrl";

export interface ScraperMappingRule {
  id: Guid;
  scraperDefinitionId: Guid;
  targetField: string;
  sourceField: string;
  combineWithField: string | null;
  order: number;
  transformType: TransformType;
  format: string | null;
  regexPattern: string | null;
  regexReplacement: string | null;
  splitDelimiter: string | null;
}

export interface ScraperAttemptHistory {
  id: Guid;
  startTimeUtc: string;
  endTimeUtc: string | null;
  success: boolean;
  extracted: number;
  mapped: number;
  created: number;
  updated: number;
  errorMessage: string;
}

export interface Event {
  id: Guid;
  title: string;
  location: string;
  description?: string;
  startUtc: string;
  endUtc?: string | null;
  imageUrl?: string | null;
  detailsUrl?: string | null;
  ticketUrl?: string | null
  category?: string | null;
  venueName?: string | null;
  venueUrl?: string | null;
}

export interface ScrapeRunResponse {
  success: boolean;
  error?: string | null;
  raw?: unknown;
  mapped?: unknown;
}
