import React, { useCallback, useEffect, useState } from 'react';
import { Card, Form, Tab } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router-dom';
import { CategoriesApi } from '../api/categories';
import { EventsApi } from '../api/events';
import { ScrapersApi } from '../api/scrapers';
import { VenuesApi } from '../api/venues';
import { SquareButton as Button, SquareButton } from '../components/button';
import { FormCheck, FormInput, FormSelect, FormText } from '../components/form';
import { Icon } from '../components/Icon';
import {
  MappingRulesTab,
  RunsTab,
  SelectorsTab,
  SummaryTab,
} from '../scrapers';
import { ClassificationRulesTab } from '../scrapers/ClassificationRulesTab';
import { OutputTab } from '../scrapers/OutputTab';
import { cardStyles, formStyles, pageStyles } from '../styles';
import type {
  Category,
  Event,
  ExecutionMode,
  ExtractionStrategy,
  Guid,
  PaginationType,
  ScraperAttemptHistory,
  ScraperDefinition,
  ScraperSelector,
  Venue,
} from '../types/api';
import styles from './ScraperEditorPage.module.css';

type TabKey =
  | 'general'
  | 'selectors'
  | 'mapping'
  | 'classification'
  | 'runs'
  | 'summary'
  | 'output';

const toOptions = (arr: string[]) =>
  arr.map((item) => ({ value: item, label: item }));

const executionModeOptions = toOptions(['Static', 'Dynamic']);

const extractionStrategyOptions = toOptions([
  'Css',
  'JsonLd',
  'XPath',
  'Regex',
]);

const paginationTypeOptions = toOptions([
  'None',
  'InfiniteScroll',
  'NextButton',
]);

