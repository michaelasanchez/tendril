// src/pages/ScraperEditorPage.tsx
import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ScrapersApi } from "../api/scrapers";
import { VenuesApi } from "../api/venues";
import { ScraperMappingRulesTab } from "../scrapers/ScraperMappingRulesTab";
import { ScraperRunsTab } from "../scrapers/ScraperRunsTab";
import { ScraperSelectorsTab } from "../scrapers/ScraperSelectorsTab";
import type {
  Guid,
  ScraperDefinition,
  ScraperSelector,
  Venue,
} from "../types/api";

type TabKey = "general" | "selectors" | "mapping" | "runs";

export const ScraperEditorPage: React.FC = () => {
  const { scraperId } = useParams();
  const navigate = useNavigate();
  const [scraper, setScraper] = useState<ScraperDefinition | null>(null);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [tab, setTab] = useState<TabKey>("general");
  const [error, setError] = useState<string | null>(null);
  const isNew = scraperId === "new";

  /* Selectors */
  const [selectors, setSelectors] = useState<ScraperSelector[]>([]);

  const loadSelectors = async () => {
    if (!scraperId) return;
    const data = await ScrapersApi.getSelectors(scraperId);
    setSelectors(data);
  };

  useEffect(() => {
    void loadSelectors();
  }, [scraperId]);
  /* Selectors */

  useEffect(() => {
    const load = async () => {
      setError(null);
      try {
        const [vs, sc] = await Promise.all([
          VenuesApi.getAll(),
          isNew || !scraperId
            ? Promise.resolve<ScraperDefinition | null>(null)
            : ScrapersApi.getById(scraperId as Guid),
        ]);
        setVenues(vs);
        setScraper(
          sc ?? {
            id: "" as Guid,
            name: "",
            baseUrl: "",
            isDynamic: true,
            schedule: "0 */6 * * *",
            state: "Unknown",
            lastSuccessUtc: null,
            lastFailureUtc: null,
            lastErrorMessage: null,
            venueId: null,
          }
        );
      } catch (e: any) {
        setError(e.message ?? "Error loading scraper.");
      }
    };
    void load();
  }, [scraperId, isNew]);

  const handleSaveGeneral = async () => {
    if (!scraper) return;
    try {
      if (isNew) {
        const created = await ScrapersApi.create({
          name: scraper.name,
          baseUrl: scraper.baseUrl,
          isDynamic: scraper.isDynamic,
          venueId: scraper.venueId ?? undefined,
          schedule: scraper.schedule,
        });
        navigate(`/scrapers/${created.id}`);
      } else if (scraperId) {
        await ScrapersApi.update(scraperId as Guid, {
          name: scraper.name,
          baseUrl: scraper.baseUrl,
          isDynamic: scraper.isDynamic,
          venueId: scraper.venueId ?? undefined,
          schedule: scraper.schedule,
        });
      }
      alert("Saved.");
    } catch (e: any) {
      alert(e.message ?? "Save failed.");
    }
  };

  if (!scraper) return <p>Loading…</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <div className="page-header">
        <h2>{isNew ? "New Scraper" : `Edit Scraper – ${scraper.name}`}</h2>
        <button onClick={() => navigate("/scrapers")}>Back</button>
      </div>

      <div className="tabs">
        <button
          className={tab === "general" ? "tab active" : "tab"}
          onClick={() => setTab("general")}
        >
          General
        </button>
        <button
          className={tab === "selectors" ? "tab active" : "tab"}
          onClick={() => setTab("selectors")}
          disabled={isNew}
        >
          Selectors
        </button>
        <button
          className={tab === "mapping" ? "tab active" : "tab"}
          onClick={() => setTab("mapping")}
          disabled={isNew}
        >
          Mapping Rules
        </button>
        <button
          className={tab === "runs" ? "tab active" : "tab"}
          onClick={() => setTab("runs")}
          disabled={isNew}
        >
          Runs
        </button>
      </div>

      {tab === "general" && (
        <div className="card">
          <div className="form-grid">
            <label>
              Name
              <input
                value={scraper.name}
                onChange={(e) =>
                  setScraper({ ...scraper, name: e.target.value })
                }
              />
            </label>
            <label>
              Base URL
              <input
                value={scraper.baseUrl}
                onChange={(e) =>
                  setScraper({ ...scraper, baseUrl: e.target.value })
                }
              />
            </label>
            <label>
              Schedule (cron)
              <input
                value={scraper.schedule}
                onChange={(e) =>
                  setScraper({ ...scraper, schedule: e.target.value })
                }
              />
            </label>
            <label>
              Venue
              <select
                value={scraper.venueId ?? ""}
                onChange={(e) =>
                  setScraper({
                    ...scraper,
                    venueId: e.target.value ? (e.target.value as Guid) : null,
                  })
                }
              >
                <option value="">(none)</option>
                {venues.map((v) => (
                  <option key={v.id} value={v.id}>
                    {v.name}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Dynamic?
              <input
                type="checkbox"
                checked={scraper.isDynamic}
                onChange={(e) =>
                  setScraper({ ...scraper, isDynamic: e.target.checked })
                }
              />
            </label>
          </div>
          <button onClick={handleSaveGeneral}>Save</button>
        </div>
      )}

      {tab === "selectors" && !isNew && (
        <ScraperSelectorsTab
          scraperId={scraperId as Guid}
          selectors={selectors}
          refresh={loadSelectors}
        />
      )}

      {tab === "mapping" && !isNew && (
        <ScraperMappingRulesTab
          scraperId={scraperId as Guid}
          selectors={selectors}
        />
      )}

      {tab === "runs" && !isNew && (
        <ScraperRunsTab scraperId={scraperId as Guid} />
      )}
    </section>
  );
};
