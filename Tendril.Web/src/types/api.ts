// src/types/api.ts
export type Guid = string;

export interface Category {
  id: Guid;
  name: string;
  description: string;
}

export interface Tag {
  id: Guid;
  name: string;
}

export interface Venue {
  id: Guid;
  name: string;
  address: string;
  website?: string;
}

export type HttpMethod = 'Get' | 'Post';

export type ApiParameterSource = 'Static' | 'Parent';

export type ApiParameterTarget = 'Query' | 'Header' | 'Body';

export interface ApiParameter {
  id: Guid;
  key: string;
  template: string;
  source: ApiParameterSource;
  target: ApiParameterTarget;
  isRequired: boolean;
}

export type ExecutionMode = 'Static' | 'Dynamic' | 'Api';

export type ExtractionStrategy =
  | 'Css'
  | 'JsonLd'
  | 'JsonPath'
  | 'Regex'
  | 'XPath';

export type PaginationType = 'None' | 'InfiniteScroll' | 'NextButton';

export type ScraperState = 'Unknown' | 'Healthy' | 'Unhealthy';

export interface ParentScraper {
  id: Guid;
  name: string;
}

export interface ScraperDefinition {
  id: Guid;
  name: string;
  baseUrl: string;
  isEventFeed: boolean;
  disabled: boolean;
  notes: string;
  hasSuggestions: boolean;
  requiresReview: boolean;
  executionMode: ExecutionMode;
  extractionStrategy: ExtractionStrategy;
  paginationType: PaginationType;
  useYearTracking: boolean;
  useHeadlessBrowser: boolean;
  state: ScraperState;
  lastSuccessUtc?: string | null;
  lastFailureUtc?: string | null;
  lastErrorMessage?: string | null;
  venueId?: Guid | null;
  parents?: ParentScraper[] | null;

  method?: HttpMethod | null;
  parameters?: ApiParameter[] | null;
}

export type ActionType =
  | 'Container'
  | 'Text'
  | 'Attribute'
  | 'Click'
  | 'Hover'
  | 'Scroll'
  | 'Input'
  | 'CaptureLink'
  | 'FollowLink'
  | 'ConstantValue'
  | 'CallApi';

export interface ScraperAction {
  id: Guid;
  scraperDefinitionId: Guid;
  name: string;
  fieldName: string;
  selector: string;
  order: number;
  root: boolean;
  type: ActionType;
  attribute: string | null;
  delay: number | null;
  constantValue: string | null;
  interactionValue: string | null;
  childScraperId: Guid | null;
  ignoreDuplicateUrls: boolean;
  isPaginationTrigger: boolean;
  disabled: boolean;
}

export type TransformType =
  | 'None'
  | 'Constant'
  | 'Trim'
  | 'RegexExtract'
  | 'RegexReplace'
  | 'Split'
  | 'Combine'
  | 'ParseDate'
  | 'ParseTime'
  | 'ParseExact'
  | 'ToLower'
  | 'ToUpper'
  | 'Currency'
  | 'DecodeHtml'
  | 'StripHtml'
  | 'SrcSetToUrl';

export interface ScraperMappingRule {
  id: Guid;
  scraperDefinitionId: Guid;
  targetField: string;
  sourceField: string;
  combineWithField: string | null;
  order: number;
  transformType: TransformType;
  constantValue: string | null;
  format: string | null;
  regexPattern: string | null;
  regexReplacement: string | null;
  splitDelimiter: string | null;
  disabled: boolean;
}

export type ConditionType =
  | 'Default'
  | 'Equals'
  | 'NotEquals'
  | 'Contains'
  | 'NotContains'
  | 'StartsWith'
  | 'EndsWith'
  | 'GreaterThan'
  | 'LessThan'
  | 'GreaterThanOrEqualTo'
  | 'LessThanOrEqualTo'
  | 'RegexMatch'
  | 'RegexNotMatch';

export interface RuleAssignment {
  id: Guid;
  categoryId: Guid | null;
  tagId: Guid | null;
}

export interface ScraperClassificationRule {
  id: Guid;
  scraperDefinitionId: Guid;
  order: number;
  disabled: boolean;
  sourceJsonPath: string;
  conditionType: ConditionType;
  conditionValue: string;
  assignments: RuleAssignment[];
}

export interface ScraperAttemptHistory {
  id: Guid;
  startTimeUtc: string;
  endTimeUtc: string | null;
  groupKey: string;
  success: boolean;
  extracted: number;
  mapped: number;
  created: number;
  updated: number;
  skipped: number;
  errored: number;
  errorMessage: string;
}

export interface MappingSummary {
  title: boolean;
  description: boolean;
  location: boolean;
  venue: boolean;
  startUtc: boolean;
  endUtc: boolean;
  minPrice: boolean;
  maxPrice: boolean;
  detailsUrl: boolean;
  imageUrl: boolean;
  ticketUrl: boolean;
}

export interface ScraperSummary {
  name: string;
  mapping: MappingSummary;
}

export interface PagedResponse {
  items: Event[];
  nextCursor: Guid | null;
  hasNextPage: boolean;
  totalCount: number;
}

export interface EventResponse extends PagedResponse {
  categoryIds: Guid[];
  venueIds: Guid[];
}

export type EventStatus = 'Pending' | 'Published' | 'Suppressed';

export interface Event {
  id: Guid;
  title: string;
  location: string;
  description: string;
  startUtc: string;
  showStartTime: boolean;
  endUtc: string | null;
  showEndTime: boolean | null;
  minPrice: number | null;
  maxPrice: number | null;
  imageUrl: string | null;
  detailsUrl: string | null;
  ticketUrl: string | null;
  categoryId: Guid | null;
  categoryName: string | null;
  venueName: string;
  venueUrl: string;
  status: EventStatus;
  isReviewRequired: boolean;
  updatedAtUtc: string;
  reviewRequiredAtUtc: string;
}

export interface ScrapeRunResponse {
  success: boolean;
  error?: string | null;
  raw?: unknown;
  mapped?: unknown;
}
