// src/scrapers/ScraperRunsTab.tsx
import React, { useState } from "react";
import type { Guid, ScrapeRunResponse } from "../types/api";
import { ScrapersApi } from "../api/scrapers";

interface Props {
  scraperId: Guid;
}

export const ScraperRunsTab: React.FC<Props> = ({ scraperId }) => {
  const [result, setResult] = useState<ScrapeRunResponse | null>(null);
  const [loading, setLoading] = useState(false);

  const run = async (kind: "selectors" | "mapping" | "test" | "now") => {
    setLoading(true);
    setResult(null);
    try {
      let res: ScrapeRunResponse;
      switch (kind) {
        case "selectors":
          res = await ScrapersApi.testSelectors(scraperId);
          break;
        case "mapping":
          res = await ScrapersApi.testMapping(scraperId);
          break;
        case "test":
          res = await ScrapersApi.testRun(scraperId);
          break;
        case "now":
        default:
          res = await ScrapersApi.runNow(scraperId);
          break;
      }
      setResult(res);
    } catch (e: any) {
      setResult({
        success: false,
        error: e.message ?? "Error running scraper."
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="card">
      <h3>Runs / Test Tools</h3>
      <div className="button-row">
        <button disabled={loading} onClick={() => run("selectors")}>
          Test Selectors
        </button>
        <button disabled={loading} onClick={() => run("mapping")}>
          Test Mapping
        </button>
        <button disabled={loading} onClick={() => run("test")}>
          Test Run (no DB write)
        </button>
        <button disabled={loading} onClick={() => run("now")}>
          Run Now (persist)
        </button>
      </div>

      {loading && <p>Running…</p>}

      {result && (
        <div className="run-result">
          <p>
            Success: <strong>{result.success ? "Yes" : "No"}</strong>
          </p>
          {result.error && <p className="error">Error: {result.error}</p>}
          <details>
            <summary>Raw</summary>
            <pre>{JSON.stringify(result.raw, null, 2)}</pre>
          </details>
          <details>
            <summary>Mapped</summary>
            <pre>{JSON.stringify(result.mapped, null, 2)}</pre>
          </details>
        </div>
      )}
    </div>
  );
};
