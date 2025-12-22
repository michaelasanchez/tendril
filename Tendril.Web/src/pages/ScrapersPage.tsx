import React, { useEffect, useState } from "react";
import { Container, Table } from "react-bootstrap";
import { useNavigate } from "react-router-dom";
import { ScrapersApi } from "../api/scrapers";
import type { ScraperDefinition } from "../types/api";
import styles from "./ScrapersPage.module.css";

export const ScrapersPage: React.FC = () => {
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await ScrapersApi.getAll();
      setScrapers(data);
    } catch (e: any) {
      setError(e.message ?? "Error loading scrapers.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleRunNow = async (id: string) => {
    if (!window.confirm("Run this scraper now?")) return;
    try {
      await ScrapersApi.runNow(id);
      await load();
    } catch (e: any) {
      alert(e.message ?? "Run failed.");
    }
  };

  return (
    <section>
        <div className={styles.pageHeader}>
          <h2>Feeds</h2>
          <button onClick={() => navigate("/scrapers/new")}>New Feed</button>
        </div>

        {loading && <p>Loading…</p>}
        {error && <p className="error">{error}</p>}

        <Table className="data-table" hover>
          <thead>
            <tr>
              <th>Name</th>
              <th>Base URL</th>
              <th>State</th>
              <th>Last Success</th>
              <th>Last Failure</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {scrapers.map((s) => (
              <tr key={s.id}>
                <td className={styles.center}>
                  <button
                    className="link"
                    onClick={() => navigate(`/scrapers/${s.id}`)}
                  >
                    {s.name}
                  </button>
                </td>
                <td>{s.baseUrl}</td>
                <td>{s.state}</td>
                <td>{s.lastSuccessUtc ?? "-"}</td>
                <td>{s.lastFailureUtc ?? "-"}</td>
                <td>
                  <button onClick={() => handleRunNow(s.id)}>Run Now</button>
                </td>
              </tr>
            ))}
            {scrapers.length === 0 && !loading && (
              <tr>
                <td colSpan={6}>No scrapers defined yet.</td>
              </tr>
            )}
          </tbody>
        </Table>
    </section>
  );
};
