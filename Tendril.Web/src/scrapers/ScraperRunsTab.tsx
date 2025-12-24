// src/scrapers/ScraperRunsTab.tsx
import React, { useEffect, useState } from "react";
import { Card, Table } from "react-bootstrap";
import { ScrapersApi } from "../api/scrapers";
import formStyles from "../components/form/Form.module.css";
import type {
  Guid,
  ScraperAttemptHistory,
  ScrapeRunResponse,
} from "../types/api";

interface Props {
  scraperId: Guid;
}

export const ScraperRunsTab: React.FC<Props> = ({ scraperId }) => {
  const [result, setResult] = useState<ScrapeRunResponse | null>(null);
  const [loading, setLoading] = useState(false);

  const [attempts, setAttempts] = useState<ScraperAttemptHistory[]>([]);

  const load = async () => {
    if (scraperId !== "new") {
      const attempts = await ScrapersApi.getAttemptHistories(scraperId);
      setAttempts(attempts);
    }
  };

  useEffect(() => {
    void load();
  }, [scraperId]);

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
        error: e.message ?? "Error running scraper.",
      });
    } finally {
      setLoading(false);
    }
  };

  return (
    <Card>
      <Card.Body>
        <h3>Test Tools</h3>
        <div className={formStyles.buttonRow}>
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
      </Card.Body>
      <Card.Body>
        <h3>Attempts</h3>
        <Table className="data-table" hover>
          <thead>
            <tr>
              <th>Start</th>
              <th>End</th>
              <th>Success</th>
              <th>Extracted</th>
              <th>Mapped</th>
              <th>Created</th>
              <th>Updated</th>
              <th>Error Message</th>
            </tr>
          </thead>
          <tbody>
            {attempts.map((a) => (
              <tr key={a.id}>
                <td>{new Date(a.startTimeUtc).toLocaleString()}</td>
                <td>
                  {Boolean(a.endTimeUtc) &&
                    new Date(a.endTimeUtc as string).toLocaleString()}
                </td>
                <td>{a.success ? "Yes" : "No"}</td>
                <td>{a.extracted}</td>
                <td>{a.mapped}</td>
                <td>{a.created}</td>
                <td>{a.updated}</td>
                <td>{a.errorMessage}</td>
              </tr>
            ))}
          </tbody>
        </Table>
      </Card.Body>
    </Card>
  );
};
