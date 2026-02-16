// src/scrapers.ts
import type {
  ExecutionMode,
  ExtractionStrategy,
  Guid,
  PaginationType,
  ScraperAttemptHistory,
  ScraperClassificationRule,
  ScraperDefinition,
  ScraperMappingRule,
  ScraperSelector,
  ScrapeRunResponse
} from "../types/api";
import { apiDelete, apiGet, apiPost, apiPut } from "./client";

export interface CreateScraperRequest {
  name: string;
  baseUrl: string;
  disabled: boolean;
  notes: string;
  executionMode: ExecutionMode;
  extractionStrategy: ExtractionStrategy;
  paginationType: PaginationType;
  venueId?: Guid | null;
  schedule?: string;
}

export interface UpdateScraperRequest extends Partial<CreateScraperRequest> {}

export const ScrapersApi = {
  getAll(): Promise<ScraperDefinition[]> {
    return apiGet("/scrapers");
  },

  getById(id: Guid): Promise<ScraperDefinition> {
    return apiGet(`/scrapers/${id}`);
  },

  create(req: CreateScraperRequest): Promise<ScraperDefinition> {
    return apiPost("/scrapers", req);
  },

  update(id: Guid, req: UpdateScraperRequest): Promise<void> {
    return apiPut(`/scrapers/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/scrapers/${id}`);
  },

  // Selectors
  getSelectors(scraperId: Guid): Promise<ScraperSelector[]> {
    return apiGet(`/scrapers/${scraperId}/selectors`);
  },

  createSelector(scraperId: Guid, req: Omit<ScraperSelector, "id" | "scraperDefinitionId">): Promise<ScraperSelector> {
    return apiPost(`/scrapers/${scraperId}/selectors`, req);
  },

  updateSelector(scraperId: Guid, selectorId: Guid, req: Partial<ScraperSelector>): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/selectors/${selectorId}`, req);
  },

  deleteSelector(scraperId: Guid, selectorId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/selectors/${selectorId}`);
  },

  // Classification rules
  getClassificationRules(scraperId: Guid, signal?: AbortSignal): Promise<ScraperClassificationRule[]> {
    return apiGet(`/scrapers/${scraperId}/classification-rules`, signal);
  },

  createClassificationRule(scraperId: Guid, req: Omit<ScraperClassificationRule, "id" | "scraperDefinitionId">): Promise<ScraperClassificationRule> {
    return apiPost(`/scrapers/${scraperId}/classification-rules`, req);
  },

  updateClassificationRule(scraperId: Guid, ruleId: Guid, req: Partial<ScraperClassificationRule>): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/classification-rules/${ruleId}`, req);
  },

  deleteClassificationRule(scraperId: Guid, ruleId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/classification-rules/${ruleId}`);
  },

  // Mapping rules
  getMappingRules(scraperId: Guid): Promise<ScraperMappingRule[]> {
    return apiGet(`/scrapers/${scraperId}/mapping-rules`);
  },

  createMappingRule(scraperId: Guid, req: Omit<ScraperMappingRule, "id" | "scraperDefinitionId">): Promise<ScraperMappingRule> {
    return apiPost(`/scrapers/${scraperId}/mapping-rules`, req);
  },

  updateMappingRule(scraperId: Guid, ruleId: Guid, req: Partial<ScraperMappingRule>): Promise<void> {
    return apiPut(`/scrapers/${scraperId}/mapping-rules/${ruleId}`, req);
  },

  deleteMappingRule(scraperId: Guid, ruleId: Guid): Promise<void> {
    return apiDelete(`/scrapers/${scraperId}/mapping-rules/${ruleId}`);
  },

  // Attempt Histories
  getAttemptHistories(scraperId: Guid): Promise<ScraperAttemptHistory[]> {
    return apiGet(`/scrapers/${scraperId}/attempt-histories`);
  },

  // Runs
  testSelectors(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/test-selectors`);
  },

  testMapping(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/test-mapping`);
  },

  testRun(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/test-run`);
  },

  runNow(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/scrapers/${scraperId}/runs/run-now`);
  }
};
