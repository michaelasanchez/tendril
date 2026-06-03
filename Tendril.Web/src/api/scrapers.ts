// src/scrapers.ts
import type {
  ApiParameter,
  EventRevision,
  ExecutionMode,
  ExtractionStrategy,
  Guid,
  HttpMethod,
  PagedResponse,
  PaginationType,
  ScrapedEventRaw,
  ScraperAction,
  ScraperAttemptHistory,
  ScraperClassificationRule,
  ScraperDefinition,
  ScraperMappingRule,
  ScraperSummary,
  ScrapeRunResponse,
} from '../types/api';
import { apiDelete, apiGet, apiPost, apiPut } from './client';

export interface CreateScraperRequest {
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
  venueId?: Guid | null;
  method?: HttpMethod | null;
  parameters?: ApiParameter[] | null;
}

export interface UpdateScraperRequest extends Partial<CreateScraperRequest> {}

export type ReorderDirection = 'up' | 'down';

export interface ReorderScraperRequest {
  direction: ReorderDirection;
}

export const ScrapersApi = {
  getAll(signal?: AbortSignal): Promise<ScraperDefinition[]> {
    return apiGet('/scrapers', signal);
  },

  getById(id: Guid): Promise<ScraperDefinition> {
    return apiGet(`/scrapers/${id}`);
  },

  create(req: CreateScraperRequest): Promise<ScraperDefinition> {
    return apiPost('/scrapers', req);
  },

  update(id: Guid, req: UpdateScraperRequest): Promise<void> {
    return apiPut(`/scrapers/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/scrapers/${id}`);
  },

  // Api Parameter
  getMApiParameter(scraperId: Guid): Promise<ApiParameter[]> {
    return apiGet(`/scrapers/${scraperId}/api-parameters`);
  },

  createApiParameter(
    scraperId: Guid,
    req: Omit<ApiParameter, 'id' | 'scraperDefinitionId'>,
  ): Promise<ApiParameter> {
    return apiPost(`/scrapers/${scraperId}/api-parameters`, req);
  },

  updateApiParameter(
    scraperId: Guid,
    ruleId: Guid,
    req: Partial<ApiParameter>,
  ): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/api-parameters/${ruleId}`, req);
  },

  deleteApiParameter(scraperId: Guid, ruleId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/api-parameters/${ruleId}`);
  },

  // Actions
  getActions(scraperId: Guid): Promise<ScraperAction[]> {
    return apiGet(`/scrapers/${scraperId}/actions`);
  },

  createAction(
    scraperId: Guid,
    req: Omit<ScraperAction, 'id' | 'scraperDefinitionId'>,
  ): Promise<ScraperAction> {
    return apiPost(`/scrapers/${scraperId}/actions`, req);
  },

  updateAction(
    scraperId: Guid,
    actionId: Guid,
    req: Partial<ScraperAction>,
  ): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/actions/${actionId}`, req);
  },

  deleteAction(scraperId: Guid, actionId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/actions/${actionId}`);
  },

  reorderAction(
    scraperId: Guid,
    actionId: Guid,
    req: ReorderScraperRequest,
  ): Promise<void> {
    return apiPost(`/scrapers/${scraperId}/actions/${actionId}/reorder`, req);
  },

  // Classification rules
  getClassificationRules(
    scraperId: Guid,
    signal?: AbortSignal,
  ): Promise<ScraperClassificationRule[]> {
    return apiGet(`/scrapers/${scraperId}/classification-rules`, signal);
  },

  createClassificationRule(
    scraperId: Guid,
    req: Omit<ScraperClassificationRule, 'id' | 'scraperDefinitionId'>,
  ): Promise<ScraperClassificationRule> {
    return apiPost(`/scrapers/${scraperId}/classification-rules`, req);
  },

  updateClassificationRule(
    scraperId: Guid,
    ruleId: Guid,
    req: Partial<ScraperClassificationRule>,
  ): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/classification-rules/${ruleId}`, req);
  },

  deleteClassificationRule(scraperId: Guid, ruleId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/classification-rules/${ruleId}`);
  },

  // Mapping rules
  getMappingRules(scraperId: Guid): Promise<ScraperMappingRule[]> {
    return apiGet(`/scrapers/${scraperId}/mapping-rules`);
  },

  createMappingRule(
    scraperId: Guid,
    req: Omit<ScraperMappingRule, 'id' | 'scraperDefinitionId'>,
  ): Promise<ScraperMappingRule> {
    return apiPost(`/scrapers/${scraperId}/mapping-rules`, req);
  },

  updateMappingRule(
    scraperId: Guid,
    ruleId: Guid,
    req: Partial<ScraperMappingRule>,
  ): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/mapping-rules/${ruleId}`, req);
  },

  deleteMappingRule(scraperId: Guid, ruleId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/mapping-rules/${ruleId}`);
  },

  // Attempt Histories
  getAttemptHistoriesByScraperId(scraperId: Guid): Promise<ScraperAttemptHistory[]> {
    return apiGet(`/attempt-histories/scrapers/${scraperId}`);
  },

  getPagedAttemptHistories(
    scraperId?: Guid,
    limit?: number,
    cursor?: string,
    signal?: AbortSignal,
  ): Promise<PagedResponse<ScraperAttemptHistory>> {
    const params = new URLSearchParams();
    if (scraperId) params.append('scraperId', scraperId);
    if (limit) params.append('limit', limit.toString());
    if (cursor) params.append('cursor', cursor);

    const query = params.toString() ? `?${params.toString()}` : '';

    console.log('QUERY', query);
    return apiGet(`/attempt-histories/paged${query}`, signal);
  },

  // Gets a single attempt's metadata
  getAttemptById(
    attemptId: string,
  ): Promise<ScraperAttemptHistory> {
    return apiGet(`/attempt-histories/${attemptId}`);
  },

  // Gets the raw JSON data for a specific run (Paginated)
  getRawEventsByAttempt(
    attemptId: string,
    limit?: number,
    cursor?: string,
  ): Promise<PagedResponse<ScrapedEventRaw>> {
    const params = new URLSearchParams();
    if (limit) params.append('limit', limit.toString());
    if (cursor) params.append('cursor', cursor);

    const query = params.toString() ? `?${params.toString()}` : '';
    return apiGet(
      `/attempt-histories/${attemptId}/raw-events${query}`,
    );
  },

  // Gets the list of actual changes made during a run
  getRevisionsByAttempt(
    attemptId: string,
  ): Promise<EventRevision[]> {
    return apiGet(
      `/attempt-histories/${attemptId}/revisions`,
    );
  },

  // Summaries
  getFeedSummaries(signal?: AbortSignal): Promise<ScraperSummary[]> {
    return apiGet(`/scrapers/summaries`, signal);
  },

  getScraperSummary(
    scraperId: Guid,
    signal?: AbortSignal,
  ): Promise<ScraperSummary> {
    return apiGet(`/scrapers/summaries/${scraperId}`, signal);
  },

  // Runs
  testActions(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/test-actions`);
  },

  testMapping(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/test-mapping`);
  },

  testRun(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/test-run`);
  },

  runNow(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/run-now`);
  },
};
