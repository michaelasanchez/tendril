import React, { useEffect, useRef, useState } from "react";
import { Table } from "react-bootstrap";
import { useNavigate } from "react-router-dom";
import { ScrapersApi } from "../api/scrapers";
import type { ScraperDefinition } from "../types/api";
import pageStyles from "./Page.module.css";
import styles from "./ScrapersPage.module.css";

type SortKey =
  | "name"
  | "baseUrl"
  | "state"
  | "lastSuccessUtc"
  | "lastFailureUtc";

type SortDirection = "asc" | "desc";

export const ScrapersPage: React.FC = () => {
  const [scrapers, setScrapers] = useState<ScraperDefinition[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [sort, setSort] = useState<{
    key: SortKey;
    direction: SortDirection;
  } | null>(null);

  const onSort = (key: SortKey) => {
    setSort((prev) => {
      if (!prev || prev.key !== key) {
        return { key, direction: "asc" };
      }

      return {
        key,
        direction: prev.direction === "asc" ? "desc" : "asc",
      };
    });
  };

  const sortIndicator = (key: SortKey) =>
    sort?.key === key ? <>&nbsp;{sort.direction === "asc" ? "▲" : "▼"}</> : "";

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

  const hasLoaded = useRef(false);

  useEffect(() => {
    if (hasLoaded.current) return;

    hasLoaded.current = true;
    void load();
  }, []);

  useEffect(() => {
    if (!sort) return;
    setScrapers((prev) => {
      const sorted = [...prev].sort((a, b) => {
        var aValue = a[sort.key];
        var bValue = b[sort.key];

        if (aValue === null || aValue === undefined) return 1;
        if (bValue === null || bValue === undefined) return -1;

        if (aValue < bValue) return sort.direction === "asc" ? -1 : 1;
        if (aValue > bValue) return sort.direction === "asc" ? 1 : -1;

        return 0;
      });

      return sorted;
    });
  }, [sort]);

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
      <div className={pageStyles.pageHeader}>
        <h2>Feeds</h2>
        <button onClick={() => navigate("/scrapers/new")}>New Feed</button>
      </div>

      {loading && <p>Loading…</p>}
      {error && <p className="error">{error}</p>}

      <Table className="data-table" hover>
        <thead>
          <tr>
            <th onClick={() => onSort("name")}>Name{sortIndicator("name")}</th>
            <th onClick={() => onSort("baseUrl")}>
              Base URL{sortIndicator("baseUrl")}
            </th>
            <th onClick={() => onSort("state")}>
              State{sortIndicator("state")}
            </th>
            <th onClick={() => onSort("lastSuccessUtc")}>
              Last Success{sortIndicator("lastSuccessUtc")}
            </th>
            <th onClick={() => onSort("lastFailureUtc")}>
              Last Failure{sortIndicator("lastFailureUtc")}
            </th>
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
              <td>
                <a href={s.baseUrl} target="_blank">{s.baseUrl}</a>
              </td>
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
