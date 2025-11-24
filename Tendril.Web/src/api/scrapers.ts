// src/api/scrapers.ts
import { apiGet, apiPost, apiPut, apiDelete } from "./client";
import type {
  ScraperDefinition,
  ScraperSelector,
  ScraperMappingRule,
  ScrapeRunResponse,
  Guid
} from "../types/api";

export interface CreateScraperRequest {
  name: string;
  baseUrl: string;
  isDynamic: boolean;
  venueId?: Guid | null;
  schedule?: string;
}

export interface UpdateScraperRequest extends Partial<CreateScraperRequest> {}

export const ScrapersApi = {
  getAll(): Promise<ScraperDefinition[]> {
    return apiGet("/api/scrapers");
  },

  getById(id: Guid): Promise<ScraperDefinition> {
    return apiGet(`/api/scrapers/${id}`);
  },

  create(req: CreateScraperRequest): Promise<ScraperDefinition> {
    return apiPost("/api/scrapers", req);
  },

  update(id: Guid, req: UpdateScraperRequest): Promise<void> {
    return apiPut(`/api/scrapers/${id}`, req);
  },

  delete(id: Guid): Promise<void> {
    return apiDelete(`/api/scrapers/${id}`);
  },

  // Selectors
  getSelectors(scraperId: Guid): Promise<ScraperSelector[]> {
    return apiGet(`/api/scrapers/${scraperId}/selectors`);
  },

  createSelector(scraperId: Guid, req: Omit<ScraperSelector, "id" | "scraperDefinitionId">): Promise<ScraperSelector> {
    return apiPost(`/api/scrapers/${scraperId}/selectors`, req);
  },

  updateSelector(scraperId: Guid, selectorId: Guid, req: Partial<ScraperSelector>): Promise<void> {
    return apiPut(`/api/scrapers/${scraperId}/selectors/${selectorId}`, req);
  },

  deleteSelector(scraperId: Guid, selectorId: Guid): Promise<void> {
    return apiDelete(`/api/scrapers/${scraperId}/selectors/${selectorId}`);
  },

  // Mapping rules
  getMappingRules(scraperId: Guid): Promise<ScraperMappingRule[]> {
    return apiGet(`/api/scrapers/${scraperId}/mapping-rules`);
  },

  createMappingRule(scraperId: Guid, req: Omit<ScraperMappingRule, "id" | "scraperDefinitionId">): Promise<ScraperMappingRule> {
    return apiPost(`/api/scrapers/${scraperId}/mapping-rules`, req);
  },

  updateMappingRule(scraperId: Guid, ruleId: Guid, req: Partial<ScraperMappingRule>): Promise<void> {
    return apiPut(`/api/scrapers/${scraperId}/mapping-rules/${ruleId}`, req);
  },

  deleteMappingRule(scraperId: Guid, ruleId: Guid): Promise<void> {
    return apiDelete(`/api/scrapers/${scraperId}/mapping-rules/${ruleId}`);
  },

  // Runs
  testSelectors(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/api/scrapers/${scraperId}/runs/test-selectors`);
  },

  testMapping(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/api/scrapers/${scraperId}/runs/test-mapping`);
  },

  testRun(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/api/scrapers/${scraperId}/runs/test-run`);
  },

  runNow(scraperId: Guid): Promise<ScrapeRunResponse> {
    return apiPost(`/api/scrapers/${scraperId}/runs/run-now`);
  }
};
