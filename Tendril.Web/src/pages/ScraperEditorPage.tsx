import cn from 'classnames';
import React, { useEffect, useState } from 'react';
import { Card, Form, Tab } from 'react-bootstrap';
import { useNavigate, useParams } from 'react-router-dom';
import { ScrapersApi } from '../api/scrapers';
import { VenuesApi } from '../api/venues';
import { AdminButton, AdminButton as Button } from '../components/button';
import { FormInput, FormSelect } from '../components/form';
import { Icon } from '../components/Icon';
import {
  ScraperMappingRulesTab,
  ScraperRunsTab,
  ScraperSelectorsTab,
} from '../scrapers';
import { buttonStyles, cardStyles, formStyles, pageStyles } from '../styles';
import type {
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

type TabKey = 'general' | 'selectors' | 'mapping' | 'runs';

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
  const [scraper, setScraper] = useState<ScraperDefinition | null>(null);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [error, setError] = useState<string | null>(null);
  const isNew = scraperId === 'new';

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

  useEffect(() => {
    if (scraperId !== 'new') {
      void loadSelectors();
      void loadAttemptHistories();
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
            executionMode: 'Static',
            extractionStrategy: 'Css',
            paginationType: 'None',
            state: 'Unknown',
            lastSuccessUtc: null,
            lastFailureUtc: null,
            lastErrorMessage: null,
            venueId: null,
          }
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
        <h2>{isNew ? 'New Scraper' : `Edit Scraper – ${scraper.name}`}</h2>
        <AdminButton onClick={() => navigate('/scrapers')}>Back</AdminButton>
      </div>

      <Tab.Container activeKey={eventKey}>
        <div className={styles.tabs}>
          <Button
            className={cn(eventKey == 'general' && buttonStyles.Active)}
            onClick={() => setEventKey('general')}
          >
            General
          </Button>
          <Button
            className={cn(eventKey == 'selectors' && buttonStyles.Active)}
            disabled={isNew}
            onClick={() => setEventKey('selectors')}
          >
            Selectors
          </Button>
          <Button
            className={cn(eventKey == 'mapping' && buttonStyles.Active)}
            disabled={isNew}
            onClick={() => setEventKey('mapping')}
          >
            Mapping Rules
          </Button>
          <Button
            className={cn(eventKey == 'runs' && buttonStyles.Active)}
            disabled={isNew}
            onClick={() => setEventKey('runs')}
          >
            Runs
          </Button>
        </div>

        <Tab.Content>
          <Tab.Pane eventKey="general">
            <Card className={cardStyles.BgCard}>
              <Card.Body>
                <Form className={formStyles.form}>
                  <FormInput
                    label="Name"
                    value={scraper.name}
                    onChange={(value) =>
                      setScraper({ ...scraper, name: value })
                    }
                  />
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
                      venues.map((v) => ({ value: v.id, label: v.name }))
                    )}
                  />
                  <div className={formStyles.formGroup}>
                    <FormInput
                      className={styles.InputGrow}
                      label="Base URL"
                      value={scraper.baseUrl}
                      onChange={(value) =>
                        setScraper({ ...scraper, baseUrl: value })
                      }
                    />
                    <Button href={scraper.baseUrl} target="_blank">
                      <Icon name="external" />
                    </Button>
                  </div>

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
                    <AdminButton onClick={handleSaveGeneral}>Save</AdminButton>
                  </div>
                </Form>
              </Card.Body>
            </Card>

            <div style={{ margin: '10em 0' }}>
              <label>Sample label</label>
              <h1>Sample Heading 1</h1>
              <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum</p>
              <h2>Sample Heading 2</h2>
              <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum</p>
              <h3>Sample Heading 3</h3>
              <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum</p>
              <h4>Sample Heading 4</h4>
              <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum</p>
              <h5>Sample Heading 5</h5>
              <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum</p>
              <h6>Sample Heading 6</h6>
              <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum</p>
            </div>
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
            <ScraperRunsTab
              scraperId={scraperId as Guid}
              attempts={attempts}
              onComplete={loadAttemptHistories}
            />
          </Tab.Pane>
        </Tab.Content>
      </Tab.Container>
    </section>
  );
};
