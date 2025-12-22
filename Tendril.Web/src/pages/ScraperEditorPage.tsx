// src/pages/ScraperEditorPage.tsx
import React, { useEffect, useState } from "react";
import { Card, Form, Nav, Tab } from "react-bootstrap";
import { useNavigate, useParams } from "react-router-dom";
import { ScrapersApi } from "../api/scrapers";
import { VenuesApi } from "../api/venues";
import { FormInput, FormSelect } from "../components/form";
import { ScraperMappingRulesTab } from "../scrapers/ScraperMappingRulesTab";
import { ScraperRunsTab } from "../scrapers/ScraperRunsTab";
import { ScraperSelectorsTab } from "../scrapers/ScraperSelectorsTab";
import formStyles from "../components/form/Form.module.css";
import type {
  Guid,
  ScraperDefinition,
  ScraperSelector,
  Venue,
} from "../types/api";
import styles from "./ScraperEditorPage.module.css";

type TabKey = "general" | "selectors" | "mapping" | "runs";

export const ScraperEditorPage: React.FC = () => {
  const { scraperId } = useParams();
  const navigate = useNavigate();
  const [scraper, setScraper] = useState<ScraperDefinition | null>(null);
  const [venues, setVenues] = useState<Venue[]>([]);
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
      <div className={styles.pageHeader}>
        <h2>{isNew ? "New Scraper" : `Edit Scraper – ${scraper.name}`}</h2>
        <button onClick={() => navigate("/scrapers")}>Back</button>
      </div>

      <Tab.Container defaultActiveKey="general">
        <Nav variant="pills" className={styles.tabs}>
          <button disabled={isNew}>
            <Nav.Link eventKey="general">General</Nav.Link>
          </button>
          <button disabled={isNew}>
            <Nav.Link eventKey="selectors">Selectors</Nav.Link>
          </button>
          <button disabled={isNew}>
            <Nav.Link eventKey="mapping">Mapping Rules</Nav.Link>
          </button>
          <button disabled={isNew}>
            <Nav.Link eventKey="runs">Runs</Nav.Link>
          </button>
        </Nav>

        <Tab.Content>
          <Tab.Pane eventKey="general">
            <Card>
              <Card.Body>
                <Form className={formStyles.form}>
                  <FormInput
                    label="Name"
                    value={scraper.name}
                    onChange={(value) =>
                      setScraper({ ...scraper, name: value })
                    }
                  />
                  <FormInput
                    label="Base URL"
                    value={scraper.baseUrl}
                    onChange={(value) =>
                      setScraper({ ...scraper, baseUrl: value })
                    }
                  />
                  {/* <FormInput
                label="Schedule (cron)"
                value={scraper.schedule}
                onChange={(value) =>
                  setScraper({ ...scraper, schedule: value })
                }
              /> */}
                  <FormSelect
                    label="Venue"
                    value={scraper.venueId ?? ""}
                    onChange={(venueId) =>
                      setScraper({
                        ...scraper,
                        venueId: venueId ? (venueId as Guid) : null,
                      })
                    }
                    options={[{ value: "", label: "(none)" }].concat(
                      venues.map((v) => ({ value: v.id, label: v.name }))
                    )}
                  />
                  {/* <FormCheck
                label="Dynamic"
                checked={scraper.isDynamic}
                onChange={(checked) =>
                  setScraper({ ...scraper, isDynamic: checked })
                }
              /> */}
                  <div>
                    <button onClick={handleSaveGeneral}>Save</button>
                  </div>
                </Form>
              </Card.Body>
            </Card>
          </Tab.Pane>

          <Tab.Pane eventKey="selectors">
            <ScraperSelectorsTab
              scraperId={scraperId as Guid}
              selectors={selectors}
              refresh={loadSelectors}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="mapping">
            <ScraperMappingRulesTab
              scraperId={scraperId as Guid}
              selectors={selectors}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="runs">
            <ScraperRunsTab scraperId={scraperId as Guid} />
          </Tab.Pane>
        </Tab.Content>
      </Tab.Container>
    </section>
  );
};