export const ScraperEditorPage: React.FC = () => {
  const { scraperId } = useParams();
  const navigate = useNavigate();
  const [events, setEvents] = useState<Event[]>([]);
  const [scraper, setScraper] = useState<ScraperDefinition | null>(null);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const isNew = scraperId === 'new';

  /* Events */
  const loadEvents = async () => {
    if (!scraperId) return;

    const events = await EventsApi.getByScraperId(scraperId);
    setEvents(events);
  };
  /* Events */

  /* Selectors */
  const [selectors, setSelectors] = useState<ScraperSelector[]>([]);

  const loadSelectors = async () => {
    if (!scraperId) return;
    const data = await ScrapersApi.getSelectors(scraperId);
    setSelectors(data);
  };
  /* Selectors */

  /* Attempts */
  const [attempts, setAttempts] = useState<ScraperAttemptHistory[]>([]);

  const loadAttemptHistories = async () => {
    if (!scraperId) return;
    const attempts = await ScrapersApi.getAttemptHistories(scraperId);
    setAttempts(attempts);
  };
  /* Attempts */

  /* Categories */
  const [categories, setCategories] = useState<Category[]>([]);

  const loadCategories = useCallback(async (signal?: AbortSignal) => {
    try {
      const result = await CategoriesApi.getAll(signal);

      const sortProp = (c: Category) => c.name;

      const sorted = result.sort((a, b) =>
        sortProp(a).localeCompare(sortProp(b)),
      );

      setCategories(sorted);
    } catch (err) {
      console.error('Failed to fetch categories', err);
    }
  }, []);
  /* Categories */

  useEffect(() => {
    if (scraperId !== 'new') {
      var abortController = new AbortController();

      void loadEvents();
      void loadSelectors();
      void loadAttemptHistories();
      void loadCategories(abortController.signal);
    }
  }, [scraperId]);

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
            id: '' as Guid,
            name: '',
            baseUrl: '',
            disabled: false,
            notes: '',
            executionMode: 'Static',
            extractionStrategy: 'Css',
            paginationType: 'None',
            state: 'Unknown',
            lastSuccessUtc: null,
            lastFailureUtc: null,
            lastErrorMessage: null,
            venueId: null,
          },
        );
      } catch (e: any) {
        setError(e.message ?? 'Error loading scraper.');
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
          disabled: scraper.disabled,
          notes: scraper.notes,
          executionMode: scraper.executionMode,
          extractionStrategy: scraper.extractionStrategy,
          paginationType: scraper.paginationType,
          venueId: scraper.venueId ?? undefined,
        });
        navigate(`/scrapers/${created.id}`);
      } else if (scraperId) {
        await ScrapersApi.update(scraperId as Guid, {
          name: scraper.name,
          baseUrl: scraper.baseUrl,
          disabled: scraper.disabled,
          notes: scraper.notes,
          executionMode: scraper.executionMode,
          extractionStrategy: scraper.extractionStrategy,
          paginationType: scraper.paginationType,
          venueId: scraper.venueId ?? undefined,
        });
      }
      alert('Saved.');
    } catch (e: any) {
      alert(e.message ?? 'Save failed.');
    }
  };

  const [eventKey, setEventKey] = useState<TabKey>('general');

  if (!scraper) return <p>Loading…</p>;
  if (error) return <p className="error">{error}</p>;

  return (
    <section>
      <div className={pageStyles.pageHeader}>
        {/* <h2>{isNew ? 'New Scraper' : `Edit Scraper – ${scraper.name}`}</h2> */}
        <h2>{scraper.name}</h2>
        <SquareButton onClick={() => navigate('/scrapers')}>Back</SquareButton>
      </div>

      <Tab.Container activeKey={eventKey}>
        <div className={styles.tabs}>
          <Button
            variant={eventKey == 'general' ? 'active' : 'default'}
            onClick={() => setEventKey('general')}
          >
            General
          </Button>
          <Button
            variant={eventKey == 'selectors' ? 'active' : 'default'}
            disabled={isNew}
            onClick={() => setEventKey('selectors')}
          >
            Selectors
          </Button>
          <Button
            variant={eventKey == 'mapping' ? 'active' : 'default'}
            disabled={isNew}
            onClick={() => setEventKey('mapping')}
          >
            Mapping
          </Button>
          <Button
            variant={eventKey == 'classification' ? 'active' : 'default'}
            disabled={isNew}
            onClick={() => setEventKey('classification')}
          >
            Classification
          </Button>
          <Button
            variant={eventKey == 'summary' ? 'active' : 'default'}
            disabled={isNew}
            onClick={() => setEventKey('summary')}
          >
            Summary
          </Button>
          <Button
            variant={eventKey == 'runs' ? 'active' : 'default'}
            disabled={isNew}
            onClick={() => setEventKey('runs')}
          >
            Runs
          </Button>
          <Button
            variant={eventKey == 'output' ? 'active' : 'default'}
            disabled={isNew}
            onClick={() => setEventKey('output')}
          >
            Output
          </Button>
        </div>

        <Tab.Content>
          <Tab.Pane eventKey="general">
            <Card className={cardStyles.BgCard}>
              <Card.Body>
                <Form className={formStyles.form}>
                  <div className={formStyles.formGroup}>
                    <FormInput
                      className={styles.InputGrow}
                      label="Name"
                      value={scraper.name}
                      onChange={(name) => setScraper({ ...scraper, name })}
                    />

                    <FormCheck
                      label="Disabled"
                      checked={scraper.disabled}
                      onChange={(disabled) =>
                        setScraper({ ...scraper, disabled })
                      }
                    />
                  </div>

                  <FormSelect
                    label="Venue"
                    value={scraper.venueId ?? ''}
                    onChange={(venueId) =>
                      setScraper({
                        ...scraper,
                        venueId: venueId ? (venueId as Guid) : null,
                      })
                    }
                    options={[{ value: '', label: '(none)' }].concat(
                      venues.map((v) => ({ value: v.id, label: v.name })),
                    )}
                  />

                  <div className={formStyles.formGroup}>
                    <FormInput
                      className={styles.InputGrow}
                      label="Base URL"
                      value={scraper.baseUrl}
                      onChange={(baseUrl) =>
                        setScraper({ ...scraper, baseUrl })
                      }
                    />
                    <Button href={scraper.baseUrl} target="_blank">
                      <Icon name="external" />
                    </Button>
                  </div>

                  <FormText
                    label="Notes"
                    value={scraper.notes}
                    onChange={(notes) => setScraper({ ...scraper, notes })}
                  />

                  <hr />

                  <FormSelect
                    label="Execution Mode"
                    value={scraper.executionMode}
                    onChange={(executionMode) =>
                      setScraper({
                        ...scraper,
                        executionMode: executionMode as ExecutionMode,
                      })
                    }
                    options={executionModeOptions}
                  />
                  <FormSelect
                    label="Extraction Strategy"
                    value={scraper.extractionStrategy}
                    onChange={(extractionStrategy) =>
                      setScraper({
                        ...scraper,
                        extractionStrategy:
                          extractionStrategy as ExtractionStrategy,
                      })
                    }
                    options={extractionStrategyOptions}
                  />
                  <FormSelect
                    label="Paging Type"
                    value={scraper.paginationType}
                    onChange={(paginationType) =>
                      setScraper({
                        ...scraper,
                        paginationType: paginationType as PaginationType,
                      })
                    }
                    options={paginationTypeOptions}
                  />
                  <div className={formStyles.buttonRow}>
                    <SquareButton onClick={handleSaveGeneral}>
                      Save
                    </SquareButton>
                  </div>
                </Form>
              </Card.Body>
            </Card>
          </Tab.Pane>

          <Tab.Pane eventKey="selectors">
            <SelectorsTab
              scraperId={scraperId as Guid}
              selectors={selectors}
              refresh={loadSelectors}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="mapping">
            <MappingRulesTab
              scraperId={scraperId as Guid}
              selectors={selectors}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="classification">
            <ClassificationRulesTab
              scraperId={scraperId as Guid}
              categories={categories}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="summary">
            <SummaryTab
              scraperId={scraperId as Guid}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="runs">
            <RunsTab
              scraperId={scraperId as Guid}
              attempts={attempts}
              onComplete={() => {
                loadEvents();
                loadAttemptHistories();
              }}
            />
          </Tab.Pane>

          <Tab.Pane eventKey="output">
            <OutputTab
              scraperId={scraperId as Guid}
              categories={categories}
              events={events}
              loadEvents={loadEvents}
            />
          </Tab.Pane>
        </Tab.Content>
      </Tab.Container>
    </section>
  );
};
